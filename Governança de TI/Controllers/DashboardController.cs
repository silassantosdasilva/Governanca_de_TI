using Governança_de_TI.Data;
using Governança_de_TI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        // OBSERVAÇÃO: Injetamos a "Fábrica" de DbContext para permitir a criação de múltiplas
        // instâncias do DbContext, o que é essencial para executar consultas em paralelo.
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public DashboardController(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetDadosDashboard()
        {
            // OBSERVAÇÃO: Todas as tarefas de busca de dados são iniciadas em paralelo.
            var cardDataTask = GetCardDataAsync();
            var fimVidaUtilTask = GetFimVidaUtilDataAsync();
            var proximaManutencaoTask = GetProximaManutencaoDataAsync();
            var consumoMesTask = GetConsumoMesDataAsync();
            var consumoAnoTask = GetConsumoAnoDataAsync();

            // Aguarda que todas as tarefas terminem a sua execução.
            await Task.WhenAll(cardDataTask, fimVidaUtilTask, proximaManutencaoTask, consumoMesTask, consumoAnoTask);

            // Monta o objeto de resposta com os resultados de todas as tarefas.
            var viewModel = new DashboardViewModel
            {
                EmissoesCo2Evitadas = "132 kg CO₂", // Valor de exemplo
                EquipamentosRecicladosPercentual = cardDataTask.Result.EquipamentosRecicladosPercentual,
                ItensPendentesDescarte = cardDataTask.Result.ItensPendentesDescarte,
                EquipamentosDescartadosCorretamente = cardDataTask.Result.EquipamentosDescartadosCorretamente,
                EquipamentosProximosFimVida = fimVidaUtilTask.Result,
                EquipamentosProximaManutencao = proximaManutencaoTask.Result,
                ConsumoKwhMes = consumoMesTask.Result,
                ConsumoKwhAno = consumoAnoTask.Result,
            };

            return Ok(viewModel);
        }

        // --- MÉTODOS PRIVADOS PARA CADA MÉTRICA ---

        // Busca os dados para os cards superiores.
        private async Task<(string EquipamentosRecicladosPercentual, int ItensPendentesDescarte, int EquipamentosDescartadosCorretamente)> GetCardDataAsync()
        {
            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                var totalDescartes = await context.Descartes.CountAsync();
                var descartesCorretos = await context.Descartes.CountAsync(d => d.Status == "Reciclado" || d.Status == "Doado");
                var itensPendentes = await context.Descartes.CountAsync(d => d.Status == "Pendente");
                var percentual = totalDescartes > 0 ? ((decimal)descartesCorretos / totalDescartes).ToString("P1") : "0,0%";
                return (percentual, itensPendentes, descartesCorretos);
            }
        }

        // Busca os dados para a lista de equipamentos próximos do fim da vida útil.
        private async Task<List<EquipamentoVencendoViewModel>> GetFimVidaUtilDataAsync()
        {
            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                var hoje = DateTime.Now.Date;
                var dataLimite = hoje.AddMonths(5);

                // OBSERVAÇÃO: A consulta foi corrigida para usar o campo 'VidaUtilFim' (que é uma data)
                // em vez de 'VidaUtilAnos' (que é um número).
                var equipamentos = await context.Equipamentos
                    .Where(e => e.VidaUtilFim.HasValue && e.VidaUtilFim.Value >= hoje && e.VidaUtilFim.Value <= dataLimite)
                    .OrderBy(e => e.VidaUtilFim)
                    .Select(e => new EquipamentoVencendoViewModel
                    {
                        CodigoItem = e.CodigoItem,
                        Descricao = e.Descricao,
                        DataVencimento = e.VidaUtilFim.Value.ToShortDateString(),
                        DiasRestantes = (e.VidaUtilFim.Value - hoje).Days
                    })
                    .ToListAsync();
                return equipamentos;
            }
        }

        // Busca os dados para a lista de equipamentos próximos da manutenção.
        private async Task<List<EquipamentoManutencaoViewModel>> GetProximaManutencaoDataAsync()
        {
            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                var hoje = DateTime.Now.Date;
                var dataLimite = hoje.AddDays(30);
                var listaManutencao = new List<EquipamentoManutencaoViewModel>();

                var equipamentosComManutencao = await context.Equipamentos
                    .Where(e => e.FrequenciaManutencao != null && e.FrequenciaManutencao != "Nenhuma")
                    .ToListAsync();

                foreach (var eq in equipamentosComManutencao)
                {
                    // Mudança feita pelos silas 18/10/2025 anterior DateTime dataBase = eq.DataUltimaManutencao ?? eq.DataCompra;

                    DateTime dataBase = (eq.DataUltimaManutencao ?? eq.DataCompra).GetValueOrDefault();
                    if (dataBase.Year <= 1) continue;

                    DateTime proximaManutencao;
                    switch (eq.FrequenciaManutencao)
                    {
                        case "Mensal": proximaManutencao = dataBase.AddMonths(1); break;
                        case "Trimestral": proximaManutencao = dataBase.AddMonths(3); break;
                        case "Anual": proximaManutencao = dataBase.AddYears(1); break;
                        default: continue;
                    }

                    if (proximaManutencao >= hoje && proximaManutencao <= dataLimite)
                    {
                        listaManutencao.Add(new EquipamentoManutencaoViewModel
                        {
                            CodigoItem = eq.CodigoItem,
                            Descricao = eq.Descricao,
                            ProximaManutencao = proximaManutencao.ToShortDateString(),
                            Frequencia = eq.FrequenciaManutencao
                        });
                    }
                }
                return listaManutencao.OrderBy(e => e.ProximaManutencao).ToList();
            }
        }

        // Busca os dados para o gráfico de consumo mensal.
        private async Task<ChartData> GetConsumoMesDataAsync()
        {
            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                var anoAtual = DateTime.Now.Year;
                var consumoMesQuery = await context.ConsumosEnergia
                    .Where(c => c.DataReferencia.Year == anoAtual)
                    .GroupBy(c => c.DataReferencia.Month)
                    .Select(g => new { Mes = g.Key, TotalKwh = g.Sum(c => c.ValorKwh) })
                    .ToListAsync();

                var labelsMes = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames.Take(12).ToArray();
                var dataMes = new decimal[12];
                foreach (var item in consumoMesQuery) { dataMes[item.Mes - 1] = item.TotalKwh; }

                return new ChartData { Labels = labelsMes, Data = dataMes };
            }
        }

        // Busca os dados para o gráfico de consumo anual.
        private async Task<ChartData> GetConsumoAnoDataAsync()
        {
            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                var consumoAno = await context.ConsumosEnergia
                   .GroupBy(c => c.DataReferencia.Year)
                   .Select(g => new { Ano = g.Key, TotalKwh = g.Sum(c => c.ValorKwh) })
                   .OrderBy(r => r.Ano)
                   .ToListAsync();

                return new ChartData
                {
                    Labels = consumoAno.Select(c => c.Ano.ToString()).ToArray(),
                    Data = consumoAno.Select(c => c.TotalKwh).ToArray()
                };
            }
        }
    }
}

