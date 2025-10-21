using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Governança_de_TI.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public UsuarioController(ApplicationDbContext context, IEmailService emailService, IAuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
        }

        // ============================================================
        // GET: Usuario
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios.OrderBy(u => u.Nome).ToListAsync();
            return View(usuarios);
        }

        // ============================================================
        // GET: Usuario/Detalhes/5
        // ============================================================
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.Id == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // ============================================================
        // GET: Usuario/Criar
        // ============================================================
        public IActionResult Criar()
        {
            return View();
        }

        // ============================================================
        // POST: Usuario/Criar
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(UsuarioModel usuario, IFormFile Imagem)
        {
            ModelState.Remove("Senha");

            if (!ModelState.IsValid)
                return View(usuario);

            // Verifica duplicidade de e-mail
            var existingUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == usuario.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Este e-mail já está sendo utilizado por outra conta.");
                return View(usuario);
            }

            // Salva imagem de perfil (se enviada)
            if (Imagem != null && Imagem.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await Imagem.CopyToAsync(ms);
                    usuario.FotoPerfil = ms.ToArray();
                }
            }

            // Gera senha aleatória e define data de criação
            string senhaAleatoria = GerarSenhaAleatoria();
            usuario.Senha = HashPassword(senhaAleatoria);
            usuario.DataDeCadastro = DateTime.Now;

            _context.Add(usuario);
            await _context.SaveChangesAsync();

            // Registra auditoria
            var executorId = await GetCurrentUserId();
            if (executorId.HasValue)
            {
                await _auditService.RegistrarAcao(
                    executorId.Value,
                    "Criou Usuário",
                    $"Usuário criado: {usuario.Nome}, E-mail: {usuario.Email}"
                );
            }

            try
            {
                var corpoEmail = $"<p>Olá {usuario.Nome},</p>" +
                                 $"<p>Sua conta foi criada com sucesso. Sua senha temporária é: <strong>{senhaAleatoria}</strong></p>";
                await _emailService.EnviarEmailAsync(usuario.Email, "Bem-vindo ao Sistema", corpoEmail);

                TempData["SuccessMessage"] = $"Utilizador {usuario.Nome} criado com sucesso! Senha enviada para {usuario.Email}.";
            }
            catch (Exception ex)
            {
                TempData["WarningMessage"] = $"Utilizador criado, mas falha ao enviar e-mail: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // GET: Usuario/Editar/5
        // ============================================================
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // ============================================================
        // POST: Usuario/Editar/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(UsuarioModel model, IFormFile Imagem)
        {
          

            var usuario = await _context.Usuarios.FindAsync(model.Id);
            if (usuario == null)
                return NotFound();

            usuario.Nome = model.Nome;
            usuario.Email = model.Email;
            usuario.Status = model.Status;
            usuario.Perfil = model.Perfil;
            usuario.Departamento = model.Departamento;

            // Atualiza imagem se houver nova
            if (Imagem != null && Imagem.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await Imagem.CopyToAsync(ms);
                    usuario.FotoPerfil = ms.ToArray();
                }
            }

            _context.Update(usuario);
            await _context.SaveChangesAsync();

            var executorId = await GetCurrentUserId();
            if (executorId.HasValue)
            {
                await _auditService.RegistrarAcao(
                    executorId.Value,
                    "Editou Usuário",
                    $"Usuário editado: {usuario.Nome}, E-mail: {usuario.Email}"
                );
            }

            TempData["Sucesso"] = "Perfil atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // POST: Usuario/RedefinirSenha
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RedefinirSenha(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Utilizador não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            string novaSenha = GerarSenhaAleatoria();
            usuario.Senha = HashPassword(novaSenha);

            _context.Update(usuario);
            await _context.SaveChangesAsync();

            var executorId = await GetCurrentUserId();
            if (executorId.HasValue)
            {
                await _auditService.RegistrarAcao(
                    executorId.Value,
                    "Redefiniu Senha",
                    $"Senha redefinida para o usuário: {usuario.Email}"
                );
            }

            try
            {
                var corpoEmail = $"<p>Olá {usuario.Nome},</p><p>Sua senha foi redefinida. Nova senha temporária: <strong>{novaSenha}</strong></p>";
                await _emailService.EnviarEmailAsync(usuario.Email, "Redefinição de Senha", corpoEmail);

                TempData["SuccessMessage"] = $"Uma nova senha foi enviada para o e-mail {usuario.Email}.";
            }
            catch (Exception ex)
            {
                TempData["WarningMessage"] = $"Senha redefinida, mas falha ao enviar e-mail: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // GET: Usuario/Excluir/5
        // ============================================================
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.Id == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // ============================================================
        // POST: Usuario/Excluir/5
        // ============================================================
        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            var executorId = await GetCurrentUserId();
            if (executorId.HasValue)
            {
                await _auditService.RegistrarAcao(
                    executorId.Value,
                    "Excluiu Usuário",
                    $"Usuário excluído: ID={usuario.Id}, E-mail={usuario.Email}"
                );
            }

            TempData["SuccessMessage"] = "Utilizador excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        #region Métodos Auxiliares
        private string GerarSenhaAleatoria(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task<int?> GetCurrentUserId()
        {
            var userEmail = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
                return null;

            var user = await _context.Usuarios.AsNoTracking()
                                              .FirstOrDefaultAsync(u => u.Email == userEmail);
            return user?.Id;
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
        #endregion
    }
}
