using GerenciadorEstoque.Client.Models;
using GerenciadorEstoque.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorEstoque.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EstoqueController : ControllerBase
{
    private readonly AppDbContext _db;

    public EstoqueController(AppDbContext db) => _db = db;

    [HttpGet("{produtoId}")]
    public async Task<ActionResult<List<EstoqueItem>>> GetByProduto(int produtoId)
    {
        return await _db.ItensEstoque
            .Where(e => e.ProdutoId == produtoId)
            .OrderBy(e => e.DataCompra)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<EstoqueItem>> Adicionar(EstoqueItem item)
    {
        item.DataCompra = DateTime.Now;
        _db.ItensEstoque.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPost("retirar")]
    public async Task<IActionResult> Retirar([FromBody] RetiradaRequest request)
    {
        var itens = await _db.ItensEstoque
            .Where(e => e.ProdutoId == request.ProdutoId)
            .OrderBy(e => e.DataCompra)
            .ToListAsync();

        int qtdParaRemover = request.Quantidade;
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
            return BadRequest("Estoque insuficiente.");

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("retirar-multiplos")]
    public async Task<IActionResult> RetirarMultiplos([FromBody] List<RetiradaRequest> requests)
    {
        foreach (var request in requests)
        {
            var itens = await _db.ItensEstoque
                .Where(e => e.ProdutoId == request.ProdutoId)
                .OrderBy(e => e.DataCompra)
                .ToListAsync();

            int qtdParaRemover = request.Quantidade;
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
                var produto = await _db.Produtos.FindAsync(request.ProdutoId);
                return BadRequest($"Estoque insuficiente para '{produto?.Nome ?? "Produto"}'.");
            }
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}

public class RetiradaRequest
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
}