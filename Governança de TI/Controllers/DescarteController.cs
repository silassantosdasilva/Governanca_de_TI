using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Governança_de_TI.Views.Services.Gamificacao;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http; // Para IFormFile
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
    // [Authorize] // Adicione se esta área for restrita
    public class DescarteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuditService _auditService;
        private readonly IGamificacaoService _gamificacaoService; // <<< NOVO

        public DescarteController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IAuditService auditService,
            IGamificacaoService gamificacaoService) // <<< NOVO
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _auditService = auditService;
            _gamificacaoService = gamificacaoService; // <<< NOVO
        }

        // GET: Descarte/Consulta
        public async Task<IActionResult> Consulta(int? id, string equipamento, string empresaColetora, string cnpj, string responsavel, DateTime? dataColeta, DateTime? dataCadastro, string status, string observacao)
        {
            var query = _context.Descartes.Include(d => d.Equipamento).AsQueryable();

            if (id.HasValue)
            {
                query = query.Where(d => d.Id == id.Value);
            }
            if (!string.IsNullOrEmpty(equipamento))
            {
                // Busca no ID ou na Descrição do equipamento relacionado
                query = query.Where(d => d.Equipamento.Descricao.Contains(equipamento) || d.Equipamento.CodigoItem.ToString().Contains(equipamento));
            }
            if (!string.IsNullOrEmpty(empresaColetora))
            {
                query = query.Where(d => d.EmpresaColetora.Contains(empresaColetora));
            }
            if (!string.IsNullOrEmpty(cnpj))
            {
                query = query.Where(d => d.CnpjEmpresa != null && d.CnpjEmpresa.Contains(cnpj));
            }
            if (!string.IsNullOrEmpty(responsavel))
            {
                query = query.Where(d => d.PessoaResponsavelColeta != null && d.PessoaResponsavelColeta.Contains(responsavel));
            }
            if (dataColeta.HasValue)
            {
                query = query.Where(d => d.DataColeta.Date == dataColeta.Value.Date);
            }
            if (dataCadastro.HasValue)
            {
                query = query.Where(d => d.DataDeCadastro.Date == dataCadastro.Value.Date);
            }
            if (!string.IsNullOrEmpty(status) && status != "Todos...") // Adicionado filtro para "Todos..."
            {
                query = query.Where(d => d.Status == status);
            }
            if (!string.IsNullOrEmpty(observacao))
            {
                query = query.Where(d => d.Observacao != null && d.Observacao.Contains(observacao));
            }

            // Guarda filtros no ViewBag para repopular a view
            ViewBag.FiltroId = id;
            ViewBag.FiltroEquipamento = equipamento;
            ViewBag.FiltroEmpresa = empresaColetora;
            ViewBag.FiltroCnpj = cnpj;
            ViewBag.FiltroResponsavel = responsavel;
            ViewBag.FiltroDataColeta = dataColeta?.ToString("yyyy-MM-dd");
            ViewBag.FiltroDataCadastro = dataCadastro?.ToString("yyyy-MM-dd");
            ViewBag.FiltroStatus = status;
            ViewBag.FiltroObservacao = observacao;


            return View(await query.OrderByDescending(d => d.DataDeCadastro).ToListAsync());
        }

        // GET: Descarte/Detalhes/5
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();
            var descarte = await _context.Descartes
                .Include(d => d.Equipamento) // Inclui o equipamento relacionado
                .FirstOrDefaultAsync(m => m.Id == id);
            if (descarte == null) return NotFound();
            return View(descarte);
        }

        // GET: Descarte/Criar
        public async Task<IActionResult> Criar()
        {
            await PopulaEquipamentosViewData();
            return View(new DescarteModel { DataColeta = DateTime.Today }); // Pré-popula data de hoje
        }

        // POST: Descarte/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Bind para propriedades e IFormFile explícito
        public async Task<IActionResult> Criar(
            [Bind("EquipamentoId,EmpresaColetora,CnpjEmpresa,PessoaResponsavelColeta,DataColeta,Status,Observacao")] DescarteModel descarte,
            IFormFile CertificadoUpload)
        {
            // Validação manual
            if (descarte.EquipamentoId == 0)
                ModelState.AddModelError("EquipamentoId", "Selecione o equipamento.");

            if (!ModelState.IsValid)
            {
                await PopulaEquipamentosViewData(descarte.EquipamentoId);
                return View(descarte); // Retorna com erros
            }

            // Busca o equipamento selecionado para copiar dados
            var equipamentoSelecionado = await _context.Equipamentos.AsNoTracking().FirstOrDefaultAsync(e => e.CodigoItem == descarte.EquipamentoId);
            if (equipamentoSelecionado != null)
            {
                descarte.Descricao = equipamentoSelecionado.Descricao; // Guarda a descrição
                descarte.ImagemEquipamentoUrl = equipamentoSelecionado.ImagemUrl; // Guarda a imagem

                // Atualiza o status do equipamento original para "Descartado"
                equipamentoSelecionado.Status = "Descartado";
                _context.Update(equipamentoSelecionado); // Marca o equipamento como modificado
            }

            if (CertificadoUpload != null)
            {
                descarte.CertificadoUrl = await SalvarFicheiro(CertificadoUpload, "certificados");
            }

            descarte.DataDeCadastro = DateTime.Now;
            if (string.IsNullOrEmpty(descarte.Status)) descarte.Status = "Agendado"; // Status padrão

            _context.Add(descarte);
            await _context.SaveChangesAsync(); // Salva o descarte E a atualização do equipamento

            var userId = await GetCurrentUserId();

            // --- [NOVO] GAMIFICAÇÃO ---
            if (userId.HasValue)
            {
                await _gamificacaoService.AdicionarPontosAsync(userId.Value, "CriouDescarteSustentavel", 20);
                TempData["GamificationMessage"] = "Uau! Você ganhou 20 pontos por registrar um descarte sustentável!";
            }
            // --- Fim Gamificação ---

            // Auditoria (em background)
            if (userId.HasValue)
            {
                _ = Task.Run(async () => {
                    await _auditService.RegistrarAcao(userId.Value, "Criou Descarte", $"ID={descarte.Id}, Equipamento ID={descarte.EquipamentoId}");
                });
            }

            TempData["SuccessMessage"] = $"Registo de descarte ({descarte.Id}) criado com sucesso!";
            return RedirectToAction(nameof(Consulta));
        }

        // GET: Descarte/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var descarte = await _context.Descartes.Include(d => d.Equipamento).FirstOrDefaultAsync(d => d.Id == id);
            if (descarte == null) return NotFound();

            // Popula o dropdown de equipamentos (incluindo o equipamento atual, mesmo que já descartado)
            await PopulaEquipamentosViewData(descarte.EquipamentoId, true);
            return View(descarte);
        }

        // POST: Descarte/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id,
            [Bind("Id,EquipamentoId,Descricao,ImagemEquipamentoUrl,EmpresaColetora,CnpjEmpresa,PessoaResponsavelColeta,DataColeta,Status,Observacao,CertificadoUrl,DataDeCadastro")] DescarteModel descarteModel,
            IFormFile CertificadoUpload)
        {
            if (id != descarteModel.Id) return NotFound();

            var descarteOriginal = await _context.Descartes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            if (descarteOriginal == null) return NotFound();

            if (descarteModel.EquipamentoId == 0)
                ModelState.AddModelError("EquipamentoId", "Selecione o equipamento.");

            if (ModelState.IsValid)
            {
                try
                {
                    // Mantém a data de cadastro original
                    descarteModel.DataDeCadastro = descarteOriginal.DataDeCadastro;

                    // Salva novo certificado se enviado, senão mantém o antigo
                    if (CertificadoUpload != null)
                    {
                        descarteModel.CertificadoUrl = await SalvarFicheiro(CertificadoUpload, "certificados");
                    }
                    else
                    {
                        descarteModel.CertificadoUrl = descarteOriginal.CertificadoUrl; // Mantém o antigo
                    }

                    // Se o equipamento foi trocado, busca novos dados
                    if (descarteOriginal.EquipamentoId != descarteModel.EquipamentoId)
                    {
                        var novoEquipamento = await _context.Equipamentos.AsNoTracking().FirstOrDefaultAsync(e => e.CodigoItem == descarteModel.EquipamentoId);
                        descarteModel.Descricao = novoEquipamento?.Descricao;
                        descarteModel.ImagemEquipamentoUrl = novoEquipamento?.ImagemUrl;

                        // Opcional: Reverter status do equipamento antigo?
                        // var equipamentoAntigo = await _context.Equipamentos.FindAsync(descarteOriginal.EquipamentoId);
                        // if(equipamentoAntigo != null) { equipamentoAntigo.Status = "Ativo"; _context.Update(equipamentoAntigo); }
                    }


                    _context.Update(descarteModel); // Atualiza o registro de descarte
                    await _context.SaveChangesAsync();

                    // Auditoria (background)
                    _ = Task.Run(async () => {
                        var userId = await GetCurrentUserId();
                        if (userId.HasValue)
                        {
                            await _auditService.RegistrarAcao(userId.Value, "Editou Descarte", $"Descarte editado: ID={descarteModel.Id}");
                        }
                    });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DescarteExists(descarteModel.Id)) return NotFound();
                    else throw;
                }
                TempData["SuccessMessage"] = "Registo de descarte atualizado com sucesso!";
                return RedirectToAction(nameof(Consulta));
            }

            // Se inválido, recarrega dropdowns e retorna
            await PopulaEquipamentosViewData(descarteModel.EquipamentoId, true);
            return View(descarteModel);
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
            if (descarte != null)
            {
                // Opcional: Deletar arquivo de certificado do servidor
                // if (!string.IsNullOrEmpty(descarte.CertificadoUrl)) { ... File.Delete ... }

                // Opcional: Reverter status do equipamento associado?
                var equipamento = await _context.Equipamentos.FindAsync(descarte.EquipamentoId);
                if (equipamento != null && equipamento.Status == "Descartado")
                {
                    equipamento.Status = "Ativo"; // Reverte para Ativo
                    _context.Update(equipamento);
                }

                _context.Descartes.Remove(descarte);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registo de descarte excluído com sucesso!";

                // Auditoria (background)
                _ = Task.Run(async () => {
                    var userId = await GetCurrentUserId();
                    if (userId.HasValue)
                    {
                        await _auditService.RegistrarAcao(userId.Value, "Excluiu Descarte", $"Descarte Excluido: ID={descarte.Id}");
                    }
                });
            }
            return RedirectToAction(nameof(Consulta));
        }

        // --- MÉTODOS AUXILIARES ---

        private bool DescarteExists(int id)
        {
            return _context.Descartes.Any(e => e.Id == id);
        }

        // Popula o dropdown de equipamentos
        private async Task PopulaEquipamentosViewData(object selectedItem = null, bool incluirDescartados = false)
        {
            var query = _context.Equipamentos.AsQueryable();

            if (!incluirDescartados)
            {
                // Por padrão, mostra apenas equipamentos que NÃO foram descartados
                query = query.Where(e => e.Status != "Descartado");
            }

            var equipamentos = await query.OrderBy(e => e.Descricao)
                                          .Select(e => new { e.CodigoItem, e.Descricao })
                                          .ToListAsync();

            var selectList = new List<SelectListItem> { new SelectListItem { Value = "", Text = "Selecione o equipamento..." } };
            selectList.AddRange(equipamentos.Select(e => new SelectListItem
            {
                Value = e.CodigoItem.ToString(),
                Text = $"{e.CodigoItem} - {e.Descricao}"
            }));

            ViewData["EquipamentoId"] = new SelectList(selectList, "Value", "Text", selectedItem?.ToString() ?? "");
        }

        // Obtém o ID do usuário logado
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

        // Salva arquivos de upload
        private async Task<string> SalvarFicheiro(IFormFile ficheiro, string subpasta)
        {
            if (ficheiro == null || ficheiro.Length == 0) return null;

            string pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", subpasta);
            Directory.CreateDirectory(pastaUploads); // Cria se não existir

            string nomeUnico = Guid.NewGuid().ToString() + Path.GetExtension(ficheiro.FileName);
            string caminhoCompleto = Path.Combine(pastaUploads, nomeUnico);

            using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await ficheiro.CopyToAsync(fileStream);
            }

            return $"/uploads/{subpasta}/{nomeUnico}"; // Retorna caminho relativo
        }
    }
}

