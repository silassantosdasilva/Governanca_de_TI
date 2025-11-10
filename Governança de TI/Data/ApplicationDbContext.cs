using Governança_de_TI.Models; // ajuste conforme o namespace dos seus modelos
// using Governança_de_TI.ViewModels; // Removido se não for usado diretamente aqui
using Microsoft.EntityFrameworkCore;

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
        public DbSet<PremioModel> Premios { get; set; } // Nome do DbSet corrigido para plural
        public DbSet<GamificacaoModel> Gamificacoes { get; set; } // Nome do DbSet corrigido para plural
        public DbSet<UsuarioPremioModel> UsuarioPremios { get; set; } // Nome do DbSet corrigido para plural

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
               .HasForeignKey<GamificacaoModel>(g => g.UsuarioId);

            // Adicione outras configurações Fluent API conforme necessário
            // Ex: Precisão de decimais, nomes de tabelas/colunas específicos, etc.
        }
    }
}

