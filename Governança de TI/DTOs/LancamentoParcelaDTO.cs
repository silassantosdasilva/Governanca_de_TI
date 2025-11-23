// DTOs/LancamentoParcelaDTO.cs

using System;
namespace Governança_de_TI.DTOs // <--- DECLARAÇÃO DO NAMESPACE CORRETO
{

    public class LancamentoParcelaDTO
    {
        public Guid Id { get; set; } // IdParcela
        public Guid IdLancamento { get; set; } // FK
        public int NumeroParcela { get; set; }
        public decimal ValorParcela { get; set; }
        public DateTime DataVencimento { get; set; }
        public int Status { get; set; } // 1=Pendente, 2=Pago, 3=Atrasado, 4=Cancelado

        // Campos de Baixa
        public DateTime? DataPagamento { get; set; }
        public Guid? IdContaBaixa { get; set; }
        public decimal ValorPago { get; set; }
    }
}