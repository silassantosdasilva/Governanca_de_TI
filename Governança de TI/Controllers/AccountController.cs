using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        #region Login e Logout
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Busca o usuário apenas uma vez
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);

            // Verifica se o usuário existe
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View(model);
            }

            // Verifica se o usuário está inativo
            if (user.Status == "Inativo")
            {
                ModelState.AddModelError(string.Empty, "Usuário inativo!");
                return View(model);
            }

            // Verifica a senha (criptografada)
            if (!VerifyPassword(model.Senha, user.Senha))
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View(model);
            }

            // Cria os claims para autenticação
            var claims = new List<Claim>
            {
              new Claim(ClaimTypes.Name, user.Email),
              new Claim("FullName", user.Nome),
              new Claim(ClaimTypes.Role, user.Perfil),
             };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToLocal(returnUrl);
        }


        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        #endregion

        #region Cadastro (Register)
        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ModelState.Remove("Senha"); // A senha é gerada, não vem do formulário
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Este e-mail já está a ser utilizado.");
                    return View(model);
                }

                string senhaAleatoria = GerarSenhaAleatoria();
                var user = new UsuarioModel
                {
                    Nome = model.Nome,
                    Email = model.Email,
                    Senha = HashPassword(senhaAleatoria),
                    Perfil = "Usuario",
                   
                };
                user.Departamento = "Admin";
                user.DataDeCadastro = DateTime.Now;
                _context.Usuarios.Add(user);
                await _context.SaveChangesAsync();

                try
                {
                    var corpoEmail = $"<p>Olá {user.Nome},</p><p>A sua conta foi criada com sucesso. A sua senha de acesso temporária é: <strong>{senhaAleatoria}</strong></p>";
                    await _emailService.EnviarEmailAsync(user.Email, "Bem-vindo ao Sistema", corpoEmail);
                    TempData["SuccessMessage"] = "Cadastro realizado com sucesso! Uma senha foi enviada para o seu e-mail.";
                }
                catch (Exception ex)
                {
                    TempData["WarningMessage"] = $"Utilizador cadastrado, mas falha ao enviar e-mail: {ex.Message}";
                }

                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }
        #endregion

        #region Recuperação de Senha
        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user != null)
                {
                    string novaSenha = GerarSenhaAleatoria();
                    user.Senha = HashPassword(novaSenha);
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    try
                    {
                        var corpoEmail = $"<p>Olá {user.Nome},</p><p>Você solicitou uma redefinição de senha. A sua nova senha de acesso temporária é: <strong>{novaSenha}</strong></p>";
                        await _emailService.EnviarEmailAsync(user.Email, "Redefinição de Senha", corpoEmail);
                    }
                    catch (Exception)
                    {
                        // Mesmo se o e-mail falhar, a mensagem de sucesso genérica é mostrada por segurança
                    }
                }
                // Por segurança, mostramos sempre a mesma mensagem, quer o e-mail exista ou não
                TempData["SuccessMessage"] = "Se um utilizador com este e-mail existir, uma nova senha será enviada.";
                return View(model);
            }
            return View(model);
        }
        #endregion

        #region Métodos Auxiliares
        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            return HashPassword(enteredPassword) == storedHash;
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private string GerarSenhaAleatoria(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
        #endregion
    }
}

