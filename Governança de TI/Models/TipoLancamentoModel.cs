// Models/Entities/TipoLancamentoModel.cs

using System;
using System.ComponentModel.DataAnnotations;

public class TipoLancamentoModel
{
    // 3.3. Tabela: TipoLancamento
    [Key]
    public Guid IdTipo { get; set; } // PK

    public string Nome { get; set; } // Nome da Categoria

    // 1 = Receita, 2 = Despesa
    public int Tipo { get; set; }
}