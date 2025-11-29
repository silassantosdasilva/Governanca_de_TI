using Governança_de_TI.Data;
using Governança_de_TI.Models.Financeiro;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

public class SaldoDiarioController : Controller
{
    private readonly ApplicationDbContext _context;

    public SaldoDiarioController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // 1. TELA DE GERENCIAMENTO (INDEX)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Index(Guid? idConta, int? mes, int? ano) // <-- idConta ajustado para Guid?
    {
        var mesAtual = mes ?? DateTime.Now.Month;
        var anoAtual = ano ?? DateTime.Now.Year;

        var query = _context.SaldosDiarios
            .Include(s => s.ContaBancaria)
            .AsNoTracking()
            .Where(s => s.Data.Month == mesAtual && s.Data.Year == anoAtual);

        if (idConta.HasValue)
        {
            query = query.Where(s => s.IdConta == idConta.Value); // GUID == GUID (OK)
        }

        ViewBag.Contas = await _context.ContasBancarias.ToListAsync();
        ViewBag.MesAtual = mesAtual;
        ViewBag.AnoAtual = anoAtual;
        ViewBag.ContaAtual = idConta;

        var lista = await query.OrderByDescending(s => s.Data).ToListAsync();
        return View(lista);
    }

    // ============================================================
    // 2. AÇÃO: SALVAR AJUSTE MANUAL
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarAjuste(Guid idConta, DateTime data, decimal novoSaldo) // <--- INT
    {
        try
        {
            var saldoExistente = await _context.SaldosDiarios
                // LINQ: int == int (Assume que SaldoDiarioModel.IdConta foi corrigido para INT)
                .FirstOrDefaultAsync(s => s.IdConta == idConta && s.Data.Date == data.Date);

            decimal diferenca;

            if (saldoExistente != null)
            {
                diferenca = novoSaldo - saldoExistente.SaldoFinal;
                saldoExistente.SaldoFinal = novoSaldo;
                _context.Update(saldoExistente);
            }
            else
            {
                var ultimoSaldoAnterior = await _context.SaldosDiarios
                    .Where(s => s.IdConta == idConta && s.Data < data)
                    .OrderByDescending(s => s.Data)
                    .Select(s => s.SaldoFinal)
                    .FirstOrDefaultAsync();

                diferenca = novoSaldo - ultimoSaldoAnterior;

                var novoRegistro = new SaldoDiarioModel
                {
                    IdConta = idConta, // int = int (OK)
                    Data = data,
                    SaldoFinal = novoSaldo
                };
                _context.Add(novoRegistro);
            }

            if (diferenca != 0)
            {
                // Execução SQL: O EF Core trata os parâmetros {0}, {1} como os tipos fornecidos (decimal, int, datetime)
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE SaldoDiario SET SaldoFinal = SaldoFinal + {0} WHERE IdConta = {1} AND Data > {2}",
                    diferenca, idConta, data
                );
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Saldo ajustado com sucesso!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Erro ao ajustar: " + ex.Message;
        }

        // Retorno: Usamos os valores da data para manter o filtro
        return RedirectToAction(nameof(Index), new { idConta = idConta, mes = data.Month, ano = data.Year });
    }
    // ============================================================
    // 3. AÇÃO: RECALCULAR TUDO
    // ============================================================
    // Controllers/SaldoDiarioController.cs

    // ... (Outros métodos) ...

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecalcularPeriodo(Guid idConta, DateTime dataInicio, DateTime dataFinal)
    {
        try
        {
            // 1. Validação Básica
            if (dataFinal < dataInicio)
                return Json(new { success = false, message = "A data final deve ser maior que a inicial." });

            // 2. DESCOBRIR O SALDO BASE (Ponto de Partida)
            // Busca o saldo no dia imediatamente anterior à data inicial do recalculo (D-1).
            var diaAnterior = dataInicio.AddDays(-1);

            // Tenta pegar o último registro de saldo fechado (snapshot)
            var ultimoSaldoRegistro = await _context.SaldosDiarios
                .Where(s => s.IdConta == idConta && s.Data <= diaAnterior)
                .OrderByDescending(s => s.Data)
                .FirstOrDefaultAsync();

            decimal saldoAcumulado = 0;

            if (ultimoSaldoRegistro != null)
            {
                // Usa o último saldo snapshot encontrado como ponto de partida
                saldoAcumulado = ultimoSaldoRegistro.SaldoFinal;
            }
            else
            {
                // Fallback: Se não houver histórico de SaldoDiario, usa o SaldoInicial cadastrado na Conta Bancária.
                var conta = await _context.ContasBancarias.FindAsync(idConta);
                if (conta != null) saldoAcumulado = conta.SaldoInicial;
            }

            // 3. CARREGAR MOVIMENTAÇÕES DO PERÍODO (Performance e Regra de Negócio)
            // Busca TODAS as parcelas pagas no período (Status == 2)
            var transacoesPeriodo = await _context.LancamentosParcelas
                .Include(p => p.LancamentoPai) // Necessário para obter o Tipo (Receita/Despesa)
                .Where(p =>
                    // Filtra pelo ID da conta (seja na baixa ou na conta original do pai)
                    ((p.IdContaBaixa == idConta) || (p.LancamentoPai.IdConta == idConta && p.IdContaBaixa == null)) &&
                    p.Status == 2 && // CRÍTICO: Apenas movimentos efetivamente PAGOS/RECEBIDOS
                    p.DataPagamento >= dataInicio && p.DataPagamento <= dataFinal
                )
                .ToListAsync();

            // 4. LIMPAR SALDOS ANTIGOS DO PERÍODO
            // Remove registros existentes para evitar duplicidade ou conflito ao reescrever a história.
            var saldosAntigos = await _context.SaldosDiarios
                .Where(s => s.IdConta == idConta && s.Data >= dataInicio && s.Data <= dataFinal)
                .ToListAsync();

            if (saldosAntigos.Any())
            {
                _context.SaldosDiarios.RemoveRange(saldosAntigos);
                await _context.SaveChangesAsync();
            }

            // 5. LOOP DIA A DIA (RECONSTRUÇÃO HISTÓRICA)
            var dataCursor = dataInicio;
            var novosSaldos = new List<SaldoDiarioModel>();

            while (dataCursor <= dataFinal)
            {
                // Filtra transações deste dia específico na memória
                var movsDoDia = transacoesPeriodo
                    .Where(m => m.DataPagamento.HasValue && m.DataPagamento.Value.Date == dataCursor.Date)
                    .ToList();

                // Aplica movimentações ao saldo acumulado
                foreach (var item in movsDoDia)
                {
                    // Verifica o Tipo do Lançamento Pai
                    if (item.LancamentoPai.TipoLancamento == 2) // 2 = Receita
                        saldoAcumulado += item.ValorPago;
                    else // 1 = Despesa
                        saldoAcumulado -= item.ValorPago;
                }

                // Cria o registro de fechamento do dia (Snapshot)
                novosSaldos.Add(new SaldoDiarioModel
                {
                    IdConta = idConta,
                    Data = dataCursor,
                    SaldoFinal = saldoAcumulado
                });

                dataCursor = dataCursor.AddDays(1);
            }

            // 6. PERSISTÊNCIA E RETORNO
            await _context.SaldosDiarios.AddRangeAsync(novosSaldos);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Saldos do período recalculados com sucesso!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Erro ao recalcular: " + ex.Message });
        }
    }
}
