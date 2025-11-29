// Models/Entities/LancamentoParcelaModel.cs

using Governança_de_TI.Models.Financeiro;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class LancamentoParcelaModel
{
    // 3.5. Tabela: LancamentoParcela

    [Key]
    public Guid IdParcela { get; set; } // PK

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdParcelaFinanceiro { get; set; } // Atual idParcela
    public Guid IdLancamento { get; set; } // FK -> LancamentoFinanceiro (Lançamento pai)

    public int NumeroParcela { get; set; } // Ex: 1, 2, 3...

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ValorParcela { get; set; }

    public DateTime DataVencimento { get; set; }

    public int Status { get; set; } // 1=Pendente, 2=Pago, 3=Atrasado, 4=Cancelado

    // Campos de Baixa (Regra 4.3)
    public DateTime? DataPagamento { get; set; } // Data real da baixa
    public Guid? IdContaBaixa { get; set; } // Conta bancária usada para o pagamento/recebimento

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ValorPago { get; set; } // Valor final pago (pode ter juros/descontos na baixa)

    // Propriedade de navegação (Relacionamento N:1)
    public LancamentoFinanceiroModel LancamentoPai { get; set; }
    
}