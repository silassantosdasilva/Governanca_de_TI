using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Governança_de_TI.Controllers
{
    public class EquipamentosController : Controller
    {
        // Serviço que fornece o caminho da pasta wwwroot e informações do ambiente web
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Contexto de banco de dados (Entity Framework)
        private readonly ApplicationDbContext _context;

        // ✅ Construtor: o ASP.NET Core injeta automaticamente o contexto e o ambiente web aqui
        public EquipamentosController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Detalhes(int id)
        {
            // Busca o equipamento pelo Id (com include se tiver relação com usuário)
            var equipamento = await _context.Equipamentos
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(e => e.CodigoItem == id);

            // Se o equipamento não for encontrado, retorna erro 404
            if (equipamento == null)
            {
                return NotFound();
            }

            // Retorna a view Detalhes.cshtml, passando o equipamento como modelo
            return View(equipamento);
        }

        // GET: Equipamentos/Consulta
        // ATUALIZAÇÃO: A Action agora aceita parâmetros de filtro
        // GET: Equipamentos/Consulta
        [HttpGet]
        public async Task<IActionResult> Consulta(int? codigoItem, string descricao, string status, DateTime? dataCompra, DateTime? dataUltimaManutencao, DateTime? dataDeCriacao)
        {
            var query = _context.Equipamentos.AsQueryable();

            if (codigoItem.HasValue)
            {
                query = query.Where(e => e.CodigoItem == codigoItem.Value);
            }

            if (!string.IsNullOrEmpty(descricao))
            {
                query = query.Where(e => e.Descricao.Contains(descricao));
            }

            if (!string.IsNullOrEmpty(status) && status != "Todos...")
            {
                query = query.Where(e => e.Status == status);
            }

            // CORREÇÃO: Filtro de data por intervalo (início do dia até início do dia seguinte)
            // Esta é a forma mais performática e compatível de filtrar por data.
            if (dataCompra.HasValue)
            {
                var dataInicio = dataCompra.Value.Date;
                var dataFim = dataInicio.AddDays(1);
                query = query.Where(e => e.DataCompra >= dataInicio && e.DataCompra < dataFim);
            }

            // CORREÇÃO: Aplicado o mesmo padrão para a data de manutenção
            if (dataUltimaManutencao.HasValue)
            {
                var dataInicio = dataUltimaManutencao.Value.Date;
                var dataFim = dataInicio.AddDays(1);
                query = query.Where(e => e.DataUltimaManutencao.HasValue && e.DataUltimaManutencao >= dataInicio && e.DataUltimaManutencao < dataFim);
            }

            // CORREÇÃO: Aplicado o mesmo padrão para a data de criação
            if (dataDeCriacao.HasValue)
            {
                var dataInicio = dataDeCriacao.Value.Date;
                var dataFim = dataInicio.AddDays(1);
                query = query.Where(e => e.DataDeCadastro >= dataInicio && e.DataDeCadastro < dataFim);
            }

            var listaDeEquipamentos = await query.ToListAsync();
            return View(listaDeEquipamentos);
        }

        // GET: Equipamentos/Consulta
        // Esta Action exibe a lista de equipamentos cadastrados
        public async Task<IActionResult> Consulta()
        {
            // Busca todos os equipamentos e inclui (join) o usuário vinculado, se houver
            var listaDeEquipamentos = await _context.Equipamentos
                .Include(e => e.Usuario)
                .ToListAsync();

            // Retorna a View com a lista
            return View(listaDeEquipamentos);
        }

        // GET: Equipamentos/Criar
        // Exibe o formulário de criação de novo equipamento
        // POST: Equipamentos/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([Bind("Descricao,Serie,Modelo,DataCompra,DataFimGarantia,Status,FrequenciaManutencao,DiasAlertaManutencao,EnviarEmailAlerta,ImagemUpload,AnexoUpload")] EquipamentoModel equipamento)
        {
            // O ModelState.IsValid verifica se todos os campos marcados como [Required] no Model foram preenchidos.
            // --- LÓGICA PARA UPLOAD DA IMAGEM ---
            if (equipamento.ImagemUpload != null)
            {
                string pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images");
                string nomeFicheiroUnico = Guid.NewGuid().ToString() + "_" + equipamento.ImagemUpload.FileName;
                string caminhoCompleto = Path.Combine(pastaUploads, nomeFicheiroUnico);

                if (!Directory.Exists(pastaUploads))
                {
                    Directory.CreateDirectory(pastaUploads);
                }
                using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
                {
                    await equipamento.ImagemUpload.CopyToAsync(fileStream);
                }
                equipamento.ImagemUrl = "/uploads/images/" + nomeFicheiroUnico;
            }

            // --- LÓGICA PARA UPLOAD DO ANEXO ---
            if (equipamento.AnexoUpload != null)
            {
                string pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "anexos");
                string nomeFicheiroUnico = Guid.NewGuid().ToString() + "_" + equipamento.AnexoUpload.FileName;
                string caminhoCompleto = Path.Combine(pastaUploads, nomeFicheiroUnico);

                if (!Directory.Exists(pastaUploads))
                {
                    Directory.CreateDirectory(pastaUploads);
                }

                using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
                {
                    await equipamento.AnexoUpload.CopyToAsync(fileStream);
                }
                equipamento.AnexoUrl = "/uploads/anexos/" + nomeFicheiroUnico;
            }

            // Adiciona a data de cadastro automaticamente
            equipamento.DataDeCadastro = DateTime.Now;

            _context.Add(equipamento);
            await _context.SaveChangesAsync();

            // ATUALIZAÇÃO: Cria a mensagem de sucesso com o ID do novo equipamento
            // e guarda no TempData para ser exibida na próxima página (Consulta).
            TempData["SuccessMessage"] = $"Item ({equipamento.CodigoItem}) criado com sucesso!";

            return RedirectToAction(nameof(Consulta));


            // Se o modelo for inválido, retorna para a mesma view para exibir os erros de validação.
        }
        // GET: Equipamentos/Excluir/5
        // Esta Action busca o equipamento pelo ID e mostra a página de confirmação (a que está no Canvas).
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipamento = await _context.Equipamentos
                .FirstOrDefaultAsync(m => m.CodigoItem == id);

            if (equipamento == null)
            {
                return NotFound();
            }

            return View(equipamento);
        }

        // POST: Equipamentos/Excluir/5
        // Esta Action é chamada quando o utilizador clica no botão "Confirmar Exclusão".
        // Ela apaga o registo do banco de dados.
        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var equipamento = await _context.Equipamentos.FindAsync(id);
            if (equipamento != null)
            {
                _context.Equipamentos.Remove(equipamento);
                await _context.SaveChangesAsync();
            }

            // Após excluir, redireciona o utilizador de volta para a lista.
            return RedirectToAction(nameof(Consulta));
        }
        // GET: Equipamentos/Consulta
        // GET: Equipamentos/Editar/5
        // GET: Equipamentos/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            // Verifica se o Id foi passado
            if (id == null)
            {
                return NotFound();
            }

            // Busca o equipamento no banco
            var equipamento = await _context.Equipamentos.FindAsync(id);
            if (equipamento == null)
            {
                return NotFound();
            }

            // Retorna a view Editar com o equipamento
            return View(equipamento);
        }

        // POST: Equipamentos/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, EquipamentoModel equipamento)
        {
            // Verifica se o Id da URL corresponde ao Id do modelo
            if (id != equipamento.CodigoItem)
            {
                return NotFound();
            }
            try
            {
                // Busca o equipamento original sem rastrear para manter URLs antigas
                var equipamentoOriginal = await _context.Equipamentos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.CodigoItem == id);

                // --- UPLOAD DE IMAGEM ---
                if (equipamento.ImagemUpload != null)
                {
                    string pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images");
                    string nomeFicheiroUnico = Guid.NewGuid() + "_" + equipamento.ImagemUpload.FileName;
                    string caminhoCompleto = Path.Combine(pastaUploads, nomeFicheiroUnico);

                    // Cria pasta se não existir
                    if (!Directory.Exists(pastaUploads))
                        Directory.CreateDirectory(pastaUploads);

                    // Salva arquivo
                    using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
                    {
                        await equipamento.ImagemUpload.CopyToAsync(fileStream);
                    }

                    // Atualiza URL no modelo
                    equipamento.ImagemUrl = "/uploads/images/" + nomeFicheiroUnico;
                }
                else
                {
                    // Mantém URL antiga caso não haja novo upload
                    equipamento.ImagemUrl = equipamentoOriginal?.ImagemUrl;
                }

                // --- UPLOAD DE ANEXO ---
                if (equipamento.AnexoUpload != null)
                {
                    string pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "anexos");
                    string nomeFicheiroUnico = Guid.NewGuid() + "_" + equipamento.AnexoUpload.FileName;
                    string caminhoCompleto = Path.Combine(pastaUploads, nomeFicheiroUnico);

                    if (!Directory.Exists(pastaUploads))
                        Directory.CreateDirectory(pastaUploads);

                    using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
                    {
                        await equipamento.AnexoUpload.CopyToAsync(fileStream);
                    }

                    equipamento.AnexoUrl = "/uploads/anexos/" + nomeFicheiroUnico;
                }
                else
                {
                    // Mantém URL antiga caso não haja novo upload
                    equipamento.AnexoUrl = equipamentoOriginal?.AnexoUrl;
                }

                // --- Ajuste de campos vazios ---
                if (string.IsNullOrEmpty(equipamento.FrequenciaManutencao))
                {
                    equipamento.FrequenciaManutencao = null;
                }

                // Atualiza equipamento no banco
                _context.Update(equipamento);
                await _context.SaveChangesAsync();

                // Mensagem de sucesso
                TempData["SuccessMessage"] = "Equipamento atualizado com sucesso!";
            }
            catch (DbUpdateConcurrencyException)
            {
                // Se o equipamento não existir mais, retorna 404
                if (!EquipamentoExists(equipamento.CodigoItem))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Redireciona para a página de consulta
            return RedirectToAction(nameof(Consulta));
        }

        // Se o modelo for inválido, retorna para a mesma view exibindo erros

        // Verifica se o equipamento existe no banco
        private bool EquipamentoExists(int id)
        {
            return _context.Equipamentos.Any(e => e.CodigoItem == id);
        }


        // OBSERVAÇÃO: Action de APOIO que busca dados para o JavaScript da tela de descarte.
        [HttpGet]
        public async Task<IActionResult> GetEquipamentoDados(int id)
        {
            var equipamento = await _context.Equipamentos.FindAsync(id);

            if (equipamento == null)
            {
                return Json(null);
            }

            return Json(new
            {
                imageUrl = string.IsNullOrEmpty(equipamento.ImagemUrl) ? null : Url.Content("~" + equipamento.ImagemUrl),
                descricao = equipamento.Descricao
            });
        }
    }
}
