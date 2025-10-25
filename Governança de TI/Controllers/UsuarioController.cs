using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic; // Necessário para List<>

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
        // Método Privado para Carregar ViewBag de Departamentos
        // ============================================================
        private async Task CarregarDepartamentosViewBag(int? selectedId = null)
        {
            // --- [TESTE] Comentada a busca na base de dados ---
            // var departamentosDb = await _context.Departamentos
            //                                   .AsNoTracking()
            //                                   .OrderBy(d => d.Nome)
            //                                   .Select(d => new SelectListItem
            //                                   {
            //                                       Value = d.Id.ToString(),
            //                                       Text = d.Nome
            //                                   })
            //                                   .ToListAsync();

            // --- [TESTE] Usando uma lista vazia para simular ---
            var departamentosDb = new List<SelectListItem>();


            // Adiciona a opção "Selecione..." no início da lista
            var selectListItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Selecione um departamento..." }
            };
            selectListItems.AddRange(departamentosDb); // Adiciona a lista (vazia neste teste)

            // Cria o SelectList usando a lista de SelectListItem
            ViewBag.DepartamentoId = new SelectList(selectListItems, "Value", "Text", selectedId?.ToString());

            // --- Fim do Teste ---

            // **NOTA:** Lembre-se de descomentar a busca na base de dados e remover a lista vazia depois de testar!
        }

        // ============================================================
        // GET: Usuario
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                                         .Include(u => u.Departamento)
                                         .OrderBy(u => u.Nome)
                                         .ToListAsync();
            return View(usuarios);
        }

        // ============================================================
        // GET: Usuario/Detalhes/5
        // ============================================================
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                                        .Include(u => u.Departamento)
                                        .FirstOrDefaultAsync(m => m.Id == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // ============================================================
        // GET: Usuario/Criar
        // ============================================================
        public async Task<IActionResult> Criar()
        {
            await CarregarDepartamentosViewBag();
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
            {
                await CarregarDepartamentosViewBag(usuario.DepartamentoId);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_CriarUsuarioPartial", usuario);
                }
                return View(usuario);
            }

            var existingUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == usuario.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Este e-mail já está sendo utilizado por outra conta.");
                await CarregarDepartamentosViewBag(usuario.DepartamentoId);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_CriarUsuarioPartial", usuario);
                }
                return View(usuario);
            }

            if (Imagem != null && Imagem.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await Imagem.CopyToAsync(ms);
                    usuario.FotoPerfil = ms.ToArray();
                }
            }

            string senhaAleatoria = GerarSenhaAleatoria();
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(senhaAleatoria);
            usuario.DataDeCadastro = DateTime.Now;

            _context.Add(usuario);
            await _context.SaveChangesAsync();

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

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { redirectTo = Url.Action(nameof(Index)) });
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

            await CarregarDepartamentosViewBag(usuario.DepartamentoId);
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

            if (!ModelState.IsValid)
            {
                await CarregarDepartamentosViewBag(model.DepartamentoId);
                return View(model);
            }

            if (usuario.Email != model.Email)
            {
                var emailExists = await _context.Usuarios.AnyAsync(u => u.Email == model.Email && u.Id != model.Id);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Este e-mail já está sendo utilizado por outra conta.");
                    await CarregarDepartamentosViewBag(model.DepartamentoId);
                    return View(model);
                }
            }

            usuario.Nome = model.Nome;
            usuario.Email = model.Email;
            usuario.Status = model.Status;
            usuario.Perfil = model.Perfil;
            usuario.DepartamentoId = model.DepartamentoId;

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

            TempData["SuccessMessage"] = "Perfil atualizado com sucesso!";
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
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(novaSenha);

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

        // ============================================================
        // GET: Usuario/_CriarUsuarioPartial
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> _CriarUsuarioPartial()
        {
            await CarregarDepartamentosViewBag();
            return PartialView("_CriarUsuarioPartial", new UsuarioModel());
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
        #endregion
    }
}

