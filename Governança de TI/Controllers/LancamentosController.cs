using Governança_de_TI.Data;
using Governança_de_TI.Views.Services.Gamificacao;
using Governança_de_TI.DTOs;
using Governança_de_TI.Services;
using Governança_de_TI.Models; // Certifique-se de ter os usings corretos para seus Models
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

public class LancamentosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IGamificacaoService _gamificacaoService;

    // Prefixo da API para chamadas AJAX
    private const string ApiRoute = "/api/financeiro/lancamentos";

    public LancamentosController(
        ApplicationDbContext context,
        IAuditService auditService,
        IGamificacaoService gamificacaoService)
    {
        _context = context;
        _auditService = auditService;
        _gamificacaoService = gamificacaoService;
    }

    // --- MÉTODOS AUXILIARES E DE NEGÓCIO ---

    private async Task<int?> GetCurrentUserId()
    {
        var userEmail = User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userEmail)) return null;
        var user = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);
        return user?.Id;
    }

    /// <summary>
    /// Atualiza o Saldo Atual da Conta e propaga a diferença para o Histórico de Saldos Diários (Efeito Cascata).
    /// </summary>
    private async Task AtualizarSaldoConta(Guid idConta, decimal valor, int tipoLancamento, DateTime dataMovimento, bool estorno = false)
    {
        var conta = await _context.ContasBancarias.FindAsync(idConta);
        if (conta == null) return;

        // 1. Define o Fator (+ ou -)
        // Tipo 2 (Receita) = Soma (+), Tipo 1 (Despesa) = Subtrai (-)
        decimal fator = (tipoLancamento == 2) ? 1 : -1;

        // Se for estorno (cancelamento), inverte o sinal
        if (estorno) fator *= -1;

        // 2. Atualiza o Saldo Atual (Instantâneo) na tabela ContaBancaria
        conta.SaldoAtual += (valor * fator);
        _context.ContasBancarias.Update(conta);

        // 3. Efeito Cascata no SaldoDiario
        // Calcula o delta (valor exato da mudança)
        decimal delta = valor * fator;

        // Atualiza todos os registros de saldo diário a partir da data do movimento
        // Isso garante que o histórico futuro reflita a mudança retroativa
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE SaldoDiario SET SaldoFinal = SaldoFinal + {0} WHERE IdConta = {1} AND Data >= {2}",
            delta, idConta, dataMovimento.Date
        );
    }

    private List<LancamentoParcelaModel> GerarParcelas(LancamentoFinanceiroModel lancamento)
    {
        var parcelas = new List<LancamentoParcelaModel>();
        var valorParcela = lancamento.ValorOriginal / lancamento.NumeroParcelas;
        var dataVenc = lancamento.DataFluxo;

        for (int i = 1; i <= lancamento.NumeroParcelas; i++)
        {
            // Ajuste de centavos na última parcela
            decimal valorFinal = (i == lancamento.NumeroParcelas)
                ? lancamento.ValorOriginal - parcelas.Sum(p => p.ValorParcela)
                : Math.Round(valorParcela, 2);

            parcelas.Add(new LancamentoParcelaModel
            {
                IdParcela = Guid.NewGuid(),
                IdLancamento = lancamento.IdLancamento,
                NumeroParcela = i,
                ValorParcela = valorFinal,
                DataVencimento = dataVenc,
                Status = 1, // 1 = Pendente
                ValorPago = 0m,
                IdContaBaixa = null // Ainda não pago
            });

            // Incrementa a data para a próxima parcela
            if (i < lancamento.NumeroParcelas)
                dataVenc = dataVenc.AddDays(lancamento.IntervaloDias > 0 ? lancamento.IntervaloDias : 30);
        }
        return parcelas;
    }

    private LancamentoFinanceiroModel ToEntity(LancamentoDTO dto)
    {
        return new LancamentoFinanceiroModel
        {
            IdLancamento = dto.Id,
            TipoLancamento = dto.TipoLancamento,
            IdPessoa = dto.IdPessoa,
            IdConta = dto.IdConta,
            IdTipo = dto.IdTipo,
            ValorOriginal = dto.ValorOriginal,
            Valor = dto.Valor, // Pode ser igual ao original inicialmente
            Documento = dto.Documento ?? string.Empty,
            Observacao = dto.Observacao ?? string.Empty,
            DataEmissao = dto.DataEmissao,
            DataFluxo = dto.DataFluxo,
            FormaPagamento = dto.FormaPagamento ?? string.Empty,
            Condicao = dto.Condicao,
            NumeroParcelas = dto.NumeroParcelas,
            IntervaloDias = dto.IntervaloDias,
            Status = dto.Status
        };
    }

    private LancamentoDTO ToDTO(LancamentoFinanceiroModel entity)
    {
        return new LancamentoDTO
        {
            Id = entity.IdLancamento,
            TipoLancamento = entity.TipoLancamento,
            // ... outros campos mapeados conforme necessidade da tela de edição
            ValorOriginal = entity.ValorOriginal,
            Documento = entity.Documento
        };
    }

    // ==========================================================
    // AÇÕES MVC (Retorno de Views/Partials)
    // ==========================================================

    // GET: /Lancamentos/CriarPartial?tipo=2
    // Carrega o modal de criação já configurado para Receita (2) ou Despesa (1)
    [HttpGet("/Lancamentos/CriarPartial")]
    public async Task<IActionResult> CriarPartial(int tipo = 1)
    {
        // Popula as listas para os Dropdowns
        ViewBag.Contas = new SelectList(await _context.ContasBancarias.AsNoTracking().ToListAsync(), "IdConta", "NomeConta");
        ViewBag.Pessoas = new SelectList(await _context.Pessoas.AsNoTracking().ToListAsync(), "IdPessoa", "Nome");
        ViewBag.Categorias = new SelectList(await _context.TiposLancamento.AsNoTracking().ToListAsync(), "IdTipo", "Descricao");

        // Inicia o modelo
        var model = new LancamentoDTO
        {
            TipoLancamento = tipo,
            DataFluxo = DateTime.Today,
            DataEmissao = DateTime.Now,
            Status = 1 // Padrão: Pendente
        };

        return PartialView("~/Views/Lancamentos/_CriarPartial.cshtml", model);
    }

    // ==========================================================
    // AÇÕES API RESTful (CRUD e Processamento)
    // ==========================================================

    // POST: /api/financeiro/lancamentos
    [HttpPost(ApiRoute)]
    public async Task<IActionResult> Criar([FromBody] LancamentoDTO dto)
    {
        var userId = await GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Cria o Lançamento Pai
            var lancamento = ToEntity(dto);
            lancamento.IdLancamento = Guid.NewGuid();

            // Se for à vista (1), status inicial é Pago (2), senão Pendente (1)
            // Nota: Se o usuário marcou "Não Pago" no formulário (Status=1) mesmo sendo à vista, respeitamos.
            // Mas geralmente à vista nasce pago. Vamos assumir a lógica do DTO.
            if (dto.Condicao == 1 && dto.Status == 0) lancamento.Status = 2;

            _context.LancamentosFinanceiros.Add(lancamento);
            await _context.SaveChangesAsync();

            // 2. Gera Parcelas
            if (lancamento.Condicao == 2) // Parcelado
            {
                var parcelas = GerarParcelas(lancamento);
                _context.LancamentosParcelas.AddRange(parcelas);
            }
            else // À Vista (Cria uma única parcela)
            {
                var parcelaUnica = new LancamentoParcelaModel
                {
                    IdParcela = Guid.NewGuid(),
                    IdLancamento = lancamento.IdLancamento,
                    NumeroParcela = 1,
                    ValorParcela = lancamento.ValorOriginal,
                    DataVencimento = lancamento.DataFluxo,

                    // Se o pai nasceu pago, a parcela nasce paga
                    Status = lancamento.Status,

                    // Se pago, preenche dados de baixa
                    DataPagamento = (lancamento.Status == 2) ? lancamento.DataFluxo : null,
                    IdContaBaixa = (lancamento.Status == 2) ? lancamento.IdConta : null,
                    ValorPago = (lancamento.Status == 2) ? lancamento.ValorOriginal : 0m
                };
                _context.LancamentosParcelas.Add(parcelaUnica);

                // 3. Atualiza Saldo (Apenas se já nasceu pago)
                if (lancamento.Status == 2)
                {
                    await AtualizarSaldoConta(lancamento.IdConta, lancamento.ValorOriginal, lancamento.TipoLancamento, lancamento.DataFluxo);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 4. Auditoria e Gamificação
            await _auditService.RegistrarAcao(userId.Value, "Criou Lançamento", $"ID: {lancamento.IdLancamento}, Doc: {lancamento.Documento}");
            await _gamificacaoService.AdicionarPontosAsync(userId.Value, "CriouLancamento", 5);

            return Json(new
            {
                success = true,
                message = "Lançamento registrado com sucesso!",
                notificacao = new { titulo = "Gamificação", texto = "+5 pontos!", tipo = "success" }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { success = false, message = "Erro ao criar lançamento: " + ex.Message });
        }
    }

    // PATCH: /api/financeiro/lancamentos/parcelas/{id}/pagar
    [HttpPatch($"{ApiRoute}/parcelas/{{id}}/pagar")]
    public async Task<IActionResult> PagarParcela(Guid id, [FromBody] BaixaDTO baixaDto)
    {
        var userId = await GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var parcela = await _context.LancamentosParcelas
                .Include(p => p.LancamentoPai)
                .FirstOrDefaultAsync(p => p.IdParcela == id);

            if (parcela == null) return NotFound(new { success = false, message = "Parcela não encontrada." });
            if (parcela.Status == 2) return BadRequest(new { success = false, message = "Parcela já está paga." });

            // 1. Efetua a Baixa na Parcela
            parcela.Status = 2; // Pago
            parcela.DataPagamento = baixaDto.DataPagamento;
            parcela.IdContaBaixa = baixaDto.IdContaBaixa;
            parcela.ValorPago = baixaDto.ValorPago;

            _context.LancamentosParcelas.Update(parcela);

            // 2. Atualiza Saldo da Conta de Baixa + Histórico Diário
            await AtualizarSaldoConta(
                baixaDto.IdContaBaixa,
                baixaDto.ValorPago,
                parcela.LancamentoPai.TipoLancamento,
                baixaDto.DataPagamento
            );

            // 3. Verifica se fecha o Lançamento Pai (se todas as parcelas estão pagas)
            bool temPendencias = await _context.LancamentosParcelas
                .AnyAsync(p => p.IdLancamento == parcela.IdLancamento && p.IdParcela != id && p.Status != 2);

            if (!temPendencias)
            {
                parcela.LancamentoPai.Status = 2; // Finalizado
                _context.LancamentosFinanceiros.Update(parcela.LancamentoPai);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.RegistrarAcao(userId.Value, "Pagou Parcela", $"ID: {id}, Valor: {baixaDto.ValorPago}");
            await _gamificacaoService.AdicionarPontosAsync(userId.Value, "QuitouParcela", 10);

            return Json(new { success = true, message = "Baixa realizada com sucesso!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { success = false, message = "Erro na baixa: " + ex.Message });
        }
    }

    // PATCH: /api/financeiro/lancamentos/{id}/cancelar
    [HttpPatch($"{ApiRoute}/{{id}}/cancelar")]
    public async Task<IActionResult> CancelarLancamento(Guid id)
    {
        var userId = await GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var lancamento = await _context.LancamentosFinanceiros
                .Include(l => l.Parcelas)
                .FirstOrDefaultAsync(l => l.IdLancamento == id);

            if (lancamento == null) return NotFound(new { success = false, message = "Lançamento não encontrado." });
            if (lancamento.Status == 3) return BadRequest(new { success = false, message = "Já está cancelado." });

            // 1. Reverte Saldos das parcelas que já foram pagas (Estorno)
            foreach (var parcela in lancamento.Parcelas.Where(p => p.Status == 2))
            {
                if (parcela.IdContaBaixa.HasValue && parcela.DataPagamento.HasValue)
                {
                    // ESTORNO: Passa true e a data original do pagamento para corrigir o histórico
                    await AtualizarSaldoConta(
                        parcela.IdContaBaixa.Value,
                        parcela.ValorPago,
                        lancamento.TipoLancamento,
                        parcela.DataPagamento.Value,
                        true // estorno = true
                    );
                }
                parcela.Status = 3; // Cancelado
            }

            // 2. Cancela parcelas pendentes
            foreach (var parcela in lancamento.Parcelas.Where(p => p.Status == 1))
            {
                parcela.Status = 3;
            }

            // 3. Cancela Pai
            lancamento.Status = 3;
            _context.Update(lancamento);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.RegistrarAcao(userId.Value, "Cancelou Lançamento", $"ID: {id}");

            return Json(new { success = true, message = "Lançamento cancelado e saldos revertidos." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { success = false, message = "Erro ao cancelar: " + ex.Message });
        }
    }

    // GET: /api/financeiro/lancamentos/{id}
    [HttpGet($"{ApiRoute}/{{id}}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var lancamento = await _context.LancamentosFinanceiros
            .Include(l => l.Parcelas)
            .FirstOrDefaultAsync(l => l.IdLancamento == id);

        if (lancamento == null) return NotFound();

        return Json(new { success = true, data = ToDTO(lancamento) });
    }
}