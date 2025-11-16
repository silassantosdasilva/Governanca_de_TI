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
    // ⚙️ SERVIÇO CENTRAL DE CONSULTAS DE DASHBOARD (v2)
    // ============================================================
    //
    // Este serviço foi atualizado para suportar dois modos:
    // 1. AGREGAÇÃO (GROUP BY): Para Gráficos e KPIs (Total, Pizza, Barra).
    // 2. LISTAGEM (WHERE): Para o tipo "Lista / Tabela".
    //
    // ============================================================
    public static class DashboardQueryService
    {
        // === ADICIONE ESTE NOVO MÉTODO PRINCIPAL ===
        public static async Task<object> GetDistinctValuesAsync(ApplicationDbContext _context, DistinctValueRequest req)
        {
            switch (req.Tabela.ToLower())
            {
                case "equipamentos":
                    return await GetDistinctEquipamentos(_context, req.Campo);

                case "descartes":
                    // (Você criaria um helper GetDistinctDescartes)
                    return new List<string>(); // Exemplo

                // Adicione outros 'case' para outras tabelas

                default:
                    throw new NotSupportedException($"Tabela '{req.Tabela}' não suporta busca de valores distintos.");
            }
        }

        // === ADICIONE ESTE MÉTODO HELPER (AUXILIAR) ===
        // Este é o "switch" manual que segue o nosso padrão
        private static async Task<object> GetDistinctEquipamentos(ApplicationDbContext _context, string campo)
        {
            switch (campo.ToLower())
            {
                case "status":
                    return await _context.Equipamentos
                        .Select(e => e.Status)
                        .Distinct()
                        .Where(s => s != null && s != "") // Não traz nulos ou vazios
                        .OrderBy(s => s)
                        .ToListAsync();

                case "modelo":
                    return await _context.Equipamentos
                        .Select(e => e.Modelo)
                        .Distinct()
                        .Where(m => m != null && m != "")
                        .OrderBy(m => m)
                        .ToListAsync();

                case "descricao":
                    // AVISO: Campos de texto livre (como 'Descricao')
                    // podem retornar milhares de valores.
                    // É melhor não permitir, ou limitar com .Take(100)
                    return new List<string>() { "Busca não suportada para 'Descricao'" };

                // Adicione 'case' para outros campos (ex: "Serie")

                default:
                    // Se o campo não for 'Status' ou 'Modelo', retorna uma lista vazia
                    return new List<string>();
            }
        }
        // ============================================================
        // 🔹 MÉTODO PRINCIPAL: ExecutarConsultaAsync()
        // ============================================================
        // (Este método permanece o mesmo - é o "despachante")
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
                    // Adicione novos 'case' aqui
                    // =====================================================
                    default:
                        return new { message = $"Tabela '{req.Tabela}' ainda não tem suporte no serviço." };
                }
            }
            catch (Exception ex)
            {
                // ... (Seu código de LogService) ...
                try
                {
                    await LogService.Gravar(_context, "DashboardQueryService", "Erro", "Falha ao processar dados do dashboard.", ex.ToString());
                }
                catch { /* fallback */ }

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
        // 🧩 CONSULTA: EQUIPAMENTOS (Atualizado)
        // ============================================================
        private static async Task<object> ConsultarEquipamentos(ApplicationDbContext _context, DashboardQueryRequest req)
        {
            var query = _context.Equipamentos.AsQueryable();

            // 🔹 Aplica filtro de data (comum a ambos os modos)
            if (req.DataInicio.HasValue && req.DataFim.HasValue)
            {
                // Você precisa definir qual campo de data usar
                // Ex: query = query.Where(e => e.DataDeCadastro >= req.DataInicio && e.DataDeCadastro <= req.DataFim);
            }

            // =======================================================
            // === LÓGICA DE BIFURCAÇÃO ===
            // =======================================================

            // SE FOR 'LISTA', APLICA O FILTRO 'WHERE'
            if (req.TipoVisualizacao == "Lista")
            {
                // Aplica o filtro WHERE dinâmico e seguro
                if (!string.IsNullOrEmpty(req.FiltroCampo) && !string.IsNullOrEmpty(req.FiltroValor))
                {
                    switch (req.FiltroCampo.ToLower())
                    {
                        case "status":
                            query = query.Where(e => e.Status == req.FiltroValor);
                            break;
                        case "descricao":
                            query = query.Where(e => e.Descricao.Contains(req.FiltroValor));
                            break;
                        case "modelo":
                            query = query.Where(e => e.Modelo.Contains(req.FiltroValor));
                            break;
                        case "serie":
                            query = query.Where(e => e.Serie == req.FiltroValor);
                            break;
                    }
                }

                // === CORREÇÃO ESTÁ AQUI ===
                // Tornamos a seleção de 'Tipo' "Null-Safe"
                return await query
                    .Include(e => e.TipoEquipamento)
                    .Select(e => new {
                        e.CodigoItem,
                        e.Descricao,
                        e.Status,
                        e.Modelo,
                        // Verificamos se TipoEquipamento é nulo antes de acessar .Nome
                        Tipo = e.TipoEquipamento != null ? e.TipoEquipamento.Nome : "N/A"
                    })
                    .Take(100)
                    .ToListAsync();
            }

            // SE FOR GRÁFICO/KPI, FAZ A AGREGAÇÃO
            else
            {
                switch (req.Dimensao)
                {
                    case "Status":
                        return await query
                            .GroupBy(e => e.Status)
                            .Select(g => new { Categoria = g.Key ?? "N/A", Valor = g.Count() })
                            .ToListAsync();

                    case "TipoEquipamento":
                        // === CORREÇÃO TAMBÉM APLICADA AQUI ===
                        return await query
                            .Include(e => e.TipoEquipamento)
                            // Verificamos se é nulo
                            .GroupBy(e => e.TipoEquipamento != null ? e.TipoEquipamento.Nome : "N/A")
                            .Select(g => new { Categoria = g.Key, Valor = g.Count() })
                            .ToListAsync();

                    default: // KPI Total
                        return new
                        {
                            Valor = await query.CountAsync(),
                            Descricao = "Total de equipamentos cadastrados"
                        };
                }
            }
        }
        // ============================================================
        // 🧩 CONSULTA: DESCARTES (Atualizado)
        // ============================================================
        private static async Task<object> ConsultarDescartes(ApplicationDbContext _context, DashboardQueryRequest req)
        {
            var query = _context.Descartes.AsQueryable();

            if (req.DataInicio.HasValue && req.DataFim.HasValue)
                query = query.Where(d => d.DataDeCadastro >= req.DataInicio && d.DataDeCadastro <= req.DataFim);

            // SE FOR 'LISTA', APLICA O FILTRO 'WHERE'
            if (req.TipoVisualizacao == "Lista")
            {
                if (!string.IsNullOrEmpty(req.FiltroCampo) && !string.IsNullOrEmpty(req.FiltroValor))
                {
                    switch (req.FiltroCampo.ToLower())
                    {
                        case "status":
                            query = query.Where(d => d.Status == req.FiltroValor);
                            break;
                        case "empresacoletora":
                            query = query.Where(d => d.EmpresaColetora.Contains(req.FiltroValor));
                            break;
                    }
                }
                return await query.Take(100).ToListAsync();
            }

            // SE FOR GRÁFICO/KPI, FAZ A AGREGAÇÃO (seu código antigo)
            else
            {
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
                        else // Contagem
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

                    default: // KPI Total
                        return new
                        {
                            Valor = await query.CountAsync(),
                            Descricao = "Total de descartes registrados"
                        };
                }
            }
        }

        // ============================================================
        // 🧩 CONSULTA: CONSUMO DE ENERGIA (Sem alteração)
        // ============================================================
        // (Esta consulta é puramente de agregação, "Lista" não se aplica
        // a menos que você a defina no MetaService)
        // ============================================================
        // ============================================================
        // 🧩 CONSULTA: CONSUMO DE ENERGIA (Corrigido)
        // ============================================================
        private static async Task<object> ConsultarConsumoEnergia(ApplicationDbContext _context, DashboardQueryRequest req)
        {
            var query = _context.ConsumosEnergia.AsQueryable();

            if (req.DataInicio.HasValue && req.DataFim.HasValue)
                query = query.Where(c => c.DataReferencia >= req.DataInicio && c.DataReferencia <= req.DataFim);

            // =======================================================
            // === LÓGICA DE BIFURCAÇÃO (Corrigida) ===
            // =======================================================

            // SE FOR GRÁFICO (Agrupado por data)
            if (req.TipoVisualizacao == "Barra" || req.TipoVisualizacao == "Linha" || req.TipoVisualizacao == "Pizza" || req.TipoVisualizacao == "Rolo")
            {
                // O único 'CampoDimensao' é "DataReferencia", que agrupamos por mês
                return await query
                    .GroupBy(c => c.DataReferencia.ToString("yyyy-MM")) // Agrupa por Mês
                    .Select(g => new { Categoria = g.Key, Valor = g.Sum(x => x.ValorKwh) })
                    .OrderBy(x => x.Categoria)
                    .ToListAsync();
            }
            // SE FOR LISTA
            else if (req.TipoVisualizacao == "Lista")
            {
                // O 'CamposFiltro' para ConsumoEnergia ainda não foi definido no MetaService,
                // então isso apenas retornará a lista bruta.
                return await query
                    .Select(c => new { c.DataReferencia, c.ValorKwh }) // Seleciona colunas
                    .OrderByDescending(c => c.DataReferencia)
                    .Take(100)
                    .ToListAsync();
            }
            // SE FOR KPI (Total) - Esta era a parte que faltava
            else
            {
                // Vamos calcular o 'Valor' total com base na operação
                decimal valorTotal = 0;
                string desc = "Total";

                // O 'CampoMetrica' para Consumo é "ValorKwh"
                if (req.Operacao == "Soma")
                {
                    valorTotal = await query.SumAsync(c => c.ValorKwh);
                    desc = "Soma Total de Kwh";
                }
                else if (req.Operacao == "Media")
                {
                    valorTotal = await query.AverageAsync(c => c.ValorKwh);
                    desc = "Média de Kwh";
                }
                else
                { // Contagem
                    valorTotal = await query.CountAsync();
                    desc = "Total de Registros";
                }

                return new
                {
                    Valor = valorTotal,
                    Descricao = desc
                };
            }
        }
    }
}