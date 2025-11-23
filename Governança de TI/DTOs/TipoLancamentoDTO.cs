// DTOs/TipoLancamentoDTO.cs

using System;

namespace Governança_de_TI.DTOs // <--- DECLARAÇÃO DO NAMESPACE CORRETO
{

    public class TipoLancamentoDTO
    {
        public Guid Id { get; set; } // Mapeia para IdTipo
        public string Nome { get; set; }
        public int Tipo { get; set; } // 1=Receita, 2=Despesa
    }
}