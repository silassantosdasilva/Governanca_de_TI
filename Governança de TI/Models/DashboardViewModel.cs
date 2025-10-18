using System.Collections.Generic;

namespace Governança_de_TI.ViewModels
{
    public class DashboardViewModel
    {
        // Cards Superiores
        public string EmissoesCo2Evitadas { get; set; }
        public string EquipamentosRecicladosPercentual { get; set; }
        public int ItensPendentesDescarte { get; set; }
        public int EquipamentosDescartadosCorretamente { get; set; }

        // NOVAS LISTAS DINÂMICAS
        public List<EquipamentoVencendoViewModel> EquipamentosProximosFimVida { get; set; }
        public List<EquipamentoManutencaoViewModel> EquipamentosProximaManutencao { get; set; }

        // Gráficos de Consumo
        public ChartData ConsumoKwhMes { get; set; }
        public ChartData ConsumoKwhAno { get; set; }
    }

    // Representa um item na lista de equipamentos a vencer
    public class EquipamentoVencendoViewModel
    {
        public int CodigoItem { get; set; }
        public string Descricao { get; set; }
        public string DataVencimento { get; set; }
        public int DiasRestantes { get; set; }
    }

    // NOVA CLASSE: Representa um item na lista de manutenções próximas
    public class EquipamentoManutencaoViewModel
    {
        public int CodigoItem { get; set; }
        public string Descricao { get; set; }
        public string ProximaManutencao { get; set; }
        public string Frequencia { get; set; }
    }

    // Classe auxiliar para os gráficos de consumo
    public class ChartData
    {
        public string[] Labels { get; set; }
        public decimal[] Data { get; set; }
    }
}

