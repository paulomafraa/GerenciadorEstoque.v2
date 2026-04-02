using System.ComponentModel.DataAnnotations;

namespace GerenciadorEstoque.Client.Models;

public class Venda
{
    public int Id { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantidade { get; set; }

    [MaxLength(200)]
    public string? Cliente { get; set; }

    /// <summary>Quem registrou esta venda/retirada</summary>
    [MaxLength(100)]
    public string? Responsavel { get; set; }

    /// <summary>Preço cobrado por cada unidade vendida</summary>
    public decimal ValorUnitario { get; set; }

    /// <summary>Quantidade * ValorUnitario</summary>
    public decimal ValorTotal { get; set; }

    public DateTime DataVenda { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string FormaPagamento { get; set; } = "Dinheiro";

    public bool Pago { get; set; }

    /// <summary>Se pago parcialmente, quanto foi pago</summary>
    public decimal? ValorPago { get; set; }

    /// <summary>Se não pago totalmente, quando será pago o restante (null = N/A)</summary>
    public DateTime? PrevisaoPagamentoRestante { get; set; }

    /// <summary>Forma de pagamento do valor restante</summary>
    [MaxLength(50)]
    public string? FormaPagamentoRestante { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    /// <summary>Nome do arquivo de comprovante de pagamento (salvo na pasta persistente)</summary>
    [MaxLength(300)]
    public string? ComprovanteUrl { get; set; }
}