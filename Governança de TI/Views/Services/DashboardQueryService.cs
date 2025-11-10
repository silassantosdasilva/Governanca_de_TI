using Governança_de_TI.Controllers.Api;
using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Services
{
    // ============================================================
    // ⚙️ SERVIÇO CENTRAL DE CONSULTAS DE DASHBOARD
    // ============================================================
    //
    // Este serviço processa todas as consultas dinâmicas solicitadas
    // pelo front-end para geração de widgets, KPIs e gráficos.
    //
    // Ele substitui as regras manuais do controller antigo, aplicando
    // filtros automaticamente e garantindo robustez e logs de erros.
    //
    // 🔧 INTERVENÇÃO FUTURA:
    // Caso adicione novas tabelas, basta incluir um novo case no switch
    // da função "ExecutarConsultaAsync".
    // ============================================================
    public static class DashboardQueryService
    {
        // ============================================================
        // 🔹 MÉTODO PRINCIPAL: ExecutarConsultaAsync()
        // ============================================================
        //
        // Recebe o contexto do banco e o modelo de requisição
        // vindo do front-end, decide qual tabela consultar e retorna
        // um conjunto de dados pronto para o gráfico.
        // ============================================================
        public static async Task<object> ExecutarConsultaAsync(ApplicationDbContext _context, DashboardQueryRequest req)
        {
            try
            {
                switch (req.Tabela)
                {
                    // =====================================================
                    // 🧩 TABELA: EQUIPAMENTOS
                    // =====================================================
                    case "Equipamentos":
                        return await ConsultarEquipamentos(_context, req);

                    // =====================================================
                    // 🧩 TABELA: DESCARTES
                    // =====================================================
                    case "Descartes":
                        return await ConsultarDescartes(_context, req);

                    // =====================================================
                    // 🧩 TABELA: CONSUMO DE ENERGIA
                    // =====================================================
                    case "ConsumoEnergia":
                        return await ConsultarConsumoEnergia(_context, req);

                    // =====================================================
                    // 🧩 NOVAS TABELAS
                    // =====================================================
                    //
                    // 🔧 INTERVENÇÃO FUTURA:
                    // Adicione aqui novos cases para tabelas criadas depois,
                    // como "Financeiro", "ContasPagar", "Receitas", etc.
                    //
                    // Cada case deve chamar um método de consulta separado.
                    // =====================================================
                    default:
                        return new { message = $"Tabela '{req.Tabela}' ainda não tem suporte no serviço." };
                }
            }
            catch (Exception ex)
            {
                // =====================================================
                // 🧾 REGISTRO DE LOG DE ERROS (para tela de Log futura)
                // =====================================================
                try
                {
                    await LogService.Gravar(_context, "DashboardQueryService", "Erro", "Falha ao processar dados do dashboard.", ex.ToString());
                }
                catch { /* fallback para evitar crash caso o log falhe */ }

                Console.Error.WriteLine($"[ERRO][DashboardQueryService]: {ex.Message}");
                return new
                {
                    success = false,
                    message = "Erro interno ao processar os dados do dashboard.",
                    detalhe = ex.Message
                };
            }
        }

        // ============================================================
        // 🧩 CONSULTA: EQUIPAMENTOS
        // ============================================================
        //
        // Processa os dados da tabela Equipamentos aplicando filtros
        // e agrupando conforme a dimensão e operação selecionadas.
        //
        // Exemplo: Agrupar por Status e contar quantidade.
        // ============================================================
        private static async Task<object> ConsultarEquipamentos(ApplicationDbContext _context, DashboardQueryRequest req)
        {
            var query = _context.Equipamentos.AsQueryable();

            // 🔹 Aplica filtro de data (quando informado)
            if (req.DataInicio.HasValue && req.DataFim.HasValue)
                query = query.Where(e => e.DataDeCadastro >= req.DataInicio && e.DataDeCadastro <= req.DataFim);

            // 🔹 Define agrupamento conforme dimensão selecionada
            switch (req.Dimensao)
            {
                case "Status":
                    return await query
                        .GroupBy(e => e.Status)
                        .Select(g => new { Categoria = g.Key ?? "N/A", Valor = g.Count() })
                        .ToListAsync();

                case "TipoEquipamento":
                    return await query
                        .Include(e => e.TipoEquipamento)
                        .GroupBy(e => e.TipoEquipamento.Nome)
                        .Select(g => new { Categoria = g.Key ?? "N/A", Valor = g.Count() })
                        .ToListAsync();

                default:
                    // 🔧 Caso queira exibir um KPI Total (sem dimensão)
                    return new
                    {
                        Valor = await query.CountAsync(),
                        Descricao = "Total de equipamentos cadastrados"
                    };
            }
        }

        // ============================================================
        // 🧩 CONSULTA: DESCARTES
        // ============================================================
        //
        // Exemplo: Soma da Quantidade agrupada por EmpresaColetora.
        // ============================================================
        private static async Task<object> ConsultarDescartes(ApplicationDbContext _context, DashboardQueryRequest req)
        {
            var query = _context.Descartes.AsQueryable();

            if (req.DataInicio.HasValue && req.DataFim.HasValue)
                query = query.Where(d => d.DataDeCadastro >= req.DataInicio && d.DataDeCadastro <= req.DataFim);

            switch (req.Dimensao)
            {
                case "EmpresaColetora":
                    if (req.Operacao == "Soma")
                    {
                        return await query
                            .GroupBy(d => d.EmpresaColetora)
                            .Select(g => new { Categoria = g.Key ?? "N/A", Valor = g.Sum(x => x.Quantidade) })
                            .ToListAsync();
                    }
                    else
                    {
                        return await query
                            .GroupBy(d => d.EmpresaColetora)
                            .Select(g => new { Categoria = g.Key ?? "N/A", Valor = g.Count() })
                            .ToListAsync();
                    }

                case "Status":
                    return await query
                        .GroupBy(d => d.Status)
                        .Select(g => new { Categoria = g.Key ?? "N/A", Valor = g.Count() })
                        .ToListAsync();

                default:
                    return new
                    {
                        Valor = await query.CountAsync(),
                        Descricao = "Total de descartes registrados"
                    };
            }
        }

        // ============================================================
        // 🧩 CONSULTA: CONSUMO DE ENERGIA
        // ============================================================
        //
        // Exemplo: Soma de kWh por mês.
        // ============================================================
        private static async Task<object> ConsultarConsumoEnergia(ApplicationDbContext _context, DashboardQueryRequest req)
        {
            var query = _context.ConsumosEnergia.AsQueryable();

            if (req.DataInicio.HasValue && req.DataFim.HasValue)
                query = query.Where(c => c.DataReferencia >= req.DataInicio && c.DataReferencia <= req.DataFim);

            return await query
                .GroupBy(c => c.DataReferencia.ToString("yyyy-MM"))
                .Select(g => new { Categoria = g.Key, Valor = g.Sum(x => x.ValorKwh) })
                .OrderBy(x => x.Categoria)
                .ToListAsync();
        }
    }
}
