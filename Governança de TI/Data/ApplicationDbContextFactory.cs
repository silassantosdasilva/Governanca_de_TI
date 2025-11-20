using Governança_de_TI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Governança_de_TI
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // 👉 Se quiser usar Azure nas migrations, coloque aqui
            var connection = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                ? "Server=(localdb)\\MSSQLLocalDB;Database=GTIDatabase;Trusted_Connection=True;"
                : "Server=tcp:sqlserver-tecgren.database.windows.net,1433;Initial Catalog=dbProjetoTecGren;Persist Security Info=False;User ID=admindb;Password=@Lituku5;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            optionsBuilder.UseSqlServer(connection);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
