using Governança_de_TI.Data;
using Governança_de_TI.DTOs; // Onde está o ExtratoViewModel
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ExtratoController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExtratoController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==========================================================
    // AÇÃO PRINCIPAL: DASHBOARD E EXTRATO (MVC)
    // ==========================================================
    [HttpGet("/Extrato/Consulta")]
    public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, string periodo = "mes")
    {
        // 1. DEFINIÇÃO DE DATAS (Regras de Filtros Rápidos)
        var hoje = DateTime.Today;
        DateTime inicio, fim;

        if (periodo == "hoje")
        {
            inicio = fim = hoje;
        }
        else if (periodo == "7dias")
        {
            inicio = hoje.AddDays(-7);
            fim = hoje;
        }
        else if (periodo == "ano")
        {
            inicio = new DateTime(hoje.Year, 1, 1);
            fim = new DateTime(hoje.Year, 12, 31);
        }
        else // Padrão: "mes" ou datas personalizadas
        {
            // Se vier datas no query param, usa elas. Senão, usa mês atual.
            inicio = dataInicio ?? new DateTime(hoje.Year, hoje.Month, 1);

            // Se dataFim não vier, define como último dia do mês da data de início
            fim = dataFim ?? inicio.AddMonths(1).AddDays(-1);
        }

        try
        {
            // 2. CARREGAR CONTAS (Para o Carrossel do Topo)
            var contas = await _context.ContasBancarias.AsNoTracking().ToListAsync();
            ViewBag.Contas = contas;

            // 3. KPI: SALDO PERÍODO ANTERIOR
            // Lógica: Busca o saldo de fechamento do dia anterior ao início do filtro (D-1)
            var diaAnterior = inicio.AddDays(-1);

            // Soma o saldo final de TODAS as contas no dia D-1
            // Usamos Subquery para garantir que pegamos o registro mais recente de cada conta até a data
            var saldoAnterior = await _context.ContasBancarias
                .AsNoTracking()
                .Select(c => _context.SaldosDiarios
                    .Where(s => s.IdConta == c.IdConta && s.Data <= diaAnterior)
                    .OrderByDescending(s => s.Data)
                    .Select(s => s.SaldoFinal)
                    .FirstOrDefault()
                )
                .SumAsync();

            // Fallback: Se o saldo for 0 e não houver histórico, assume o Saldo Inicial das contas
            if (saldoAnterior == 0 && !await _context.SaldosDiarios.AnyAsync(s => s.Data <= diaAnterior))
            {
                saldoAnterior = contas.Sum(c => c.SaldoInicial);
            }

            // 4. CONSULTA DE TRANSAÇÕES (PARCELAS)
            // Buscamos as parcelas que vencem (ou foram pagas) dentro do período
            // Include no Pai para ter acesso à Descrição, Categoria e Tipo
            var queryParcelas = _context.LancamentosParcelas
                .Include(p => p.LancamentoPai)
                .Include(p => p.LancamentoPai.Tipo)   // Categoria
                .Include(p => p.LancamentoPai.Conta)  // Conta Original
                .AsNoTracking()
                .Where(p =>
                    // Filtro por Vencimento (para previsão) OU Pagamento (para fluxo realizado)
                    // Aqui usamos Vencimento como padrão para mostrar tudo que está previsto no mês
                    p.DataVencimento >= inicio && p.DataVencimento <= fim
                );

            var listaParcelas = await queryParcelas
                .OrderByDescending(p => p.DataVencimento)
                .ToListAsync();

            // 5. CÁLCULO DE RECEITAS E DESPESAS (Cards Coloridos)
            // TipoLancamento: 2 = Receita, 1 = Despesa
            var receitas = listaParcelas
                .Where(p => p.LancamentoPai.TipoLancamento == 2)
                .Sum(p => p.Status == 2 ? p.ValorPago : p.ValorParcela); // Se pago usa valor real, senão nominal

            var despesas = listaParcelas
                .Where(p => p.LancamentoPai.TipoLancamento == 1)
                .Sum(p => p.Status == 2 ? p.ValorPago : p.ValorParcela);

            // 6. KPI: SALDO PREVISTO / FINAL (Card Azul Claro)
            decimal saldoFinalPeriodo;

            if (fim >= DateTime.Today)
            {
                // Se o período inclui o futuro, o saldo previsto é o Saldo Atual Real dos bancos
                // Somado ao que falta entrar/sair até o fim do período (Opcional: aqui pegamos o Saldo Atual puro conforme solicitado)
                saldoFinalPeriodo = (decimal)contas.Sum(c => c.SaldoAtual);
            }
            else
            {
                // Se é passado (histórico fechado), busca no SaldoDiario da data fim
                saldoFinalPeriodo = await _context.ContasBancarias
                    .AsNoTracking()
                    .Select(c => _context.SaldosDiarios
                        .Where(s => s.IdConta == c.IdConta && s.Data <= fim)
                        .OrderByDescending(s => s.Data)
                        .Select(s => s.SaldoFinal)
                        .FirstOrDefault()
                    )
                    .SumAsync();
            }

            // 7. DADOS PARA O GRÁFICO (Agrupamento)
            var dadosGrafico = listaParcelas
                .Where(p => p.LancamentoPai.TipoLancamento == 1) // Apenas Despesas
                .GroupBy(p => p.LancamentoPai.Tipo?.Nome?? "Sem Categoria")
                .ToDictionary(g => g.Key, g => g.Sum(p => p.ValorParcela));

            // 8. MONTAGEM DO VIEWMODEL
            var model = new ExtratoViewModel
            {
                DataInicio = inicio,
                DataFim = fim,
                FiltroPeriodo = periodo,

                // Cards
                SaldoPeriodoAnterior = saldoAnterior,
                ReceitasPeriodo = receitas,
                DespesasPeriodo = despesas,
                SaldoPrevisto = saldoFinalPeriodo,

                // Lista Principal
                Transacoes = listaParcelas,

                // Gráfico
                DespesasPorCategoria = dadosGrafico
            };

            return View("~/Views/Extrato/Consulta.cshtml",model);
        }
        catch (Exception ex)
        {
            // Em produção, use um logger. Aqui retornamos o erro na tela para debug se necessário.
            return Content($"Erro crítico ao carregar extrato: {ex.Message}");
        }
    }
}