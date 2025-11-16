using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    [Route("TipoEquipamento")]
    public class TipoEquipamentoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TipoEquipamentoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========== MODAL CRIAR ==========
        [HttpGet("CriarModal")]
        public IActionResult CriarModal()
        {
            return PartialView("~/Views/TipoEquipamento/_ModalCriarTipo.cshtml");
        }

        // ========== MODAL LISTA ==========
        [HttpGet("ListaModal")]
        public async Task<IActionResult> ListaModal()
        {
            var lista = await _context.TiposEquipamento
                                      .OrderBy(t => t.Nome)
                                      .ToListAsync();

            return PartialView("~/Views/TipoEquipamento/_ModalListarTipos.cshtml", lista);
        }

        // ========== CRIAR (POST) ==========
        [HttpPost("Criar")]
        public async Task<IActionResult> Criar([FromBody] TipoEquipamentoModel model)
        {
            if (model == null || String.IsNullOrWhiteSpace(model.Nome))
                return Json(new { success = false, message = "Nome inválido." });

            // Verifica duplicidade
            if (await _context.TiposEquipamento.AnyAsync(t => t.Nome == model.Nome))
                return Json(new { success = false, message = "Esse tipo já existe." });

            _context.Add(model);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                data = new { model.Id, model.Nome }
            });
        }

        // ========== EXCLUIR ==========
        [HttpDelete("Excluir/{id}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var tipo = await _context.TiposEquipamento.FindAsync(id);

            if (tipo == null)
                return Json(new { success = false, message = "Tipo não encontrado." });

            bool emUso = await _context.Equipamentos.AnyAsync(e => e.TipoEquipamentoId == id);

            if (emUso)
                return Json(new { success = false, message = "Não é possível excluir, existe equipamento usando este tipo." });

            _context.Remove(tipo);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }

}
