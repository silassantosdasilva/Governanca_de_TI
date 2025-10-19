using Governan�a_de_TI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// OBSERVA��O: Registamos a "F�brica" de DbContext, que permite a cria��o de m�ltiplas
// inst�ncias do DbContext para as consultas em paralelo do DashboardController.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// OBSERVA��O: Adicionamos tamb�m esta linha. Ela injeta uma inst�ncia �nica do DbContext
// por pedido, que � usada pelos controllers de CRUD (Equipamentos, Descarte, etc.).
builder.Services.AddScoped(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());


// Adiciona os servi�os ao contentor.
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

