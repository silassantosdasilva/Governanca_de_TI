// Services/ExtratoService.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Governança_de_TI.DTOs.Financeiro;

// Assumindo a interface IExtratoRepository para acesso a dados.
public class ExtratoService : IExtratoService
{
    private readonly IExtratoRepository _repository;

    public ExtratoService(IExtratoRepository repository)
    {
        _repository = repository;
    }

    // 5.6. Endpoint principal para consulta de extrato (6.4. Query consolidada)
    public async Task<ExtratoResponseDTO> FiltrarExtratoAsync(ExtratoRequestDTO request)
    {
        // 1. Simulação: Busca de Dados (Na implementação real, seria uma query LINQ complexa com JOINS e WHERE clauses)
        var itensFiltrados = await _repository.GetItensExtratoAsync(request);

        // 2. Simulação: Cálculo do Saldo Anterior (Deve ser baseado no SaldoAtual da conta anterior à data inicial do request)
        decimal saldoAnterior = await _repository.GetSaldoAnteriorAsync(request.IdConta, request.PeriodoInicial);

        // 3. 6.4. Somatórios
        // Total Receitas: Soma dos valores movimentados onde TipoLancamento é 2 (Receita)
        var totalReceitas = itensFiltrados
            .Where(i => i.TipoLancamento == 2)
            .Sum(i => i.ValorMovimentado);

        // Total Despesas: Soma dos valores movimentados onde TipoLancamento é 1 (Despesa)
        var totalDespesas = itensFiltrados
            .Where(i => i.TipoLancamento == 1)
            .Sum(i => i.ValorMovimentado);

        // Saldo Final: SaldoAnterior + Receitas - Despesas
        var saldoFinal = saldoAnterior + totalReceitas - totalDespesas;

        return new ExtratoResponseDTO
        {
            SaldoAnterior = saldoAnterior,
            TotalReceitas = totalReceitas,
            TotalDespesas = totalDespesas,
            SaldoFinal = saldoFinal,
            Itens = itensFiltrados.ToList()
        };
    }
}

// Repositório de apoio necessário para o Service
public interface IExtratoRepository
{
    // Método que simula a query complexa que consolida Lançamentos e Parcelas
    Task<IEnumerable<ExtratoItemDTO>> GetItensExtratoAsync(ExtratoRequestDTO request);

    // Método que busca o saldo da conta na data anterior ao período inicial.
    Task<decimal> GetSaldoAnteriorAsync(Guid? idConta, DateTime dataInicial);
}