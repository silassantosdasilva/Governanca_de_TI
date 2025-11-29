using System;
using System.Collections.Generic;

namespace Governança_de_TI.DTOs.Financeiro
{
    public class LancamentoDTO
    {
        // 8.2. Estrutura de DTOs
        public Guid Id { get; set; } // IdLancamento
        public int IdLancamento { get; set; } //Atual id
        public int TipoLancamento { get; set; } // 1=Despesa, 2=Receita

        // Chaves Estrangeiras
        public Guid IdPessoa { get; set; }
        public Guid IdConta { get; set; }
        public Guid IdTipo { get; set; } // Categoria Principal
        public Guid? IdSubcategoria { get; set; } // <--- NOVO CAMPO (Subcategoria Opcional)

        // Valores
        public decimal ValorOriginal { get; set; }
        public decimal Valor { get; set; }

        // Detalhes
        public string Documento { get; set; }
        public int SequenciaDocumento { get; set; }
        public string Observacao { get; set; }

        // Datas
        public DateTime DataEmissao { get; set; }
        public DateTime DataFluxo { get; set; } // Vencimento ou Fluxo de Caixa

        // Condições
        public string FormaPagamento { get; set; }
        public int Condicao { get; set; } // 1=À vista, 2=Parcelado
        public int NumeroParcelas { get; set; }
        public int IntervaloDias { get; set; }
        public int Status { get; set; } // 1=Aberto, 2=Pago, 3=Cancelado

        // Anexo (Regra 3.4 - Mapeia para base64 ou path)
        public string Anexo { get; set; }

        // Relacionamento (Retorno para lançamentos parcelados)
        public List<LancamentoParcelaDTO> Parcelas { get; set; } = new List<LancamentoParcelaDTO>();
    }
}