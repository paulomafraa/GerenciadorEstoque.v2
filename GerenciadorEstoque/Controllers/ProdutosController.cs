using GerenciadorEstoque.Client.Models;
using GerenciadorEstoque.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorEstoque.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProdutosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Produto>>> GetAll()
    {
        return await _db.Produtos
            .Include(p => p.ItensEstoque)
            .Include(p => p.Vendas)
            .OrderBy(p => p.Nome)
            .Select(p => new Produto
            {
                Id = p.Id,
                Nome = p.Nome,
                Tipo = p.Tipo,
                PrecoTabelado = p.PrecoTabelado,
                PrecoVenda = p.PrecoVenda,
                ImagemUrl = p.ImagemUrl,
                ImagemContentType = p.ImagemContentType,
                ItensEstoque = p.ItensEstoque,
                Vendas = p.Vendas
            })
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Produto>> Get(int id)
    {
        var produto = await _db.Produtos
            .Include(p => p.ItensEstoque)
            .Include(p => p.Vendas)
            .Where(p => p.Id == id)
            .Select(p => new Produto
            {
                Id = p.Id,
                Nome = p.Nome,
                Tipo = p.Tipo,
                PrecoTabelado = p.PrecoTabelado,
                PrecoVenda = p.PrecoVenda,
                ImagemUrl = p.ImagemUrl,
                ImagemContentType = p.ImagemContentType,
                ItensEstoque = p.ItensEstoque,
                Vendas = p.Vendas
            })
            .FirstOrDefaultAsync();

        return produto is null ? NotFound() : produto;
    }

    [HttpGet("{id}/imagem")]
    public async Task<IActionResult> GetImagem(int id)
    {
        var dados = await _db.Produtos
            .Where(p => p.Id == id)
            .Select(p => new { p.ImagemDados, p.ImagemContentType })
            .FirstOrDefaultAsync();

        if (dados?.ImagemDados is null || dados.ImagemDados.Length == 0)
            return NotFound();

        return File(dados.ImagemDados, dados.ImagemContentType ?? "application/octet-stream");
    }

    [HttpGet("imagem/{nomeArquivo}")]
    public async Task<IActionResult> GetImagemPorNome(string nomeArquivo)
    {
        var dados = await _db.Produtos
            .Where(p => p.ImagemUrl == nomeArquivo)
            .Select(p => new { p.ImagemDados, p.ImagemContentType })
            .FirstOrDefaultAsync();

        if (dados?.ImagemDados is null || dados.ImagemDados.Length == 0)
            return NotFound();

        return File(dados.ImagemDados, dados.ImagemContentType ?? "application/octet-stream");
    }

    [HttpPost]
    public async Task<ActionResult<Produto>> Create(Produto produto)
    {
        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = produto.Id }, produto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Produto produto)
    {
        if (id != produto.Id) return BadRequest();

        _db.Entry(produto).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null) return NotFound();

        _db.Produtos.Remove(produto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/imagem")]
    public async Task<ActionResult<string>> UploadImagem(int id, IFormFile arquivo)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null) return NotFound();

        using var ms = new MemoryStream();
        await arquivo.CopyToAsync(ms);

        produto.ImagemDados = ms.ToArray();
        produto.ImagemContentType = ObterContentType(arquivo.FileName);
        produto.ImagemUrl = $"{Guid.NewGuid()}{Path.GetExtension(arquivo.FileName)}";
        await _db.SaveChangesAsync();

        return Ok(produto.ImagemUrl);
    }

    private static string ObterContentType(string nomeArquivo)
    {
        var extensao = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        return extensao switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
