// Models/Entities/LancamentoFinanceiroModel.cs (CORRIGIDO E SINCRONIZADO)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class LancamentoFinanceiroModel
{
    // 3.4. Tabela: LancamentoFinanceiro

    [Key]
    public Guid IdLancamento { get; set; } // Chave Primária (PK)

    // Chaves Estrangeiras (FKs)
    public Guid IdPessoa { get; set; } // FK -> Pessoa
    public Guid IdConta { get; set; } // FK -> ContaBancaria (Conta padrão para o lançamento)
    public Guid IdTipo { get; set; } // FK -> TipoLancamento (Categoria)

    // CAMPO ADICIONADO: Essencial para o Service determinar a operação (crédito/débito) e para os filtros.
    // 1=Despesa, 2=Receita
    public int TipoLancamento { get; set; }

    // Valores
    [Column(TypeName = "decimal(18, 2)")]
    public decimal ValorOriginal { get; set; } // Valor total sem juros/descontos

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Valor { get; set; } // Valor atual (ValorOriginal + ajustes, se houver)

    // Detalhes do Documento
    public string Documento { get; set; } // Ex: NF, Boleto
    public int SequenciaDocumento { get; set; } // Usado em documentos com múltiplos lançamentos
    public string Observacao { get; set; }

    // Datas
    public DateTime DataEmissao { get; set; } // Data do documento original
    public DateTime DataFluxo { get; set; } // Data de vencimento/pagamento (impacta o fluxo)

    // Condições
    public string FormaPagamento { get; set; }
    public int Condicao { get; set; } // 1=À vista, 2=Parcelado
    public int NumeroParcelas { get; set; } // Se Condicao=2
    public int IntervaloDias { get; set; } // Ex: 30 dias entre parcelas
    public int Status { get; set; } // 1=Aberto, 2=Pago, 3=Cancelado

    // Relacionamentos (Propriedades de navegação do EF Core)
    public ICollection<LancamentoParcelaModel> Parcelas { get; set; }

    // NOVAS PROPRIEDADES DE NAVEGAÇÃO (CORREÇÃO)
    public PessoaModel Pessoa { get; set; }
    public ContaBancariaModel Conta { get; set; }
    public TipoLancamentoModel Tipo { get; set; }
}