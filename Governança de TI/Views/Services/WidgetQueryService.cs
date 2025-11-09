using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace Governança_de_TI.Services
{
    /// <summary>
    /// Serviço responsável por gerar consultas dinâmicas para os widgets.
    /// Ele interpreta o tipo de visualização e monta a consulta adequada
    /// sobre a tabela de origem escolhida pelo usuário.
    /// </summary>
    public class WidgetQueryService
    {
        private readonly ApplicationDbContext _context;

        public WidgetQueryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object?> ObterDadosAsync(DashboardWidgetModel widget)
        {
            if (widget == null || string.IsNullOrWhiteSpace(widget.TabelaFonte))
                return null;

            var entityType = _context.Model
                .GetEntityTypes()
                .FirstOrDefault(t => t.ClrType.Name == widget.TabelaFonte);

            if (entityType == null)
                return null;

            var entityClrType = entityType.ClrType;
            var dbSet = _context.GetType()
                .GetProperties()
                .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                     p.PropertyType.GetGenericArguments()[0] == entityClrType)?
                .GetValue(_context) as IEnumerable;

            if (dbSet == null)
                return null;

            // ========= TOTAL / KPI =========
            if (widget.TipoVisualizacao == "Total")
            {
                if (widget.Operacao == "Contagem Total")
                {
                    int count = dbSet.Cast<object>().Count();
                    return new { Valor = count };
                }
                else if (widget.Operacao == "Soma" && !string.IsNullOrEmpty(widget.CampoMetrica))
                {
                    var soma = dbSet.Cast<object>()
                        .Select(x => (double?)GetPropertyValue(x, widget.CampoMetrica))
                        .Where(v => v.HasValue)
                        .Sum() ?? 0;
                    return new { Valor = soma };
                }
            }

            // ========= PIZZA / BARRA / ROLO =========
            if (widget.TipoVisualizacao is "Pizza" or "Barra" or "Rolo")
            {
                if (string.IsNullOrEmpty(widget.CampoDimensao))
                    return null;

                var grupos = dbSet.Cast<object>()
                    .GroupBy(x => GetPropertyValue(x, widget.CampoDimensao)?.ToString() ?? "Indefinido")
                    .Select(g => new
                    {
                        Categoria = g.Key,
                        Valor = (widget.Operacao == "Soma" && !string.IsNullOrEmpty(widget.CampoMetrica))
                            ? g.Sum(x => Convert.ToDouble(GetPropertyValue(x, widget.CampoMetrica) ?? 0))
                            : g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToList();

                return grupos;
            }

            // ========= LISTA =========
            if (widget.TipoVisualizacao == "Lista" && !string.IsNullOrEmpty(widget.CamposLista))
            {
                var campos = widget.CamposLista.Split(',').Select(c => c.Trim()).ToList();

                var lista = dbSet.Cast<object>()
                    .Take(20)
                    .Select(x => new Dictionary<string, object?>(
                        campos.Select(c => new KeyValuePair<string, object?>(c, GetPropertyValue(x, c)))
                    ))
                    .ToList();

                return lista;
            }

            return null;
        }

        // Helper: acesso via reflexão
        private static object? GetPropertyValue(object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            return prop?.GetValue(obj);
        }
    }
}
