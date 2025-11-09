using Governança_de_TI.Data;
using Governança_de_TI.Services;
using Governança_de_TI.Views.Services.Gamificacao;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ========== BANCO DE DADOS ==========
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

// ========== SERVIÇOS ==========
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IEmailService, MailKitEmailService>();
builder.Services.AddScoped<IGamificacaoService, GamificacaoService>();
builder.Services.AddHttpContextAccessor();

// ========== AUTENTICAÇÃO ==========
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

//DASHBOARD DINÂMICA
builder.Services.AddScoped<WidgetQueryService>();

var app = builder.Build();

// ========== MIDDLEWARE ==========
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// HTTPS e arquivos estáticos
app.UseHttpsRedirection();
app.UseStaticFiles();
// Permite servir arquivos da pasta "uploads"
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads"
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// === HTTPS LIMPO: força somente 5005 ===
app.Urls.Clear();
app.Urls.Add("https://localhost:5005");

app.Run();
