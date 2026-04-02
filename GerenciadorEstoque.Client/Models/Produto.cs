using System.ComponentModel.DataAnnotations;

namespace GerenciadorEstoque.Client.Models;

public class Produto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tipo é obrigatório")]
    [MaxLength(100)]
    public string Tipo { get; set; } = string.Empty;

    public decimal PrecoTabelado { get; set; }

    public decimal PrecoVenda { get; set; }

    public string? ImagemUrl { get; set; }

    public byte[]? ImagemDados { get; set; }

    public string? ImagemContentType { get; set; }

    public ICollection<EstoqueItem> ItensEstoque { get; set; } = new List<EstoqueItem>();
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}