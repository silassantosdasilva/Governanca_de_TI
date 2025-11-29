// Repositories/LancamentoRepository.cs

using Governança_de_TI.Data;
using Governança_de_TI.Models.Financeiro;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Repositories
{
    // Implementa a interface ILancamentoRepository
    public class LancamentoRepository : GenericRepository<LancamentoFinanceiroModel>, ILancamentoRepository
    {
        public LancamentoRepository(ApplicationDbContext context) : base(context) { }

        // Implementações de ILancamentoRepository:
        public Task<LancamentoFinanceiroModel> GetLancamentoByIdAsync(Guid id) => GetByIdAsync(id);
        public Task AddLancamentoAsync(LancamentoFinanceiroModel lancamento) => AddAsync(lancamento);
        public Task UpdateLancamentoAsync(LancamentoFinanceiroModel lancamento) => UpdateAsync(lancamento);

        public async Task AddParcelasAsync(IEnumerable<LancamentoParcelaModel> parcelas)
        {
            await _context.LancamentosParcelas.AddRangeAsync(parcelas);
            await _context.SaveChangesAsync();
        }

        public async Task AddParcelaAsync(LancamentoParcelaModel parcela)
        {
            await _context.LancamentosParcelas.AddAsync(parcela);
            await _context.SaveChangesAsync();
        }

        public Task<LancamentoParcelaModel> GetParcelaByIdAsync(Guid id)
        {
            return _context.LancamentosParcelas
                .Include(lp => lp.LancamentoPai) // Necessário para acessar TipoLancamento na baixa
                .FirstOrDefaultAsync(lp => lp.IdParcela == id);
        }

        public async Task UpdateParcelaAsync(LancamentoParcelaModel parcela)
        {
            _context.Entry(parcela).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AreAllParcelasPaid(Guid idLancamento)
        {
            // Verifica se existe *alguma* parcela que AINDA está Pendente (Status != 2)
            return !await _context.LancamentosParcelas
                .AnyAsync(lp => lp.IdLancamento == idLancamento && lp.Status != 2);
        }
    }
}