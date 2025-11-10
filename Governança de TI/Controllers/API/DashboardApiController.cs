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
    /// <summary>
    /// Controlador de API responsável por fornecer dados e metadados
    /// para os dashboards dinâmicos do sistema.
    /// 
    /// Esta API é consumida pelo front-end (JavaScript) e serve como camada
    /// única de comunicação para todos os widgets e monitoramentos.
    /// 
    /// ➕ Ao criar novas tabelas:
    /// Registre-as apenas em DashboardMetaService.cs — esta API já reconhece automaticamente.
    /// </summary>
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
        // 🔹 SEÇÃO 1: METADADOS DE DASHBOARD
        // ============================================================
        /// <summary>
        /// Retorna o mapeamento completo das tabelas disponíveis
        /// e seus campos (dimensão, métricas, datas, operações, etc.).
        /// 
        /// Endpoint: GET /api/dashboard/schema
        /// </summary>
        [HttpGet("schema")]
        public IActionResult GetSchema()
        {
            try
            {
                var schema = DashboardMetaService.Map;
                return Ok(schema);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][DashboardApiController/GetSchema]: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao carregar metadados do dashboard.", detalhe = ex.Message });
            }
        }

        // ============================================================
        // 🔹 SEÇÃO 2: CONSULTA DE DADOS PARA WIDGETS
        // ============================================================
        /// <summary>
        /// Retorna dados processados para renderizar um gráfico ou KPI,
        /// conforme parâmetros enviados pelo front-end.
        /// 
        /// Endpoint: POST /api/dashboard/query
        /// </summary>
        [HttpPost("query")]
        public async Task<IActionResult> QueryDashboard([FromBody] DashboardQueryRequest req)
        {
            try
            {
                // ✅ Validação do payload
                if (string.IsNullOrEmpty(req.Tabela))
                    return BadRequest(new { message = "O campo 'tabela' é obrigatório." });

                // ✅ Recupera metadados da tabela
                var meta = DashboardMetaService.ObterTabela(req.Tabela);
                if (meta == null)
                    return BadRequest(new { message = $"Tabela '{req.Tabela}' não é suportada pelo dashboard." });

                // ✅ Processa a consulta genérica via serviço
                var resultado = await DashboardQueryService.ExecutarConsultaAsync(_context, req);

                // 🧾 Registra log informativo
                await LogService.Gravar(_context, "DashboardApi", "Info", $"Consulta executada: {req.Tabela}/{req.TipoVisualizacao}");

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][DashboardApiController/QueryDashboard]: {ex.Message}");
                await LogService.Gravar(_context, "DashboardApi", "Erro", "Falha ao consultar dados do dashboard.", ex.ToString());
                return StatusCode(500, new { message = "Erro ao consultar dados do dashboard.", detalhe = ex.Message });
            }
        }

        // ============================================================
        // 🔹 SEÇÃO 3: STATUS DO SISTEMA / LOGS RECENTES
        // ============================================================
        /// <summary>
        /// Retorna um resumo do status atual do sistema:
        /// últimos logs, falhas recentes e hora da última atualização.
        /// 
        /// Endpoint: GET /api/dashboard/status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetSystemStatus()
        {
            try
            {
                // 🔍 Busca os últimos 5 logs
                var logsRecentes = await _context.Logs
                    .OrderByDescending(l => l.DataRegistro)
                    .Take(5)
                    .Select(l => new
                    {
                        l.Id,
                        l.DataRegistro,
                        l.Origem,
                        l.Tipo,
                        l.Mensagem
                    })
                    .ToListAsync();

                var falhasRecentes = logsRecentes.Any(l => l.Tipo == "Erro");

                var status = new
                {
                    Status = falhasRecentes ? "⚠️ Falhas recentes detectadas" : "✅ Sistema estável",
                    Critico = falhasRecentes,
                    Logs = logsRecentes,
                    UltimaAtualizacao = DateTime.Now
                };

                await LogService.Gravar(_context, "DashboardApi", "Info", "Status do sistema consultado com sucesso.");

                return Ok(status);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][DashboardApiController/GetSystemStatus]: {ex.Message}");
                await LogService.Gravar(_context, "DashboardApi", "Erro", "Falha ao consultar status do sistema.", ex.ToString());
                return StatusCode(500, new { message = "Erro ao consultar status do sistema.", detalhe = ex.Message });
            }
        }

        // ============================================================
        // 🔹 SEÇÃO 4: GRÁFICO DE LOGS – ERROS × TEMPO
        // ============================================================
        /// <summary>
        /// Retorna dados agregados dos logs (Erros × Avisos × Infos)
        /// agrupados por dia, para exibição gráfica.
        /// 
        /// Endpoint: GET /api/dashboard/logs-grafico
        /// </summary>
        [HttpGet("logs-grafico")]
        public async Task<IActionResult> GetLogsGrafico()
        {
            try
            {
                var hoje = DateTime.Now.Date;
                var seteDiasAtras = hoje.AddDays(-6);

                var dados = await _context.Logs
     .Where(l => l.DataRegistro != null && l.DataRegistro >= seteDiasAtras)
     .GroupBy(l => new { Dia = l.DataRegistro.Value.Date, l.Tipo }) // ✅ Aqui o .Value garante acesso ao Date
     .Select(g => new
     {
         Dia = g.Key.Dia.ToString("dd/MM"),
         Tipo = g.Key.Tipo,
         Quantidade = g.Count()
     })
     .ToListAsync();

                // Garante que cada dia tenha todas as categorias
                var dias = Enumerable.Range(0, 7)
                    .Select(i => hoje.AddDays(-i).ToString("dd/MM"))
                    .Reverse()
                    .ToList();

                var resultado = dias.Select(dia => new
                {
                    Dia = dia,
                    Erro = dados.FirstOrDefault(x => x.Dia == dia && x.Tipo == "Erro")?.Quantidade ?? 0,
                    Aviso = dados.FirstOrDefault(x => x.Dia == dia && x.Tipo == "Aviso")?.Quantidade ?? 0,
                    Info = dados.FirstOrDefault(x => x.Dia == dia && x.Tipo == "Info")?.Quantidade ?? 0
                });

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][DashboardApiController/GetLogsGrafico]: {ex.Message}");
                await LogService.Gravar(_context, "DashboardApi", "Erro", "Falha ao gerar gráfico de logs.", ex.ToString());
                return StatusCode(500, new { message = "Erro ao gerar gráfico de logs.", detalhe = ex.Message });
            }
        }
    }

    // ============================================================
    // 🔹 SEÇÃO 5: MODELO DE REQUISIÇÃO DE DASHBOARD
    // ============================================================
    /// <summary>
    /// Modelo enviado pelo front-end para solicitar um conjunto de dados.
    /// </summary>
    public class DashboardQueryRequest
    {
        // Nome da tabela (ex: "Equipamentos", "Descartes", etc.)
        public string? Tabela { get; set; }

        // Campo para agrupamento (ex: "Status", "EmpresaColetora", etc.)
        public string? Dimensao { get; set; }

        // Campo a ser somado/médiado, se aplicável
        public string? Metrica { get; set; }

        // Operação a ser realizada: Soma, Média, Contagem
        public string? Operacao { get; set; }

        // Tipo de visualização: Pizza, Barra, Rolo, Total
        public string? TipoVisualizacao { get; set; }

        // Filtros opcionais por data
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }
}
