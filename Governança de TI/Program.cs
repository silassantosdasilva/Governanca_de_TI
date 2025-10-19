using Governan�a_de_TI.Data;
using Governan�a_de_TI.Services; // Adiciona o namespace dos seus servi�os
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Configura��o da Base de Dados ---

// OBSERVA��O: Registamos a "F�brica" de DbContext, que permite a cria��o de m�ltiplas
// inst�ncias do DbContext para as consultas em paralelo do DashboardController.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// OBSERVA��O: Adicionamos tamb�m esta linha. Ela injeta uma inst�ncia �nica do DbContext
// por pedido, que � usada pelos controllers de CRUD (Equipamentos, Descarte, etc.).
builder.Services.AddScoped(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";        // rota da p�gina de login
        //options.LogoutPath = "/Account/Logout";      // rota do logout
        //options.AccessDeniedPath = "/Conta/AcessoNegado"; // opcional
    });

builder.Services.AddScoped<IAuditService, AuditService>();
// --- Configura��o dos Servi�os ---

// OBSERVA��O: Registamos o nosso servi�o de e-mail.
// Sempre que uma classe (como o UsuarioController) pedir um IEmailService,
// o sistema ir� fornecer uma inst�ncia de MailKitEmailService.
builder.Services.AddScoped<IEmailService, MailKitEmailService>();

// Adiciona os servi�os do MVC ao contentor.
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

