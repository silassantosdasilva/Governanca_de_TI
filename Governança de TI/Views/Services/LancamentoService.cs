// Services/LancamentoService.cs - REESCRITO PARA ACESSAR O CONTEXTO DIRETAMENTE

using Governança_de_TI.DTOs;
using Governança_de_TI.Services; // IAuditService
using Governança_de_TI.Data; // ApplicationDbContext
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// NOTE: O enum TipoOperacao foi movido para o namespace Global ou para a classe Auxiliar.
public enum TipoOperacao { Credito, Debito }

public class LancamentoService : ILancamentoService
{
    private readonly ILancamentoRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ApplicationDbContext _context; // NOVO: Acesso direto ao Contexto DB

    public LancamentoService(
        ILancamentoRepository repository,
        // REMOVIDO: IContaBancariaService contaService,
        IAuditService auditService,
        ApplicationDbContext context) // NOVO: Contexto injetado
    {
        _repository = repository;
        _auditService = auditService;
        _context = context; // Contexto atribuído
    }

    // --- Métodos de Conversão (Omitidos por brevidade) ---
    private LancamentoFinanceiroModel ToEntity(LancamentoDTO dto)
    {
        // IMPLEMENTAÇÃO COMPLETA DO MAPEAMENTO
        return new LancamentoFinanceiroModel
        {
            IdLancamento = dto.Id,
            TipoLancamento = dto.TipoLancamento,
            IdPessoa = dto.IdPessoa,
            IdConta = dto.IdConta,
            IdTipo = dto.IdTipo,
            ValorOriginal = dto.ValorOriginal,
            Valor = dto.Valor,
            Documento = dto.Documento ?? string.Empty,
            Observacao = dto.Observacao ?? string.Empty,
            DataEmissao = dto.DataEmissao,
            DataFluxo = dto.DataFluxo,
            FormaPagamento = dto.FormaPagamento ?? string.Empty,
            Condicao = dto.Condicao,
            NumeroParcelas = dto.NumeroParcelas,
            IntervaloDias = dto.IntervaloDias,
            Status = dto.Status,
        };
    }

    private LancamentoDTO ToDTO(LancamentoFinanceiroModel entity)
    {
        // IMPLEMENTAÇÃO COMPLETA DO MAPEAMENTO
        return new LancamentoDTO
        {
            Id = entity.IdLancamento,
            TipoLancamento = entity.TipoLancamento,
            IdPessoa = entity.IdPessoa,
            IdConta = entity.IdConta,
            IdTipo = entity.IdTipo,
            ValorOriginal = entity.ValorOriginal,
            Valor = entity.Valor,
            Documento = entity.Documento,
            Observacao = entity.Observacao,
            DataEmissao = entity.DataEmissao,
            DataFluxo = entity.DataFluxo,
            FormaPagamento = entity.FormaPagamento,
            Condicao = entity.Condicao,
            NumeroParcelas = entity.NumeroParcelas,
            IntervaloDias = entity.IntervaloDias,
            Status = entity.Status,
            Parcelas = entity.Parcelas?.Select(ToDTO).ToList() ?? new List<LancamentoParcelaDTO>()
        };
    }

    private LancamentoParcelaDTO ToDTO(LancamentoParcelaModel entity)
    {
        return new LancamentoParcelaDTO
        {
            Id = entity.IdParcela,
            IdLancamento = entity.IdLancamento,
            NumeroParcela = entity.NumeroParcela,
            ValorParcela = entity.ValorParcela,
            DataVencimento = entity.DataVencimento,
            Status = entity.Status,
            DataPagamento = entity.DataPagamento,
            IdContaBaixa = entity.IdContaBaixa,
            ValorPago = entity.ValorPago
        };
    }

    // --- MÉTODOS DE REGRAS DE NEGÓCIO (Acesso ao Contexto) ---

