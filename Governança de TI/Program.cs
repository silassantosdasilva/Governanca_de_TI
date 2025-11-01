using Governan�a_de_TI.Data;
using Governan�a_de_TI.Services; // Adiciona o namespace dos seus servi�os
using Governan�a_de_TI.Views.Services.Gamificacao;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Configura��o da Base de Dados ---
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

// --- Configura��o da Autentica��o ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // rota da p�gina de login
        //options.LogoutPath = "/Account/Logout";      // rota do logout (geralmente gerida por POST)
        //options.AccessDeniedPath = "/Conta/AcessoNegado"; // opcional
    });




//Gamificacao
builder.Services.AddScoped<IGamificacaoService, GamificacaoService>();
// --- Configura��o dos Servi�os ---
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IEmailService, MailKitEmailService>();

// Adiciona os servi�os do MVC ao contentor.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configura o pipeline de pedidos HTTP.
if (!app.Environment.IsDevelopment())
{
    // --- [AJUSTE AQUI] ---
    // Middleware de tratamento de exce��es para produ��o.
    // Redireciona para a nossa p�gina de erro personalizada.
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // Garante que HSTS est� ativo para seguran�a.
    app.UseHsts();
}
else
{
    // Em desenvolvimento, continuamos a usar a p�gina de erro detalhada.
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

