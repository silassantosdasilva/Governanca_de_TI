using System;
using System.Collections.Generic;

namespace Governança_de_TI.Models.Financeiro // Ou namespace ViewModels
{
    public class ExtratoViewModel
    {
        // Filtros
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string FiltroPeriodo { get; set; }

        // KPIs
        public decimal SaldoPeriodoAnterior { get; set; }
        public decimal SaldoPrevisto { get; set; }
        public decimal ReceitasPeriodo { get; set; }
        public decimal DespesasPeriodo { get; set; }

        // A LISTA AGORA É DE PARCELAS (pois elas têm a data de vencimento)
        public List<LancamentoParcelaModel> Parcelas { get; set; }
        public List<LancamentoFinanceiroModel> Transacoes { get; set; }
        // Gráfico
        public Dictionary<string, decimal> DespesasPorCategoria { get; set; }
        public Dictionary<string, decimal> ReceitasPorCategoria { get; set; }

    }
}