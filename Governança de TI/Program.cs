using Microsoft.EntityFrameworkCore;
using Governança_de_TI.Data;

var builder = WebApplication.CreateBuilder(args);

// OBSERVAÇÃO: A forma de registar a conexão com a base de dados foi atualizada.
// A linha abaixo regista a "Fábrica" de DbContext, que permite a criação de múltiplas
// instâncias do DbContext para as consultas em paralelo do seu DashboardController.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// OBSERVAÇÃO: Adicionamos também esta linha. Ela garante que os seus outros controllers
// (Equipamentos, Descarte, etc.) continuem a receber uma instância única do DbContext
// por pedido, como já faziam antes, evitando que o resto da sua aplicação quebre.
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
