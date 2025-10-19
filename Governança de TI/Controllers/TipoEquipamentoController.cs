using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    /// <summary>
    /// Controller de API para gerir os Tipos de Equipamento.
    /// </summary>
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
        // OBSERVAÇÃO: Busca e retorna a lista de todos os tipos de equipamento existentes,
        // ordenados por nome. É usado para popular a lista no modal.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tipos = await _context.TiposEquipamento.OrderBy(t => t.Nome).ToListAsync();
            return Ok(tipos);
        }

        // POST: api/TipoEquipamento
        // OBSERVAÇÃO: Cria um novo tipo de equipamento a partir do nome enviado pelo modal.
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] TipoEquipamentoModel tipoEquipamento)
        {
            if (tipoEquipamento == null || string.IsNullOrWhiteSpace(tipoEquipamento.Nome))
            {
                return BadRequest("O nome do tipo de equipamento é obrigatório.");
            }

            // Verifica se já existe um tipo com o mesmo nome para evitar duplicados
            var tipoExistente = await _context.TiposEquipamento.FirstOrDefaultAsync(t => t.Nome.ToUpper() == tipoEquipamento.Nome.ToUpper());
            if (tipoExistente != null)
            {
                return BadRequest("Já existe um tipo de equipamento com este nome.");
            }

            _context.TiposEquipamento.Add(tipoEquipamento);
            await _context.SaveChangesAsync();

            // Retorna o objeto completo com o novo ID gerado pelo banco de dados
            return Ok(tipoEquipamento);
        }

        // DELETE: api/TipoEquipamento/5
        // OBSERVAÇÃO: Exclui um tipo de equipamento, mas apenas se ele não estiver em uso.
        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var tipoEquipamento = await _context.TiposEquipamento.FindAsync(id);
            if (tipoEquipamento == null)
            {
                return NotFound();
            }

            // Regra de negócio: Verifica se o tipo está a ser utilizado por algum equipamento.
            // Se estiver, a exclusão é bloqueada e uma mensagem de erro é retornada.
            var isUsed = await _context.Equipamentos.AnyAsync(e => e.TipoEquipamentoId == id);
            if (isUsed)
            {
                return BadRequest("Este tipo de equipamento está em uso e não pode ser excluído.");
            }

            _context.TiposEquipamento.Remove(tipoEquipamento);
            await _context.SaveChangesAsync();

            return NoContent(); // Retorna 204 No Content, indicando sucesso na exclusão.
        }
    }
}

