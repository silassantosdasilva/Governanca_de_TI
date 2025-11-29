// DTOs/PessoaDTO.cs

using System;
using System.Collections.Generic;
// Removidos os marcadores de citação (cite: XXX)

namespace Governança_de_TI.DTOs.Financeiro // <--- DECLARAÇÃO DO NAMESPACE CORRETO
{
    public class PessoaDTO
    {
        // Mapeia para os campos da Entidade (8.1)
        public Guid Id { get; set; } // "id"

        public int IdPessoa { get; set; } // Atual idPessoa
        public string Nome { get; set; } // "nome"
        public int TipoPessoa { get; set; } // "tipoPessoa" (1 ou 2)
        public string? Documento { get; set; } // "documento"
        public string? Observacao { get; set; } // "observacao"

        // O DTO apresenta telefones e e-mails como listas
        public List<string>? Telefones { get; set; }
        public List<string>? Emails { get; set; }

        // Estrutura de Endereço aninhada (8.1)
        public EnderecoDTO? Endereco { get; set; }

        // Campos Calculados (retornados pelo PessoaService)
        public decimal? SaldoAPagar { get; set; } // Saldo a Pagar
        public decimal? SaldoAReceber { get; set; } // Saldo a Receber
    }

    // Sub-DTO para a estrutura aninhada (8.1)
    public class EnderecoDTO
    {
        public string? Logradouro { get; set; } // "logradouro"
        public string? Numero { get; set; } // "numero"
        public string? Complemento { get; set; } // "complemento"
        public string? Bairro { get; set; } // "bairro"
        public string? Cidade { get; set; } // "cidade"
        public string? Uf { get; set; } // "uf"
        public string? Cep { get; set; } // "cep"
    }
}