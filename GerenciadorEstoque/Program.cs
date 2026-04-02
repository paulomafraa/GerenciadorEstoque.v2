using GerenciadorEstoque.Components;
using GerenciadorEstoque.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Text.Json;
using System.Collections.Generic;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Detecta se está rodando no Cloud Run ou localmente
var isCloudRun = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("K_SERVICE"));

// Pasta para imagens e backups
string pastaApp;
if (isCloudRun)
{
    pastaApp = "/tmp/GerenciadorEstoque";
    Directory.CreateDirectory(pastaApp);
    Console.WriteLine($"[Cloud Run] Usando pasta temporária para imagens/backups: {pastaApp}");
}
else
{
    pastaApp = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GerenciadorEstoque");
    Directory.CreateDirectory(pastaApp);
    Console.WriteLine($"[Local] Usando pasta: {pastaApp}");
}

// Connection string MySQL — env var DB_CONNECTION sobrescreve appsettings
var envConn = Environment.GetEnvironmentVariable("DB_CONNECTION");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "server=127.0.0.1;port=3307;database=estoque";

string connectionString;

static string BuildConnectionString(string rawConn, string password)
{
    if (string.IsNullOrEmpty(rawConn))
        return rawConn ?? string.Empty;

    var parts = rawConn.Split(';', StringSplitOptions.RemoveEmptyEntries);
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var p in parts)
    {
        var idx = p.IndexOf('=');
        if (idx > 0)
        {
            var k = p.Substring(0, idx).Trim();
            var v = p.Substring(idx + 1).Trim();
            dict[k] = v;
        }
    }

    // If server points to Cloud SQL unix socket path, add ConnectionProtocol=unix
    // MySqlConnector expects: Server=/cloudsql/...;ConnectionProtocol=unix
    if (dict.TryGetValue("server", out var serverVal) && serverVal.StartsWith("/cloudsql", StringComparison.OrdinalIgnoreCase))
    {
        dict["ConnectionProtocol"] = "unix";
        // Keep server as the socket path — MySqlConnector uses it directly for unix connections
    }

    // Inject password if provided
    if (!string.IsNullOrEmpty(password))
    {
        dict["password"] = password;
    }

    var sb = new StringBuilder();
    foreach (var kv in dict)
    {
        if (sb.Length > 0) sb.Append(';');
        sb.Append(kv.Key).Append('=').Append(kv.Value);
    }

    return sb.ToString();
}

if (!string.IsNullOrEmpty(envConn))
{
    connectionString = BuildConnectionString(envConn, dbPassword);

    // Warn if using Cloud SQL socket but DB_PASSWORD not present
    if (envConn.IndexOf("/cloudsql", StringComparison.OrdinalIgnoreCase) >= 0 && string.IsNullOrEmpty(dbPassword) && !System.Text.RegularExpressions.Regex.IsMatch(envConn, @"password=", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
    {
        Console.WriteLine("[DB] AVISO: DB_CONNECTION aponta para /cloudsql mas DB_PASSWORD não está definida. Habilite uma versão do secret 'db-password' ou injete DB_PASSWORD para permitir conexão.");
    }
}
else
{
    connectionString = defaultConn;
}

Console.WriteLine($"[DB] Connection string usada (senha ocultada): {System.Text.RegularExpressions.Regex.Replace(connectionString ?? string.Empty, @"password=[^;]+", "password=***")} ");

// Configura porta para Cloud Run (via variável de ambiente PORT)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5199";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)))
           .EnableDetailedErrors()
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

builder.Services.AddMudServices();

// ForwardedHeaders para Cloud Run (HTTPS atrás de proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Autenticação por cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// HttpClient para prerender
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("ServerAPI", (sp, client) =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;
    if (request is not null)
    {
        client.BaseAddress = new Uri($"{request.Scheme}://{request.Host}");
    }
    else
    {
        client.BaseAddress = new Uri("http://localhost:5199");
    }
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

var app = builder.Build();

// Cria o banco se não existir E adiciona colunas/tabelas novas sem perder dados
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("[DB] Testando conexão com o banco...");
        db.Database.OpenConnection();
        db.Database.CloseConnection();
        Console.WriteLine("[DB] Conexão OK!");

        Console.WriteLine("[DB] Criando schema se não existir...");
        db.Database.EnsureCreated();
        Console.WriteLine("[DB] Schema OK!");

        AtualizarSchema(db);
        Console.WriteLine("[DB] Schema atualizado com sucesso!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB] ERRO CRÍTICO: {ex.GetType().Name}");
        Console.WriteLine($"[DB] Mensagem: {ex.Message}");
        Console.WriteLine($"[DB] Connection string (senha ocultada): {System.Text.RegularExpressions.Regex.Replace(connectionString, @"password=[^;]+", "password=***")}");
        Console.WriteLine($"[DB] StackTrace: {ex.StackTrace}");
    }
}

// Migra imagens antigas do wwwroot para a pasta persistente (executa uma única vez)
MigrarImagensAntigas(app, pastaApp);

// Backup automático ao iniciar — salva JSON na pasta do app
FazerBackup(app, pastaApp);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// Proteção server-side: redireciona para /login se não autenticado
// Impede que o prerender mostre conteúdo protegido antes do WASM carregar
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // Rotas públicas que não precisam de autenticação
    bool isPublic = path.Equals("/login", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/_", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains('.');

    if (!isPublic && context.User?.Identity?.IsAuthenticated != true)
    {
        context.Response.Redirect("/login");
        return;
    }

    await next();
});

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GerenciadorEstoque.Client._Imports).Assembly);

