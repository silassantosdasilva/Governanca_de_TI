using Governança_de_TI.Data;
using Governança_de_TI.Views.Services.Gamificacao;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Governança_de_TI.Models.Financeiro;
using Governança_de_TI.DTOs.Financeiro;
using Governança_de_TI.Models.Financeiro.TpLancamento;

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

    private async Task AtualizarSaldoConta(Guid idConta, decimal valor, int tipoLancamento, DateTime dataMovimento, bool estorno = false)
    {
        var conta = await _context.ContasBancarias.FindAsync(idConta);
        if (conta == null) return;

        // Tipo 2 (Receita) = Soma (+), Tipo 1 (Despesa) = Subtrai (-)
        decimal fator = (tipoLancamento == 2) ? 1 : -1;

        // Se for estorno, inverte o sinal
        if (estorno) fator *= -1;

        conta.SaldoAtual += (valor * fator);
        _context.ContasBancarias.Update(conta);

        // Efeito Cascata no SaldoDiario
        decimal delta = valor * fator;

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
                IdContaBaixa = null
            });

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
            IdSubcategoria = dto.IdSubcategoria, // <--- NOVO CAMPO
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
            Status = dto.Status
        };
    }

    private LancamentoDTO ToDTO(LancamentoFinanceiroModel entity)
    {
        return new LancamentoDTO
        {
            Id = entity.IdLancamento,
            TipoLancamento = entity.TipoLancamento,
            ValorOriginal = entity.ValorOriginal,
            Documento = entity.Documento,
            IdTipo = entity.IdTipo,
            IdSubcategoria = entity.IdSubcategoria 
        };
    }

    // ==========================================================
    // AÇÕES MVC (Views)
    // ==========================================================

    [HttpGet("/Lancamentos/CriarPartial")]
    public async Task<IActionResult> CriarPartial(int tipo = 1)
    {
        ViewBag.Contas = new SelectList(await _context.ContasBancarias.AsNoTracking().ToListAsync(), "IdConta", "NomeConta");
        ViewBag.Pessoas = new SelectList(await _context.Pessoas.AsNoTracking().ToListAsync(), "IdPessoa", "Nome");

        // Filtra categorias pelo tipo (Receita/Despesa)
        ViewBag.Categorias = new SelectList(await _context.TiposLancamento
            .AsNoTracking()
            .Where(c => c.Tipo == tipo)
            .ToListAsync(), "IdTipo", "Nome");

        var model = new LancamentoDTO
        {
            TipoLancamento = tipo,
            DataFluxo = DateTime.Today,
            DataEmissao = DateTime.Now,
            Status = 1 
        };

        return PartialView("~/Views/Lancamentos/_CriarPartial.cshtml", model);
    }

    // [NOVO] Endpoint para carregar Subcategorias via AJAX
    [HttpGet]
    public async Task<IActionResult> GetSubcategorias(Guid categoriaId)
    {
        var subcategorias = await _context.SubCategorias
            .AsNoTracking()
            .Where(s => s.IdTipo == categoriaId)
            .OrderBy(s => s.Nome)
            .Select(s => new { id = s.IdSubcategoria, nome = s.Nome })
            .ToListAsync();

        return Json(subcategorias);
    }

    // --- NOVOS ENDPOINTS DE CRIAÇÃO RÁPIDA ---

    [HttpPost("Lancamentos/SalvarCategoriaRapida")]
    public async Task<IActionResult> SalvarCategoriaRapida(string nome, int tipo)
    {
        if (string.IsNullOrWhiteSpace(nome)) return Json(new { success = false, message = "O nome da categoria é obrigatório." });

        try
        {
            var novaCategoria = new TipoLancamentoModel
            {
                IdTipo = Guid.NewGuid(),
                Nome = nome,
                Tipo = tipo
            };

            _context.TiposLancamento.Add(novaCategoria);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = novaCategoria.IdTipo, nome = novaCategoria.Nome });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Erro ao salvar: " + ex.Message });
        }
    }

    [HttpPost("Lancamentos/SalvarSubcategoriaRapida")]
    public async Task<IActionResult> SalvarSubcategoriaRapida(string nome, Guid idTipo)
    {
        if (string.IsNullOrWhiteSpace(nome)) return Json(new { success = false, message = "O nome da subcategoria é obrigatório." });
        if (idTipo == Guid.Empty) return Json(new { success = false, message = "Categoria pai não identificada." });

        try
        {
            var novaSub = new SubCategoriaModel
            {
                IdSubcategoria = Guid.NewGuid(),
                Nome = nome,
                IdTipo = idTipo
            };

            _context.SubCategorias.Add(novaSub);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = novaSub.IdSubcategoria, nome = novaSub.Nome });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Erro ao salvar: " + ex.Message });
        }
    }

    // ==========================================================
    // AÇÕES API RESTful
    // ==========================================================

    [HttpPost(ApiRoute)]
    public async Task<IActionResult> Criar([FromBody] LancamentoDTO dto)
    {
        var userId = await GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var lancamento = ToEntity(dto);
            lancamento.IdLancamento = Guid.NewGuid();

            // Regra: Se for à vista e vier marcado como pendente (0 no DTO padrão ou 1 se usou switch errado), forçamos pago se a lógica de negócio exigir.
            // Mas aqui mantemos flexível: só força pago se o usuário explicitamente não marcou pendente.
            if (dto.Condicao == 1 && dto.Status == 0) lancamento.Status = 2;

            _context.LancamentosFinanceiros.Add(lancamento);
            await _context.SaveChangesAsync();

            if (lancamento.Condicao == 2) // Parcelado
            {
                var parcelas = GerarParcelas(lancamento);
                _context.LancamentosParcelas.AddRange(parcelas);
            }
            else // À Vista
            {
                var parcelaUnica = new LancamentoParcelaModel
                {
                    IdParcela = Guid.NewGuid(),
                    IdLancamento = lancamento.IdLancamento,
                    NumeroParcela = 1,
                    ValorParcela = lancamento.ValorOriginal,
                    DataVencimento = lancamento.DataFluxo,
                    Status = lancamento.Status,
                    DataPagamento = (lancamento.Status == 2) ? lancamento.DataFluxo : null,
                    IdContaBaixa = (lancamento.Status == 2) ? lancamento.IdConta : null,
                    ValorPago = (lancamento.Status == 2) ? lancamento.ValorOriginal : 0m
                   
                };
                _context.LancamentosParcelas.Add(parcelaUnica);

                // Atualiza Saldo se já nasceu pago
                if (lancamento.Status == 2)
                {
                    await AtualizarSaldoConta(lancamento.IdConta, lancamento.ValorOriginal, lancamento.TipoLancamento, lancamento.DataFluxo);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

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

            parcela.Status = 2;
            parcela.DataPagamento = baixaDto.DataPagamento;
            parcela.IdContaBaixa = baixaDto.IdContaBaixa;
            parcela.ValorPago = baixaDto.ValorPago;

            _context.LancamentosParcelas.Update(parcela);

            await AtualizarSaldoConta(
                baixaDto.IdContaBaixa,
                baixaDto.ValorPago,
                parcela.LancamentoPai.TipoLancamento,
                baixaDto.DataPagamento
            );

            bool temPendencias = await _context.LancamentosParcelas
                .AnyAsync(p => p.IdLancamento == parcela.IdLancamento && p.IdParcela != id && p.Status != 2);

            if (!temPendencias)
            {
                parcela.LancamentoPai.Status = 2;
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

    [HttpPut($"{ApiRoute}/{{id}}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] LancamentoDTO dto)
    {
        if (id != dto.Id) return BadRequest(new { success = false, message = "ID incompatível." });
        var userId = await GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var lancamento = await _context.LancamentosFinanceiros.Include(l => l.Parcelas).FirstOrDefaultAsync(l => l.IdLancamento == id);
            if (lancamento == null) return NotFound(new { success = false, message = "Lançamento não encontrado." });

            if (lancamento.Parcelas.Any(p => p.Status == 2))
                return BadRequest(new { success = false, message = "Não é possível editar lançamentos com parcelas já pagas." });

            if (lancamento.Parcelas.Any()) _context.LancamentosParcelas.RemoveRange(lancamento.Parcelas);

            // Atualiza campos
            lancamento.IdPessoa = dto.IdPessoa; lancamento.IdConta = dto.IdConta; lancamento.IdTipo = dto.IdTipo;
            lancamento.IdSubcategoria = dto.IdSubcategoria; lancamento.ValorOriginal = dto.ValorOriginal;
            lancamento.Valor = dto.Valor; lancamento.Documento = dto.Documento; lancamento.Observacao = dto.Observacao;
            lancamento.DataEmissao = dto.DataEmissao; lancamento.DataFluxo = dto.DataFluxo;
            lancamento.FormaPagamento = dto.FormaPagamento; lancamento.Condicao = dto.Condicao;
            lancamento.NumeroParcelas = dto.NumeroParcelas; lancamento.IntervaloDias = dto.IntervaloDias;
            lancamento.Status = dto.Status;

            _context.LancamentosFinanceiros.Update(lancamento);
            await _context.SaveChangesAsync();

            // Recria Parcelas
            if (lancamento.Condicao == 2)
            {
                var novas = GerarParcelas(lancamento);
                if (lancamento.Status == 2) foreach (var p in novas) { p.Status = 2; p.ValorPago = p.ValorParcela; p.DataPagamento = lancamento.DataFluxo; p.IdContaBaixa = lancamento.IdConta; }
                _context.LancamentosParcelas.AddRange(novas);
            }
            else
            {
                var p = new LancamentoParcelaModel { IdParcela = Guid.NewGuid(), IdLancamento = lancamento.IdLancamento, NumeroParcela = 1, ValorParcela = lancamento.ValorOriginal, DataVencimento = lancamento.DataFluxo, Status = lancamento.Status, DataPagamento = (lancamento.Status == 2) ? lancamento.DataFluxo : null, IdContaBaixa = (lancamento.Status == 2) ? lancamento.IdConta : null, ValorPago = (lancamento.Status == 2) ? lancamento.ValorOriginal : 0m };
                _context.LancamentosParcelas.Add(p);
            }

            if (lancamento.Status == 2)
                await AtualizarSaldoConta(lancamento.IdConta, lancamento.ValorOriginal, lancamento.TipoLancamento, lancamento.DataFluxo);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _auditService.RegistrarAcao(userId.Value, "Editou Lançamento", $"ID: {id}");

            return Json(new { success = true, message = "Lançamento atualizado!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { success = false, message = "Erro ao editar: " + ex.Message });
        }
    }

    [HttpPatch($"{ApiRoute}/{{id}}/cancelar")]
    public async Task<IActionResult> CancelarLancamento(Guid id)
    {
        var userId = await GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var lancamento = await _context.LancamentosFinanceiros.Include(l => l.Parcelas).FirstOrDefaultAsync(l => l.IdLancamento == id);
            if (lancamento == null) return NotFound(new { success = false, message = "Lançamento não encontrado." });

            // SE O ERRO 400 ESTIVER ACONTECENDO AQUI, É PORQUE JÁ ESTÁ CANCELADO
            if (lancamento.Status == 3) return BadRequest(new { success = false, message = "Lançamento já está cancelado." });

            foreach (var parcela in lancamento.Parcelas.Where(p => p.Status == 2))
            {
                if (parcela.IdContaBaixa.HasValue && parcela.DataPagamento.HasValue)
                {
                    await AtualizarSaldoConta(parcela.IdContaBaixa.Value, parcela.ValorPago, lancamento.TipoLancamento, parcela.DataPagamento.Value, true);
                }
                parcela.Status = 3;
            }
            foreach (var parcela in lancamento.Parcelas.Where(p => p.Status == 1)) parcela.Status = 3;

            lancamento.Status = 3;
            _context.Update(lancamento);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _auditService.RegistrarAcao(userId.Value, "Cancelou Lançamento", $"ID: {id}");

            return Json(new { success = true, message = "Lançamento cancelado!" });
        }
        catch (Exception) // Não use 'ex', apenas a mensagem genérica
        {
            await transaction.RollbackAsync();
            // Mensagem genérica, ou use um logger para registrar 'ex'
            return BadRequest(new { success = false, message = "Erro crítico ao processar o cancelamento. Verifique os logs do servidor." });
        }
    }
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