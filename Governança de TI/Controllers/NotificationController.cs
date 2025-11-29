using Governança_de_TI.Data;
using Governança_de_TI.Models.Gamificacao;
using Governança_de_TI.ViewModels; // Garanta que este namespace está correto
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // Para List
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    /// <summary>
    /// API para buscar dados para o menu de notificações.
    /// </summary>
    [Route("api/NotificationApi")] // Rota corrigida
    [ApiController]
    public class NotificationApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Busca um resumo dos dados de gamificação, equipamentos a vencer e descartes recentes.
        /// </summary>
        [HttpGet("summary")] // Rota: /api/NotificationApi/summary
        public async Task<IActionResult> GetSummary()
        {
            var userId = await GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            // ⚠️ Execução SEQUENCIAL — evita acesso concorrente ao DbContext
            var gamificacao = await GetGamificacaoDataAsync(userId.Value);
            var equipamentos = await GetEquipamentosVencendoAsync();
            var descartes = await GetDescartesRecentesAsync();

            var summary = new NotificationSummaryViewModel
            {
                Gamificacao = gamificacao,
                EquipamentosVencendo = equipamentos,
                DescartesRecentes = descartes
            };

            return Ok(summary);
        }

        // --- MÉTODOS AUXILIARES DE BUSCA ---

        // Especifica que o retorno é do namespace ViewModels
        private async Task<ViewModels.GamificacaoViewModel> GetGamificacaoDataAsync(int usuarioId)
        {
            var gamificacao = await _context.Gamificacoes
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(g => g.UsuarioId == usuarioId);

            //if (gamificacao == null)
            //{
            //    // Especifica que o modelo é do namespace Models
            //    gamificacao = new Models.GamificacaoModel { Pontos = 0, Nivel = "Iniciante" };
            //}

            // Lógica de cálculo
            string icone = "🌱"; int pontosProximoNivel = 100; int pontosNivelAtual = 0; string mensagem = "Faltam 100 pontos!";
            if (gamificacao.Nivel == "Ecopensante") { icone = "🌿"; pontosNivelAtual = 100; pontosProximoNivel = 250; }
            else if (gamificacao.Nivel == "Guardião Verde") { icone = "🌳"; pontosNivelAtual = 250; pontosProximoNivel = 500; }
            else if (gamificacao.Nivel == "Mestre ESG") { icone = "🌎"; pontosNivelAtual = 500; pontosProximoNivel = 500; }

            double percentualProgresso = 0; int pontosParaSubir = pontosProximoNivel - pontosNivelAtual;
            if (pontosParaSubir > 0) { int pontosFeitosNesteNivel = gamificacao.Pontos - pontosNivelAtual; int pontosQueFaltam = pontosProximoNivel - gamificacao.Pontos; percentualProgresso = Math.Max(2, Math.Round(((double)pontosFeitosNesteNivel / pontosParaSubir) * 100)); mensagem = $"Faltam {pontosQueFaltam} pontos!"; }
            else if (gamificacao.Nivel == "Mestre ESG") { percentualProgresso = 100; mensagem = "Nível Máximo!"; }

            // Especifica que o retorno é do namespace ViewModels
            return new ViewModels.GamificacaoViewModel
            {
                PontosAtuais = gamificacao.Pontos,
                NivelAtual = gamificacao.Nivel,
                IconeNivel = icone,
                PontosProximoNivel = pontosProximoNivel,
                PercentualProgresso = (int)percentualProgresso,
                MensagemNivel = mensagem
            };
        }

        private async Task<List<EquipamentoVencendoNotificacaoViewModel>> GetEquipamentosVencendoAsync()
        {
            var hoje = DateTime.Now.Date;
            var dataLimite = hoje.AddDays(30);

            return await _context.Equipamentos
                .Where(e => e.VidaUtilFim.HasValue && e.VidaUtilFim.Value >= hoje && e.VidaUtilFim.Value <= dataLimite)
                .OrderBy(e => e.VidaUtilFim)
                .Take(3)
                .Select(e => new EquipamentoVencendoNotificacaoViewModel
                {
                    CodigoItem = e.CodigoItem,
                    Descricao = e.Descricao,
                    DiasRestantes = (e.VidaUtilFim.Value - hoje).Days,
                    Data = e.VidaUtilFim.Value.ToString("dd/MM/yyyy")
                })
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<List<DescarteRecenteNotificacaoViewModel>> GetDescartesRecentesAsync()
        {
            var dataLimite = DateTime.Now.AddDays(-7);

            return await _context.Descartes
                .Include(d => d.Equipamento)
                .Where(d => d.DataDeCadastro >= dataLimite)
                .OrderByDescending(d => d.DataDeCadastro)
                .Take(3)
                .Select(d => new DescarteRecenteNotificacaoViewModel
                {
                    Id = d.Id,
                    DescricaoEquipamento = d.Equipamento.Descricao,
                    EmpresaColetora = d.EmpresaColetora,
                    Data = d.DataDeCadastro.ToString("dd/MM/yyyy")
                })
                .AsNoTracking()
                .ToListAsync();
        }


        // Método auxiliar para obter ID do usuário logado
        private async Task<int?> GetCurrentUserId()
        {
            var userEmail = HttpContext?.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail)) return null;
            var user = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);
            return user?.Id;
        }
    }
}

