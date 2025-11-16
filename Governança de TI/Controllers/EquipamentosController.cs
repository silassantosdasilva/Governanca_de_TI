using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Governança_de_TI.Views.Services.Gamificacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // Para List<SelectListItem>
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    // [Authorize] // Descomente se precisar de autorização
    public class EquipamentosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuditService _auditService;
        private readonly IGamificacaoService _gamificacaoService; // <<< SERVIÇO DE GAMIFICAÇÃO

        public EquipamentosController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IAuditService auditService,
            IGamificacaoService gamificacaoService) // <<< INJETADO AQUI
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _auditService = auditService;
            _gamificacaoService = gamificacaoService; // <<< ATRIBUÍDO AQUI
        }

        #region Consulta
        public async Task<IActionResult> Consulta(int? codigoItem, string descricao, int? tipoEquipamentoId, string status)
        {
            var query = _context.Equipamentos.Include(e => e.TipoEquipamento).AsQueryable();

            if (codigoItem.HasValue)
                query = query.Where(e => e.CodigoItem == codigoItem.Value);

            if (!string.IsNullOrEmpty(descricao))
                query = query.Where(e => e.Descricao.Contains(descricao));

            if (tipoEquipamentoId.HasValue)
                query = query.Where(e => e.TipoEquipamentoId == tipoEquipamentoId.Value);

            if (!string.IsNullOrEmpty(status) && status != "Todos...")
                query = query.Where(e => e.Status == status);

            await PopulaTiposEquipamentoViewData(tipoEquipamentoId);

            // Guarda filtros no ViewBag para repopular a view
            ViewBag.FiltroCodigoItem = codigoItem;
            ViewBag.FiltroDescricao = descricao;
            ViewBag.FiltroStatus = status;

            return View(await query.OrderBy(e => e.CodigoItem).ToListAsync());
        }
        #endregion


        public async Task<IActionResult> Detalhes(int id)
        {
            var equipamento = await _context.Equipamentos
                .Include(e => e.TipoEquipamento)
                .FirstOrDefaultAsync(e => e.CodigoItem == id);

            if (equipamento == null)
                return NotFound();

            // Detecta se é uma chamada AJAX (modal)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Retorna apenas a partial, sem layout
                return PartialView("_DetalhesEquipamentosPartial", equipamento);
            }

            // Caso contrário, abre página normal (fallback)
            return View(equipamento);
        }

        #region Criar
        public async Task<IActionResult> Criar()
        {
            await PopulaTiposEquipamentoViewData();

            var model = new EquipamentoModel
            {
                DataCompra = DateTime.Today
            };

            // 🔹 Verifica se a requisição veio via AJAX (abrindo em modal)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Retorna apenas o conteúdo da partial sem layout
                return PartialView("_CriarEquipamentosPartial", model);
            }

            // 🔹 Caso contrário, abre a tela completa (modo tradicional)
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(
         [Bind("Descricao,TipoEquipamentoId,Serie,Modelo,DataCompra,VidaUtilAnos,DataFimGarantia,Status,FrequenciaManutencao,DataUltimaManutencao,DiasAlertaManutencao,EnviarEmailAlerta")]
         EquipamentoModel equipamento,
         IFormFile ImagemUpload,
         IFormFile AnexoUpload)
        {
            // --- Validação do model ---
            if (equipamento.TipoEquipamentoId == 0)
            {
                ModelState.AddModelError("TipoEquipamentoId", "O campo Tipo de Equipamento é obrigatório.");
            }

            // --- Regras de negócio ---
            var dataBase = equipamento.DataCompra ?? DateTime.Now;
            if (equipamento.VidaUtilAnos.HasValue && equipamento.VidaUtilAnos > 0)
                equipamento.VidaUtilFim = dataBase.AddYears(equipamento.VidaUtilAnos.Value);

            if (ImagemUpload != null)
                equipamento.ImagemUrl = await SalvarFicheiro(ImagemUpload, "imagens");

            if (AnexoUpload != null)
                equipamento.AnexoUrl = await SalvarFicheiro(AnexoUpload, "anexos");

            equipamento.DataDeCadastro = DateTime.Now;
            equipamento.Status ??= "Ativo";

            _context.Add(equipamento);
            await _context.SaveChangesAsync();

            // --- Gamificação ---
            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                await _gamificacaoService.AdicionarPontosAsync(userId.Value, "CadastrouEquipamento", 5);
                _ = Task.Run(() => _auditService.RegistrarAcao(userId.Value, "Criou Equipamento",
                    $"ID: {equipamento.CodigoItem} - {equipamento.Descricao}"));
            }

            // 🔹 Se for AJAX (modal), retorna JSON em vez de recarregar página
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = $"Equipamento '{equipamento.Descricao}' cadastrado com sucesso!"
                });
            }
           
                var id = await GetCurrentUserId();
                if (id.HasValue)
                  await _auditService.RegistrarAcao(id.Value, "Criou Equipamento", $"ID equipamento: {equipamento.CodigoItem}");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    sucesso = true,
                    message = "Equipamento criado com sucesso!",
                    notificacao = new
                    {
                        titulo = "Gamificação",
                        texto = "Você ganhou +5 pontos!",
                        tipo = "success"
                    }
                });
            }
         
            // 🔹 Fluxo normal (sem AJAX)
            TempData["SuccessMessage"] = $"Item ({equipamento.CodigoItem}) criado com sucesso!";
            return RedirectToAction(nameof(Consulta));


        }


        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
                return NotFound();

            var equipamento = await _context.Equipamentos
                .Include(e => e.TipoEquipamento)
                .FirstOrDefaultAsync(e => e.CodigoItem == id);

            if (equipamento == null)
                return NotFound();

            await PopulaTiposEquipamentoViewData(equipamento.TipoEquipamentoId);

            // 🔹 Se for chamada AJAX (modal), retorna a partial
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditarEquipamentosPartial", equipamento);
            }

            // 🔹 Caso contrário, renderiza a página completa
            return View(equipamento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id,
            [Bind("CodigoItem,Descricao,TipoEquipamentoId,Serie,Modelo,DataCompra,VidaUtilAnos,DataFimGarantia,Status,FrequenciaManutencao,DataUltimaManutencao,DiasAlertaManutencao,EnviarEmailAlerta,DataDeCadastro,ImagemUrl,AnexoUrl")] EquipamentoModel equipamento,
            IFormFile ImagemUpload, IFormFile AnexoUpload)
        {
            //if (id != equipamento.CodigoItem) return BadRequest();

            // Busca a entidade original com tracking
            var equipamentoOriginal = await _context.Equipamentos.FirstOrDefaultAsync(e => e.CodigoItem == id);
            if (equipamentoOriginal == null) return NotFound();

            if (equipamento.TipoEquipamentoId == 0)
            {
                ModelState.AddModelError("TipoEquipamentoId", "O campo Tipo de Equipamento é obrigatório.");
            }

           
                try
                {
                    // Atualiza campos
                    equipamentoOriginal.Descricao = equipamento.Descricao;
                    equipamentoOriginal.TipoEquipamentoId = equipamento.TipoEquipamentoId;
                    equipamentoOriginal.Serie = equipamento.Serie;
                    equipamentoOriginal.Modelo = equipamento.Modelo;
                    equipamentoOriginal.DataCompra = equipamento.DataCompra;
                    equipamentoOriginal.DataUltimaManutencao = equipamento.DataUltimaManutencao;
                    equipamentoOriginal.Status = equipamento.Status;
                    equipamentoOriginal.FrequenciaManutencao = equipamento.FrequenciaManutencao;
                    equipamentoOriginal.DiasAlertaManutencao = equipamento.DiasAlertaManutencao;
                    equipamentoOriginal.EnviarEmailAlerta = equipamento.EnviarEmailAlerta;
                    equipamentoOriginal.VidaUtilAnos = equipamento.VidaUtilAnos;
                    equipamentoOriginal.DataFimGarantia = equipamento.DataFimGarantia;

                    // Recalcular VidaUtilFim
                    var dataBase = equipamentoOriginal.DataCompra ?? equipamentoOriginal.DataDeCadastro;
                    if (equipamentoOriginal.VidaUtilAnos.HasValue && equipamentoOriginal.VidaUtilAnos > 0)
                    {
                        equipamentoOriginal.VidaUtilFim = dataBase.AddYears(equipamentoOriginal.VidaUtilAnos.Value);
                    }
                    else
                    {
                        equipamentoOriginal.VidaUtilFim = null; // Limpa se não houver anos
                    }

                    // Salvar arquivos (mantém os antigos se nada for enviado)
                    if (ImagemUpload != null)
                        equipamentoOriginal.ImagemUrl = await SalvarFicheiro(ImagemUpload, "imagens");

                    if (AnexoUpload != null)
                        equipamentoOriginal.AnexoUrl = await SalvarFicheiro(AnexoUpload, "anexos");

                    // Não precisa chamar _context.Update() pois a entidade 'equipamentoOriginal' está sendo rastreada
                    await _context.SaveChangesAsync();
   

       
                   var idEquip = await GetCurrentUserId();
                   if (idEquip.HasValue)
                    await _auditService.RegistrarAcao(idEquip.Value, "Editou Equipamento", $"ID: {equipamentoOriginal.CodigoItem}, Desc: {equipamentoOriginal.Descricao}");


            }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EquipamentoExists(equipamento.CodigoItem)) return NotFound();
                    else throw;
                }
              
           

            // Se inválido, recarrega dropdown e retorna
            //await PopulaTiposEquipamentoViewData(equipamento.TipoEquipamentoId);
            TempData["SuccessMessage"] = $"Equipamento {id} atualizado com sucesso!";
            return RedirectToAction(nameof(Consulta));
        }
        #endregion

        #region Excluir
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null)
                return NotFound();

            var equipamento = await _context.Equipamentos
                .Include(e => e.TipoEquipamento)
                .FirstOrDefaultAsync(e => e.CodigoItem == id);

            if (equipamento == null)
                return NotFound();

            // 🔹 Se for uma chamada AJAX (abertura via modal)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ExcluirEquipamentosPartial", equipamento);
            }

            // 🔹 Caso contrário, navegação normal (rota direta)
            return View(equipamento);
        }


        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var equipamento = await _context.Equipamentos.FindAsync(id);
            if (equipamento == null)
                return NotFound();

            bool hasDescartes = await _context.Descartes.AnyAsync(d => d.EquipamentoId == id);
            if (hasDescartes)
            {
                var msg = "Nao e possivel excluir este equipamento, pois existem descartes associados a ele.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = msg });

                TempData["ErrorMessage"] = msg;
                return RedirectToAction(nameof(Consulta));
            }

            _context.Equipamentos.Remove(equipamento);
            await _context.SaveChangesAsync();

            // Caso AJAX (modal)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = $"Equipamento '{equipamento.Descricao}' foi excluido com sucesso!"
                });
            }
     

            var idEquip = await GetCurrentUserId();
            if (idEquip.HasValue)
                await _auditService.RegistrarAcao(idEquip.Value, "Excluiu Equipamento", $"Equipamento Excluido: ID={equipamento.Descricao}");

            // Caso tradicional
            TempData["SuccessMessage"] = $"Equipamento excluido com sucesso!";
            return RedirectToAction(nameof(Consulta));
        }
        #endregion

        #region API (GET)
        // API chamada pelo formulário de Descarte para popular a imagem/descrição
        [HttpGet]
        public async Task<IActionResult> GetEquipamentoDados(int id)
        {
            var equipamento = await _context.Equipamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.CodigoItem == id);

            if (equipamento == null)
            {
                return Json(null);
            }

            string imageUrl;

            if (string.IsNullOrEmpty(equipamento.ImagemUrl))
            {
                // Caminho padrão correto
                imageUrl = Url.Content("~/img/default-equip.png");
            }
            else
            {
                // SEMPRE gera URL válida (transforma ~/uploads/... em /uploads/...)
                imageUrl = Url.Content($"~{equipamento.ImagemUrl}");
            }

            return Json(new
            {
                descricao = equipamento.Descricao,
                imageUrl = imageUrl
            });
        }




        #endregion

        #region Métodos Auxiliares
        private bool EquipamentoExists(int id)
        {
            return _context.Equipamentos.Any(e => e.CodigoItem == id);
        }

        private async Task PopulaTiposEquipamentoViewData(object selectedType = null)
        {
            var tiposQuery = await _context.TiposEquipamento.OrderBy(t => t.Nome).ToListAsync();
            // Adiciona "Selecione..."
            var selectList = new List<SelectListItem> { new SelectListItem { Value = "", Text = "Selecione..." } };
            selectList.AddRange(tiposQuery.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Nome }));

            ViewData["TipoEquipamentoId"] = new SelectList(selectList, "Value", "Text", selectedType?.ToString() ?? "");
        }

        private async Task<string> SalvarFicheiro(IFormFile ficheiro, string subpasta)
        {
            if (ficheiro == null || ficheiro.Length == 0) return null;

            string pastaDestino = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", subpasta);
            Directory.CreateDirectory(pastaDestino); // Cria se não existir

            string nomeFicheiroUnico = Guid.NewGuid().ToString() + Path.GetExtension(ficheiro.FileName);
            string caminhoCompleto = Path.Combine(pastaDestino, nomeFicheiroUnico);

            using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await ficheiro.CopyToAsync(fileStream);
            }

            return $"/uploads/{subpasta}/{nomeFicheiroUnico}"; // Retorna caminho relativo
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
        #endregion

        private int GetUserId()
        {
            var claim = User.FindFirst("UserId")?.Value
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(claim, out int id))
                return id;

            return 0; // Ou lance uma exceção
        }

    }
}

