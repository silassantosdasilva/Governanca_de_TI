using Governança_de_TI;
using Governança_de_TI.Data;
using Governança_de_TI.Middlewares;
using Governança_de_TI.Services; // Contém as implementações dos Services e Interfaces de Service
using Governança_de_TI.Views.Services.Gamificacao;
using Governança_de_TI.Repositories; // <--- NOVO: Contém as implementações dos Repositórios
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.IO;


var builder = WebApplication.CreateBuilder(args);

// =========================================
// BANCO DE DADOS
// =========================================
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
 options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(p =>
 p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

// =========================================
// SERVIÇOS DO CORE (AUDITORIA/EMAIL/GAMIFICAÇÃO)
// =========================================
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IEmailService, MailKitEmailService>();
builder.Services.AddScoped<IGamificacaoService, GamificacaoService>();
builder.Services.AddScoped<WidgetQueryService>();
builder.Services.AddHttpContextAccessor();


// =========================================
// REGISTROS DO MÓDULO FINANCEIRO (OTIMIZADO)
// =========================================

// --- 1. Repositórios (TODOS MANTIDOS) ---
// Note que as interfaces I...Repository estão em Governança_de_TI.Repositories
//builder.Services.AddScoped<IPessoaRepository, PessoaRepository>();
//builder.Services.AddScoped<IContaBancariaRepository, ContaBancariaRepository>();
//builder.Services.AddScoped<ITipoLancamentoRepository, TipoLancamentoRepository>();
builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();
builder.Services.AddScoped<IExtratoRepository, ExtratoRepository>();

// --- 2. Services (APENAS FLUXO DE CAIXA COMPLEXO) ---
// Note que as interfaces I...Service estão em Governança_de_TI.Services
builder.Services.AddScoped<ILancamentoService, LancamentoService>();
builder.Services.AddScoped<IExtratoService, ExtratoService>();

// =========================================
// AUTENTICAÇÃO
// =========================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
 .AddCookie(options =>
 {
     options.Cookie.Name = "GovernancaTI.Auth";
     options.Cookie.HttpOnly = true;
     options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
     options.Cookie.SameSite = SameSiteMode.Lax;

     options.LoginPath = "/Account/Login";
     options.LogoutPath = "/Account/Logout";
     options.AccessDeniedPath = "/Account/AcessoNegado";

     options.SlidingExpiration = true;
     options.ExpireTimeSpan = TimeSpan.FromHours(2);
 });

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// =========================================
// EXCEPTION HANDLER (DEVE VIR ANTES DE TUDO)
// =========================================
if (!app.Environment.IsDevelopment())
{
    // Encaminha todos os erros para a página customizada
    app.UseExceptionHandler("/Erro/Geral");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// =========================================
// MIDDLEWARE DE PIPELINE (ORDEM CORRETA)
// =========================================
app.UseHttpsRedirection();
app.UseStaticFiles();

// Servir arquivos da pasta /uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
  Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads"
});

// 🚨 IMPORTANTE: MOVER O ROUTING PARA CIMA
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// =========================================
// MIDDLEWARE PERSONALIZADO DE LOG DE ERROS
// (AGORA NA POSIÇÃO CORRETA DO PIPELINE)
// =========================================
app.UseMiddleware<BadRequestMiddleware>();
app.UseMiddleware<JsonExceptionMiddleware>();

// =========================================
// ROUTES
// =========================================
app.MapControllerRoute(
 name: "default",
 pattern: "{controller=Home}/{action=Landing}/{id?}");

// =========================================
// HTTPS LIMPO – força somente 5005
// =========================================
app.Run();


