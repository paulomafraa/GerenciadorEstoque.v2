using GerenciadorEstoque.Client.Models;
using GerenciadorEstoque.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorEstoque.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsuariosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Usuario>>> GetAll()
    {
        return await _db.Usuarios.OrderBy(u => u.Nome).ToListAsync();
    }

    [HttpGet("ativos")]
    public async Task<ActionResult<List<Usuario>>> GetAtivos()
    {
        return await _db.Usuarios.Where(u => u.Ativo).OrderBy(u => u.Nome).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Usuario>> Get(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        return usuario is null ? NotFound() : usuario;
    }

    [HttpPost]
    public async Task<ActionResult<Usuario>> Create(Usuario usuario)
    {
        usuario.DataCadastro = DateTime.Now;
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = usuario.Id }, usuario);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Usuario usuario)
    {
        if (id != usuario.Id) return BadRequest();
        _db.Entry(usuario).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}