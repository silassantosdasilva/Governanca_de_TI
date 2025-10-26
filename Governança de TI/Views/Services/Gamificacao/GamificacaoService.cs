using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Governança_de_TI.Views.Services.Gamificacao;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Para logs (opcional, mas recomendado)
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Services
{
    /// <summary>
    /// Implementação do serviço de gamificação.
    /// Contém a lógica para pontuar, nivelar e premiar usuários.
    /// </summary>
    public class GamificacaoService : IGamificacaoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GamificacaoService> _logger; // Opcional: para registrar logs

        public GamificacaoService(ApplicationDbContext context, ILogger<GamificacaoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Adiciona pontos, recalcula nível e verifica prêmios.
        /// </summary>
        public async Task AdicionarPontosAsync(int usuarioId, string acao, int pontos)
        {
            if (pontos <= 0) return; // Não adiciona pontos negativos ou zero por aqui

            try
            {
                // 1. Encontra ou cria o registro de gamificação do usuário
                var gamificacao = await _context.Gamificacoes.FirstOrDefaultAsync(g => g.UsuarioId == usuarioId);

                if (gamificacao == null)
                {
                    gamificacao = new GamificacaoModel { UsuarioId = usuarioId, Pontos = 0, Nivel = "Iniciante" };
                    _context.Gamificacoes.Add(gamificacao);
                    // Não salva ainda, espera adicionar os pontos
                }

                // 2. Adiciona os pontos
                gamificacao.Pontos += pontos;

                // 3. Recalcula o Nível
                gamificacao.Nivel = CalcularNivel(gamificacao.Pontos);

                // Marca a entidade como modificada (se não for adicionada)
                if (_context.Entry(gamificacao).State != EntityState.Added)
                {
                    _context.Update(gamificacao);
                }

                // 4. Salva as alterações de pontos e nível
                await _context.SaveChangesAsync();

                // 5. Verifica e registra novos prêmios (após salvar os pontos)
                await VerificarEAdicionarPremiosAsync(usuarioId, gamificacao.Pontos);

                _logger?.LogInformation($"Gamificação: Usuário ID {usuarioId} ganhou {pontos} pontos pela ação '{acao}'. Pontuação total: {gamificacao.Pontos}, Nível: {gamificacao.Nivel}");

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Erro ao adicionar pontos de gamificação para usuário ID {usuarioId} pela ação '{acao}'.");
                // Considerar como tratar o erro (ex: tentar novamente, logar em outro local)
                // Por enquanto, apenas logamos e continuamos
            }
        }

        /// <summary>
        /// Calcula o nível com base na pontuação total.
        /// </summary>
        private string CalcularNivel(int pontuacao)
        {
            if (pontuacao >= 500) return "Mestre ESG";
            if (pontuacao >= 250) return "Guardião Verde";
            if (pontuacao >= 100) return "Ecopensante";
            return "Iniciante";
        }

        /// <summary>
        /// Verifica quais prêmios o usuário pode ter conquistado com a nova pontuação
        /// e registra aqueles que ele ainda não possui.
        /// </summary>
        private async Task VerificarEAdicionarPremiosAsync(int usuarioId, int pontuacaoAtual)
        {
            try
            {
                // Busca TODOS os prêmios que o usuário JÁ conquistou
                var premiosJaConquistadosIds = await _context.UsuarioPremios
                    .Where(up => up.UsuarioId == usuarioId)
                    .Select(up => up.PremioId)
                    .ToListAsync();

                // Busca TODOS os prêmios disponíveis no sistema que podem ser conquistados
                // com a pontuação atual E que o usuário ainda NÃO conquistou
                var novosPremiosParaConquistar = await _context.Premios
                    .Where(p => p.PontosNecessarios <= pontuacaoAtual && !premiosJaConquistadosIds.Contains(p.Id))
                    .ToListAsync();

                if (novosPremiosParaConquistar.Any())
                {
                    foreach (var premio in novosPremiosParaConquistar)
                    {
                        var novaConquista = new UsuarioPremioModel
                        {
                            UsuarioId = usuarioId,
                            PremioId = premio.Id,
                            DataConquista = DateTime.Now // Data atual da conquista
                        };
                        _context.UsuarioPremios.Add(novaConquista);

                        _logger?.LogInformation($"Gamificação: Usuário ID {usuarioId} conquistou o prêmio '{premio.Nome}'!");
                        // Aqui poderíamos disparar uma notificação (passo futuro)
                    }
                    // Salva todas as novas conquistas de uma vez
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Erro ao verificar/adicionar prêmios para usuário ID {usuarioId}.");
                // Continua mesmo se houver erro ao adicionar prêmio
            }

        }
    }
}
