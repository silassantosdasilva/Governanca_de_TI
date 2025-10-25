using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    /// <summary>
    /// API para gerenciar Departamentos via modal (AJAX).
    /// </summary>
    [Route("api/departamento")]
    [ApiController]
    public class DepartamentoApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartamentoApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // DTO simples para a criação
        public class CriarDepartamentoDto
        {
            public string Nome { get; set; }
        }

        // GET: api/departamento
        [HttpGet]
        public async Task<IActionResult> GetDepartamentos()
        {
            var departamentos = await _context.Departamentos
                                              .OrderBy(d => d.Nome)
                                              .Select(d => new { d.Id, d.Nome })
                                              .ToListAsync();
            return Ok(departamentos);
        }

        // POST: api/departamento
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarDepartamentoDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Nome))
            {
                return BadRequest("O nome é obrigatório.");
            }

            var nomeNormalizado = dto.Nome.Trim();
            var exists = await _context.Departamentos.AnyAsync(d => d.Nome.ToUpper() == nomeNormalizado.ToUpper());
            if (exists)
            {
                return BadRequest("Este departamento já existe.");
            }

            var departamento = new DepartamentoModel
            {
                Nome = nomeNormalizado
            };

            _context.Departamentos.Add(departamento);
            await _context.SaveChangesAsync();

            // Retorna o objeto criado (incluindo o novo ID)
            return Ok(new { departamento.Id, departamento.Nome });
        }

        // DELETE: api/departamento/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var departamento = await _context.Departamentos.FindAsync(id);
            if (departamento == null)
            {
                return NotFound();
            }

            // Verifica se o departamento está em uso antes de excluir
            var emUso = await _context.Usuarios.AnyAsync(u => u.DepartamentoId == id);
            if (emUso)
            {
                return BadRequest("Este departamento está em uso e não pode ser excluído.");
            }

            _context.Departamentos.Remove(departamento);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
