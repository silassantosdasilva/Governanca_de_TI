using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers // Certifique-se de que o namespace está correto
{
    public class ConsumoEnergiaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConsumoEnergiaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ConsumoEnergia
        public async Task<IActionResult> Index()
        {
            var consumos = await _context.ConsumosEnergia.OrderByDescending(c => c.DataReferencia).ToListAsync();
            return View(consumos);
        }

        // GET: ConsumoEnergia/Criar
        public IActionResult Criar()
        {
            return View();
        }

        // POST: ConsumoEnergia/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([Bind("DataReferencia,ValorKwh")] ConsumoEnergiaModel consumo)
        {
            if (ModelState.IsValid)
            {
                // Garante que a data seja sempre o primeiro dia do mês
                consumo.DataReferencia = new System.DateTime(consumo.DataReferencia.Year, consumo.DataReferencia.Month, 1);
                _context.Add(consumo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registo de consumo criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(consumo);
        }

        // ... (Futuramente, pode adicionar as Actions Editar e Excluir aqui)
    }
}