    // Regra 6.2: Atualização de Saldo Automática (Lógica movida para cá)
    private async Task AtualizarSaldoConta(Guid idConta, decimal valor, int tipoLancamento, bool estorno = false)
    {
        // Esta lógica deve ser idêntica à que está no LancamentosController.Criar (mas mantida no service para reuso)
        var conta = await _context.ContasBancarias.FindAsync(idConta);
        if (conta == null) return;

        decimal fator = (tipoLancamento == 2) ? 1 : -1;
        if (estorno) fator *= -1;

        conta.SaldoAtual += (valor * fator);
        _context.ContasBancarias.Update(conta);

        // **IMPORTANTE**: O SaveChanges() NÃO é chamado aqui, ele é chamado pelo método Criar/PagarParcela
        // para garantir que tudo seja feito em uma única transação (BeginTransaction).
    }


    // Regra 6.3 / 4.2: Geração de Parcelas
    private List<LancamentoParcelaModel> GerarParcelas(LancamentoFinanceiroModel lancamento)
    {
        if (lancamento.Condicao == 1 || lancamento.NumeroParcelas <= 1)
        {
            return new List<LancamentoParcelaModel>();
        }

        var parcelas = new List<LancamentoParcelaModel>();
        var valorParcela = lancamento.ValorOriginal / lancamento.NumeroParcelas;
        var dataVenc = lancamento.DataFluxo;

        for (int i = 1; i <= lancamento.NumeroParcelas; i++)
        {
            decimal valorFinalParcela = (i == lancamento.NumeroParcelas)
                ? lancamento.ValorOriginal - parcelas.Sum(p => p.ValorParcela)
                : Math.Round(valorParcela, 2);

            parcelas.Add(new LancamentoParcelaModel
            {
                IdParcela = Guid.NewGuid(),
                IdLancamento = lancamento.IdLancamento,
                NumeroParcela = i,
                ValorParcela = valorFinalParcela,
                DataVencimento = dataVenc,
                Status = 1, // 1=Pendente
                ValorPago = 0m
            });

            if (i < lancamento.NumeroParcelas)
                dataVenc = dataVenc.AddDays(lancamento.IntervaloDias);
        }
        return parcelas;
    }

    // --- Implementação da Interface ILancamentoService ---

    // 7. Fluxo Técnico do Lançamento (Criação)
    public async Task<LancamentoDTO> CreateAsync(LancamentoDTO dto, int userId)
    {
        // 1. Validar Request (simplificado)
        if (dto.ValorOriginal <= 0) throw new ArgumentException("O valor do lançamento deve ser positivo.");

        var lancamento = ToEntity(dto);
        lancamento.IdLancamento = Guid.NewGuid();
        lancamento.DataEmissao = DateTime.Now;
        lancamento.Status = (dto.Condicao == 1) ? 2 : 1;

        // O CONTROLLER DEVE GERENCIAR A TRANSAÇÃO (BeginTransaction/Commit/Rollback)
        // O Service foca na lógica e na manipulação do DbSets.

        // 2. Criar registro em LancamentoFinanceiro
        await _repository.AddLancamentoAsync(lancamento);

        // 3. Se parcelado → criar registros em LancamentoParcela
        var parcelas = GerarParcelas(lancamento);
        lancamento.Parcelas = parcelas;
        if (parcelas.Any())
        {
            await _repository.AddParcelasAsync(parcelas);
        }

        // 4. Atualizar saldo em ContaBancaria (Apenas se à vista e já pago)
        if (lancamento.Condicao == 1 && lancamento.Status == 2)
        {
            var operacao = (lancamento.TipoLancamento == 1) ? TipoOperacao.Debito : TipoOperacao.Credito;
            await AtualizarSaldoConta(lancamento.IdConta, lancamento.Valor, (int)operacao);

            // Se for à vista, a parcela única precisa ser marcada como paga/recebida
            var parcelaUnica = new LancamentoParcelaModel
            {
                IdParcela = Guid.NewGuid(),
                IdLancamento = lancamento.IdLancamento,
                NumeroParcela = 1,
                ValorParcela = lancamento.Valor,
                DataVencimento = lancamento.DataFluxo,
                Status = 2,
                DataPagamento = DateTime.Now,
                IdContaBaixa = lancamento.IdConta,
                ValorPago = lancamento.Valor
            };
            await _repository.AddParcelaAsync(parcelaUnica);
        }

        // REGISTRO DE LOG (Auditoria)
        await _auditService.RegistrarAcao(
            userId,
            "Criou Lançamento Financeiro",
            $"ID: {lancamento.IdLancamento}, Tipo: {(lancamento.TipoLancamento == 1 ? "Despesa" : "Receita")}"
        );

        return ToDTO(lancamento);
    }

