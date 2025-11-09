using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Necessário para [NotMapped]

namespace Governança_de_TI.Models
{
    public class DashboardWidgetModel
    {
        public int Id { get; set; }

        [Required]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public int Posicao { get; set; } // 1 a 6

        [Required]
        public string TipoVisualizacao { get; set; } = "Total"; // Total, Pizza, Barra, Rolo, Lista

        [Required]
        public string TabelaFonte { get; set; } = string.Empty;

        public string? CampoMetrica { get; set; } // Ex: ValorAquisicao
        public string? CampoDimensao { get; set; } // Ex: Status
        public string? Operacao { get; set; } // Ex: Contagem, Soma, Média

        public string? CamposLista { get; set; } // Ex: "Nome,DataAquisicao,Valor"
        public string? OrdenarPor { get; set; }
        public string? Ordem { get; set; }

        // Avançado
        public string? Campo1 { get; set; }
        public string? OperadorAvancado { get; set; } // +, -, *, /, %
        public string? Campo2 { get; set; }

        // Controle
        public string UsuarioId { get; set; } = string.Empty; // chave do usuário

        // =========================================================
        // NOVOS CAMPOS PARA FILTRO DE DATA
        // =========================================================

        /// <summary>
        /// O nome do campo de data que será usado para filtrar (ex: "DataAquisicao", "DataDescarte")
        /// </summary>
        public string? CampoDataFiltro { get; set; }

        /// <summary>
        /// A data "DE" (início) do filtro
        /// </summary>
        public DateTime? DataFiltroInicio { get; set; }

        /// <summary>
        /// A data "ATÉ" (fim) do filtro
        /// </summary>
        public DateTime? DataFiltroFim { get; set; }

        // =========================================================
        // NOVOS CAMPOS [NotMapped] PARA PASSAR DADOS (Corrige os erros)
        // =========================================================

        /// <summary>
        /// [Não Mapeado] Usado para carregar o resultado de um KPI (ex: 42.5)
        /// </summary>
        [NotMapped]
        public object? Resultado { get; set; }

        /// <summary>
        /// [Não Mapeado] Usado para carregar a lista de dados de um gráfico
        /// </summary>
        [NotMapped]
        public List<object>? Dados { get; set; }
    }
}