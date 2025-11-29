using Governança_de_TI.Models.Financeiro.TpLancamento;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.Financeiro
{
    [Table("LancamentoFinanceiro")]
    public class LancamentoFinanceiroModel
    {
        // 3.4. Tabela: LancamentoFinanceiro


        public Guid IdLancamento { get; set; } // Chave Primária (PK)

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdLancamentoFinanceiro{ get; set; } // Identificador Único

        // --- Chaves Estrangeiras (FKs) ---

        public Guid IdPessoa { get; set; } // FK -> Pessoa
        public Guid IdConta { get; set; } // FK -> ContaBancaria
        public Guid IdTipo { get; set; } // FK -> TipoLancamento (Categoria)

        // [NOVO] Subcategoria é opcional (pode ser null)
        public Guid? IdSubcategoria { get; set; } // FK -> SubCategoriaModel

        // --- Campos de Negócio ---

        // 1=Despesa, 2=Receita
        public int TipoLancamento { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ValorOriginal { get; set; } // Valor total sem juros/descontos

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Valor { get; set; } // Valor atual

        // Detalhes do Documento
        public string? Documento { get; set; } // Ex: NF, Boleto
        public int? SequenciaDocumento { get; set; }
        public string? Observacao { get; set; }

        // Datas
        public DateTime DataEmissao { get; set; }
        public DateTime DataFluxo { get; set; } // Vencimento ou Fluxo de Caixa

        // Condições
        public string FormaPagamento { get; set; }
        public int Condicao { get; set; } // 1=À vista, 2=Parcelado
        public int NumeroParcelas { get; set; }
        public int IntervaloDias { get; set; }
        public int Status { get; set; } // 1=Aberto, 2=Pago, 3=Cancelado

        // --- Relacionamentos (Navegação EF Core) ---

        public virtual ICollection<LancamentoParcelaModel> Parcelas { get; set; }

        [ForeignKey("IdPessoa")]
        public virtual PessoaModel Pessoa { get; set; }

        [ForeignKey("IdConta")]
        public virtual ContaBancariaModel Conta { get; set; }

        [ForeignKey("IdTipo")]
        public virtual TipoLancamentoModel Tipo { get; set; }

        // [NOVO] Propriedade de Navegação para Subcategoria
        [ForeignKey("IdSubcategoria")]
        public virtual SubCategoriaModel SubCategoria { get; set; }
    }
}