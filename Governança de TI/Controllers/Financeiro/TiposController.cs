// Controllers/TiposController.cs - PADRÃO SIMPLIFICADO (Acesso Direto ao Contexto)

using Governança_de_TI.Data;
using Governança_de_TI.Services;
using Governança_de_TI.Views.Services.Gamificacao;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Governança_de_TI.Models.Financeiro.TpLancamento;
using Governança_de_TI.DTOs.Financeiro;

[Route("api/financeiro/[controller]")]
public class TiposController : Controller
{
    private readonly ApplicationDbContext _context;
    // O campo _tipoService foi removido.
    private readonly IAuditService _auditService;
    private readonly IGamificacaoService _gamificacaoService;

    public TiposController(
        ApplicationDbContext context,
        IAuditService auditService,
        IGamificacaoService gamificacaoService)
    {
        _context = context;
        // _tipoService removido do construtor
        _auditService = auditService;
        _gamificacaoService = gamificacaoService;
    }

    // Método auxiliar para obter ID do usuário logado
    private async Task<int?> GetCurrentUserId()
    {
        var userEmail = User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userEmail)) return null;

        var user = await _context.Usuarios.AsNoTracking()
                                         .FirstOrDefaultAsync(u => u.Email == userEmail);
        return user?.Id;
    }

    // Método de Conversão (DTO simples para retorno de detalhes)
    private TipoLancamentoDTO ToDTO(TipoLancamentoModel tipo)
    {
        return new TipoLancamentoDTO
        {
            Id = tipo.IdTipo,
            Nome = tipo.Nome,
            Tipo = tipo.Tipo
        };
    }

    // --- AÇÕES MVC E API ---

    // 5.3. GET /tipos - Consulta (Lista de Categorias)
    [HttpGet]
    public async Task<IActionResult> Consulta()
    {
        var tipos = await _context.TiposLancamento.ToListAsync();
        return Json(new { success = true, data = tipos.Select(ToDTO).ToList() });
    }

    // 5.3. POST /tipos - Criação, Auditoria e Gamificação
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] TipoLancamentoDTO tipoDto)
    {
        try
        {
            // Validação simples (Regra 6.3)
            if (string.IsNullOrWhiteSpace(tipoDto.Nome))
                throw new ArgumentException("O nome da categoria é obrigatório.");
            if (tipoDto.Tipo != 1 && tipoDto.Tipo != 2)
                throw new ArgumentException("O tipo de lançamento deve ser 1 (Despesa) ou 2 (Receita).");

            // Persistência Direta
            var newTipo = new TipoLancamentoModel
            {
                IdTipo = Guid.NewGuid(),
                Nome = tipoDto.Nome,
                Tipo = tipoDto.Tipo
            };

            _context.TiposLancamento.Add(newTipo);
            await _context.SaveChangesAsync();

            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditService.RegistrarAcao(userId.Value, "Criou Categoria Financeira", $"ID Tipo: {newTipo.IdTipo}, Nome: {newTipo.Nome}");
                await _gamificacaoService.AdicionarPontosAsync(userId.Value, "CadastrouTipoLancamento", 1);
            }

            return Json(new
            {
                success = true,
                message = $"Categoria '{newTipo.Nome}' cadastrada com sucesso!",
                notificacao = new
                {
                    titulo = "Gamificação",
                    texto = "Você ganhou +1 ponto por organizar suas finanças!",
                    tipo = "info"
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // 5.3. DELETE /tipos/{id} - Exclusão e Auditoria
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        try
        {
            var existing = await _context.TiposLancamento.FirstOrDefaultAsync(t => t.IdTipo == id);
            if (existing == null) throw new KeyNotFoundException("Categoria não encontrada.");

            // ** Regra de Negócio: Verificar se está em uso antes de deletar **
            bool estaEmUso = await _context.LancamentosFinanceiros.AnyAsync(l => l.IdTipo == id);
            if (estaEmUso)
                throw new InvalidOperationException("Não é possível excluir esta categoria, pois ela está associada a lançamentos financeiros.");

            _context.TiposLancamento.Remove(existing);
            await _context.SaveChangesAsync();

            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditService.RegistrarAcao(userId.Value, "Deletou Categoria Financeira", $"ID Tipo: {id}");
            }

            return Json(new { success = true, message = "Categoria excluída com sucesso!" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Categoria não encontrada." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}