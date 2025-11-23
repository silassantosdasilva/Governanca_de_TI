using System;
using System.Collections.Generic;
using Governança_de_TI.Models; // Garanta que seus models estão aqui

namespace Governança_de_TI.DTOs // Ou namespace ViewModels
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
        public List<LancamentoParcelaModel> Transacoes { get; set; }

        // Gráfico
        public Dictionary<string, decimal> DespesasPorCategoria { get; set; }
    }
}