
using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Governança_de_TI.DTOs.Financeiro;

namespace Governança_de_TI.Repositories
{
    // A interface IExtratoRepository deve estar definida para que esta classe compile corretamente.
    public class ExtratoRepository : IExtratoRepository
    {
        private readonly ApplicationDbContext _context;

        public ExtratoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Busca itens de extrato (Lançamentos Parcela) com base em filtros.
        /// Faz a query complexa com JOINs necessários para exibir detalhes na UI.
        /// </summary>
        /// <param name="request">DTO contendo os filtros (PeriodoInicial, PeriodoFinal, IdPessoa, etc.).</param>
        /// <returns>Uma lista de ExtratoItemDTO.</returns>
        public async Task<IEnumerable<ExtratoItemDTO>> GetItensExtratoAsync(ExtratoRequestDTO request)
        {
            // 1. Inicializa a query na tabela de parcelas (LancamentosParcelas)
            var query = _context.LancamentosParcelas
                // Inclui o Lançamento Pai para acessar dados principais (ValorOriginal, Documento, Tipos)
                .Include(lp => lp.LancamentoPai)
                // Inclui a Pessoa (Cliente/Fornecedor) para exibir o nome no extrato
                .Include(lp => lp.LancamentoPai.Pessoa)
                // Inclui o Tipo/Categoria para exibir o nome da categoria no extrato
                .Include(lp => lp.LancamentoPai.Tipo)
                .AsQueryable();

            // 2. Aplica filtro obrigatório de Período (Data de Vencimento)
            query = query.Where(lp => lp.DataVencimento >= request.PeriodoInicial && lp.DataVencimento <= request.PeriodoFinal);

            // 3. Aplica filtros condicionais

            // Filtro por Pessoa (Cliente/Fornecedor)
            if (request.IdPessoa.HasValue)
                query = query.Where(lp => lp.LancamentoPai.IdPessoa == request.IdPessoa.Value);

            // Filtro por Conta Bancária (Conta principal do lançamento)
            if (request.IdConta.HasValue)
                query = query.Where(lp => lp.LancamentoPai.IdConta == request.IdConta.Value);

            // Filtro por Status da Parcela (Pendente, Pago, Cancelado)
            if (request.Status.HasValue)
                query = query.Where(lp => lp.Status == request.Status.Value);

            // Filtro por Tipo de Lançamento (Receita/Despesa)
            if (request.TipoLancamento.HasValue)
                query = query.Where(lp => lp.LancamentoPai.TipoLancamento == request.TipoLancamento.Value);

            // 4. Executa a query
            var resultados = await query.ToListAsync();

            // 5. Mapeia os resultados para o DTO de Extrato
            return resultados.Select(lp => new ExtratoItemDTO
            {
                Id = lp.IdParcela,
                IdLancamentoPai = lp.IdLancamento,
                TipoLancamento = lp.LancamentoPai.TipoLancamento,
                ValorMovimentado = lp.ValorParcela,
                DataVencimento = lp.DataVencimento,
                DataPagamento = lp.DataPagamento,
                Status = lp.Status,

                // Mapeia o Documento/Descrição e os nomes das entidades relacionadas
                Descricao = lp.LancamentoPai.Documento,
                NomePessoa = lp.LancamentoPai.Pessoa?.Nome ?? "Pessoa Desconhecida",
                NomeCategoria = lp.LancamentoPai.Tipo?.Nome ?? "Categoria Não Classificada",
            }).ToList();
        }

        /// <summary>
        /// Retorna o Saldo Atual da Conta Bancária especificada (usado como Saldo Anterior na simulação do Extrato).
        /// </summary>
        public async Task<decimal> GetSaldoAnteriorAsync(Guid? idConta, DateTime dataInicial)
        {
            if (!idConta.HasValue) return 0m;

            // Na implementação real do TechGreen, esta query seria ajustada para calcular o saldo
            // somando transações até dataInicial - 1 dia.

            // SIMULAÇÃO: Retorna o saldo atual para compilação e base de cálculo inicial.
            var conta = await _context.ContasBancarias.FindAsync(idConta.Value);
            return conta?.SaldoAtual ?? 0m;
        }
    }
}