using Governança_de_TI.Models; // ajuste conforme o namespace dos seus modelos
using Governança_de_TI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Governança_de_TI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Suas tabelas (DbSets)
        public DbSet<EquipamentoModel> Equipamentos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<DescarteModel> Descartes { get; set; }
        public DbSet<ConsumoEnergiaModel> ConsumosEnergia { get; set; }

        public DbSet<TipoEquipamentoModel> TiposEquipamento { get; set; }




        // Adicione mais conforme seus modelos
    }
}