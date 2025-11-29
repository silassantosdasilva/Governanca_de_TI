// Models/Entities/Pessoa.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PessoaModel
{
    // Chave Primária (PK)
    [Key]
    public Guid IdPessoa { get; set; } // Tipo GUID [cite: 19]

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdPessoaInt { get; set; } 

    // Campos Essenciais
    public string Nome { get; set; }
    public int TipoPessoa { get; set; } 
    public string? Documento { get; set; }
    public string? Telefone1 { get; set; } 
    public string? Telefone2 { get; set; } 
    public string? Email { get; set; } 
    public string? Observacao { get; set; } 
    public DateTime DataCadastro { get; set; } 

    public DateTime DataAtualizacao { get; set; }

    // Campos de Endereço
    public string? EnderecoLogradouro { get; set; } 
    public string? EnderecoNumero { get; set; } 
    public string? EnderecoComplemento { get; set; } 
    public string? EnderecoBairro { get; set; } 
    public string? EnderecoCidade { get; set; } 
    public string? EnderecoUF { get; set; } 
    public string? EnderecoCEP { get; set; }
}