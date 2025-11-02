using BCrypt.Net;
using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly string _pastaPerfil;

        public UsuarioController(ApplicationDbContext context, IEmailService emailService, IAuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;

            // Caminho físico da pasta de imagens de perfil
            _pastaPerfil = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "perfil");
            if (!Directory.Exists(_pastaPerfil))
                Directory.CreateDirectory(_pastaPerfil);
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
        public async Task<IActionResult> Criar(UsuarioModel usuario)
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

            // === [UPLOAD DE IMAGEM COM NOME SEQUENCIAL] ===
            if (usuario.FotoUpload != null && usuario.FotoUpload.Length > 0)
            {
                var arquivosExistentes = Directory.GetFiles(_pastaPerfil, "perfil-*").Length;
                var proximoNumero = arquivosExistentes + 1;
                var extensao = Path.GetExtension(usuario.FotoUpload.FileName);
                var nomeArquivo = $"perfil-{proximoNumero}{extensao}";
                var caminhoArquivo = Path.Combine(_pastaPerfil, nomeArquivo);

                using (var stream = new FileStream(caminhoArquivo, FileMode.Create))
                    await usuario.FotoUpload.CopyToAsync(stream);

                usuario.CaminhoFotoPerfil = $"/perfil/{nomeArquivo}";
            }

            string senhaAleatoria = GerarSenhaAleatoria();
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(senhaAleatoria);
            usuario.DataDeCadastro = DateTime.Now;
            usuario.Status ??= "Ativo";

            _context.Add(usuario);
            await _context.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                var id = await GetCurrentUserId();
                if (id.HasValue)
                    await _auditService.RegistrarAcao(id.Value, "Criou Usuário", $"Usuário: {usuario.Nome}");

                try
                {
                    await _emailService.EnviarEmailAsync(
                        usuario.Email,
                        "Bem-vindo",
                        $"<p>Olá {usuario.Nome},</p><p>Sua senha: <strong>{senhaAleatoria}</strong></p>");
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
        public async Task<IActionResult> Editar(UsuarioModel model)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == model.Id);
            if (usuario == null)
                return BadRequest(new { message = "Usuário não encontrado." });

            // Atualiza dados principais
            usuario.Nome = model.Nome;
            usuario.Email = model.Email;
            usuario.Perfil = model.Perfil;
            usuario.Status = model.Status;
            usuario.DepartamentoId = model.DepartamentoId;

            // === [ATUALIZA IMAGEM SE ENVIADA COM NOME SEQUENCIAL] ===
            if (model.FotoUpload != null && model.FotoUpload.Length > 0)
            {
                // Remove imagem antiga (se existir)
                if (!string.IsNullOrEmpty(usuario.CaminhoFotoPerfil))
                {
                    var caminhoAntigo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", usuario.CaminhoFotoPerfil.TrimStart('/'));
                    if (System.IO.File.Exists(caminhoAntigo))
                        System.IO.File.Delete(caminhoAntigo);
                }

                // Cria novo nome sequencial
                var arquivosExistentes = Directory.GetFiles(_pastaPerfil, "perfil-*").Length;
                var proximoNumero = arquivosExistentes + 1;
                var extensao = Path.GetExtension(model.FotoUpload.FileName);
                var nomeArquivo = $"perfil-{proximoNumero}{extensao}";
                var caminhoArquivo = Path.Combine(_pastaPerfil, nomeArquivo);

                using (var stream = new FileStream(caminhoArquivo, FileMode.Create))
                    await model.FotoUpload.CopyToAsync(stream);

                usuario.CaminhoFotoPerfil = $"/perfil/{nomeArquivo}";
            }

            await _context.SaveChangesAsync();

            // === [ATUALIZA CLAIMS SE O USUÁRIO LOGADO FOR O MESMO] ===
            var currentEmail = User?.Identity?.Name;
            if (!string.IsNullOrEmpty(currentEmail) &&
                string.Equals(currentEmail, usuario.Email, StringComparison.OrdinalIgnoreCase))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.Email),
            new Claim("FullName", usuario.Nome ?? string.Empty),
            new Claim(ClaimTypes.Role, usuario.Perfil ?? string.Empty),
            new Claim("CaminhoFotoPerfil", usuario.CaminhoFotoPerfil ?? string.Empty)
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
            }

            // === [REGISTRA AUDITORIA] ===
            _ = Task.Run(async () =>
            {
                var id = await GetCurrentUserId();
                if (id.HasValue)
                    await _auditService.RegistrarAcao(id.Value, "Editou Usuário", $"Usuário: {usuario.Nome}");
            });

            return Ok(new { redirectTo = Url.Action(nameof(Index)), message = "Usuário atualizado com sucesso!" });
        }

        // ============================================================
        // DETALHES
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

            var atividades = await _context.AuditLogs
                .Where(a => a.UsuarioId == id)
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Atividades = atividades;
            return PartialView("_DetalhesUsuarioPartial", usuario);
        }

        // ============================================================
        // EXCLUIR
        // ============================================================
        [HttpPost("Usuario/Excluir/{id}")]
        [ActionName("ExcluirConfirmado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return BadRequest(new { message = "Utilizador não encontrado." });

            if (usuario.Perfil == "Admin" && await _context.Usuarios.CountAsync(u => u.Perfil == "Admin" && u.Id != id) == 0)
                return BadRequest(new { message = "Não é possível excluir o único administrador." });

            if (!string.IsNullOrEmpty(usuario.CaminhoFotoPerfil))
            {
                var caminho = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", usuario.CaminhoFotoPerfil.TrimStart('/'));
                if (System.IO.File.Exists(caminho))
                    System.IO.File.Delete(caminho);
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
        // AUXILIARES
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
