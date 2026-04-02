using GerenciadorEstoque.Client.Models;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorEstoque.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<EstoqueItem> ItensEstoque => Set<EstoqueItem>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.PrecoTabelado).HasColumnType("decimal(18,2)");
            e.Property(p => p.PrecoVenda).HasColumnType("decimal(18,2)");
            e.Property(p => p.ImagemDados).HasColumnType("LONGBLOB");
        });

        modelBuilder.Entity<EstoqueItem>(e =>
        {
            e.HasKey(ei => ei.Id);
            e.Property(ei => ei.PrecoCompra).HasColumnType("decimal(18,2)");
            e.HasOne(ei => ei.Produto)
             .WithMany(p => p.ItensEstoque)
             .HasForeignKey(ei => ei.ProdutoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Venda>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.ValorUnitario).HasColumnType("decimal(18,2)");
            e.Property(v => v.ValorTotal).HasColumnType("decimal(18,2)");
            e.Property(v => v.ValorPago).HasColumnType("decimal(18,2)");
            e.HasOne(v => v.Produto)
             .WithMany(p => p.Vendas)
             .HasForeignKey(v => v.ProdutoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasKey(u => u.Id);
        });
    }
}