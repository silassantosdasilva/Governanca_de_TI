using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Governança_de_TI.Data;
using Governança_de_TI.Services;
using Governança_de_TI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers.Api
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public DashboardApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔹 SEÇÃO 1: METADADOS (Schema)
        // ============================================================
        [HttpGet("schema")]
        public IActionResult GetSchema()
        {
            // ... (Seu código existente)
            try
            {
                var schema = DashboardMetaService.Map;
                return Ok(schema);
            }
            catch (Exception ex)
            {
                // ...
                return StatusCode(500, new { message = "Erro ao carregar metadados.", detalhe = ex.Message });
            }
        }

        // ============================================================
        // 🔹 SEÇÃO 2: CONSULTA DE DADOS (Query)
        // ============================================================
        [HttpPost("query")]
        public async Task<IActionResult> QueryDashboard([FromBody] DashboardQueryRequest req)
        {
            // ... (Seu código existente)
            try
            {
                if (string.IsNullOrEmpty(req.Tabela))
                    return BadRequest(new { message = "O campo 'tabela' é obrigatório." });

                var meta = DashboardMetaService.ObterTabela(req.Tabela);
                if (meta == null)
                    return BadRequest(new { message = $"Tabela '{req.Tabela}' não é suportada." });

                var resultado = await DashboardQueryService.ExecutarConsultaAsync(_context, req);
                await LogService.Gravar(_context, "DashboardApi", "Info", $"Consulta executada: {req.Tabela}/{req.TipoVisualizacao}");

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                // ...
                return StatusCode(500, new { message = "Erro ao consultar dados.", detalhe = ex.Message });
            }
        }

        // ============================================================
        // 🔹 SEÇÃO 3: STATUS DO SISTEMA
        // ============================================================
        [HttpGet("status")]
        public async Task<IActionResult> GetSystemStatus()
        {
            // ... (Seu código existente)
            return Ok(); // Exemplo
        }

        // ============================================================
        // 🔹 SEÇÃO 4: GRÁFICO DE LOGS
        // ============================================================
        [HttpGet("logs-grafico")]
        public async Task<IActionResult> GetLogsGrafico()
        {
            // ... (Seu código existente)
            return Ok(); // Exemplo
        }

        // ============================================================
        // === 🔹 SEÇÃO 5: VALORES DISTINTOS (NOVO MÉTODO) ===
        // === (COLE DENTRO DA CLASSE DashboardApiController) ===
        // ============================================================
        [HttpPost("distinct-values")]
        public async Task<IActionResult> GetDistinctValues([FromBody] DistinctValueRequest req)
        {
            if (string.IsNullOrEmpty(req.Tabela) || string.IsNullOrEmpty(req.Campo))
                // Agora 'BadRequest' existe
                return BadRequest(new { message = "Tabela e Campo são obrigatórios." });

            try
            {
                // Agora '_context' existe
                var valores = await DashboardQueryService.GetDistinctValuesAsync(_context, req);
                // Agora 'Ok' existe
                return Ok(valores);
            }
            catch (Exception ex)
            {
                await LogService.Gravar(_context, "DashboardApi", "Erro", "Falha ao buscar valores distintos.", ex.ToString());
                // Agora 'StatusCode' existe
                return StatusCode(500, new { message = "Erro ao buscar valores distintos.", detalhe = ex.Message });
            }
        }

    } // <-- FIM DA CLASSE 'DashboardApiController'


    // ============================================================
    // 🔹 SEÇÃO DE MODELOS (DTOs)
    // ============================================================

    /// <summary>
    /// Modelo enviado pelo front-end para solicitar um conjunto de dados.
    /// </summary>
    public class DashboardQueryRequest
    {
        // ... (Suas propriedades existentes)
        public string? Tabela { get; set; }
        public string? Dimensao { get; set; }
        public string? Metrica { get; set; }
        public string? Operacao { get; set; }
        public string? TipoVisualizacao { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? FiltroCampo { get; set; }
        public string? FiltroValor { get; set; }
    }

    // ============================================================
    // === 🔹 NOVA CLASSE DTO (NOVA) ===
    // === (COLE FORA DA CLASSE, MAS DENTRO DO NAMESPACE) ===
    // ============================================================
    public class DistinctValueRequest
    {
        public string Tabela { get; set; }
        public string Campo { get; set; }
    }
}