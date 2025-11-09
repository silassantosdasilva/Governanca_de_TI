// =========================================================
// 1️⃣ USINGS E NAMESPACE
// =========================================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Governança_de_TI.Models;
using Governança_de_TI.Data;
using Newtonsoft.Json;

namespace Governança_de_TI.Controllers
{
    // =========================================================
    // 2️⃣ CLASSE PRINCIPAL DO DASHBOARD DINÂMICO
    // =========================================================
    public class DashboardDinamicaController : Controller
    {
        private readonly ApplicationDbContext _context;

        // =========================================================
        // 🟩 4️⃣ MAPA DE COMBINAÇÕES VÁLIDAS (FORMATO CORRIGIDO)
        // =========================================================
        private readonly Dictionary<string, List<object>> _regrasValidas = new()
        {
            ["Equipamentos"] = new()
            {
                new { Dimensao = "Status",           Metrica = (string?)null, Operacao = "Contagem", TipoVisualizacao = "Pizza" },
                new { Dimensao = "Status",           Metrica = (string?)null, Operacao = "Contagem", TipoVisualizacao = "Rolo" },
                new { Dimensao = "Status",           Metrica = (string?)null, Operacao = "Contagem", TipoVisualizacao = "Barra" },
                new { Dimensao = "TipoEquipamento",  Metrica = (string?)null, Operacao = "Contagem", TipoVisualizacao = "Pizza" },
                new { Dimensao = "TipoEquipamento",  Metrica = (string?)null, Operacao = "Contagem", TipoVisualizacao = "Rolo" },
                new { Dimensao = "TipoEquipamento",  Metrica = (string?)null, Operacao = "Contagem", TipoVisualizacao = "Barra" },
                new { Dimensao = "Status",           Metrica = (string?)null, Operacao = "Contagem", TipoVisualizacao = "Total" },
                new { Dimensao = "TipoEquipamento",  Metrica = (string?)null, Operacao = "Contagem", TipoVisualizacao = "Total" }
            },

            ["Descartes"] = new()
            {
                new { Dimensao = "EmpresaColetora",  Metrica = "Quantidade", Operacao = "Soma",      TipoVisualizacao = "Pizza" },
                new { Dimensao = "EmpresaColetora",  Metrica = "Quantidade", Operacao = "Soma",      TipoVisualizacao = "Rolo" },
                new { Dimensao = "EmpresaColetora",  Metrica = "Quantidade", Operacao = "Soma",      TipoVisualizacao = "Barra" },
                new { Dimensao = "EmpresaColetora",  Metrica = "Quantidade", Operacao = "Soma",      TipoVisualizacao = "Total" },
                new { Dimensao = "Status",           Metrica = (string?)null, Operacao = "Contagem",  TipoVisualizacao = "Barra" },
                new { Dimensao = "Status",           Metrica = (string?)null, Operacao = "Contagem",  TipoVisualizacao = "Pizza" },
                new { Dimensao = "Status",           Metrica = (string?)null, Operacao = "Contagem",  TipoVisualizacao = "Rolo" }
            },

            ["ConsumoEnergia"] = new()
            {
                new { Dimensao = "DataReferencia",   Metrica = "ValorKwh",   Operacao = "Soma",      TipoVisualizacao = "Pizza" },
                new { Dimensao = "DataReferencia",   Metrica = "ValorKwh",   Operacao = "Soma",      TipoVisualizacao = "Rolo" },
                new { Dimensao = "DataReferencia",   Metrica = "ValorKwh",   Operacao = "Soma",      TipoVisualizacao = "Barra" },
                new { Dimensao = "DataReferencia",   Metrica = "ValorKwh",   Operacao = "Soma",      TipoVisualizacao = "Total" }
            }
        };

        public DashboardDinamicaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // 3️⃣ INDEX
        // =========================================================
        public async Task<IActionResult> Index()
        {
            try
            {
                var widgets = await _context.DashboardWidgets.ToListAsync();
                return View(widgets);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][Index]: {ex.Message}");
                TempData["ErrorMessage"] = "Erro ao carregar o painel de widgets.";
                return View(new List<DashboardWidgetModel>());
            }
        }

