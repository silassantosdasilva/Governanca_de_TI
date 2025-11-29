using Governança_de_TI.Data;
using Governança_de_TI.DTOs.Financeiro;
using Governança_de_TI.Models;
using Governança_de_TI.Services; // Para IAuditService
using Governança_de_TI.Views.Services.Gamificacao;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Nota: O namespace da Controller não está definido no arquivo original, 
// assumindo o escopo global ou um namespace implícito.
public class PessoasController : Controller
{
    private readonly ApplicationDbContext _context;
    // O campo _pessoaRepository FOI REMOVIDO
    private readonly IAuditService _auditService;
    private readonly IGamificacaoService _gamificacaoService;

    // Prefixo da API REST (usado apenas para a chamada de dados do Grid e Modais)
    private const string ApiRoute = "/api/financeiro/Pessoas";

    public PessoasController(
        ApplicationDbContext context,
        IAuditService auditService,
        IGamificacaoService gamificacaoService)
    {
        _context = context;
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
    private PessoaDTO ToDTO(PessoaModel pessoa)
    {
        return new PessoaDTO
        {
            Id = pessoa.IdPessoa,
            Nome = pessoa.Nome,
            TipoPessoa = pessoa.TipoPessoa,
            Documento = pessoa.Documento,
            Emails = pessoa.Email?.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
            Observacao = pessoa.Observacao,
            Endereco = new EnderecoDTO
            {
                Logradouro = pessoa.EnderecoLogradouro,
                Numero = pessoa.EnderecoNumero,
                Complemento = pessoa.EnderecoComplemento,
                Bairro = pessoa.EnderecoBairro,
                Cidade = pessoa.EnderecoCidade,
                Uf = pessoa.EnderecoUF,
                Cep = pessoa.EnderecoCEP
            }
        };
    }

    // ==========================================================
    // AÇÕES MVC (Retorno de Views)
    // ==========================================================

    // 1. AÇÃO MVC PRINCIPAL (Consulta)
    [HttpGet("/Pessoas/Consulta")]
    public async Task<IActionResult> Consulta(string descricao, int? tipoPessoa)
    {
        var query = _context.Pessoas.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(descricao))
        {
            query = query.Where(p => p.Nome.Contains(descricao) || p.Documento.Contains(descricao));
        }

        if (tipoPessoa.HasValue)
        {
            query = query.Where(p => p.TipoPessoa == tipoPessoa.Value);
        }

        var pessoas = await query.ToListAsync();
        var dtos = pessoas.Select(ToDTO).ToList();

        return View(dtos);
    }

    // 2. AÇÃO MVC PARA CARREGAR O MODAL DE CRIAÇÃO
    [HttpGet("/Pessoas/_CriarEditarPartial")]
    public IActionResult _CriarEditarPartial()
    {
        // Retorna a PartialView para criação (sem Model)
        return PartialView("~/Views/Pessoas/_CriarEditarPartial.cshtml");
    }

    // 3. NOVO MÉTODO MVC PARA CARREGAR O MODAL DE EDIÇÃO (PADRÃO EQUIPAMENTOS)
    // Rota: /Pessoas/Editar/{id}
    [HttpGet("/Pessoas/Editar/{id:guid}")]
    public async Task<IActionResult> Editar(Guid id)
    {
        var pessoa = await _context.Pessoas.FirstOrDefaultAsync(p => p.IdPessoa == id);

        if (pessoa == null)
            return NotFound();

        // 🔹 Verifica se a requisição veio via AJAX (modal)
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {

            return PartialView("_CriarEditarPartial", pessoa);
        }

        // Caso contrário (acesso direto), redireciona para a consulta ou mostra a tela completa.
        return RedirectToAction(nameof(Consulta));
    }


    // --- MÉTODOS API RESTful (CUD) ---

    // GET /api/financeiro/Pessoas/{id} - Detalhes (MANTIDO, caso o frontend ainda precise do JSON)
    [HttpGet($"{ApiRoute}/{{id:guid}}")]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        var pessoa = await _context.Pessoas.AsNoTracking().FirstOrDefaultAsync(p => p.IdPessoa == id);
        if (pessoa == null) return NotFound(new { success = false, message = "Pessoa não encontrada." });

