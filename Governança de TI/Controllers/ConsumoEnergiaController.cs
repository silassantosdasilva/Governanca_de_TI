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
        public IActionResult Criar()
        {
            // Sugere o mês atual
            return View(new ConsumoEnergiaModel { DataReferencia = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) });
        }

        // POST: ConsumoEnergia/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([Bind("DataReferencia,ValorKwh")] ConsumoEnergiaModel consumo)
        {
            // Padroniza a data para o dia 1 do mês
            consumo.DataReferencia = new DateTime(consumo.DataReferencia.Year, consumo.DataReferencia.Month, 1);

            // Validação de duplicidade
            bool jaExiste = await _context.ConsumosEnergia
                                        .AnyAsync(c => c.DataReferencia.Year == consumo.DataReferencia.Year &&
                                                       c.DataReferencia.Month == consumo.DataReferencia.Month);
            if (jaExiste)
            {
                ModelState.AddModelError("DataReferencia", $"Já existe um registo de consumo para {consumo.DataReferencia:MM/yyyy}.");
            }

            // --- [CORREÇÃO] Movido o ModelState.IsValid para o local correto ---
            if (ModelState.IsValid)
            {
                _context.Add(consumo);
                await _context.SaveChangesAsync(); // Salva

                var userId = await GetCurrentUserId(); // Pega o ID do usuário

                // --- [GAMIFICAÇÃO] ---
                if (userId.HasValue)
                {
                    // Adiciona 30 pontos por registrar consumo (conforme doc)
                    await _gamificacaoService.AdicionarPontosAsync(userId.Value, "RegistrouConsumoEnergia", 30);
                }
                // --- Fim Gamificação ---

                // --- [AUDITORIA] (Executa em background) ---
                if (userId.HasValue)
                {
                    _ = Task.Run(async () => {
                        await _auditService.RegistrarAcao(userId.Value, "Registrou Consumo", $"Mês/Ano: {consumo.DataReferencia:MM/yyyy}, Valor: {consumo.ValorKwh} kWh");
                    });
                }
                // --- Fim Auditoria ---

                TempData["SuccessMessage"] = "Registo de consumo criado com sucesso!";
                // --- [CORREÇÃO] Redireciona para o Index após sucesso ---
                return RedirectToAction(nameof(Index));
            }
            // --- Fim da Correção ---

            return View(consumo); // Retorna com erros
        }

        // GET: ConsumoEnergia/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var consumo = await _context.ConsumosEnergia.FindAsync(id);
            if (consumo == null) return NotFound();
            return View(consumo);
        }

        // POST: ConsumoEnergia/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("Id,DataReferencia,ValorKwh")] ConsumoEnergiaModel consumo)
        {
            if (id != consumo.Id) return NotFound();

            consumo.DataReferencia = new DateTime(consumo.DataReferencia.Year, consumo.DataReferencia.Month, 1);

            // Validação de duplicidade na edição
            bool conflitoMesAno = await _context.ConsumosEnergia
                                        .AnyAsync(c => c.DataReferencia.Year == consumo.DataReferencia.Year &&
                                                       c.DataReferencia.Month == consumo.DataReferencia.Month &&
                                                       c.Id != consumo.Id);
            if (conflitoMesAno)
            {
                ModelState.AddModelError("DataReferencia", $"Já existe outro registo para {consumo.DataReferencia:MM/yyyy}.");
            }

            // --- [CORREÇÃO] Movido o ModelState.IsValid para o local correto ---
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(consumo);
                    await _context.SaveChangesAsync();

                    // --- [AUDITORIA] (Executa em background) ---
                    var userId = await GetCurrentUserId();
                    if (userId.HasValue)
                    {
                        _ = Task.Run(async () => {
                            await _auditService.RegistrarAcao(userId.Value, "Editou Consumo", $"ID={consumo.Id}, Mês/Ano: {consumo.DataReferencia:MM/yyyy}");
                        });
                    }
                    // --- Fim Auditoria ---
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConsumoEnergiaExists(consumo.Id)) return NotFound(); else throw;
                }
                TempData["SuccessMessage"] = "Registo de consumo atualizado!";
                // --- [CORREÇÃO] Redireciona para o Index após sucesso ---
                return RedirectToAction(nameof(Index));
            }

            return View(consumo);
        }

        // GET: ConsumoEnergia/Excluir/5
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null) return NotFound();
            var consumo = await _context.ConsumosEnergia.FirstOrDefaultAsync(m => m.Id == id);
            if (consumo == null) return NotFound();
            return View(consumo);
        }

        // POST: ConsumoEnergia/Excluir/5
        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var consumo = await _context.ConsumosEnergia.FindAsync(id);
            if (consumo != null)
            {
                _context.ConsumosEnergia.Remove(consumo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registo de consumo excluído!";

                // --- [AUDITORIA] (Executa em background) ---
                var userId = await GetCurrentUserId();
                if (userId.HasValue)
                {
                    _ = Task.Run(async () => {
                        await _auditService.RegistrarAcao(userId.Value, "Excluiu Consumo", $"ID={consumo.Id}, Mês/Ano: {consumo.DataReferencia:MM/yyyy}");
                    });
                }
                // --- Fim Auditoria ---
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ConsumoEnergiaExists(int id)
        {
            return _context.ConsumosEnergia.Any(e => e.Id == id);
        }

        // Método auxiliar para obter ID do usuário logado
        private async Task<int?> GetCurrentUserId()
        {
            var userEmail = User?.Identity?.Name; // Obtém o email do usuário logado
            if (string.IsNullOrWhiteSpace(userEmail)) return null;

            // Busca o usuário no banco pelo email
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

