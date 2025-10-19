using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    /// <summary>
    /// Controller responsável pela gestão dos registos de consumo de energia.
    /// </summary>
    public class ConsumoEnergiaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConsumoEnergiaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ConsumoEnergia
        // OBSERVAÇÃO: Esta é a página principal que exibe a lista de todos os consumos registados.
        public async Task<IActionResult> Index()
        {
            var consumos = await _context.ConsumosEnergia
                                         .OrderByDescending(c => c.DataReferencia)
                                         .ToListAsync();
            return View(consumos);
        }

        // GET: ConsumoEnergia/Criar
        // OBSERVAÇÃO: Esta Action simplesmente exibe o formulário de criação em branco.
        public IActionResult Criar()
        {
            return View();
        }

        // POST: ConsumoEnergia/Criar
        // OBSERVAÇÃO: Esta Action recebe os dados do formulário e salva um novo registo na base de dados.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([Bind("DataReferencia,ValorKwh")] ConsumoEnergiaModel consumo)
        {
            if (ModelState.IsValid)
            {
                // Garante que a data de referência seja sempre o primeiro dia do mês para padronização.
                consumo.DataReferencia = new System.DateTime(consumo.DataReferencia.Year, consumo.DataReferencia.Month, 1);

                _context.Add(consumo);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registo de consumo criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            // Se o modelo for inválido, retorna para a mesma view para exibir os erros de validação.
            return View(consumo);
        }

        // GET: ConsumoEnergia/Editar/5
        // OBSERVAÇÃO: Busca um registo pelo ID e exibe o formulário de edição com os dados preenchidos.
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consumo = await _context.ConsumosEnergia.FindAsync(id);
            if (consumo == null)
            {
                return NotFound();
            }
            return View(consumo);
        }

        // POST: ConsumoEnergia/Editar/5
        // OBSERVAÇÃO: Recebe os dados do formulário de edição e atualiza o registo na base de dados.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("Id,DataReferencia,ValorKwh")] ConsumoEnergiaModel consumo)
        {
            if (id != consumo.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    consumo.DataReferencia = new System.DateTime(consumo.DataReferencia.Year, consumo.DataReferencia.Month, 1);
                    _context.Update(consumo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConsumoEnergiaExists(consumo.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["SuccessMessage"] = "Registo de consumo atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(consumo);
        }

        // GET: ConsumoEnergia/Excluir/5
        // OBSERVAÇÃO: Mostra uma página de confirmação antes de apagar um registo.
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consumo = await _context.ConsumosEnergia
                .FirstOrDefaultAsync(m => m.Id == id);
            if (consumo == null)
            {
                return NotFound();
            }

            return View(consumo);
        }

        // POST: ConsumoEnergia/Excluir/5
        // OBSERVAÇÃO: Efetivamente apaga o registo da base de dados após a confirmação.
        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var consumo = await _context.ConsumosEnergia.FindAsync(id);
            _context.ConsumosEnergia.Remove(consumo);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Registo de consumo excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para verificar se um registo existe.
        private bool ConsumoEnergiaExists(int id)
        {
            return _context.ConsumosEnergia.Any(e => e.Id == id);
        }
    }
}

