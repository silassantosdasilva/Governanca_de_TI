// Models/Entities/Pessoa.cs

using System;
using System.ComponentModel.DataAnnotations;

public class PessoaModel
{
    // Chave Primária (PK)
    [Key]
    public Guid IdPessoa { get; set; } // Tipo GUID [cite: 19]

    // Campos Essenciais
    public string Nome { get; set; } // string(200) [cite: 20]
    public int TipoPessoa { get; set; } // int (1=Física, 2=Jurídica) [cite: 21]
    public string? Documento { get; set; } // string(20) (CPF ou CNPJ) [cite: 22]
    public string? Telefone1 { get; set; } // string(20) [cite: 23]
    public string? Telefone2 { get; set; } // string(20) [cite: 24]
    public string? Email { get; set; } // string(500) (múltiplos separados por ";") [cite: 25]
    public string? Observacao { get; set; } // text [cite: 26]
    public DateTime DataCadastro { get; set; } // datetime [cite: 34]

    public DateTime DataAtualizacao { get; set; }

    // Campos de Endereço
    public string? EnderecoLogradouro { get; set; } // string(200) [cite: 27]
    public string? EnderecoNumero { get; set; } // string(20) [cite: 28]
    public string? EnderecoComplemento { get; set; } // string(100) [cite: 29]
    public string? EnderecoBairro { get; set; } // string(100) [cite: 30]
    public string? EnderecoCidade { get; set; } // string(100) [cite: 31]
    public string? EnderecoUF { get; set; } // string(2) [cite: 32]
    public string? EnderecoCEP { get; set; } // string(10) [cite: 33]
}