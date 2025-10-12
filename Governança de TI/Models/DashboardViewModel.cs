namespace Governança_de_TI.ViewModels
{
    // Esta classe agrupa todos os dados necessários para o dashboard
    public class DashboardViewModel
    {
        // Dados para os cards superiores
        public string EmissoesCo2Evitadas { get; set; }
        public string EquipamentosRecicladosPercentual { get; set; }
        public int ItensPendentesDescarte { get; set; }
        public int EquipamentosDescartadosCorretamente { get; set; }

        // Dados para os gráficos inferiores (as chaves e valores)
        public ChartData EconomiaGerada { get; set; }
        public ChartData ConsumoKwhMes { get; set; }
        public ChartData ConsumoKwhAno { get; set; }
        public ChartData ConsumoPorSetor { get; set; }
    }

    // Classe auxiliar para representar os dados de um gráfico
    public class ChartData
    {
        public string[] Labels { get; set; }
        public decimal[] Data { get; set; }
    }
}
