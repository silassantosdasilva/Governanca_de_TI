using System.Collections.Generic;

namespace Governança_de_TI.ViewModels
{
    /// <summary>
    /// Agrupa todos os dados necessários para a renderização do dashboard principal.
    /// Esta classe é usada como o objeto de retorno da API do DashboardController.
    /// </summary>
    public class DashboardViewModel
    {
        // Propriedades para os cards de métricas superiores
        public string EmissoesCo2Evitadas { get; set; }
        public string EquipamentosRecicladosPercentual { get; set; }
        public int ItensPendentesDescarte { get; set; }
        public int EquipamentosDescartadosCorretamente { get; set; }

        // Propriedades para as listas dinâmicas
        public List<EquipamentoVencendoViewModel> EquipamentosProximosFimVida { get; set; }
        public List<EquipamentoManutencaoViewModel> EquipamentosProximaManutencao { get; set; }

        // Propriedades para os gráficos de consumo restantes
        public ChartData ConsumoKwhMes { get; set; }
        public ChartData ConsumoKwhAno { get; set; }
    }

    /// <summary>
    /// Representa um único item na lista de equipamentos próximos do fim da vida útil.
    /// </summary>
    public class EquipamentoVencendoViewModel
    {
        public int CodigoItem { get; set; }
        public string Descricao { get; set; }
        public string DataVencimento { get; set; }
        public int DiasRestantes { get; set; }
    }

    /// <summary>
    /// Representa um único item na lista de equipamentos com manutenção próxima.
    /// </summary>
    public class EquipamentoManutencaoViewModel
    {
        public int CodigoItem { get; set; }
        public string Descricao { get; set; }
        public string ProximaManutencao { get; set; }
        public string Frequencia { get; set; }
    }

    /// <summary>
    /// Classe auxiliar que representa a estrutura de dados para os gráficos (Chart.js).
    /// </summary>
    public class ChartData
    {
        public string[] Labels { get; set; }
        public decimal[] Data { get; set; }
    }
}
