using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    public class EquipamentosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuditService _auditService; // Injeta o serviço de auditoria

        public EquipamentosController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IAuditService auditService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _auditService = auditService;
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
            return View(await query.ToListAsync());
        }
        #endregion

        #region Detalhes
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();

            var equipamento = await _context.Equipamentos
                .Include(e => e.TipoEquipamento)
                .FirstOrDefaultAsync(e => e.CodigoItem == id);

            if (equipamento == null) return NotFound();

            return View(equipamento);
        }
        #endregion

        #region Criar
        public async Task<IActionResult> Criar()
        {
            await PopulaTiposEquipamentoViewData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([Bind("Descricao,TipoEquipamentoId,Serie,Modelo,DataCompra,VidaUtilFim,Status,FrequenciaManutencao,DataUltimaManutencao,ImagemUrl,AnexoUrl,DiasAlertaManutencao,EnviarEmailAlerta")] EquipamentoModel equipamento, IFormFile ImagemUpload, IFormFile AnexoUpload)
        {
        

            // Calcular VidaUtilFim se necessário
            var dataBase = equipamento.DataUltimaManutencao ?? equipamento.DataCompra ?? DateTime.Now;
            if (equipamento.VidaUtilFim == null && equipamento.VidaUtilAnos.HasValue)
            {
                equipamento.VidaUtilFim = dataBase.AddYears(equipamento.VidaUtilAnos.Value);
            }

            // Salvar arquivos
            if (ImagemUpload != null)
                equipamento.ImagemUrl = await SalvarFicheiro(ImagemUpload, "imagens");

            if (AnexoUpload != null)
                equipamento.AnexoUrl = await SalvarFicheiro(AnexoUpload, "anexos");

            equipamento.DataDeCadastro = DateTime.Now;
            
            _context.Add(equipamento);
            await _context.SaveChangesAsync();

            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditService.RegistrarAcao(userId.Value, "Criou Equipamento", $"ID: {equipamento.CodigoItem}");
            }

            TempData["SuccessMessage"] = $"Item ({equipamento.CodigoItem}) criado com sucesso!";
            return RedirectToAction(nameof(Consulta));
        }
        #endregion

        #region Editar
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();

            var equipamento = await _context.Equipamentos.FindAsync(id);
            if (equipamento == null) return NotFound();

            await PopulaTiposEquipamentoViewData(equipamento.TipoEquipamentoId);
            return View(equipamento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("Descricao,TipoEquipamentoId,Serie,Modelo,DataCompra,VidaUtilFim,Status,FrequenciaManutencao,DataUltimaManutencao,ImagemUrl,AnexoUrl,DiasAlertaManutencao,EnviarEmailAlerta,VidaUtilAnos")] EquipamentoModel equipamento, IFormFile ImagemUpload, IFormFile AnexoUpload)
        {
            if (!EquipamentoExists(id)) return NotFound();

            var equipamentoOriginal = await _context.Equipamentos.FirstOrDefaultAsync(e => e.CodigoItem == id);
            if (equipamentoOriginal == null) return NotFound();

            try
            {
                // Atualizar campos comuns
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

                // Calcular VidaUtilFim se necessário
                var dataBase = equipamento.DataUltimaManutencao ?? equipamento.DataCompra ?? DateTime.Now;
                if (equipamento.VidaUtilFim == null && equipamento.VidaUtilAnos.HasValue)
                {
                    equipamentoOriginal.VidaUtilFim = dataBase.AddYears(equipamento.VidaUtilAnos.Value);
                }

                // Salvar arquivos se houver
                if (ImagemUpload != null)
                    equipamentoOriginal.ImagemUrl = await SalvarFicheiro(ImagemUpload, "imagens");

                if (AnexoUpload != null)
                    equipamentoOriginal.AnexoUrl = await SalvarFicheiro(AnexoUpload, "anexos");

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Equipamento atualizado com sucesso!";

                var userId = await GetCurrentUserId();
                if (userId.HasValue)
                {
                    await _auditService.RegistrarAcao(userId.Value, "Editou Equipamento", $"ID: {equipamento.CodigoItem}");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipamentoExists(equipamento.CodigoItem)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Consulta));
        }
        #endregion

        #region Excluir
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null) return NotFound();

            var equipamento = await _context.Equipamentos
                .Include(e => e.TipoEquipamento)
                .FirstOrDefaultAsync(e => e.CodigoItem == id);

            if (equipamento == null) return NotFound();

            return View(equipamento);
        }

        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var equipamento = await _context.Equipamentos.FindAsync(id);
            if (equipamento != null)
            {
                _context.Equipamentos.Remove(equipamento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Equipamento excluído com sucesso!";

                var userId = await GetCurrentUserId();
                if (userId.HasValue)
                {
                    await _auditService.RegistrarAcao(userId.Value, "Excluiu Equipamento", $"ID: {equipamento.CodigoItem}");
                }

            }


            return RedirectToAction(nameof(Consulta));
        }

        // --- MÉTODO AUXILIAR ---
        private async Task<int?> GetCurrentUserId()
        {
            // Obtém o e-mail do utilizador a partir do cookie de autenticação
            var userEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return null;
            }
            // Busca o utilizador na base de dados pelo e-mail para encontrar o seu ID
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == userEmail);
            return user?.Id;
        }

        [HttpGet]
        public async Task<IActionResult> GetEquipamentoDados(int id)
        {
            var equipamento = await _context.Equipamentos.FindAsync(id);
            if (equipamento == null) return Json(null);

            return Json(new
            {
                imageUrl = string.IsNullOrEmpty(equipamento.ImagemUrl) ? null : Url.Content("~" + equipamento.ImagemUrl),
                descricao = equipamento.Descricao
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
            ViewData["TipoEquipamentoId"] = new SelectList(tiposQuery, "Id", "Nome", selectedType);
        }

        private async Task<string> SalvarFicheiro(IFormFile ficheiro, string subpasta)
        {
            if (ficheiro == null || ficheiro.Length == 0) return null;

            string pastaDestino = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", subpasta);
            if (!Directory.Exists(pastaDestino))
                Directory.CreateDirectory(pastaDestino);

            string nomeFicheiroUnico = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ficheiro.FileName);
            string caminhoCompleto = Path.Combine(pastaDestino, nomeFicheiroUnico);

            using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await ficheiro.CopyToAsync(fileStream);
            }

            return $"/uploads/{subpasta}/{nomeFicheiroUnico}";
        }
        #endregion
    }
}
