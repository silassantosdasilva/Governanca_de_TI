// Adicione as referências para as Models que criamos (assumindo que estão no namespace Governança_de_TI.Models)
// using Governança_de_TI.Models; 
using Governança_de_TI.Models.Auditoria;
using Governança_de_TI.Models.Financeiro;
using Governança_de_TI.Models.Financeiro.TpLancamento;
using Governança_de_TI.Models.Gamificacao;
using Governança_de_TI.Models.TecgreenModels;
using Governança_de_TI.Models.Usuario;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Necessário para a configuração do relacionamento 1:N

namespace Governança_de_TI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
          : base(options)
        {
        }

        // DbSets principais
        public DbSet<EquipamentoModel> Equipamentos { get; set; }
        public DbSet<UsuarioModel> Usuarios { get; set; }
        public DbSet<DescarteModel> Descartes { get; set; }
        public DbSet<ConsumoEnergiaModel> ConsumosEnergia { get; set; }
        public DbSet<TipoEquipamentoModel> TiposEquipamento { get; set; }
        public DbSet<AuditLogModel> AuditLogs { get; set; } // Verifique se o nome do modelo está correto
        public DbSet<DepartamentoModel> Departamentos { get; set; }
        public DbSet<LogModel> Logs { get; set; }
        public DbSet<DashboardWidgetModel> DashboardWidgets { get; set; }
        // DbSets da Gamificação
        public DbSet<SaldoDiarioModel> SaldosDiarios { get; set; }
        public DbSet<PremioModel> Premios { get; set; } // Nome do DbSet corrigido para plural
        public DbSet<GamificacaoModel> Gamificacoes { get; set; } // Nome do DbSet corrigido para plural
        public DbSet<UsuarioPremioModel> UsuarioPremios { get; set; } // Nome do DbSet corrigido para plural

        // === DbSets DO MÓDULO FINANCEIRO (NOVOS) ===
        public DbSet<PessoaModel> Pessoas { get; set; }
        public DbSet<ContaBancariaModel> ContasBancarias { get; set; }
        public DbSet<TipoLancamentoModel> TiposLancamento { get; set; }
        public DbSet<SubCategoriaModel> SubCategorias { get; set; }
        public DbSet<LancamentoFinanceiroModel> LancamentosFinanceiros { get; set; }
        public DbSet<LancamentoParcelaModel> LancamentosParcelas { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Mantenha esta linha

            // --- Configuração da Chave Composta para UsuarioPremioModel ---
            modelBuilder.Entity<UsuarioPremioModel>()
        .HasKey(up => new { up.UsuarioId, up.PremioId }); // Define a chave composta

            // Configura as relações Muitos-para-Muitos (boa prática)
            modelBuilder.Entity<UsuarioPremioModel>()
        .HasOne(up => up.Usuario)
        .WithMany(u => u.UsuarioPremios) // Garanta que UsuarioModel tem a coleção 'UsuarioPremios'
                .HasForeignKey(up => up.UsuarioId)
        .OnDelete(DeleteBehavior.Cascade); // Opcional: define comportamento ao deletar usuário

            modelBuilder.Entity<UsuarioPremioModel>()
        .HasOne(up => up.Premio)
        .WithMany(p => p.UsuariosQueConquistaram) // Garanta que PremioModel tem a coleção 'UsuariosQueConquistaram'
                .HasForeignKey(up => up.PremioId)
        .OnDelete(DeleteBehavior.Cascade); // Opcional: define comportamento ao deletar prêmio


            // --- Outras Configurações ---
            // Índice único de Email para UsuarioModel
            modelBuilder.Entity<UsuarioModel>()
        .HasIndex(u => u.Email)
        .IsUnique();

            // Relação One-to-One entre UsuarioModel e GamificacaoModel (se aplicável)
            // Garante que UsuarioId em GamificacaoModel é PK e FK
            modelBuilder.Entity<GamificacaoModel>()
       .HasOne(g => g.Usuario)
       .WithOne(u => u.Gamificacao) // Assumindo que UsuarioModel terá uma propriedade 'Gamificacao'
               .HasForeignKey<GamificacaoModel>(g => g.UsuarioId)
       .IsRequired(false);   // ⬅ TORNA OPCIONAL

            // === CONFIGURAÇÃO ESPECÍFICA DO MÓDULO FINANCEIRO ===

            // Configuração da precisão de colunas decimais para evitar avisos do EF Core
            modelBuilder.Entity<ContaBancariaModel>()
                .Property(c => c.SaldoAtual)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<LancamentoFinanceiroModel>()
                .Property(l => l.ValorOriginal)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<LancamentoFinanceiroModel>()
                .Property(l => l.Valor)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<LancamentoParcelaModel>()
                .Property(lp => lp.ValorParcela)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<LancamentoParcelaModel>()
                .Property(lp => lp.ValorPago)
                .HasColumnType("decimal(18, 2)");

            // Configuração do relacionamento 1:N entre Lançamento e Parcelas
            modelBuilder.Entity<LancamentoParcelaModel>()
                .HasOne(lp => lp.LancamentoPai) // Cada parcela tem um Lançamento Pai
                .WithMany(lf => lf.Parcelas) // Um Lançamento Pai tem muitas parcelas
                .HasForeignKey(lp => lp.IdLancamento)
                .OnDelete(DeleteBehavior.Cascade); // Se o Lançamento for excluído, exclui as Parcelas

            // Adicione outras configurações Fluent API conforme necessário
        }
    }
}