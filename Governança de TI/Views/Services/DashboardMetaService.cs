using System.Collections.Generic;

namespace Governança_de_TI.Services
{
    // ============================================================
    // 📘 SERVIÇO CENTRAL DE METADADOS DOS DASHBOARDS
    // ============================================================
    //
    // Este serviço funciona como um "catálogo" das tabelas disponíveis
    // no sistema para criação de dashboards dinâmicos.
    //
    // Toda vez que uma nova tabela for criada (ex: Financeiro, Contas a Pagar,
    // Receitas, Estoque, etc.), basta adicioná-la aqui, sem alterar Controllers.
    //
    // O front-end consome esses metadados via API (DashboardApiController)
    // e monta automaticamente os filtros, métricas e gráficos.
    //
    // ============================================================
    public static class DashboardMetaService
    {
        // ============================================================
        // 🧠 MAPA PRINCIPAL DAS TABELAS
        // ============================================================
        //
        // Dicionário (Dictionary) que armazena o nome da tabela
        // e suas propriedades de metadados (dimensão, métrica, etc.).
        //
        // 🔧 INTERVENÇÃO FUTURA:
        // Quando criar uma nova tabela no sistema (ex: Financeiro),
        // basta adicionar uma nova entrada neste dicionário seguindo o mesmo padrão.
        // ============================================================
        public static Dictionary<string, DashboardTableMeta> Map => new()
        {
            // ============================================================
            // 🧩 TABELA: EQUIPAMENTOS
            // ============================================================
            ["Equipamentos"] = new DashboardTableMeta
            {
                // Campos possíveis para agrupar (Eixo X em gráficos)
                CamposDimensao = new() { "Status", "TipoEquipamento" },

                // Campos numéricos usados em operações (ex: soma, média)
                CamposMetrica = new() { },

                // Campos de data para aplicar filtros temporais
                CamposData = new()
                {
                    "DataCompra", "DataFimGarantia",
                    "DataUltimaManutencao", "DataDeCadastro"
                },

                // Tipos de operações permitidas
                OperacoesSuportadas = new() { "Contagem" },

                // Tipos de visualizações disponíveis para esta tabela
                Visualizacoes = new() { "Total", "Pizza", "Barra", "Rolo" }
            },

            // ============================================================
            // 🧩 TABELA: DESCARTES
            // ============================================================
            ["Descartes"] = new DashboardTableMeta
            {
                CamposDimensao = new() { "EmpresaColetora", "Status" },
                CamposMetrica = new() { "Quantidade" },
                CamposData = new() { "DataColeta", "DataDeCadastro" },
                OperacoesSuportadas = new() { "Soma", "Contagem" },
                Visualizacoes = new() { "Total", "Pizza", "Barra", "Rolo" }
            },

            // ============================================================
            // 🧩 TABELA: CONSUMO DE ENERGIA
            // ============================================================
            ["ConsumoEnergia"] = new DashboardTableMeta
            {
                CamposDimensao = new() { "DataReferencia" },
                CamposMetrica = new() { "ValorKwh" },
                CamposData = new() { "DataReferencia" },
                OperacoesSuportadas = new() { "Soma", "Média" },
                Visualizacoes = new() { "Total", "Pizza", "Rolo", "Barra", "Linha" }
            }
        };

        // ============================================================
        // 🔍 MÉTODO: ObterTabela()
        // ============================================================
        //
        // Retorna os metadados de uma tabela específica,
        // com base no nome informado.
        //
        // ✅ Exemplo de uso:
        // var meta = DashboardMetaService.ObterTabela("Equipamentos");
        //
        // 🔧 INTERVENÇÃO FUTURA:
        // Caso deseje implementar logs de auditoria,
        // é possível registrar aqui sempre que o front-end solicitar metadados.
        // ============================================================
        public static DashboardTableMeta? ObterTabela(string nomeTabela)
        {
            return Map.TryGetValue(nomeTabela, out var meta) ? meta : null;
        }

        // ============================================================
        // 📋 MÉTODO: ListarTabelas()
        // ============================================================
        //
        // Retorna apenas a lista com os nomes das tabelas registradas.
        //
        // ✅ Usado pelo front-end para montar o menu ou dropdown
        // de seleção de tabelas disponíveis para dashboards.
        //
        // 🔧 INTERVENÇÃO FUTURA:
        // Pode-se incluir lógica de permissão (ex: esconder certas tabelas
        // de usuários não administradores).
        // ============================================================
        public static IEnumerable<string> ListarTabelas()
        {
            return Map.Keys;
        }
    }

    // ============================================================
    // 🧩 CLASSE DE SUPORTE: DashboardTableMeta
    // ============================================================
    //
    // Esta classe define a estrutura de configuração de uma tabela.
    //
    // Cada tabela tem:
    // - Campos de dimensão (agrupamento)
    // - Campos de métrica (soma/média)
    // - Campos de data (filtros)
    // - Operações suportadas (Soma, Média, Contagem, etc.)
    // - Tipos de visualização disponíveis (Pizza, Barra, Linha, etc.)
    //
    // 🔧 INTERVENÇÃO FUTURA:
    // Pode-se expandir esta classe para incluir:
    // - Campos de descrição amigável (para exibir nomes legíveis no front)
    // - Cores padrão dos gráficos
    // - Filtros automáticos adicionais
    // ============================================================
    public class DashboardTableMeta
    {
        // Lista de campos que podem ser usados como "Dimensão"
        // Exemplo: Status, Categoria, Tipo, Empresa...
        public List<string> CamposDimensao { get; set; } = new();

        // Lista de campos numéricos que podem ser utilizados em operações
        public List<string> CamposMetrica { get; set; } = new();

        // Campos de data para filtros temporais
        public List<string> CamposData { get; set; } = new();

        // Tipos de operação suportados pela tabela (Soma, Média, Contagem...)
        public List<string> OperacoesSuportadas { get; set; } = new();

        // Tipos de visualizações que essa tabela pode usar (Pizza, Barra, Linha, etc.)
        public List<string> Visualizacoes { get; set; } = new();
    }
}
