using BCrypt.Net;
using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        // VIEWBAG DEPARTAMENTOS
        // ============================================================
        private async Task CarregarDepartamentosViewBag(int? selectedId = null)
        {
            var departamentos = await _context.Departamentos
                .AsNoTracking()
                .OrderBy(d => d.Nome)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Nome
                }).ToListAsync();

            var lista = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Selecione um departamento..." }
            };
            lista.AddRange(departamentos);

            ViewBag.DepartamentoId = new SelectList(lista, "Value", "Text", selectedId?.ToString());
        }

        // ============================================================
        // INDEX
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
        // CRIAR USUÁRIO (MODAL)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> _CriarUsuarioPartial()
        {
            await CarregarDepartamentosViewBag();
            return PartialView("_CriarUsuarioPartial", new UsuarioModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(UsuarioModel usuario, IFormFile Imagem)
        {
            ModelState.Remove("Senha");

            if (!ModelState.IsValid)
            {
                await CarregarDepartamentosViewBag(usuario.DepartamentoId);
                return PartialView("_CriarUsuarioPartial", usuario);
            }

            if (await _context.Usuarios.AsNoTracking().AnyAsync(u => u.Email == usuario.Email))
            {
                ModelState.AddModelError("Email", "Este e-mail já está sendo utilizado.");
                await CarregarDepartamentosViewBag(usuario.DepartamentoId);
                return PartialView("_CriarUsuarioPartial", usuario);
            }

            if (Imagem?.Length > 0)
            {
                using var ms = new MemoryStream();
                await Imagem.CopyToAsync(ms);
                usuario.FotoPerfil = ms.ToArray();
            }

            string senhaAleatoria = GerarSenhaAleatoria();
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(senhaAleatoria);
            usuario.DataDeCadastro = DateTime.Now;
            usuario.Status ??= "Ativo";

            _context.Add(usuario);
            await _context.SaveChangesAsync();

            // Auditoria e envio de email
            _ = Task.Run(async () =>
            {
                var id = await GetCurrentUserId();
                if (id.HasValue)
                    await _auditService.RegistrarAcao(id.Value, "Criou Usuário", $"Usuário: {usuario.Nome}");

                try
                {
                    await _emailService.EnviarEmailAsync(usuario.Email, "Bem-vindo", $"<p>Olá {usuario.Nome},</p><p>Sua senha: <strong>{senhaAleatoria}</strong></p>");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Falha no envio de e-mail para {usuario.Email}: {ex.Message}");
                }
            });

            return Ok(new { redirectTo = Url.Action(nameof(Index)), message = $"Usuário {usuario.Nome} criado com sucesso!" });
        }

        // ============================================================
        // EDITAR USUÁRIO (MODAL)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> _EditarUsuarioPartial(int id)
        {
            var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
                return NotFound($"Usuário ID {id} não encontrado.");

            await CarregarDepartamentosViewBag(usuario.DepartamentoId);
            return PartialView("_EditarUsuarioPartial", usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(UsuarioModel model, IFormFile Imagem)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == model.Id);
            if (usuario == null)
                return BadRequest(new { message = "Usuário não encontrado." });

            usuario.Nome = model.Nome;
            usuario.Email = model.Email;
            usuario.Perfil = model.Perfil;
            usuario.Status = model.Status;
            usuario.DepartamentoId = model.DepartamentoId;

            if (Imagem?.Length > 0)
            {
                using var ms = new MemoryStream();
                await Imagem.CopyToAsync(ms);
                usuario.FotoPerfil = ms.ToArray();
            }

            await _context.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                var id = await GetCurrentUserId();
                if (id.HasValue)
                    await _auditService.RegistrarAcao(id.Value, "Editou Usuário", $"Usuário: {usuario.Nome}");
            });

            return Ok(new { redirectTo = Url.Action(nameof(Index)), message = "Usuário atualizado com sucesso!" });
        }

        // ============================================================
        // DETALHES USUÁRIO (MODAL)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> _DetalhesUsuarioPartial(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Departamento)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                return NotFound($"Usuário com ID {id} não encontrado.");

            // === [TRECHO CORRIGIDO] ===
            var atividades = await _context.AuditLogs
                .Where(a => a.UsuarioId == id)
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            // Envia as atividades para a partial
            ViewBag.Atividades = atividades;

            return PartialView("_DetalhesUsuarioPartial", usuario);
        }

        // ============================================================
        // REDEFINIR SENHA
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RedefinirSenha(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            string novaSenha = GerarSenhaAleatoria();
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(novaSenha);
            await _context.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                var executorId = await GetCurrentUserId();
                if (executorId.HasValue)
                    await _auditService.RegistrarAcao(executorId.Value, "Redefiniu Senha", $"Usuário: {usuario.Email}");

                try
                {
                    await _emailService.EnviarEmailAsync(usuario.Email, "Redefinição de Senha", $"<p>Olá {usuario.Nome},</p><p>Sua nova senha: <strong>{novaSenha}</strong></p>");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Falha no envio de e-mail: {ex.Message}");
                }
            });

            TempData["SuccessMessage"] = $"Nova senha enviada para {usuario.Email}.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // GET: Usuario/_ExcluirUsuarioPartial/{id} (Para o Modal de Exclusão)
        // ============================================================
        [HttpGet("_ExcluirUsuarioPartial/{id}")]
        [HttpGet("Usuario/_ExcluirUsuarioPartial/{id}")]
        public async Task<IActionResult> _ExcluirUsuarioPartial(int id)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Select(u => new UsuarioModel
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    FotoPerfil = u.FotoPerfil,
                    Perfil = u.Perfil
                })
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                return NotFound($"Usuário com ID {id} não encontrado.");

            return PartialView("_ExcluirUsuarioPartial", usuario);
        }

        // ============================================================
        // EXCLUSÃO CONFIRMADA (POST)
        // ============================================================
        [HttpPost("Usuario/Excluir/{id}")]
        [ActionName("ExcluirConfirmado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return BadRequest(new { message = "Utilizador não encontrado." });

            // Verifica se é o único admin
            if (usuario.Perfil == "Admin" && await _context.Usuarios.CountAsync(u => u.Perfil == "Admin" && u.Id != id) == 0)
            {
                return BadRequest(new { message = "Não é possível excluir o único administrador." });
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                var executorId = await GetCurrentUserId();
                if (executorId.HasValue)
                    await _auditService.RegistrarAcao(executorId.Value, "Excluiu Usuário", $"ID={usuario.Id}, Email={usuario.Email}");
            });

            return Ok(new
            {
                redirectTo = Url.Action(nameof(Index)),
                message = "Usuário excluído com sucesso!"
            });
        }

        // ============================================================
        // MÉTODOS AUXILIARES
        // ============================================================
        private string GerarSenhaAleatoria(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task<int?> GetCurrentUserId()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;

            var user = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            return user?.Id;
        }
    }
}
