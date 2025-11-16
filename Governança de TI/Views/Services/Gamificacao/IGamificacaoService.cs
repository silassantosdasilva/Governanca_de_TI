using System.Threading.Tasks;

namespace Governança_de_TI.Views.Services.Gamificacao
{
    /// <summary>
    /// Interface para o serviço de gamificação.
    /// Define os métodos para adicionar pontos e verificar conquistas.
    /// </summary>
    public interface IGamificacaoService
    {
        /// <summary>
        /// Adiciona pontos a um usuário por uma determinada ação, atualiza o nível
        /// e verifica/registra novos prêmios conquistados.
        /// </summary>
        /// <param name="usuarioId">ID do usuário que realizou a ação.</param>
        /// <param name="acao">Nome da ação realizada (ex: "Descarte Sustentavel", "Cadastro Equipamento").</param>
        /// <param name="pontos">Quantidade de pontos a serem adicionados.</param>
        /// <returns>Task</returns>
        Task AdicionarPontosAsync(int usuarioId, string acao, int pontos);

        // Poderíamos adicionar outros métodos aqui no futuro, como:
        // Task<GamificacaoModel> GetProgressoUsuarioAsync(int usuarioId);
        // Task<List<PremioModel>> GetPremiosUsuarioAsync(int usuarioId);


    }
}
