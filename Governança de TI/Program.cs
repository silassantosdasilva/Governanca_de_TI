using Governança_de_TI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// OBSERVAÇÃO: Registamos a "Fábrica" de DbContext, que permite a criação de múltiplas
// instâncias do DbContext para as consultas em paralelo do DashboardController.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// OBSERVAÇÃO: Adicionamos também esta linha. Ela injeta uma instância única do DbContext
// por pedido, que é usada pelos controllers de CRUD (Equipamentos, Descarte, etc.).
builder.Services.AddScoped(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());


// Adiciona os serviços ao contentor.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configura o pipeline de pedidos HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

