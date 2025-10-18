using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    public class EquipamentosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EquipamentosController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Equipamentos/Consulta
        public async Task<IActionResult> Consulta(int? codigoItem, string descricao, string status, DateTime? dataCompra, DateTime? dataUltimaManutencao, DateTime? dataDeCriacao)
        {
            // OBSERVAÇÃO: A chamada '.Include(e => e.TipoEquipamento)' foi removida
            // porque 'TipoEquipamento' agora é um campo de texto e não uma tabela relacionada.
            var query = _context.Equipamentos.AsQueryable();

            if (codigoItem.HasValue)
            {
                query = query.Where(e => e.CodigoItem == codigoItem.Value);
            }
            if (!string.IsNullOrEmpty(descricao))
            {
                query = query.Where(e => e.Descricao.Contains(descricao));
            }
            if (!string.IsNullOrEmpty(status) && status != "Todos...")
            {
                query = query.Where(e => e.Status == status);
            }
            if (dataCompra.HasValue)
            {
                var dataFim = dataCompra.Value.AddDays(1);
                query = query.Where(e => e.DataCompra >= dataCompra.Value && e.DataCompra < dataFim);
            }
            if (dataUltimaManutencao.HasValue)
            {
                var dataFim = dataUltimaManutencao.Value.AddDays(1);
                query = query.Where(e => e.DataUltimaManutencao.HasValue && e.DataUltimaManutencao >= dataFim.AddDays(-1) && e.DataUltimaManutencao < dataFim);
            }
            if (dataDeCriacao.HasValue)
            {
                var dataFim = dataDeCriacao.Value.AddDays(1);
                query = query.Where(e => e.DataDeCadastro >= dataFim.AddDays(-1) && e.DataDeCadastro < dataFim);
            }

            return View(await query.ToListAsync());
        }

        // GET: Equipamentos/Detalhes/5
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();

            // OBSERVAÇÃO: A chamada '.Include(e => e.TipoEquipamento)' também foi removida daqui.
            var equipamento = await _context.Equipamentos
                .FirstOrDefaultAsync(e => e.CodigoItem == id);

            if (equipamento == null) return NotFound();
            return View(equipamento);
        }

        // GET: Equipamentos/Criar
        public async Task<IActionResult> Criar()
        {
            await PopulaTiposEquipamentoViewData();
            return View();
        }

        // POST: Equipamentos/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([Bind("Descricao,TipoEquipamento,Serie,Modelo,DataCompra,DataFimGarantia,VidaUtil,Status,FrequenciaManutencao,DataUltimaManutencao,ImagemUpload,AnexoUpload")] EquipamentoModel equipamento)
        {
          
                // Lógica de uploads...
                equipamento.DataDeCadastro = DateTime.Now;
                _context.Add(equipamento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Item ({equipamento.CodigoItem}) criado com sucesso!";
                return RedirectToAction(nameof(Consulta));
           
        }

        // GET: Equipamentos/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var equipamento = await _context.Equipamentos.FindAsync(id);
            if (equipamento == null) return NotFound();
            await PopulaTiposEquipamentoViewData(equipamento.TipoEquipamento);
            return View(equipamento);
        }

        // POST: Equipamentos/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("CodigoItem,Descricao,TipoEquipamento,Serie,Modelo,DataCompra,DataFimGarantia,VidaUtil,Status,FrequenciaManutencao,DataUltimaManutencao,ImagemUpload,AnexoUpload,DataDeCadastro")] EquipamentoModel equipamento)
        {
            if (id != equipamento.CodigoItem) return NotFound();


                try
                {
                    var equipamentoOriginal = await _context.Equipamentos.AsNoTracking().FirstOrDefaultAsync(e => e.CodigoItem == id);
                    // (Lógica de upload aqui...)

                    _context.Update(equipamento);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EquipamentoExists(equipamento.CodigoItem)) return NotFound();
                    else throw;
                }
                TempData["SuccessMessage"] = "Equipamento atualizado com sucesso!";
                return RedirectToAction(nameof(Consulta));
        }

        // ... (Actions Excluir e GetEquipamentoDados) ...

        // --- MÉTODOS AUXILIARES ---

        private bool EquipamentoExists(int id)
        {
            return _context.Equipamentos.Any(e => e.CodigoItem == id);
        }

        private async Task PopulaTiposEquipamentoViewData(object selectedType = null)
        {
            var tiposQuery = await _context.TiposEquipamento.OrderBy(t => t.Nome).ToListAsync();
            // OBSERVAÇÃO: O ViewData foi atualizado para o novo nome da propriedade.
            ViewData["TipoEquipamento"] = new SelectList(tiposQuery, "Nome", "Nome", selectedType);
        }
    }
}

