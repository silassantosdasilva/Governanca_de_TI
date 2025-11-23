// Models/Entities/ContaBancaria.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ContaBancariaModel
{
    // 3.2. Tabela: ContaBancaria

    [Key]
    public Guid IdConta { get; set; } // PK - AJUSTADO PARA INT

    public string Banco { get; set; }
    public string NomeConta { get; set; }
    public string? NumeroConta { get; set; }
    public string? Agencia { get; set; }

    public int StatusConta { get; set; }
    public int TipoConta { get; set; }

    // decimal(18,2) - O campo mais importante para o fluxo de caixa
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? SaldoAtual { get; set; }

    // CAMPO REQUERIDO PARA O CÁLCULO DO SALDO DIÁRIO
    [Column(TypeName = "decimal(18, 2)")]
    public decimal SaldoInicial { get; set; } // Saldo de partida (Usado no recalculo total)

    public DateTime DataCadastro { get; set; }
}