    // Implementação de Pagar Parcela (Ação Crítica)
    public async Task<LancamentoParcelaDTO> PagarParcelaAsync(Guid idParcela, BaixaDTO baixaDto, int userId)
    {
        var parcela = await _repository.GetParcelaByIdAsync(idParcela) ?? throw new KeyNotFoundException("Parcela não encontrada.");
        if (parcela.Status == 2) throw new InvalidOperationException("Esta parcela já está paga.");

        // 1. Atualiza os dados da parcela
        parcela.Status = 2; // Pago
        parcela.DataPagamento = baixaDto.DataPagamento;
        parcela.IdContaBaixa = baixaDto.IdContaBaixa;
        parcela.ValorPago = baixaDto.ValorPago;

        await _repository.UpdateParcelaAsync(parcela);

        // 2. Atualiza o saldo da conta de baixa
        var lancamentoPai = await _repository.GetLancamentoByIdAsync(parcela.IdLancamento) ?? throw new KeyNotFoundException("Lançamento pai não encontrado.");

        var operacao = (lancamentoPai.TipoLancamento == 1) ? TipoOperacao.Debito : TipoOperacao.Credito;
        await AtualizarSaldoConta(baixaDto.IdContaBaixa, baixaDto.ValorPago, (int)operacao);

        // 3. Verifica se todas as parcelas foram pagas
        var todasPagas = await _repository.AreAllParcelasPaid(parcela.IdLancamento);
        if (todasPagas)
        {
            lancamentoPai.Status = 2; // Marca o lançamento principal como Pago
            await _repository.UpdateLancamentoAsync(lancamentoPai);
        }

        // REGISTRO DE LOG
        await _auditService.RegistrarAcao(
            userId,
            "Pagou Parcela",
            $"ID Parcela: {idParcela}, Lançamento Pai: {parcela.IdLancamento}, Valor: {baixaDto.ValorPago:C2}"
        );

        return ToDTO(parcela);
    }

    // Implementação dos demais métodos (GetByIdAsync, GetAllAsync, UpdateAsync, DeleteAsync, CancelarLancamentoAsync)
    public Task<LancamentoDTO> GetByIdAsync(Guid id) => throw new NotImplementedException();
    public Task<IEnumerable<LancamentoDTO>> GetAllAsync() => throw new NotImplementedException();
    public Task<LancamentoDTO> UpdateAsync(Guid id, LancamentoDTO dto, int userId) => throw new NotImplementedException();
    public Task DeleteAsync(Guid id, int userId) => throw new NotImplementedException();
    public Task CancelarLancamentoAsync(Guid idLancamento, int userId) => throw new NotImplementedException();
}

// Repositórios de apoio (necessários para o Service)
public interface ILancamentoRepository
{
    Task<LancamentoFinanceiroModel> GetLancamentoByIdAsync(Guid id);
    Task AddLancamentoAsync(LancamentoFinanceiroModel lancamento);
    Task UpdateLancamentoAsync(LancamentoFinanceiroModel lancamento);
    // ... CRUD Lancamento Financeiro ...

    Task AddParcelasAsync(IEnumerable<LancamentoParcelaModel> parcelas);
    Task AddParcelaAsync(LancamentoParcelaModel parcela); // Para lançamentos à vista
    Task<LancamentoParcelaModel> GetParcelaByIdAsync(Guid id);
    Task UpdateParcelaAsync(LancamentoParcelaModel parcela);
    Task<bool> AreAllParcelasPaid(Guid idLancamento);
    // ... CRUD Lancamento Parcela ...
}