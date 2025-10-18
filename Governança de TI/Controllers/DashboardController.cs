using Governança_de_TI.Data;
using Governança_de_TI.Models;
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
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public DashboardController(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetDadosDashboard()
        {
            var cardDataTask = GetCardDataAsync();
            var fimVidaUtilTask = GetFimVidaUtilDataAsync();
            var proximaManutencaoTask = GetProximaManutencaoDataAsync();
            var consumoMesTask = GetConsumoMesDataAsync();
            var consumoAnoTask = GetConsumoAnoDataAsync();
            //var tiposDescarteTask = GetTiposDescarteDataAsync(); // Adicionado para completar

            await Task.WhenAll(cardDataTask, fimVidaUtilTask, proximaManutencaoTask, consumoMesTask, consumoAnoTask);

            var viewModel = new DashboardViewModel
            {
                EmissoesCo2Evitadas = "132 kg CO₂",
                EquipamentosRecicladosPercentual = cardDataTask.Result.EquipamentosRecicladosPercentual,
                ItensPendentesDescarte = cardDataTask.Result.ItensPendentesDescarte,
                EquipamentosDescartadosCorretamente = cardDataTask.Result.EquipamentosDescartadosCorretamente,
                EquipamentosProximosFimVida = fimVidaUtilTask.Result,
                EquipamentosProximaManutencao = proximaManutencaoTask.Result,
                ConsumoKwhMes = consumoMesTask.Result,
                ConsumoKwhAno = consumoAnoTask.Result,
                //TiposDeDescarte = tiposDescarteTask.Result // Adicionado para completar
            };

            return Ok(viewModel);
        }

        // --- MÉTODOS PRIVADOS PARA CADA MÉTRICA ---

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
                    DateTime dataBase;

                    if (eq.DataUltimaManutencao.HasValue && eq.DataUltimaManutencao.Value.Year > 1)
                    {
                        dataBase = eq.DataUltimaManutencao.Value;
                    }
                    else
                    {
                        dataBase = (DateTime)eq.DataCompra;
                    }

                    if (dataBase.Year <= 1) continue;

                    DateTime proximaManutencao;

                    switch (eq.FrequenciaManutencao)
                    {
                        case "Mensal":
                            proximaManutencao = dataBase.AddMonths(1);
                            break;
                        case "Trimestral":
                            proximaManutencao = dataBase.AddMonths(3);
                            break;
                        case "Anual":
                            proximaManutencao = dataBase.AddMonths(12);
                            break;
                        default:
                            continue;
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

                return listaManutencao.OrderBy(e => DateTime.Parse(e.ProximaManutencao)).ToList();
            }
        }

        private async Task<List<EquipamentoVencendoViewModel>> GetFimVidaUtilDataAsync()
        {
            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                var hoje = DateTime.Now.Date;
                var dataLimite = hoje.AddMonths(5);

                var equipamentos = await context.Equipamentos
                    .Where(e => e.VidaUtil.HasValue && e.VidaUtil.Value >= hoje && e.VidaUtil.Value <= dataLimite)
                    .OrderBy(e => e.VidaUtil)
                    .Select(e => new EquipamentoVencendoViewModel
                    {
                        CodigoItem = e.CodigoItem,
                        Descricao = e.Descricao,
                        DataVencimento = e.VidaUtil.Value.ToShortDateString(),
                        DiasRestantes = (e.VidaUtil.Value - hoje).Days
                    })
                    .ToListAsync();
                return equipamentos;
            }
        }

        // OBSERVAÇÃO: Definição do método GetCardDataAsync que estava em falta.
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

        // OBSERVAÇÃO: Definição do método GetConsumoMesDataAsync que estava em falta.
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
                foreach (var item in consumoMesQuery)
                {
                    dataMes[item.Mes - 1] = item.TotalKwh;
                }

                return new ChartData { Labels = labelsMes, Data = dataMes };
            }
        }

        // OBSERVAÇÃO: Definição do método GetConsumoAnoDataAsync que estava em falta.
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

        // OBSERVAÇÃO: Adicionado o método GetTiposDescarteDataAsync para completar o ViewModel.
        //private async Task<ChartData> GetTiposDescarteDataAsync()
        //{
        //    using (var context = await _contextFactory.CreateDbContextAsync())
        //    {
        //        var tiposDescarteQuery = await context.Descartes
        //            .Where(d => d.Status == "Doado" || d.Status == "Reciclado")
        //            .GroupBy(d => d.Status)
        //            .Select(g => new { Tipo = g.Key, Contagem = g.Count() })
        //            .ToListAsync();

        //        var contagemDoado = tiposDescarteQuery.FirstOrDefault(t => t.Tipo == "Doado")?.Contagem ?? 0;
        //        var contagemReciclado = tiposDescarteQuery.FirstOrDefault(t => t.Tipo == "Reciclado")?.Contagem ?? 0;

        //        return new ChartData
        //        {
        //            Labels = new[] { "Doado", "Reciclado" },
        //            Data = new[] { (decimal)contagemDoado, (decimal)contagemReciclado }
        //        };
        //    }
        //}
    }
}