app.Run();

/// <summary>
/// Migra imagens que estavam no wwwroot para a pasta persistente.
/// Executa apenas se a pasta antiga existir e tiver arquivos.
/// </summary>
static void MigrarImagensAntigas(WebApplication app, string pastaApp)
{
    try
    {
        var pastaAntiga = Path.Combine(app.Environment.WebRootPath, "images", "produtos");
        var pastaNova = Path.Combine(pastaApp, "imagens");
        Directory.CreateDirectory(pastaNova);

        if (!Directory.Exists(pastaAntiga)) return;

        var arquivos = Directory.GetFiles(pastaAntiga);
        if (arquivos.Length == 0) return;

        foreach (var arquivo in arquivos)
        {
            var nomeArquivo = Path.GetFileName(arquivo);
            var destino = Path.Combine(pastaNova, nomeArquivo);
            if (!File.Exists(destino))
            {
                File.Copy(arquivo, destino);
                Console.WriteLine($"[Migração] Imagem copiada: {nomeArquivo}");
            }
        }

        Console.WriteLine($"[Migração] {arquivos.Length} imagem(ns) migrada(s) para pasta persistente.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Migração] Erro: {ex.Message}");
    }
}

/// <summary>
/// Adiciona colunas que faltam nas tabelas existentes — NUNCA perde dados.
/// </summary>
static void AtualizarSchema(AppDbContext db)
{
    // Garante que a tabela Usuarios exista (MySQL syntax)
    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS `Usuarios` (
                `Id` INT NOT NULL AUTO_INCREMENT,
                `Nome` VARCHAR(500) NOT NULL DEFAULT '',
                `Cargo` VARCHAR(500),
                `Ativo` TINYINT(1) NOT NULL DEFAULT 1,
                `DataCadastro` DATETIME NOT NULL DEFAULT '2025-01-01',
                PRIMARY KEY (`Id`)
            )");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Schema] Erro ao criar tabela Usuarios: {ex.Message}");
    }

    var colunasEsperadas = new (string Tabela, string Coluna, string TipoSql)[]
    {
        ("Vendas", "Responsavel", "TEXT"),
        ("Vendas", "ValorPago", "TEXT"),
        ("Vendas", "PrevisaoPagamentoRestante", "TEXT"),
        ("Vendas", "FormaPagamentoRestante", "TEXT"),
        ("Vendas", "Observacoes", "TEXT"),
        ("Vendas", "Pago", "INT DEFAULT 0"),
        ("Vendas", "ComprovanteUrl", "TEXT"),
        ("ItensEstoque", "Responsavel", "TEXT"),
        ("Produtos", "ImagemDados", "LONGBLOB"),
        ("Produtos", "ImagemContentType", "VARCHAR(100)"),
    };

    foreach (var (tabela, coluna, tipoSql) in colunasEsperadas)
    {
        if (!ColunaExiste(db, tabela, coluna))
        {
            try
            {
                db.Database.ExecuteSqlRaw(
                    $"ALTER TABLE `{tabela}` ADD COLUMN `{coluna}` {tipoSql}");
                Console.WriteLine($"[Schema] Coluna '{tabela}.{coluna}' adicionada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Schema] Erro ao adicionar '{tabela}.{coluna}': {ex.Message}");
            }
        }
    }
}

static bool ColunaExiste(AppDbContext db, string tabela, string coluna)
{
    var conn = db.Database.GetDbConnection();
    if (conn.State != System.Data.ConnectionState.Open)
        conn.Open();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tabela}' AND COLUMN_NAME = '{coluna}'";
    var result = cmd.ExecuteScalar();
    return Convert.ToInt32(result) > 0;
}

static void FazerBackup(WebApplication app, string pastaApp)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var produtos = db.Produtos
            .Include(p => p.ItensEstoque)
            .Include(p => p.Vendas)
            .Select(p => new
            {
                p.Id, p.Nome, p.Tipo, p.PrecoTabelado, p.PrecoVenda, p.ImagemUrl,
                ItensEstoque = p.ItensEstoque,
                Vendas = p.Vendas
            })
            .ToList();

        if (!produtos.Any()) return;

        var pastaBackup = Path.Combine(pastaApp, "backups");
        Directory.CreateDirectory(pastaBackup);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var arquivo = Path.Combine(pastaBackup, $"backup_{timestamp}.json");

        var dados = new
        {
            DataBackup = DateTime.Now,
            Produtos = produtos,
            TotalProdutos = produtos.Count,
            TotalItensEstoque = produtos.Sum(p => p.ItensEstoque?.Count ?? 0),
            TotalVendas = produtos.Sum(p => p.Vendas?.Count ?? 0)
        };

        var json = JsonSerializer.Serialize(dados, new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        });

        File.WriteAllText(arquivo, json);

        var backupsAntigos = Directory.GetFiles(pastaBackup, "backup_*.json")
            .OrderByDescending(f => f)
            .Skip(30);
        foreach (var antigo in backupsAntigos)
            File.Delete(antigo);

        Console.WriteLine($"[Backup] Salvo em: {arquivo}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Backup] Erro: {ex.Message}");
    }
}