        return Json(new { success = true, data = ToDTO(pessoa) });
    }

    // POST /api/financeiro/Pessoas - Criação (MANTIDO)
    [HttpPost]
   [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(
    [Bind("IdPessoa, DataCadastro, Nome, TipoPessoa, Documento, Email, Observacao, EnderecoLogradouro, EnderecoNumero, EnderecoComplemento, EnderecoBairro, EnderecoCidade, EnderecoUF, EnderecoCEP")]
    PessoaModel pessoa)
    {
        try
        {
            // Verifica se é novo pelo ID (Guid vazio = Novo)
            bool isNew = pessoa.IdPessoa == Guid.Empty;
            string mensagemSucesso;

            if (isNew) // --- CRIAÇÃO ---
            {
                pessoa.IdPessoa = Guid.NewGuid();
                pessoa.DataCadastro = DateTime.Now;
                // DataAtualizacao é opcional, mas boa prática se tiver no model
                // pessoa.DataAtualizacao = DateTime.Now; 

                _context.Pessoas.Add(pessoa);
                mensagemSucesso = $"Pessoa ({pessoa.Nome}) criada com sucesso!";
            }
            else // --- EDIÇÃO ---
            {
                // Busca o registro original para garantir que existe e para rastreamento
                var existing = await _context.Pessoas.FirstOrDefaultAsync(p => p.IdPessoa == pessoa.IdPessoa);

                if (existing == null)
                {
                    return NotFound(new { success = false, message = "Registro não encontrado para edição." });
                }

                // Atualiza os campos (Mapeamento Manual é mais seguro aqui)
                existing.Nome = pessoa.Nome;
                existing.TipoPessoa = pessoa.TipoPessoa;
                existing.Documento = pessoa.Documento;
                existing.Email = pessoa.Email;
                existing.Observacao = pessoa.Observacao;

                // Endereço
                existing.EnderecoCEP = pessoa.EnderecoCEP;
                existing.EnderecoLogradouro = pessoa.EnderecoLogradouro;
                existing.EnderecoNumero = pessoa.EnderecoNumero;
                existing.EnderecoComplemento = pessoa.EnderecoComplemento;
                existing.EnderecoBairro = pessoa.EnderecoBairro;
                existing.EnderecoCidade = pessoa.EnderecoCidade;
                existing.EnderecoUF = pessoa.EnderecoUF;

                // Preserva DataCadastro original e atualiza DataAtualizacao (se existir no model)
                // existing.DataAtualizacao = DateTime.Now; 

                _context.Update(existing);
                mensagemSucesso = $"Pessoa ({existing.Nome}) atualizada com sucesso!";
            }

            await _context.SaveChangesAsync();

            // --- Auditoria e Gamificação ---
            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                string acao = isNew ? "Criou Pessoa Financeira" : "Editou Pessoa Financeira";
                await _auditService.RegistrarAcao(userId.Value, acao, $"ID Pessoa: {pessoa.IdPessoa}, Nome: {pessoa.Nome}");

                // Pontos apenas na criação (opcional)
                if (isNew)
                {
                    await _gamificacaoService.AdicionarPontosAsync(userId.Value, "CadastrouPessoa", 2);
                }
            }

            return Ok(new { success = true, message = mensagemSucesso, data = ToDTO(isNew ? pessoa : await _context.Pessoas.FindAsync(pessoa.IdPessoa)) });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Erro ao salvar: " + ex.Message });
        }
    }

    // PUT /api/financeiro/Pessoas/{id} - Edição (API RESTful) (MANTIDO)
    [HttpPut($"{ApiRoute}/{{id:guid}}")]
    public async Task<IActionResult> Editar(
        Guid id,
        [Bind("IdPessoa, Nome, TipoPessoa, Documento, Email, Observacao, EnderecoLogradouro, EnderecoNumero, EnderecoComplemento, EnderecoBairro, EnderecoCidade, EnderecoUF, EnderecoCEP, DataCadastro")]
        PessoaModel pessoaAtualizada)
    {
        if (id != pessoaAtualizada.IdPessoa)
        {
            pessoaAtualizada.IdPessoa = id;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Dados inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var pessoaExistente = await _context.Pessoas.FirstOrDefaultAsync(p => p.IdPessoa == id);

            if (pessoaExistente == null)
            {
                return NotFound(new { success = false, message = "Pessoa não encontrada para atualização." });
            }

            // O uso de 'TryUpdateModelAsync' seria mais limpo, mas mantemos a atualização manual para consistência com o Bind.
            pessoaExistente.Nome = pessoaAtualizada.Nome;
            pessoaExistente.TipoPessoa = pessoaAtualizada.TipoPessoa;
            pessoaExistente.Documento = pessoaAtualizada.Documento;
            pessoaExistente.Email = pessoaAtualizada.Email;
            pessoaExistente.Observacao = pessoaAtualizada.Observacao;
            pessoaExistente.EnderecoLogradouro = pessoaAtualizada.EnderecoLogradouro;
            pessoaExistente.EnderecoNumero = pessoaAtualizada.EnderecoNumero;
            pessoaExistente.EnderecoComplemento = pessoaAtualizada.EnderecoComplemento;
            pessoaExistente.EnderecoBairro = pessoaAtualizada.EnderecoBairro;
            pessoaExistente.EnderecoCidade = pessoaAtualizada.EnderecoCidade;
            pessoaExistente.EnderecoUF = pessoaAtualizada.EnderecoUF;
            pessoaExistente.EnderecoCEP = pessoaAtualizada.EnderecoCEP;

            pessoaExistente.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();

            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditService.RegistrarAcao(userId.Value, "Editou Pessoa Financeira", $"ID Pessoa: {id}, Nome: {pessoaAtualizada.Nome}");
                await _gamificacaoService.AdicionarPontosAsync(userId.Value, "EditouPessoa", 1);
            }

            TempData["SuccessMessage"] = $"Pessoa ({pessoaAtualizada.Nome}) atualizada com sucesso!";
            return Ok(new { success = true, message = TempData["SuccessMessage"].ToString(), data = ToDTO(pessoaExistente) });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Erro ao atualizar: " + ex.Message });
        }
    }


    // DELETE /api/financeiro/Pessoas/{id} - Exclusão (MANTIDO)
    [HttpDelete($"{ApiRoute}/{{id:guid}}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        try
        {
            var pessoa = await _context.Pessoas.FirstOrDefaultAsync(p => p.IdPessoa == id);
            if (pessoa == null) return NotFound(new { success = false, message = "Pessoa não encontrada para exclusão." });

            _context.Pessoas.Remove(pessoa);
            await _context.SaveChangesAsync();

            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditService.RegistrarAcao(userId.Value, "Deletou Pessoa Financeira", $"ID Pessoa: {id}, Nome: {pessoa.Nome}");
            }

            return Json(new { success = true, message = $"Pessoa ({pessoa.Nome}) excluída com sucesso!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Erro ao excluir: " + ex.Message });
        }
    }
}