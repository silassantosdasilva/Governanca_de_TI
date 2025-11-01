using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.ViewModels;
using Governança_de_TI.Views.Services.Gamificacao;
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
        private readonly IGamificacaoService _gamificacaoService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardController(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IGamificacaoService gamificacaoService,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _gamificacaoService = gamificacaoService;
            _httpContextAccessor = httpContextAccessor;
        }

        // ============================================================
        // ENDPOINT PRINCIPAL DA DASHBOARD
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                int? userId = await GetCurrentUserId();

                // ⚠️ Permite continuar mesmo sem usuário logado (para teste local)
                if (!userId.HasValue)
                {
                    userId = 1; // força o ID 1 (ou um ID de usuário existente)
                }

                var cardDataTask = GetCardDataAsync();
                var fimVidaUtilTask = GetFimVidaUtilDataAsync();
                var proximaManutencaoTask = GetProximaManutencaoDataAsync();
                var consumoMesTask = GetConsumoMesDataAsync();
                var consumoAnoTask = GetConsumoAnoDataAsync();
                var gamificacaoTask = GetGamificacaoDataAsync(userId.Value);

                await Task.WhenAll(cardDataTask, fimVidaUtilTask, proximaManutencaoTask, consumoMesTask, consumoAnoTask, gamificacaoTask);

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
                    Gamificacao = gamificacaoTask.Result
                };

                return Ok(viewModel);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao carregar os dados da dashboard.", error = ex.Message });
            }
        }

        // ============================================================
        // [MÉTODOS PRIVADOS]
        // ============================================================

        // --- Obter dados de gamificação ---
        private async Task<ViewModels.GamificacaoViewModel> GetGamificacaoDataAsync(int usuarioId)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Busca o progresso ESG do usuário
                var gamificacao = await context.Gamificacoes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.UsuarioId == usuarioId);

                // Se não encontrar, cria um modelo padrão
                if (gamificacao == null)
                {
                    gamificacao = new GamificacaoModel
                    {
                        UsuarioId = usuarioId,
                        Pontos = 0,
                        Nivel = "Iniciante"
                    };

                    // (Opcional) Adiciona automaticamente para debug
                    context.Gamificacoes.Add(gamificacao);
                    await context.SaveChangesAsync();
                }

                // Define ícone e limites de nível
                string icone = "🌱";
                int pontosNivelAtual = 0;
                int pontosProximoNivel = 100;

                switch (gamificacao.Nivel)
                {
                    case "Ecopensante":
                        icone = "🌿";
                        pontosNivelAtual = 100;
                        pontosProximoNivel = 250;
                        break;
                    case "Guardião Verde":
                        icone = "🌳";
                        pontosNivelAtual = 250;
                        pontosProximoNivel = 500;
                        break;
                    case "Mestre ESG":
                        icone = "🌎";
                        pontosNivelAtual = 500;
                        pontosProximoNivel = 500;
                        break;
                }

                double percentual = 0;
                if (pontosProximoNivel > pontosNivelAtual)
                {
                    int progresso = gamificacao.Pontos - pontosNivelAtual;
                    int total = pontosProximoNivel - pontosNivelAtual;
                    percentual = Math.Round(((double)progresso / total) * 100, 1);
                }
                else if (gamificacao.Nivel == "Mestre ESG")
                {
                    percentual = 100;
                }

                return new ViewModels.GamificacaoViewModel
                {
                    PontosAtuais = gamificacao.Pontos,
                    NivelAtual = gamificacao.Nivel,
                    IconeNivel = icone,
                    PontosProximoNivel = pontosProximoNivel,
                    PercentualProgresso = (int)percentual
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter gamificação: {ex.Message}");
                // Retorna um modelo neutro para não quebrar a view
                return new ViewModels.GamificacaoViewModel
                {
                    PontosAtuais = 0,
                    NivelAtual = "Iniciante",
                    IconeNivel = "🌱",
                    PontosProximoNivel = 100,
                    PercentualProgresso = 0
                };
            }
        }

        // --- Cards superiores ---
        private async Task<(string EquipamentosRecicladosPercentual, int ItensPendentesDescarte, int EquipamentosDescartadosCorretamente)> GetCardDataAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var totalDescartes = await context.Descartes.CountAsync();
            var descartesCorretos = await context.Descartes.CountAsync(d => d.Status == "Reciclado" || d.Status == "Doado");
            var itensPendentes = await context.Descartes.CountAsync(d => d.Status == "Pendente");

            var percentual = totalDescartes > 0
                ? ((decimal)descartesCorretos / totalDescartes).ToString("P1")
                : "0,0%";

            return (percentual, itensPendentes, descartesCorretos);
        }

        // --- Equipamentos próximos do fim da vida útil ---
        private async Task<List<EquipamentoVencendoViewModel>> GetFimVidaUtilDataAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var hoje = DateTime.Now.Date;
            var dataLimite = hoje.AddMonths(5);

            return await context.Equipamentos
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
        }

        // --- Equipamentos próximos da manutenção ---
        private async Task<List<EquipamentoManutencaoViewModel>> GetProximaManutencaoDataAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var hoje = DateTime.Now.Date;
            var dataLimite = hoje.AddDays(30);
            var resultado = new List<EquipamentoManutencaoViewModel>();

            var equipamentos = await context.Equipamentos
                .Where(e => e.FrequenciaManutencao != null && e.FrequenciaManutencao != "Nenhuma")
                .ToListAsync();

            foreach (var eq in equipamentos)
            {
                var dataBase = (eq.DataUltimaManutencao ?? eq.DataCompra).GetValueOrDefault();
                if (dataBase.Year <= 1) continue;

                DateTime proximaManutencao = eq.FrequenciaManutencao switch
                {
                    "Mensal" => dataBase.AddMonths(1),
                    "Trimestral" => dataBase.AddMonths(3),
                    "Anual" => dataBase.AddYears(1),
                    _ => DateTime.MinValue
                };

                if (proximaManutencao >= hoje && proximaManutencao <= dataLimite)
                {
                    resultado.Add(new EquipamentoManutencaoViewModel
                    {
                        CodigoItem = eq.CodigoItem,
                        Descricao = eq.Descricao,
                        ProximaManutencao = proximaManutencao.ToShortDateString(),
                        Frequencia = eq.FrequenciaManutencao
                    });
                }
            }

            return resultado.OrderBy(r => r.ProximaManutencao).ToList();
        }

        // --- Consumo mensal ---
        private async Task<ChartData> GetConsumoMesDataAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var anoAtual = DateTime.Now.Year;
            var consumoMesQuery = await context.ConsumosEnergia
                .Where(c => c.DataReferencia.Year == anoAtual)
                .GroupBy(c => c.DataReferencia.Month)
                .Select(g => new { Mes = g.Key, TotalKwh = g.Sum(c => c.ValorKwh) })
                .ToListAsync();

            var labels = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames.Take(12).ToArray();
            var dados = new decimal[12];

            foreach (var item in consumoMesQuery)
                dados[item.Mes - 1] = item.TotalKwh;

            return new ChartData { Labels = labels, Data = dados };
        }

        // --- Consumo anual ---
        private async Task<ChartData> GetConsumoAnoDataAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

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

        // --- Obter ID do usuário logado ---
        private async Task<int?> GetCurrentUserId()
        {
            var userEmail = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
                return null;

            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);
            return user?.Id;
        }
    }
}