        // =========================================================
        // 3️⃣ CRIAR OU EDITAR
        // =========================================================
        [HttpGet]
        public IActionResult CriarOuEditar(int? id, int? pos)
        {
            try
            {
                // 🔹 Tabelas disponíveis no dashboard
                var tabelas = new List<string> { "Equipamentos", "Descartes", "ConsumoEnergia" };
                ViewBag.Tabelas = tabelas;

                // 🔹 Campos disponíveis por tabela (dimensões, métricas, datas)
                var camposPorTabela = new Dictionary<string, object>
                {
                    ["Equipamentos"] = new
                    {
                        Dimensoes = new List<string> { "Status", "TipoEquipamento" },
                        Metricas = new List<string>(),
                        Datas = new List<string> { "DataCompra", "DataFimGarantia", "DataUltimaManutencao", "DataDeCadastro" }
                    },
                    ["Descartes"] = new
                    {
                        Dimensoes = new List<string> { "EmpresaColetora", "Status" },
                        Metricas = new List<string> { "Quantidade" },
                        Datas = new List<string> { "DataColeta", "DataDeCadastro" }
                    },
                    ["ConsumoEnergia"] = new
                    {
                        Dimensoes = new List<string> { "DataReferencia" },
                        Metricas = new List<string> { "ValorKwh" },
                        Datas = new List<string> { "DataReferencia" }
                    }
                };

                // 🔹 Serializa para o front-end
                ViewBag.CamposJson = JsonConvert.SerializeObject(camposPorTabela);
                ViewBag.RegrasValidasJson = JsonConvert.SerializeObject(_regrasValidas);

                DashboardWidgetModel model;
                if (id.HasValue)
                {
                    model = _context.DashboardWidgets.Find(id.Value);
                    if (model == null)
                        return Content("<div class='text-danger small'>Widget não encontrado no banco de dados.</div>");
                }
                else
                {
                    model = new DashboardWidgetModel { Posicao = pos ?? 1 };
                }

                var tabelaSelecionada = model.TabelaFonte ?? tabelas.First();
                ViewBag.CamposIniciais = camposPorTabela.ContainsKey(tabelaSelecionada)
                    ? camposPorTabela[tabelaSelecionada]
                    : new { Dimensoes = new List<string>(), Metricas = new List<string>(), Datas = new List<string>() };

                return PartialView("_CriarOuEditarWidgetPartial", model);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][CriarOuEditar]: {ex.Message}");
                return Content($@"
                    <div class='alert alert-danger small p-3'>
                        <strong>Erro interno:</strong> {ex.Message}<br/>
                        <code>Verifique os campos e tente novamente.</code>
                    </div>");
            }
        }

