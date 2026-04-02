using GerenciadorEstoque.Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GerenciadorEstoque.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var usuario = Environment.GetEnvironmentVariable("APP_USER");
        var senha = Environment.GetEnvironmentVariable("APP_PASSWORD");

        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senha))
            return StatusCode(500, new LoginResponse { Sucesso = false, Mensagem = "Credenciais não configuradas no servidor (APP_USER/APP_PASSWORD)" });

        if (request.Usuario == usuario && request.Senha == senha)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, request.Usuario)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            return Ok(new LoginResponse { Sucesso = true });
        }

        return Ok(new LoginResponse { Sucesso = false, Mensagem = "Usuário ou senha inválidos" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Ok(new { autenticado = true, usuario = User.Identity.Name });

        return Unauthorized(new { autenticado = false });
    }
}
