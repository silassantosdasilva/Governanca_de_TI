using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipoEquipamentoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TipoEquipamentoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TipoEquipamento
        // OBSERVAÇÃO: Novo método para buscar a lista de todos os tipos existentes.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tipos = await _context.TiposEquipamento.OrderBy(t => t.Nome).ToListAsync();
            return Ok(tipos);
        }

        // POST: api/TipoEquipamento
        // OBSERVAÇÃO: Este método, que já existia, salva um novo tipo de equipamento.
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] TipoEquipamentoModel tipoEquipamento)
        {
            if (tipoEquipamento == null || string.IsNullOrWhiteSpace(tipoEquipamento.Nome))
            {
                return BadRequest("O nome do tipo de equipamento é obrigatório.");
            }

            _context.TiposEquipamento.Add(tipoEquipamento);
            await _context.SaveChangesAsync();

            return Ok(tipoEquipamento);
        }

        // DELETE: api/TipoEquipamento/5
        // OBSERVAÇÃO: Novo método para excluir um tipo de equipamento.
        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var tipoEquipamento = await _context.TiposEquipamento.FindAsync(id);
            if (tipoEquipamento == null)
            {
                return NotFound();
            }

            // Regra de negócio: Verifica se o tipo está em uso antes de excluir.
            var isUsed = await _context.Equipamentos.FirstOrDefaultAsync(m => m.CodigoItem == id);

            if (isUsed != null )
            {
                return BadRequest("Este tipo de equipamento está em uso e não pode ser excluído.");
            }

            _context.TiposEquipamento.Remove(tipoEquipamento);
            await _context.SaveChangesAsync();

            return NoContent(); // Retorna sucesso sem conteúdo
        }
    }
}

