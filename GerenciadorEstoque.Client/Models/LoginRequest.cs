namespace GerenciadorEstoque.Client.Models;

public class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Sucesso { get; set; }
    public string? Mensagem { get; set; }
}
