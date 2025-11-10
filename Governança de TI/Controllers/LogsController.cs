using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Governança_de_TI.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    // ============================================================
    // 🧾  TELA DE LOGS DO SISTEMA (ACESSO RESTRITO A ADMIN)
    // ============================================================
    //
    //  - Somente usuários com Perfil = "Admin" podem acessar.
    //  - Exibe todos os registros gravados pelo LogService.
    //  - Permite limpar (apagar todos os logs).
    //
    // ============================================================
    public class LogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔸 Verifica se o usuário logado é Admin
        // ============================================================
        private bool UsuarioEhAdmin()
        {
            try
            {
                // Pega o e-mail do usuário logado (definido no Identity ou login manual)
                var email = User.Identity?.Name;
                if (string.IsNullOrEmpty(email))
                    return false;

                // Busca o usuário no banco
                var usuario = _context.Usuarios
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Email == email);

                // Retorna verdadeiro se for admin
                return usuario != null && usuario.Perfil?.ToLower() == "admin";
            }
            catch
            {
                return false;
            }
        }

        // ✅ Retorna os últimos registros (carregados no modal via AJAX)
        [HttpGet]
        public IActionResult ListarUltimos()
        {
            var logs = _context.Logs
                .OrderByDescending(l => l.DataRegistro)
                .Take(30)
                .ToList();

            return PartialView("_LogsPartial", logs);
        }


        // ============================================================
        // 🔹 INDEX — exibe a lista de logs
        // ============================================================
        public async Task<IActionResult> Index()
        {
            // 🔒 Verifica permissão
            if (!UsuarioEhAdmin())
            {
                TempData["ErrorMessage"] = "Acesso negado. Apenas administradores podem visualizar os logs do sistema.";
                return RedirectToAction("Index", "Home");
            }

            var logs = await _context.Logs
                .OrderByDescending(l => l.DataRegistro)
                .Take(300) // limite inicial para não pesar
                .ToListAsync();

            return View(logs);
        }
        // ============================================================
        // 🔹 LIMPAR (usado no modal AJAX)
        // ============================================================
        [HttpPost]
        public IActionResult Limpar()
        {
            try
            {
                // Apenas Admin pode apagar
                if (!UsuarioEhAdmin())
                    return Forbid("Apenas administradores podem apagar logs.");

                var allLogs = _context.Logs.ToList();
                if (allLogs.Any())
                {
                    _context.Logs.RemoveRange(allLogs);
                    _context.SaveChanges();
                }

                return Ok(); // ✅ usado pelo modal no _Layout.cshtml
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][Log/Limpar]: {ex.Message}");
                return StatusCode(500, "Erro interno ao apagar logs.");
            }
        }

        // ============================================================
        // 🔸 LIMPAR CONFIRMADO (usado em página própria)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> LimparConfirmado()
        {
            if (!UsuarioEhAdmin())
            {
                TempData["ErrorMessage"] = "Acesso negado. Apenas administradores podem apagar os logs.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _context.Logs.RemoveRange(_context.Logs);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Todos os logs foram apagados com sucesso.";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][LogsController/LimparConfirmado]: {ex.Message}");
                TempData["ErrorMessage"] = "Erro ao apagar os logs.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
