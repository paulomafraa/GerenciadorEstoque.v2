# 📦 Gerenciador de Estoque

Sistema web completo de gerenciamento de estoque com autenticação, controle de produtos, vendas e relatórios. Desenvolvido com Blazor WebAssembly (.NET 9) e deployado no Google Cloud Run com banco de dados MySQL.

## 🔗 Demo

🌐 [estoque.kairyuutcg.com.br](https://estoque.kairyuutcg.com.br)

## 🛠️ Tecnologias

| Camada | Tecnologia |
|---|---|
| Frontend | Blazor WebAssembly (.NET 9) + MudBlazor |
| Backend | ASP.NET Core 9 Web API |
| Banco de dados | MySQL 8 via Pomelo EF Core |
| Deploy | Google Cloud Run (containerizado) |
| Banco cloud | Google Cloud SQL (MySQL) |
| Segredos | Google Secret Manager |
| Autenticação | Cookie Authentication (ASP.NET Core) |
| Build | Google Cloud Build + Docker |

## ✨ Funcionalidades

- 🔐 **Login com CAPTCHA matemático** — proteção contra bots
- 📦 **Produtos** — cadastro com imagem (armazenada como BLOB no MySQL)
- 📊 **Estoque** — controle de entradas e saídas
- 🛒 **Vendas** — registro e acompanhamento
- 📈 **Relatórios** — visão geral do negócio
- 👥 **Usuários** — gerenciamento de equipe
- 💾 **Backup automático** — exportação JSON ao iniciar
- 🌙 **Modo escuro** — alternância de tema

## 🏗️ Arquitetura

```
GerenciadorEstoque/               ← Projeto servidor (ASP.NET Core)
├── Controllers/                  ← APIs REST (protegidas com [Authorize])
├── Data/AppDbContext.cs           ← EF Core + MySQL
├── Components/App.razor           ← Entry point Blazor
└── Program.cs                    ← Middleware, auth, DB migration

GerenciadorEstoque.Client/        ← Projeto cliente (Blazor WASM)
├── Pages/                        ← Páginas do SPA
├── Layout/MainLayout.razor        ← Layout principal com verificação de auth
└── Layout/LoginLayout.razor       ← Layout da tela de login
```

## 🚀 Como fazer deploy no Google Cloud Run

### Pré-requisitos

- Conta no [Google Cloud Platform](https://console.cloud.google.com)
- [Google Cloud SDK](https://cloud.google.com/sdk/docs/install) instalado
- Instância Cloud SQL MySQL criada
- Secret Manager configurado com a senha do banco

### 1. Configurar o script

Edite `deploy-cloudrun.ps1` e preencha suas informações:

```powershell
$PROJECT_ID = "SEU-PROJETO-GCP"
$INSTANCE_CONNECTION = "SEU-PROJETO:REGIAO:NOME-DA-INSTANCIA"
$DB_USER = "USUARIO-DO-BANCO"
```

### 2. Configurar credenciais de login

Após o deploy, defina as credenciais de acesso ao sistema:

```powershell
gcloud run services update gerenciador-estoque `
    --region us-central1 `
    --update-env-vars "APP_USER=SEU_USUARIO,APP_PASSWORD=SUA_SENHA"
```

### 3. Executar o deploy

```powershell
cd "Controle Estoque V2\GerenciadorEstoque"
.\deploy-cloudrun.ps1
```

### Variáveis de ambiente necessárias no Cloud Run

| Variável | Descrição | Tipo |
|---|---|---|
| `DB_CONNECTION` | Connection string MySQL (sem senha) | Env var |
| `DB_PASSWORD` | Senha do banco | Secret Manager |
| `APP_USER` | Usuário para login no sistema | Env var |
| `APP_PASSWORD` | Senha para login no sistema | Env var |

## 💻 Executar localmente

```bash
# Requisitos: .NET 9 SDK + MySQL local ou cloud-sql-proxy

cd "Controle Estoque V2/GerenciadorEstoque/GerenciadorEstoque"

# Configurar variáveis de ambiente
$env:DB_CONNECTION = "server=127.0.0.1;port=3307;user=SEU_USER;database=estoque"
$env:DB_PASSWORD   = "SUA_SENHA"
$env:APP_USER      = "admin"
$env:APP_PASSWORD  = "suasenha"

dotnet run
# Acesse: http://localhost:5199
```

## 📁 Estrutura de segurança

- **Sem credenciais no código** — tudo via variáveis de ambiente
- **Senhas no Secret Manager** — injetadas como segredos no Cloud Run
- **Cookies HttpOnly** com SlidingExpiration de 12h
- **Proteção server-side** — middleware redireciona para `/login` antes do prerender
- **[Authorize]** em todos os controllers de dados
