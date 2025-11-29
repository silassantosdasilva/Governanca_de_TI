// DTOs/TipoLancamentoDTO.cs

using System;

namespace Governança_de_TI.DTOs.Financeiro 
{

    public class TipoLancamentoDTO
    {
        public Guid Id { get; set; } // Mapeia para IdTipo

        public int IdTipo { get; set; } // Atual idTipo
        public string Nome { get; set; }
        public int Tipo { get; set; } // 1=Receita, 2=Despesa
    }
}