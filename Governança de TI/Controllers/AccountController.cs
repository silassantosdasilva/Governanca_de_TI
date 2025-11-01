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
using System.Security.Cryptography; // Mantido apenas para o método de migração SHA256
using System.Text; // Mantido apenas para o método de migração SHA256
using System.Threading.Tasks;
using BCrypt.Net; // Importar o BCrypt

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

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View(model);
            }

            if (user.Status?.Equals("Inativo", StringComparison.OrdinalIgnoreCase) == true)
            {
                ModelState.AddModelError(string.Empty, "Usuário inativo!");
                return View(model);
            }

            // === [INÍCIO DA LÓGICA DE MIGRAÇÃO DE HASH] ===
            if (!VerifyPassword(model.Senha, user.Senha, out bool needsRehash))
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View(model);
            }

            // O utilizador é válido.
            // Atualiza a data de último login e, se necessário, o hash da senha.
            user.DataUltimoLogin = DateTime.Now;
            if (needsRehash)
            {
                // A senha bateu com o SHA256 antigo. Vamos atualizá-la para BCrypt.
                user.Senha = BCrypt.Net.BCrypt.HashPassword(model.Senha);
            }

            _context.Update(user);
            await _context.SaveChangesAsync();
            // === [FIM DA LÓGICA DE MIGRAÇÃO] ===


            // === [CRIAÇÃO DE CLAIMS] ===
            // (Esta lógica foi refatorada para usar a propriedade 'Claims' do UsuarioModel)
            var claims = user.Claims.ToList(); // Usamos a lógica já definida no UsuarioModel

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            TempData["SuccessMessage"] = $"Bem-vindo, {user.Nome}!";
            return RedirectToAction("Index", "Home");
        }


        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }


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
            ModelState.Remove("Senha");
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
                    // === [ALTERAÇÃO DE SEGURANÇA] ===
                    // Usando BCrypt para criar o hash
                    Senha = BCrypt.Net.BCrypt.HashPassword(senhaAleatoria),
                    Perfil = "Usuario",
                    DataDeCadastro = DateTime.Now,
                    Status = "Ativo" // Garantindo que o Status é definido no cadastro
                };

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

                return RedirectToAction("Login", "Account");
            }
            return View(model);
        }

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

                    // === [ALTERAÇÃO DE SEGURANÇA] ===
                    // Usando BCrypt para criar o hash
                    user.Senha = BCrypt.Net.BCrypt.HashPassword(novaSenha);
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

                TempData["SuccessMessage"] = "Se um utilizador com este e-mail existir, uma nova senha será enviada.";
                return View(model); // Retorna para a View (em vez de Redirecionar) para mostrar a TempData
            }
            return View(model);
        }
        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Verifica a senha usando o novo método BCrypt e, se falhar,
        /// tenta o método antigo SHA256 (para migração).
        /// </summary>
        /// <param name="enteredPassword">Senha digitada pelo utilizador.</param>
        /// <param name="storedHash">Hash guardado na BD (pode ser BCrypt ou SHA256).</param>
        /// <param name="needsRehash">OUT: Retorna true se a senha for válida mas usar o formato antigo (SHA256).</param>
        /// <returns>True se a senha for válida.</returns>
        private bool VerifyPassword(string enteredPassword, string storedHash, out bool needsRehash)
        {
            needsRehash = false;

            try
            {
                // 1. Tenta verificar usando o novo padrão (BCrypt)
                if (BCrypt.Net.BCrypt.Verify(enteredPassword, storedHash))
                {
                    return true;
                }

                // (BCrypt.Verify retorna false se não for um hash válido ou se a senha não bater, 
                // mas pode lançar exceção se o hash não estiver no formato esperado)
                return false;
            }
            catch (Exception)
            {
                // 2. Se o BCrypt falhar (ex: SaltParseException), pode ser um hash SHA256 antigo.
                // Tenta verificar usando o método antigo.
                string oldHashAttempt = HashPasswordSHA256_OLD(enteredPassword);

                if (oldHashAttempt == storedHash)
                {
                    // A senha está correta, mas usa o formato antigo.
                    // Sinaliza que precisa ser atualizada.
                    needsRehash = true;
                    return true;
                }

                // Falhou em ambos os métodos
                return false;
            }
        }

        /// <summary>
        /// MÉTODO DE HASH ANTIGO (INSEGURO) - MANTIDO APENAS PARA MIGRAÇÃO.
        /// NÃO USAR PARA CRIAR NOVOS HASHES.
        /// </summary>
        private string HashPasswordSHA256_OLD(string password)
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