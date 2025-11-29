// DTOs/ContaBancariaDTO.cs (CORREÇÃO DE NAMESPACE)

using System;

namespace Governança_de_TI.DTOs.Financeiro // <--- NAMESPACE CORRETO PARA A ESTRUTURA DO SEU PROJETO
{
    public class ContaBancariaDTO
    {
        public Guid Id { get; set; } // Mapeia para IdConta

        public int IdConta { get; set; } // Atual idConta
        public string Banco { get; set; }
        public string NomeConta { get; set; }
        public string? NumeroConta { get; set; }
        public string? Agencia { get; set; }
        public int StatusConta { get; set; }
        public int TipoConta { get; set; }

        // O saldo é crucial no DTO de retorno, mas pode ser opcional no POST/PUT
        public decimal SaldoInicial { get; set; } // Saldo de partida da conta (Ex: R$ 0,00)

        public decimal SaldoAtual { get; set; }
    }

    // DTO específico para o endpoint de ajuste manual (PATCH /contas/{id}/ajuste-saldo)
    public class AjusteSaldoDTO
    {
        public decimal NovoSaldo { get; set; }
        public string Observacao { get; set; } // Motivo do ajuste (para rastreabilidade/auditoria)
    }
}