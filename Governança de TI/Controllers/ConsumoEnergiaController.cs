using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services; // Adicionado para IAuditService e IGamificacaoService
using Governança_de_TI.Views.Services.Gamificacao;

// using Governança_de_TI.Views.Services.Gamificacao; // Comentado - Namespace parece incorreto
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Adicionado para DateTime
using System.Globalization; // Adicionado para CultureInfo
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    /// <summary>
    /// Controller responsável pela gestão dos registos de consumo de energia.
    /// </summary>
    // [Authorize] // Adicione se esta área for restrita
    public class ConsumoEnergiaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGamificacaoService _gamificacaoService; // <<< NOVO
        private readonly IAuditService _auditService; // <<< NOVO

        public ConsumoEnergiaController(
            ApplicationDbContext context,
            IGamificacaoService gamificacaoService, // <<< NOVO
            IAuditService auditService) // <<< NOVO
        {
            _context = context;
            _gamificacaoService = gamificacaoService;
            _auditService = auditService;
        }

        // GET: ConsumoEnergia
        public async Task<IActionResult> Index()
        {
            var consumos = await _context.ConsumosEnergia
                                         .OrderByDescending(c => c.DataReferencia)
                                         .ToListAsync();
            return View(consumos);
        }

        // GET: ConsumoEnergia/Criar
        // ============================================================
        // GET: ConsumoEnergia/Criar
        // ============================================================
        public async Task<IActionResult> Criar()
        {
            // Cria o modelo inicial com a data do mês atual
            var model = new ConsumoEnergiaModel
            {
                DataReferencia = DateTime.Now
            };

            // 🔹 Sempre retorna a partial (modal), independente de AJAX
            // Isso evita o erro "View 'Criar' not found" quando aberto fora da modal
            return PartialView("_CriarConsumoPartial", model);
        }


        // ============================================================
        // POST: ConsumoEnergia/Criar
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([Bind("DataReferencia,ValorKwh,CustoEnergia,Observacao")] ConsumoEnergiaModel consumo)
        {
            // Padroniza data (sempre dia 1)
            consumo.DataReferencia = new DateTime(consumo.DataReferencia.Year, consumo.DataReferencia.Month, 1);

            // Validação de duplicidade
            bool jaExiste = await _context.ConsumosEnergia
                .AnyAsync(c => c.DataReferencia.Year == consumo.DataReferencia.Year &&
                               c.DataReferencia.Month == consumo.DataReferencia.Month);


            if (jaExiste) {

                TempData["ErrorMessage"] = $"Ja existe registro para o mes {consumo.DataReferencia.Month}/{consumo.DataReferencia.Year} Por favor, alterar o mes existente! ";
                return RedirectToAction(nameof(Index));

            }
        // === Salvar no banco ===
        _context.Add(consumo);
            await _context.SaveChangesAsync();

            var userId = await GetCurrentUserId();

            // --- GAMIFICAÇÃO ---
            if (userId.HasValue)
                await _gamificacaoService.AdicionarPontosAsync(userId.Value, "RegistrouConsumoEnergia", 10);

    


            var IdConsumo = await GetCurrentUserId();
            if (IdConsumo.HasValue)
                await _auditService.RegistrarAcao(IdConsumo.Value, "Criou Consumo de Energia",
                        $"Mês/Ano: {consumo.DataReferencia:MM/yyyy}, Valor: {consumo.ValorKwh} kWh");

            // === Resposta AJAX ===
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = "Consumo registrado com sucesso!"
                });
            }

            // === Fallback padrão ===
            TempData["SuccessMessage"] = "Registo criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // GET: ConsumoEnergia/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();

            var consumo = await _context.ConsumosEnergia.FindAsync(id);
            if (consumo == null) return NotFound();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_EditarConsumoPartial", consumo);

            return View("_EditarConsumoPartial", consumo);
        }

        // POST: ConsumoEnergia/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("Id,DataReferencia,ValorKwh")] ConsumoEnergiaModel consumo)
        {
            if (id != consumo.Id) return NotFound();

            consumo.DataReferencia = new DateTime(consumo.DataReferencia.Year, consumo.DataReferencia.Month, 1);
      

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("_EditarConsumoPartial", consumo);

                return View(consumo);
            }

            _context.Update(consumo);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = "Registo de consumo atualizado com sucesso!" });

            var IdConsumo = await GetCurrentUserId();
            if (IdConsumo.HasValue)
                await _auditService.RegistrarAcao(IdConsumo.Value, "Editou Consumo de Energia",
                        $"Mês/Ano: {consumo.DataReferencia:MM/yyyy}, Valor: {consumo.ValorKwh} kWh");

            TempData["SuccessMessage"] = "Registo de consumo atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // GET: ConsumoEnergia/Excluir/5
        // ============================================================
        // GET: ConsumoEnergia/Excluir/5
        // ============================================================
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null) return NotFound();

            var consumo = await _context.ConsumosEnergia.FirstOrDefaultAsync(m => m.Id == id);
            if (consumo == null) return NotFound();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_ExcluirConsumoPartial", consumo);

            return View("_ExcluirConsumoPartial", consumo);
        }

        // ============================================================
        // POST: ConsumoEnergia/ExcluirConfirmado/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var consumo = await _context.ConsumosEnergia.FindAsync(id);
            if (consumo == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Registo não encontrado." });

                TempData["ErrorMessage"] = "Registo não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            _context.ConsumosEnergia.Remove(consumo);
            await _context.SaveChangesAsync();

            // === Auditoria ===
            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                
                var IdConsumo = await GetCurrentUserId();
                if (IdConsumo.HasValue)
                    await _auditService.RegistrarAcao(IdConsumo.Value, "Excluiu Consumo", $"ID={consumo.Id}, Mês/Ano: {consumo.DataReferencia:MM/yyyy}");
            }


            // === Gamificação (opcional: subtrai pontos) ===
            if (userId.HasValue)
            {
                await _gamificacaoService.AdicionarPontosAsync(userId.Value, "ExcluiuConsumoEnergia", -5);
            }
            var user = await GetCurrentUserId();
            if (user.HasValue)
            {
               

                    var IdConsumo = await GetCurrentUserId();
                    if (IdConsumo.HasValue)
                        await _auditService.RegistrarAcao(IdConsumo.Value, "Excluiu Consumo", $"ID={consumo.Id}, Mês/Ano: {consumo.DataReferencia:MM/yyyy}");

                }
            // === AJAX (resposta JSON) ===
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = "Registo de consumo excluído com sucesso!"
                });
            }

            // === Fallback normal ===
            TempData["SuccessMessage"] = "Registo de consumo excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }


        // --- [AUDITORIA] (Executa em background) ---

       private bool ConsumoEnergiaExists(int id)
        {
            return _context.ConsumosEnergia.Any(e => e.Id == id);
        }

        // Método auxiliar para obter ID do usuário logado
        private async Task<int?> GetCurrentUserId()
        {
            var userEmail = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail)) return null;

            var user = await _context.Usuarios.AsNoTracking()
                                       .FirstOrDefaultAsync(u => u.Email == userEmail);
            return user?.Id;
        }

        // ============================================================
        // [NOVO] API PARA GRÁFICO MENSAL (Chamado pelo site.js)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ObterConsumoMensal()
        {
            try
            {
                var anoAtual = DateTime.Now.Year;
                var dados = await _context.ConsumosEnergia
                    .Where(c => c.DataReferencia.Year == anoAtual)
                    .GroupBy(c => c.DataReferencia.Month)
                    .Select(g => new
                    {
                        Mes = g.Key,
                        TotalKwh = g.Sum(c => c.ValorKwh)
                    })
                    .OrderBy(g => g.Mes)
                    .ToListAsync();

                // Retorna os dados brutos que o site.js espera (mes, totalKwh)
                return Json(dados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao buscar dados mensais: {ex.Message}");
            }
        }

        // ============================================================
        // [NOVO] API PARA GRÁFICO ANUAL (Chamado pelo site.js)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ObterConsumoAnual()
        {
            try
            {
                var dados = await _context.ConsumosEnergia
                    .GroupBy(c => c.DataReferencia.Year)
                    .Select(g => new
                    {
                        Ano = g.Key,
                        TotalKwh = g.Sum(c => c.ValorKwh)
                    })
                    .OrderBy(g => g.Ano)
                    .ToListAsync();

                // Retorna os dados brutos que o site.js espera (ano, totalKwh)
                return Json(dados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao buscar dados anuais: {ex.Message}");
            }
        }
    }
}

