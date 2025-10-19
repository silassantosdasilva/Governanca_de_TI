using Governança_de_TI.Data;
using Governança_de_TI.Services; // Adiciona o namespace dos seus serviços
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Configuração da Base de Dados ---

// OBSERVAÇÃO: Registamos a "Fábrica" de DbContext, que permite a criação de múltiplas
// instâncias do DbContext para as consultas em paralelo do DashboardController.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// OBSERVAÇÃO: Adicionamos também esta linha. Ela injeta uma instância única do DbContext
// por pedido, que é usada pelos controllers de CRUD (Equipamentos, Descarte, etc.).
builder.Services.AddScoped(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";        // rota da página de login
        //options.LogoutPath = "/Account/Logout";      // rota do logout
        //options.AccessDeniedPath = "/Conta/AcessoNegado"; // opcional
    });

builder.Services.AddScoped<IAuditService, AuditService>();
// --- Configuração dos Serviços ---

// OBSERVAÇÃO: Registamos o nosso serviço de e-mail.
// Sempre que uma classe (como o UsuarioController) pedir um IEmailService,
// o sistema irá fornecer uma instância de MailKitEmailService.
builder.Services.AddScoped<IEmailService, MailKitEmailService>();

// Adiciona os serviços do MVC ao contentor.
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.Run();

