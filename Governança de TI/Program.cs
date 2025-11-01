using Governança_de_TI.Data;
using Governança_de_TI.Services; // Adiciona o namespace dos seus serviços
using Governança_de_TI.Views.Services.Gamificacao;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Configuração da Base de Dados ---
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

// --- Configuração da Autenticação ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // rota da página de login
        //options.LogoutPath = "/Account/Logout";      // rota do logout (geralmente gerida por POST)
        //options.AccessDeniedPath = "/Conta/AcessoNegado"; // opcional
    });




//Gamificacao
builder.Services.AddScoped<IGamificacaoService, GamificacaoService>();
// --- Configuração dos Serviços ---
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IEmailService, MailKitEmailService>();

// Adiciona os serviços do MVC ao contentor.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configura o pipeline de pedidos HTTP.
if (!app.Environment.IsDevelopment())
{
    // --- [AJUSTE AQUI] ---
    // Middleware de tratamento de exceções para produção.
    // Redireciona para a nossa página de erro personalizada.
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // Garante que HSTS está ativo para segurança.
    app.UseHsts();
}
else
{
    // Em desenvolvimento, continuamos a usar a página de erro detalhada.
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// --- [ORDEM IMPORTANTE] ---
// UseRouting deve vir DEPOIS do UseExceptionHandler
app.UseRouting();

// UseAuthentication e UseAuthorization devem vir DEPOIS de UseRouting
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

