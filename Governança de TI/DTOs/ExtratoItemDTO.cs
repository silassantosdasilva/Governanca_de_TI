// DTOs/ExtratoDTOs.cs

using System;
using System.Collections.Generic;

// -----------------------------------------------------
// DTO de Requisição (5.6. Request exemplo)
// -----------------------------------------------------
namespace Governança_de_TI.DTOs // <--- DECLARAÇÃO DO NAMESPACE CORRETO
{

    public class ExtratoRequestDTO
    {
        // 4.3. Parâmetros do Extrato
        public DateTime PeriodoInicial { get; set; }
        public DateTime PeriodoFinal { get; set; }
        public int? TipoLancamento { get; set; } // 1=Despesa, 2=Receita
        public int? Status { get; set; } // 1=Aberto, 2=Pago, 3=Cancelado
        public Guid? IdConta { get; set; }
        public Guid? IdPessoa { get; set; }
        public Guid? IdTipo { get; set; } // Categoria
        public string? FormaPagamento { get; set; }
    }

    // -----------------------------------------------------
    // DTO de Resposta (Consolidação)
    // -----------------------------------------------------
    public class ExtratoResponseDTO
    {
        // 4.3. Resultados Consolidados
        public decimal SaldoAnterior { get; set; }
        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal SaldoFinal { get; set; } // SaldoAnterior + TotalReceitas - TotalDespesas

        // O Extrato retorna uma lista de Lançamentos ou Parcelas detalhadas
        public List<ExtratoItemDTO> Itens { get; set; } = new List<ExtratoItemDTO>();
    }

    // -----------------------------------------------------
    // DTO para cada linha do Extrato (AJUSTADO)
    // -----------------------------------------------------
    public class ExtratoItemDTO
    {
        public Guid Id { get; set; } // Id da Parcela (ou Lancamento, se à vista)
        public Guid IdLancamentoPai { get; set; }
        public int TipoLancamento { get; set; } // 1=Despesa, 2=Receita

        public decimal ValorMovimentado { get; set; } // Valor da Parcela

        // RENOMEADO para refletir a necessidade
        public DateTime DataVencimento { get; set; }

        // NOVO CAMPO: Para exibir a data real da baixa, se for pago
        public DateTime? DataPagamento { get; set; }

        // Detalhes de Exibição
        public string Descricao { get; set; }
        public string NomeCategoria { get; set; }
        public string NomePessoa { get; set; }
        public int Status { get; set; } // 1=Pendente, 2=Pago, etc.
    }
}