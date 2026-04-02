using GerenciadorEstoque.Client.Models;
using GerenciadorEstoque.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GerenciadorEstoque.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly string _pastaComprovantes;

    public VendasController(AppDbContext db)
    {
        _db = db;
        _pastaComprovantes = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GerenciadorEstoque", "comprovantes");
        Directory.CreateDirectory(_pastaComprovantes);
    }

    [HttpGet]
    public async Task<ActionResult<List<Venda>>> GetAll()
    {
        return await _db.Vendas
            .Include(v => v.Produto)
            .OrderByDescending(v => v.DataVenda)
            .ToListAsync();
    }

    [HttpGet("produto/{produtoId}")]
    public async Task<ActionResult<List<Venda>>> GetByProduto(int produtoId)
    {
        return await _db.Vendas
            .Where(v => v.ProdutoId == produtoId)
            .OrderByDescending(v => v.DataVenda)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Venda>> Criar(Venda venda)
    {
        var itens = await _db.ItensEstoque
            .Where(e => e.ProdutoId == venda.ProdutoId)
            .OrderBy(e => e.DataCompra)
            .ToListAsync();

        int qtdParaRemover = venda.Quantidade;
        foreach (var item in itens)
        {
            if (qtdParaRemover <= 0) break;
            if (item.Quantidade <= qtdParaRemover)
            {
                qtdParaRemover -= item.Quantidade;
                _db.ItensEstoque.Remove(item);
            }
            else
            {
                item.Quantidade -= qtdParaRemover;
                qtdParaRemover = 0;
            }
        }

        if (qtdParaRemover > 0)
            return BadRequest("Estoque insuficiente para esta venda.");

        venda.ValorTotal = venda.Quantidade * venda.ValorUnitario;
        venda.DataVenda = DateTime.Now;
        if (venda.Pago)
            venda.ValorPago = venda.ValorTotal;

        _db.Vendas.Add(venda);
        await _db.SaveChangesAsync();

        return Ok(venda);
    }

    [HttpPost("multiplos")]
    public async Task<ActionResult<List<Venda>>> CriarMultiplos([FromBody] List<Venda> vendas)
    {
        var vendasCriadas = new List<Venda>();

        foreach (var venda in vendas)
        {
            var itens = await _db.ItensEstoque
                .Where(e => e.ProdutoId == venda.ProdutoId)
                .OrderBy(e => e.DataCompra)
                .ToListAsync();

            int qtdParaRemover = venda.Quantidade;
            foreach (var item in itens)
            {
                if (qtdParaRemover <= 0) break;
                if (item.Quantidade <= qtdParaRemover)
                {
                    qtdParaRemover -= item.Quantidade;
                    _db.ItensEstoque.Remove(item);
                }
                else
                {
                    item.Quantidade -= qtdParaRemover;
                    qtdParaRemover = 0;
                }
            }

            if (qtdParaRemover > 0)
            {
                var produto = await _db.Produtos.FindAsync(venda.ProdutoId);
                return BadRequest($"Estoque insuficiente para '{produto?.Nome ?? "Produto"}'.");
            }

            venda.ValorTotal = venda.Quantidade * venda.ValorUnitario;
            venda.DataVenda = DateTime.Now;
            if (venda.Pago)
                venda.ValorPago = venda.ValorTotal;

            _db.Vendas.Add(venda);
            vendasCriadas.Add(venda);
        }

        await _db.SaveChangesAsync();
        return Ok(vendasCriadas);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(int id, Venda venda)
    {
        if (id != venda.Id) return BadRequest();
        _db.Entry(venda).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/comprovante")]
    public async Task<ActionResult<string>> UploadComprovante(int id, IFormFile arquivo)
    {
        var venda = await _db.Vendas.FindAsync(id);
        if (venda is null) return NotFound();

        if (!string.IsNullOrEmpty(venda.ComprovanteUrl))
        {
            var antigo = Path.Combine(_pastaComprovantes, venda.ComprovanteUrl);
            if (System.IO.File.Exists(antigo))
                System.IO.File.Delete(antigo);
        }

        var extensao = Path.GetExtension(arquivo.FileName);
        var nomeArquivo = $"venda_{id}_{Guid.NewGuid()}{extensao}";
        var caminho = Path.Combine(_pastaComprovantes, nomeArquivo);

        using var stream = new FileStream(caminho, FileMode.Create);
        await arquivo.CopyToAsync(stream);

        venda.ComprovanteUrl = nomeArquivo;
        await _db.SaveChangesAsync();

        return Ok(nomeArquivo);
    }

    [HttpGet("{id}/comprovante")]
    public async Task<IActionResult> GetComprovante(int id)
    {
        var venda = await _db.Vendas.FindAsync(id);
        if (venda is null || string.IsNullOrEmpty(venda.ComprovanteUrl))
            return NotFound();

        var caminho = Path.Combine(_pastaComprovantes, venda.ComprovanteUrl);
        if (!System.IO.File.Exists(caminho))
            return NotFound();

        var ext = Path.GetExtension(venda.ComprovanteUrl).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return PhysicalFile(caminho, contentType, venda.ComprovanteUrl);
    }

    [HttpGet("{id}/ficha")]
    public async Task<IActionResult> GerarFicha(int id)
    {
        var venda = await _db.Vendas
            .Include(v => v.Produto)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venda is null) return NotFound();

        var html = GerarHtmlFicha(venda);
        return Content(html, "text/html", Encoding.UTF8);
    }

    private static string GerarHtmlFicha(Venda venda)
    {
        var e = new Func<string?, string>(t => System.Net.WebUtility.HtmlEncode(t ?? "—"));
        var statusCor = venda.Pago ? "#22c55e" : "#f59e0b";
        var statusTexto = venda.Pago ? "&#10004; PAGO" : "&#9203; PENDENTE";
        var restante = venda.ValorTotal - (venda.ValorPago ?? 0);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"pt-BR\"><head><meta charset=\"UTF-8\">");
        sb.Append("<title>Comprovante de Venda #").Append(venda.Id).Append("</title>");
        sb.Append("<style>");
        sb.Append("*{margin:0;padding:0;box-sizing:border-box}");
        sb.Append("body{font-family:'Segoe UI',system-ui,-apple-system,sans-serif;background:#f0f2f5;padding:20px;color:#1a1a2e}");
        sb.Append(".receipt{max-width:420px;margin:0 auto;background:#fff;border-radius:16px;box-shadow:0 8px 32px rgba(0,0,0,0.12);overflow:hidden}");
        sb.Append(".header{background:linear-gradient(135deg,#1565C0,#1976D2);color:white;padding:28px 24px;text-align:center}");
        sb.Append(".header h1{font-size:20px;font-weight:700;letter-spacing:1px}");
        sb.Append(".header .id{font-size:13px;opacity:0.85;margin-top:4px}");
        sb.Append(".header .date{font-size:12px;opacity:0.7;margin-top:2px}");
        sb.Append(".status-badge{display:inline-block;margin-top:12px;padding:6px 18px;border-radius:20px;background:").Append(statusCor).Append(";color:white;font-weight:700;font-size:14px}");
        sb.Append(".body{padding:24px}");
        sb.Append(".section{margin-bottom:20px}");
        sb.Append(".section-title{font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:1.5px;color:#9ca3af;margin-bottom:10px;padding-bottom:6px;border-bottom:1px solid #f3f4f6}");
        sb.Append(".row{display:flex;justify-content:space-between;padding:6px 0;font-size:14px}");
        sb.Append(".row .label{color:#6b7280}");
        sb.Append(".row .value{font-weight:600;text-align:right}");
        sb.Append(".total-box{background:linear-gradient(135deg,#f0fdf4,#dcfce7);border:2px solid #22c55e;border-radius:12px;padding:16px;text-align:center;margin:20px 0}");
        sb.Append(".total-box .label{font-size:12px;color:#16a34a;font-weight:600;text-transform:uppercase;letter-spacing:1px}");
        sb.Append(".total-box .amount{font-size:28px;font-weight:800;color:#15803d;margin-top:4px}");
        sb.Append(".pending-box{background:linear-gradient(135deg,#fffbeb,#fef3c7);border:2px solid #f59e0b;border-radius:12px;padding:14px;margin:16px 0}");
        sb.Append(".pending-box .row .label{color:#92400e}");
        sb.Append(".pending-box .row .value{color:#b45309}");
        sb.Append(".obs-box{background:#f8fafc;border-left:4px solid #3b82f6;border-radius:0 8px 8px 0;padding:12px 16px;margin:16px 0;font-size:13px;color:#475569;line-height:1.5}");
        sb.Append(".footer{text-align:center;padding:20px 24px;border-top:1px dashed #e5e7eb;color:#9ca3af;font-size:11px}");
        sb.Append(".divider{border:none;border-top:1px dashed #e5e7eb;margin:16px 0}");
        sb.Append(".tag-sim{display:inline-block;padding:4px 10px;border-radius:6px;font-size:12px;font-weight:600;background:#dbeafe;color:#1d4ed8}");
        sb.Append(".tag-nao{display:inline-block;padding:4px 10px;border-radius:6px;font-size:12px;font-weight:600;background:#f3f4f6;color:#9ca3af}");
        sb.Append("@media print{body{background:white;padding:0}.receipt{box-shadow:none}.no-print{display:none}}");
        sb.Append("</style></head><body>");
        sb.Append("<div class=\"receipt\">");

        // Header
        sb.Append("<div class=\"header\">");
        sb.Append("<h1>&#128230; COMPROVANTE DE VENDA</h1>");
        sb.AppendFormat("<div class=\"id\">N&ordm; {0:D6}</div>", venda.Id);
        sb.AppendFormat("<div class=\"date\">{0:dd/MM/yyyy} &agrave;s {0:HH:mm}</div>", venda.DataVenda);
        sb.AppendFormat("<div class=\"status-badge\">{0}</div>", statusTexto);
        sb.Append("</div>");

        // Body
        sb.Append("<div class=\"body\">");

        // Pessoas
        sb.Append("<div class=\"section\">");
        sb.Append("<div class=\"section-title\">Pessoas</div>");
        sb.AppendFormat("<div class=\"row\"><span class=\"label\">Vendedor</span><span class=\"value\">{0}</span></div>", e(venda.Responsavel));
        sb.AppendFormat("<div class=\"row\"><span class=\"label\">Cliente</span><span class=\"value\">{0}</span></div>", e(venda.Cliente));
        sb.Append("</div>");

        // Produto
        sb.Append("<div class=\"section\">");
        sb.Append("<div class=\"section-title\">Produto</div>");
        sb.AppendFormat("<div class=\"row\"><span class=\"label\">Nome</span><span class=\"value\">{0}</span></div>", e(venda.Produto?.Nome));
        sb.AppendFormat("<div class=\"row\"><span class=\"label\">Tipo</span><span class=\"value\">{0}</span></div>", e(venda.Produto?.Tipo));
        sb.AppendFormat("<div class=\"row\"><span class=\"label\">Quantidade</span><span class=\"value\">{0}</span></div>", venda.Quantidade);
        sb.AppendFormat("<div class=\"row\"><span class=\"label\">Valor Unit&aacute;rio</span><span class=\"value\">R$ {0:N2}</span></div>", venda.ValorUnitario);
        sb.Append("</div>");

        // Total
        sb.Append("<div class=\"total-box\">");
        sb.Append("<div class=\"label\">Valor Total</div>");
        sb.AppendFormat("<div class=\"amount\">R$ {0:N2}</div>", venda.ValorTotal);
        sb.Append("</div>");

        // Pagamento
        sb.Append("<div class=\"section\">");
        sb.Append("<div class=\"section-title\">Pagamento</div>");
        sb.AppendFormat("<div class=\"row\"><span class=\"label\">Forma</span><span class=\"value\">{0}</span></div>", e(venda.FormaPagamento));
        if (!venda.Pago)
        {
            sb.AppendFormat("<div class=\"row\"><span class=\"label\">Valor Pago</span><span class=\"value\">R$ {0:N2}</span></div>", venda.ValorPago ?? 0);
        }
        sb.Append("</div>");

        // Pendente
        if (!venda.Pago)
        {
            sb.Append("<div class=\"pending-box\">");
            sb.AppendFormat("<div class=\"row\"><span class=\"label\">&#128184; Restante</span><span class=\"value\" style=\"font-size:18px;font-weight:800\">R$ {0:N2}</span></div>", restante);
            if (venda.PrevisaoPagamentoRestante.HasValue)
                sb.AppendFormat("<div class=\"row\"><span class=\"label\">&#128197; Previs&atilde;o</span><span class=\"value\">{0:dd/MM/yyyy}</span></div>", venda.PrevisaoPagamentoRestante);
            if (!string.IsNullOrEmpty(venda.FormaPagamentoRestante))
                sb.AppendFormat("<div class=\"row\"><span class=\"label\">&#128179; Forma restante</span><span class=\"value\">{0}</span></div>", e(venda.FormaPagamentoRestante));
            sb.Append("</div>");
        }

        // Observações
        if (!string.IsNullOrEmpty(venda.Observacoes))
        {
            sb.AppendFormat("<div class=\"obs-box\">&#128221; {0}</div>", e(venda.Observacoes));
        }

        // Comprovante
        sb.Append("<hr class=\"divider\"/>");
        var temComprovante = !string.IsNullOrEmpty(venda.ComprovanteUrl);
        sb.AppendFormat("<div class=\"row\"><span class=\"label\">Comprovante anexado</span><span class=\"{0}\">{1}</span></div>",
            temComprovante ? "tag-sim" : "tag-nao",
            temComprovante ? "&#10003; Sim" : "N&atilde;o");

        sb.Append("</div>"); // body

        // Footer
        sb.Append("<div class=\"footer\">");
        sb.AppendFormat("<p>Gerenciador de Estoque &mdash; Comprovante gerado em {0:dd/MM/yyyy HH:mm}</p>", DateTime.Now);
        sb.Append("<p style=\"margin-top:8px\" class=\"no-print\">");
        sb.Append("<button onclick=\"window.print()\" style=\"padding:8px 24px;border:2px solid #1976D2;background:white;color:#1976D2;border-radius:8px;cursor:pointer;font-weight:600\">");
        sb.Append("&#128424; Imprimir / Salvar PDF</button></p>");
        sb.Append("</div>");

        sb.Append("</div></body></html>");
        return sb.ToString();
    }
}