using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    /// <summary>
    /// Controller responsável pela gestão completa dos registos de descarte.
    /// </summary>
    public class DescarteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuditService _auditService;
        public DescarteController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IAuditService auditService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _auditService = auditService;
        }

        // GET: Descarte/Consulta
        // OBSERVAÇÃO: Action principal que exibe a lista de descartes e aplica os filtros da pesquisa.
        public async Task<IActionResult> Consulta(int? id, string equipamento, string empresaColetora, string cnpj, string responsavel, DateTime? dataColeta, DateTime? dataCadastro, string status, string observacao)
        {
            var query = _context.Descartes.Include(d => d.Equipamento).AsQueryable();

            if (id.HasValue)
            {
                query = query.Where(d => d.Id == id.Value);
            }
            if (!string.IsNullOrEmpty(equipamento))
            {
                query = query.Where(d => d.Equipamento.Descricao.Contains(equipamento) || d.Equipamento.CodigoItem.ToString().Contains(equipamento));
            }
            if (!string.IsNullOrEmpty(empresaColetora))
            {
                query = query.Where(d => d.EmpresaColetora.Contains(empresaColetora));
            }
            if (!string.IsNullOrEmpty(cnpj))
            {
                query = query.Where(d => d.CnpjEmpresa.Contains(cnpj));
            }
            if (!string.IsNullOrEmpty(responsavel))
            {
                query = query.Where(d => d.PessoaResponsavelColeta.Contains(responsavel));
            }
            if (dataColeta.HasValue)
            {
                query = query.Where(d => d.DataColeta.Date == dataColeta.Value.Date);
            }
            if (dataCadastro.HasValue)
            {
                query = query.Where(d => d.DataDeCadastro.Date == dataCadastro.Value.Date);
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
            }
            if (!string.IsNullOrEmpty(observacao))
            {
                query = query.Where(d => d.Observacao.Contains(observacao));
            }

            return View(await query.OrderByDescending(d => d.DataDeCadastro).ToListAsync());
        }

        // GET: Descarte/Detalhes/5
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();
            var descarte = await _context.Descartes
                .Include(d => d.Equipamento)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (descarte == null) return NotFound();
            return View(descarte);
        }

        // GET: Descarte/Criar
        public async Task<IActionResult> Criar()
        {
            await PopulaEquipamentosViewData();
            return View();
        }

        // POST: Descarte/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(DescarteModel descarte)
        {
           
                // Busca a descrição do equipamento selecionado para guardar no registo de descarte
                var equipamentoSelecionado = await _context.Equipamentos.FindAsync(descarte.EquipamentoId);
                if (equipamentoSelecionado != null)
                {
                    descarte.Observacao = equipamentoSelecionado.Descricao;
                }

                if (descarte.CertificadoUpload != null)
                {
                    descarte.CertificadoUrl = await SalvarFicheiro(descarte.CertificadoUpload, "certificados");
                }

                descarte.DataDeCadastro = DateTime.Now;
                _context.Add(descarte);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Registo de descarte ({descarte.Id}) Descrição({descarte.Descricao}) criado com sucesso!";
                return RedirectToAction(nameof(Consulta));
            
          
        }

        // GET: Descarte/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var descarte = await _context.Descartes.Include(d => d.Equipamento).FirstOrDefaultAsync(d => d.Id == id);
            if (descarte == null) return NotFound();
            return View(descarte);
        }

        // POST: Descarte/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, DescarteModel descarteModel)
        {
           
                try
                {
                    var descarteOriginal = await _context.Descartes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
                    if (descarteModel.CertificadoUpload != null)
                    {
                        descarteModel.CertificadoUrl = await SalvarFicheiro(descarteModel.CertificadoUpload, "certificados");
                    }
                    else
                    {
                        descarteModel.CertificadoUrl = descarteOriginal.CertificadoUrl; // Mantém o ficheiro antigo se nenhum novo for enviado
                    }

                    _context.Update(descarteModel);
                    await _context.SaveChangesAsync();

                var userId = await GetCurrentUserId();
                if (userId.HasValue)
                {
                    await _auditService.RegistrarAcao(
                        userId.Value,
                        "Editou Descarte", // Descrição da ação
                        $"Descarte editado: ID={descarteModel.Id}, Descrição={descarteModel.Descricao}" // Detalhes exibidos nas atividades recentes
                    );
                }
            }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DescarteExists(descarteModel.Id)) return NotFound();
                    else throw;
                }
                TempData["SuccessMessage"] = "Registo de descarte atualizado com sucesso!";
                return RedirectToAction(nameof(Consulta));
          
        }

        // GET: Descarte/Excluir/5
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null) return NotFound();
            var descarte = await _context.Descartes.Include(d => d.Equipamento).FirstOrDefaultAsync(m => m.Id == id);
            if (descarte == null) return NotFound();
            return View(descarte);
        }

        // POST: Descarte/Excluir/5
        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var descarte = await _context.Descartes.FindAsync(id);
            _context.Descartes.Remove(descarte);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Registo de descarte excluído com sucesso!";

            var userId = await GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditService.RegistrarAcao(
                    userId.Value,
                    "Excluiu Descarte", // Descrição da ação
                    $"Descarte Excluido: ID={descarte.Id}, Descrição={descarte.Descricao}" // Detalhes exibidos nas atividades recentes
                );
            }
            return RedirectToAction(nameof(Consulta));
        }

        // --- MÉTODOS AUXILIARES ---

        private bool DescarteExists(int id)
        {
            return _context.Descartes.Any(e => e.Id == id);
        }

        // OBSERVAÇÃO: Método auxiliar para popular o dropdown de equipamentos.
        private async Task PopulaEquipamentosViewData(object selectedItem = null)
        {
            var equipamentos = await _context.Equipamentos.OrderBy(e => e.Descricao).ToListAsync();
            ViewData["EquipamentoId"] = new SelectList(equipamentos, "CodigoItem", "Descricao", selectedItem);
        }
        // Esse método é reutilizado para registrar logs de criação, edição e exclusão.
        private async Task<int?> GetCurrentUserId()
        {
            var userEmail = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
                return null;

            var user = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            return user?.Id;
        }
        // OBSERVAÇÃO: Método auxiliar para salvar ficheiros de upload.
        private async Task<string> SalvarFicheiro(IFormFile ficheiro, string subpasta)
        {
            string pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", subpasta);
            if (!Directory.Exists(pastaUploads))
            {
                Directory.CreateDirectory(pastaUploads);
            }

            string nomeUnico = Guid.NewGuid().ToString() + "_" + ficheiro.FileName;
            string caminhoCompleto = Path.Combine(pastaUploads, nomeUnico);

            using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await ficheiro.CopyToAsync(fileStream);
            }

            return $"/uploads/{subpasta}/{nomeUnico}";
        }
    }
}