        // =========================================================
        // 3️⃣ SALVAR
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CriarOuEditar(DashboardWidgetModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Campos obrigatórios não preenchidos." });

                if (model.Id == 0)
                    _context.DashboardWidgets.Add(model);
                else
                    _context.DashboardWidgets.Update(model);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Widget salvo com sucesso!" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][Salvar Widget]: {ex.Message}");
                return Json(new { success = false, message = "Erro ao salvar o widget. Verifique os dados e tente novamente." });
            }
        }

        // =========================================================
        // 5️⃣ CARREGAR WIDGET
        // =========================================================
        public async Task<IActionResult> CarregarWidget(int id)
        {
            try
            {
                var widget = await _context.DashboardWidgets.AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == id);

                if (widget == null)
                    return Content("<div class='text-danger small'>Widget não encontrado</div>");

                bool combinacaoValida = _regrasValidas.TryGetValue(widget.TabelaFonte ?? "", out var regras)
     && regras.Any(r =>
     {
         dynamic d = r;

         string regraDim = d.Dimensao ?? "";
         string regraMet = d.Metrica ?? "";
         string regraOp = d.Operacao ?? "";
         string regraVis = d.TipoVisualizacao ?? "";

         string widDim = widget.CampoDimensao ?? "";
         string widMet = widget.CampoMetrica ?? "";
         string widOp = widget.Operacao ?? "";
         string widVis = widget.TipoVisualizacao ?? "";

         return
             // Se for Total, ignora dimensão
             (widVis == "Total" || regraDim.Equals(widDim, StringComparison.OrdinalIgnoreCase))
             && regraOp.Equals(widOp, StringComparison.OrdinalIgnoreCase)
             && regraVis.Equals(widVis, StringComparison.OrdinalIgnoreCase)
             && (string.IsNullOrEmpty(regraMet) || regraMet.Equals(widMet, StringComparison.OrdinalIgnoreCase));
     });

                // 🔹 Processamento do widget
                object dadosProcessados = widget.TipoVisualizacao switch
                {
                    "Total" => await ProcessarWidgetKpiAsync(widget),
                    "Pizza" or "Barra" or "Rolo" or "Lista" => await ProcessarWidgetGraficoAsync(widget),
                    _ => null
                };

                return PartialView("_WidgetPartial", (widget, dadosProcessados));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][CarregarWidget]: {ex.Message}");
                return Content("<div class='text-danger small p-3'>Erro inesperado ao processar o widget.</div>");
            }
        }

        // =========================================================
        // 5️⃣ EXCLUIR
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var widget = await _context.DashboardWidgets.FindAsync(id);
                if (widget == null)
                    return Json(new { success = false, message = "Widget não encontrado." });

                _context.DashboardWidgets.Remove(widget);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Widget excluído com sucesso!" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][Excluir]: {ex.Message}");
                return Json(new { success = false, message = "Erro ao excluir o widget. Tente novamente." });
            }
        }

        // =========================================================
        // 5️⃣ PROCESSAMENTO DE WIDGETS
        // =========================================================
        private async Task<DashboardWidgetModel> ProcessarWidgetKpiAsync(DashboardWidgetModel widget)
        {
            try
            {
                double valor = await GetKpiValueAsync(widget.TabelaFonte, widget.Operacao, widget.CampoMetrica, widget);
                widget.Resultado = valor;
                return widget;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][KPI]: {ex.Message}");
                widget.Resultado = 0;
                return widget;
            }
        }

        private async Task<DashboardWidgetModel> ProcessarWidgetGraficoAsync(DashboardWidgetModel widget)
        {
            var dadosGrafico = new List<dynamic>();

            try
            {
                switch (widget.TabelaFonte)
                {
                    case "Equipamentos":
                        var queryEquip = AplicarFiltroDataEquipamentos(_context.Equipamentos.AsQueryable(), widget);

                        if (widget.CampoDimensao == "TipoEquipamento")
                        {
                            dadosGrafico = (await queryEquip
                                .Include(e => e.TipoEquipamento)
                                .GroupBy(e => e.TipoEquipamento.Nome)
                                .Select(g => new { Categoria = g.Key ?? "Sem Tipo", Valor = g.Count() })
                                .ToListAsync()).Cast<dynamic>().ToList();
                        }
                        else if (widget.CampoDimensao == "Status")
                        {
                            dadosGrafico = (await queryEquip
                                .GroupBy(e => e.Status)
                                .Select(g => new { Categoria = g.Key ?? "Sem Status", Valor = g.Count() })
                                .ToListAsync()).Cast<dynamic>().ToList();
                        }

                        // 🟡 Fallback se o gráfico vier vazio (garante renderização)
                        if (dadosGrafico.Count == 0)
                        {
                            dadosGrafico.Add(new { Categoria = "Sem dados", Valor = 0 });
                        }
                        break;


                    case "Descartes":
                        var queryDesc = AplicarFiltroDataDescartes(_context.Descartes.AsQueryable(), widget);
                        if (widget.CampoDimensao == "EmpresaColetora")
                        {
                            dadosGrafico = (await queryDesc
                                .GroupBy(d => d.EmpresaColetora)
                                .Select(g => new { Categoria = g.Key, Valor = g.Sum(d => d.Quantidade) })
                                .ToListAsync()).Cast<dynamic>().ToList();
                        }
                        else if (widget.CampoDimensao == "Status")
                        {
                            dadosGrafico = (await queryDesc
                                .GroupBy(d => d.Status)
                                .Select(g => new { Categoria = g.Key, Valor = g.Count() })
                                .ToListAsync()).Cast<dynamic>().ToList();
                        }
                        break;

                    case "ConsumoEnergia":
                        var queryCons = AplicarFiltroDataConsumoEnergia(_context.ConsumosEnergia.AsQueryable(), widget);
                        dadosGrafico = (await queryCons
                            .GroupBy(c => c.DataReferencia.ToString("yyyy-MM"))
                            .Select(g => new { Categoria = g.Key, Valor = g.Sum(e => e.ValorKwh) })
                            .ToListAsync()).Cast<dynamic>().ToList();
                        break;
                }

                widget.Dados = dadosGrafico.Cast<object>().ToList();
                return widget;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][ProcessarWidgetGraficoAsync]: {ex.Message}");
                throw;
            }
        }

        // =========================================================
        // 6️⃣ HELPERS
        // =========================================================
        private async Task<double> GetKpiValueAsync(string tabela, string operacao, string campo, DashboardWidgetModel widget)
        {
            if (string.IsNullOrEmpty(tabela) || string.IsNullOrEmpty(operacao)) return 0;

            switch (tabela)
            {
                case "Equipamentos":
                    return await _context.Equipamentos.CountAsync();

                case "Descartes":
                    return operacao switch
                    {
                        "Soma" => (double)(await _context.Descartes.SumAsync(d => (decimal?)d.Quantidade) ?? 0),
                        "Contagem" => await _context.Descartes.CountAsync(),
                        _ => 0
                    };

                case "ConsumoEnergia":
                    return operacao switch
                    {
                        "Soma" => (double)(await _context.ConsumosEnergia.SumAsync(c => (decimal?)c.ValorKwh) ?? 0),
                        "Media" => (double)(await _context.ConsumosEnergia.AverageAsync(c => (decimal?)c.ValorKwh) ?? 0),
                        _ => 0
                    };

                default:
                    return 0;
            }
        }

        private IQueryable<EquipamentoModel> AplicarFiltroDataEquipamentos(IQueryable<EquipamentoModel> query, DashboardWidgetModel widget)
        {
            if (string.IsNullOrEmpty(widget.CampoDataFiltro)) return query;
            if (!widget.DataFiltroInicio.HasValue && !widget.DataFiltroFim.HasValue) return query;

            var inicio = widget.DataFiltroInicio ?? DateTime.MinValue;
            var fim = (widget.DataFiltroFim ?? DateTime.MaxValue).Date.AddDays(1).AddTicks(-1);
            return query.Where(e => e.DataDeCadastro >= inicio && e.DataDeCadastro <= fim);
        }

        private IQueryable<DescarteModel> AplicarFiltroDataDescartes(IQueryable<DescarteModel> query, DashboardWidgetModel widget)
        {
            if (string.IsNullOrEmpty(widget.CampoDataFiltro)) return query;
            if (!widget.DataFiltroInicio.HasValue && !widget.DataFiltroFim.HasValue) return query;

            var inicio = widget.DataFiltroInicio ?? DateTime.MinValue;
            var fim = (widget.DataFiltroFim ?? DateTime.MaxValue).Date.AddDays(1).AddTicks(-1);
            return query.Where(e => e.DataDeCadastro >= inicio && e.DataDeCadastro <= fim);
        }

        private IQueryable<ConsumoEnergiaModel> AplicarFiltroDataConsumoEnergia(IQueryable<ConsumoEnergiaModel> query, DashboardWidgetModel widget)
        {
            if (string.IsNullOrEmpty(widget.CampoDataFiltro)) return query;
            if (!widget.DataFiltroInicio.HasValue && !widget.DataFiltroFim.HasValue) return query;

            var inicio = widget.DataFiltroInicio ?? DateTime.MinValue;
            var fim = (widget.DataFiltroFim ?? DateTime.MaxValue).Date.AddDays(1).AddTicks(-1);
            return query.Where(e => e.DataReferencia >= inicio && e.DataReferencia <= fim);
        }
    }
}
