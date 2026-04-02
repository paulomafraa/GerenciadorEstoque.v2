using System.ComponentModel.DataAnnotations;

namespace GerenciadorEstoque.Client.Models;

public class EstoqueItem
{
    public int Id { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
    public int Quantidade { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Preço de compra deve ser maior que zero")]
    public decimal PrecoCompra { get; set; }

    public DateTime DataCompra { get; set; } = DateTime.Now;

    /// <summary>Quem registrou esta entrada de estoque</summary>
    [MaxLength(100)]
    public string? Responsavel { get; set; }
}