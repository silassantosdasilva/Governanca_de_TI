// Repositories/PessoaRepository.cs

using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Repositories
{
    // Implementa a interface IPessoaRepository
    public class PessoaRepository : GenericRepository<PessoaModel>
    {
        public PessoaRepository(ApplicationDbContext context) : base(context) { }

        /// <summary>
        /// Calcula o saldo pendente (a pagar ou a receber) para uma pessoa específica.
        /// Regra 3.1: Soma ValorParcela onde Status=1 (Pendente) e TipoLancamento corresponde ao pedido.
        /// </summary>
        /// <param name="idPessoa">ID do Cliente/Fornecedor.</param>
        /// <param name="tipoLancamento">1 = Despesa (A Pagar), 2 = Receita (A Receber).</param>
        /// <returns>O valor decimal do saldo pendente.</returns>
        public async Task<decimal> CalcularSaldoPendenteAsync(Guid idPessoa, int tipoLancamento)
        {
            // O tipoLancamento é 1=Despesa (A Pagar) ou 2=Receita (A Receber).

            // Query LINQ: Busca todas as parcelas pendentes para a Pessoa específica,
            // filtrando pelo TipoLancamento (Receita ou Despesa).
            var saldo = await _context.LancamentosParcelas
                // Inclui o Lançamento Pai para acessar a FK da Pessoa e o TipoLancamento
                .Include(lp => lp.LancamentoPai)

                // Filtros Obrigatórios:
                .Where(lp => lp.Status == 1) // 1=Apenas parcelas Pendentes
                .Where(lp => lp.LancamentoPai.IdPessoa == idPessoa) // Relacionado à pessoa consultada
                .Where(lp => lp.LancamentoPai.TipoLancamento == tipoLancamento) // Receita (2) ou Despesa (1)

                // Executa a soma dos valores das parcelas
                .SumAsync(lp => lp.ValorParcela);

            return saldo;
        }

        // Os métodos de CRUD (GetByIdAsync, GetAllAsync, AddAsync, etc.) são herdados do GenericRepository.
    }
}