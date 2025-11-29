// Controllers/ContasController.cs - PADRÃO SIMPLIFICADO (Acesso Direto ao Contexto)

using Governança_de_TI.Data;
using Governança_de_TI.Services;
using Governança_de_TI.Views.Services.Gamificacao;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Governança_de_TI.DTOs.Financeiro;

public class ContasController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IGamificacaoService _gamificacaoService;

    public ContasController(
        ApplicationDbContext context,
        IAuditService auditService,
        IGamificacaoService gamificacaoService)
    {
        _context = context;
        _auditService = auditService;
        _gamificacaoService = gamificacaoService;
    }

    private async Task<int?> GetCurrentUserId()
    {
        var userEmail = User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userEmail)) return null;

        var user = await _context.Usuarios.AsNoTracking()
                                         .FirstOrDefaultAsync(u => u.Email == userEmail);
        return user?.Id;
    }

    private ContaBancariaDTO ToDTO(ContaBancariaModel entity)
    {
        return new ContaBancariaDTO
        {
            Id = entity.IdConta,
            Banco = entity.Banco,
            NomeConta = entity.NomeConta,
            NumeroConta = entity.NumeroConta,
            Agencia = entity.Agencia,
            StatusConta = entity.StatusConta,
            TipoConta = entity.TipoConta,
            SaldoAtual = entity.SaldoAtual.GetValueOrDefault(0),
            SaldoInicial = entity.SaldoInicial
            // Note: SaldoInicial não está no DTO, mas estaria aqui se necessário
        };
    }

    [HttpGet("/Contas/_CriarEditarPartial")]
    public IActionResult ConsultaView()
    {
        return View("_CriarEditarPartial");
    }

    [HttpGet("/Contas/Consulta")]
    public async Task<IActionResult> Consulta() // Este é o método que carrega a tela
    {
        // 1. Buscando os dados (como na imagem de depuração)
        var contas = await _context.ContasBancarias.AsNoTracking().ToListAsync();

        // 2. Mapeia para DTO
        var dtos = contas.Select(ToDTO).ToList();

        // 3. RETORNA A VIEW COM O MODEL POPULADO (Server-Side Rendering)
        return View("Consulta", dtos);
    }

    // ================================
    //  Ajuste: INT → GUID
    // ================================
    [HttpGet("/api/financeiro/Contas/{id:guid}")] // <--- ROTA AJUSTADA PARA GUID
    public async Task<IActionResult> Detalhes(Guid id) // <--- ID É GUID
    {
        var conta = await _context.ContasBancarias
            .FirstOrDefaultAsync(c => c.IdConta == id);

        if (conta == null)
            return NotFound();

        return Json(new { success = true, data = ToDTO(conta) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(
     [Bind("IdConta,NomeConta,Banco,NumeroConta,Agencia,TipoConta,StatusConta,SaldoAtual,SaldoInicial,DataCadastro")]
ContaBancariaModel conta)
    {
        bool isNew = conta.IdConta == Guid.Empty;
        string successMessage = "";

        // 1. Validação Personalizada (Regras de Negócio)
        if (string.IsNullOrWhiteSpace(conta.Banco))
        {
            ModelState.AddModelError("Banco", "O campo Instituição (Banco) é obrigatório.");
        }

        if (conta.SaldoInicial < 0)
        {
            ModelState.AddModelError("SaldoInicial", "O Saldo Inicial não pode ser negativo.");
        }

        // ===============================================

        // ===============================================
        // 3. EXECUÇÃO DA TRANSAÇÃO (CRIAÇÃO OU EDIÇÃO)
        // ===============================================
        try
        {
            if (isNew) // CRIAÇÃO
            {
                conta.IdConta = Guid.NewGuid();
                conta.DataCadastro = DateTime.Now;
                conta.SaldoAtual = 0m;

                _context.ContasBancarias.Add(conta);
                successMessage = $"Conta '{conta.NomeConta}' cadastrada com sucesso!";
            }
            else // EDIÇÃO
            {
                // 1. Busca a entidade para rastreamento (REMOVENDO .AsNoTracking())
                var existingTracked = await _context.ContasBancarias.FirstOrDefaultAsync(c => c.IdConta == conta.IdConta);

                if (existingTracked == null)
                {
                    return Json(new { success = false, message = "Conta não encontrada para atualização." });
                }

                // NOTA: A lógica 'var SaldoAtualizado = conta.SaldoInicial + conta.SaldoAtual;'
                // é removida, pois SaldoAtual é mantido pelo motor de transações.
                // O valor de conta.SaldoAtual vindo do formulário é o valor que estava lá ANTES da edição.

                // 2. Mapeamento de Campos Editáveis (transferindo do Model recebido para o Model rastreado)
                existingTracked.NomeConta = conta.NomeConta;
                existingTracked.Banco = conta.Banco;
                existingTracked.Agencia = conta.Agencia;
                existingTracked.NumeroConta = conta.NumeroConta;
                existingTracked.TipoConta = conta.TipoConta;
                existingTracked.StatusConta = conta.StatusConta;

                // 3. Atualiza o Ponto de Partida (Saldo Inicial)
                // Se esta conta já tem transações, mudar o SaldoInicial afeta todos os relatórios.
                existingTracked.SaldoInicial = conta.SaldoInicial;

                // O SaldoAtual (o valor calculado pelo sistema) É PRESERVADO, 
                // pois ele reflete o total de movimentações.

                // O EF Core já está rastreando existingTracked. 
                // Não precisamos chamar _context.Update(existing) nem _context.Entry().State = Modified;

                successMessage = $"Conta '{existingTracked.NomeConta}' atualizada com sucesso!";
            }

            await _context.SaveChangesAsync();

            // --- Log e Gamificação ---
            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                string acao = isNew ? "Criou Conta" : "Editou Conta";
                await _auditService.RegistrarAcao(userId.Value, acao, $"Conta: {conta.NomeConta}");

                if (isNew)
                    await _gamificacaoService.AdicionarPontosAsync(userId.Value, "CadastrouConta", 3);
            }

            // 🔹 RETORNO AJAX JSON UNIFICADO (SUCESSO)
            // Isso é o que o seu JS espera para fechar o modal e dar reload.
            return Json(new { success = true, message = successMessage });
        }
        catch (Exception ex)
        {
            // 🔹 RETORNO AJAX JSON (ERRO DE SERVIDOR)
            // Retorna o erro no JSON para o JS tratar.
            return Json(new { success = false, message = "Erro inesperado ao salvar: " + ex.Message });
        }
    }


    [HttpGet("/Contas/Editar/{id:guid}")]
    public async Task<IActionResult> Editar(Guid id) 
    {
        var conta = await _context.ContasBancarias.AsNoTracking().FirstOrDefaultAsync(c => c.IdConta == id);
        if (conta == null) return NotFound();
        // Retorna a PartialView com o Model preenchido
        return PartialView("~/Views/Contas/_CriarEditarPartial.cshtml", conta);
    }

    [HttpPatch("/api/financeiro/Contas/{id:guid}/ajuste-saldo")] // <--- ROTA AJUSTADA PARA GUID
    public async Task<IActionResult> AjustarSaldo(Guid id, [FromBody] AjusteSaldoDTO ajusteDto) // <--- ID É GUID
    {
        try
        {
            var userId = await GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Usuário não autenticado para realizar ajuste crítico." });
            }

            var conta = await _context.ContasBancarias.FirstOrDefaultAsync(c => c.IdConta == id);
            if (conta == null) return NotFound();

            var saldoAntigo = conta.SaldoAtual;
            conta.SaldoAtual = ajusteDto.NovoSaldo;

            _context.Entry(conta).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Chamada ao SaldoDiarioService ou trigger manual de recálculo (Não está incluído aqui, mas é o próximo passo lógico)

            await _auditService.RegistrarAcao(
                userId.Value,
                "Ajuste Manual de Saldo",
                $"Conta ID: {id}, Diferença: {conta.SaldoAtual - saldoAntigo:N2}, Saldo Novo: {conta.SaldoAtual:C2}. Motivo: {ajusteDto.Observacao}"
            );

            return Json(new
            {
                success = true,
                message = $"Saldo da conta '{conta.NomeConta}' ajustado para {conta.SaldoAtual:C2}.",
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // ================================
    //  Ajuste: INT → GUID

    [HttpPost("/Contas/ExcluirPost/{id:guid}")] // Rota MVC que aceita o POST
    [ValidateAntiForgeryToken] // Proteção contra CSRF
    public async Task<IActionResult> ExcluirPost(Guid id)
    {
        try
        {
            var conta = await _context.ContasBancarias.FirstOrDefaultAsync(c => c.IdConta == id);
            if (conta == null)
            {
                TempData["WarningMessage"] = "Conta não encontrada.";
                return RedirectToAction(nameof(Consulta));
            }

            // Lógica de exclusão
            _context.ContasBancarias.Remove(conta);
            await _context.SaveChangesAsync();

            // Auditoria
            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditService.RegistrarAcao(userId.Value, "Deletou Conta Bancária", $"ID Conta: {id}, Nome: {conta.NomeConta}");
            }

            // RETORNO CLÁSSICO MVC: Redireciona com a mensagem de sucesso via TempData
            TempData["SuccessMessage"] = $"Conta '{conta.NomeConta}' excluida com sucesso!";
            return RedirectToAction(nameof(Consulta));
        }
        catch (Exception ex)
        {
            // Se falhar (ex: FK constraint), redireciona com mensagem de erro
            TempData["ErrorMessage"] = $"Erro ao excluir a conta: {ex.Message}";
            return RedirectToAction(nameof(Consulta));
        }
    }
}