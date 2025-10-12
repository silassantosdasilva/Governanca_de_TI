using Governança_de_TI.Data;
using Governança_de_TI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    [Route("api/[controller]")] // Define a rota da API para 'api/dashboard'
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet] // Esta Action responde a pedidos GET para 'api/dashboard'
        public async Task<IActionResult> GetDadosDashboard()
        {
            // --- CÁLCULO DAS MÉTRICAS ---
            // OBSERVAÇÃO: Estes são cálculos de exemplo. Adapte-os à sua regra de negócio.

            var totalDescartes = await _context.Descartes.CountAsync();
            var descartesCorretos = await _context.Descartes.CountAsync(d => d.Status == "Reciclado" || d.Status == "Doado");

            var viewModel = new DashboardViewModel
            {
                // Cards Superiores
                EmissoesCo2Evitadas = "132 kg CO₂", // Valor de exemplo
                EquipamentosRecicladosPercentual = totalDescartes > 0 ? ((decimal)descartesCorretos / totalDescartes).ToString("P1") : "0.0%",
                ItensPendentesDescarte = await _context.Descartes.CountAsync(d => d.Status == "Pendente"),
                EquipamentosDescartadosCorretamente = descartesCorretos,

                // Gráficos Inferiores (com dados de exemplo)
                EconomiaGerada = new ChartData
                {
                    Labels = new[] { "Booked", "Flopped" },
                    Data = new[] { 36.2m, 63.8m }
                },
                ConsumoKwhMes = new ChartData
                {
                    Labels = new[] { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" },
                    Data = new[] { 7m, 12m, 3m, 6m, 10m, 11m, 7m, 13m, 19m, 26m, 8m, 14m }
                },
                ConsumoKwhAno = new ChartData
                {
                    Labels = new[] { "2012", "2013", "2014", "2015", "2016", "2017", "2018", "2019", "2020", "2021", "2022", "2023" },
                    Data = new[] { 5000m, 15000m, 9500m, 15500m, 12500m, 7500m, 19000m, 17500m, 24500m, 30500m, 14500m, 10000m }
                },
                ConsumoPorSetor = new ChartData
                {
                    Labels = new[] { "Q1", "Q2", "Q3", "Q4" },
                    Data = new[] { 13.1m, 28.6m, 28m, 30.3m }
                }
            };

            return Ok(viewModel); // Retorna os dados em formato JSON
        }
    }
}
