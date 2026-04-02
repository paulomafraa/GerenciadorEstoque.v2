using GerenciadorEstoque.Client.Models;
using GerenciadorEstoque.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorEstoque.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BackupController : ControllerBase
{
    private readonly AppDbContext _db;

    public BackupController(AppDbContext db) => _db = db;

    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar()
    {
        var dados = new
        {
            DataExportacao = DateTime.Now,
            Produtos = await _db.Produtos.Select(p => new
            {
                p.Id, p.Nome, p.Tipo, p.PrecoTabelado, p.PrecoVenda, p.ImagemUrl
            }).ToListAsync(),
            ItensEstoque = await _db.ItensEstoque.ToListAsync(),
            Vendas = await _db.Vendas.ToListAsync()
        };

        return Ok(dados);
    }
}