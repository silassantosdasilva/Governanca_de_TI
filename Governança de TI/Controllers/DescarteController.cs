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
        // GET: Descarte/Detalhes/5
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();

            var descarte = await _context.Descartes
                .Include(d => d.Equipamento)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (descarte == null) return NotFound();

            // Se for chamado via AJAX (modal)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_DetalhesDescartePartial", descarte);

            // Caso contrário (navegação normal)
            return View("_DetalhesDescartePartial", descarte);
        }


        // GET: Descarte/Criar
        // GET: Descarte/Criar
        public async Task<IActionResult> Criar()
        {
            await PopulaEquipamentosViewData();

            // Corrigido: agora instanciando o tipo correto
            var model = new DescarteModel
            {
                DataColeta = DateTime.Today
            };

            // 🔹 Se for uma requisição AJAX (carregada na modal)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Retorna a partial dentro da pasta Partials
                return PartialView("_CriarDescartePartial", model);
            }

            // 🔹 Caso seja acesso direto via /Descarte/Criar
            return View("_CriarDescartePartial", model);
        }

        // POST: Descarte/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Bind para propriedades e IFormFile explícito
        public async Task<IActionResult> Criar(
            [Bind("EquipamentoId,EmpresaColetora,CnpjEmpresa,PessoaResponsavelColeta,DataColeta,Status,Observacao,EmailEmpresa")] DescarteModel descarte,
            IFormFile CertificadoUpload)
        {
            // Validação manual
            if (descarte.EquipamentoId == 0)
                ModelState.AddModelError("EquipamentoId", "Selecione o equipamento.");

           
                await PopulaEquipamentosViewData(descarte.EquipamentoId);

                // Se veio de AJAX (modal), devolve a partial com os erros
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("_CriarDescartePartial", descarte);

        

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
        // GET: Descarte/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
                return NotFound();

            var descarte = await _context.Descartes
                .Include(d => d.Equipamento)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (descarte == null)
                return NotFound();

            // Popula o dropdown de equipamentos (mesmo padrão usado em criar, caso queira reuso futuro)
            await PopulaEquipamentosViewData(descarte.EquipamentoId, true);

            // --- 🔹 Retorna a partial se for requisição AJAX (abrindo em modal)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EditarDescartePartial", descarte);
            }

            // --- 🔹 Caso contrário (chamado direto), abre em tela completa (modo fallback)
            return View(descarte);
        }

        // POST: Descarte/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
         int id,
         [Bind("Id,EquipamentoId,Descricao,ImagemEquipamentoUrl,EmpresaColetora,CnpjEmpresa,EmailEmpresa,PessoaResponsavelColeta,DataColeta,Status,Observacao,CertificadoUrl,DataDeCadastro,Quantidade,EnviarEmail")]
    DescarteModel descarteModel,
         IFormFile CertificadoUpload)
        {
            if (id != descarteModel.Id)
                return NotFound();

            var descarteOriginal = await _context.Descartes
                .Include(d => d.Equipamento)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (descarteOriginal == null)
                return NotFound();

            // --- 🔹 Validações básicas
            if (descarteModel.EquipamentoId == 0)
                ModelState.AddModelError("EquipamentoId", "Selecione o equipamento.");
                await PopulaEquipamentosViewData(descarteModel.EquipamentoId, true);

            try
            {
                // Mantém informações que não podem ser alteradas
                descarteModel.DataDeCadastro = descarteOriginal.DataDeCadastro;

                // Atualiza informações do equipamento, caso ele tenha sido alterado
                if (descarteOriginal.EquipamentoId != descarteModel.EquipamentoId)
                {
                    var novoEquipamento = await _context.Equipamentos
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.CodigoItem == descarteModel.EquipamentoId);

                    if (novoEquipamento != null)
                    {
                        descarteModel.Descricao = novoEquipamento.Descricao;
                        descarteModel.ImagemEquipamentoUrl = novoEquipamento.ImagemUrl;
                    }
                }
                else
                {
                    descarteModel.Descricao = descarteOriginal.Descricao;
                    descarteModel.ImagemEquipamentoUrl = descarteOriginal.ImagemEquipamentoUrl;
                }

                // --- 🔹 Upload de novo certificado (se enviado)
                if (CertificadoUpload != null && CertificadoUpload.Length > 0)
                {
                    descarteModel.CertificadoUrl = await SalvarFicheiro(CertificadoUpload, "certificados");
                }

                // --- 🔹 Atualiza registro no banco
                _context.Update(descarteModel);
                await _context.SaveChangesAsync();

                // --- 🔹 Auditoria e Gamificação (opcional)
                var userId = await GetCurrentUserId();
                if (userId.HasValue)
                {
                    _ = Task.Run(async () =>
                    {
                        await _auditService.RegistrarAcao(userId.Value, "Editou Descarte", $"Descarte ID={descarteModel.Id} atualizado.");
                    });
                }

                TempData["SuccessMessage"] = $"Registo de descarte ({descarteModel.Id}) atualizado com sucesso!";
                return RedirectToAction(nameof(Consulta));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Descartes.Any(e => e.Id == descarteModel.Id))
                    return NotFound();
                else
                    throw;
            }
        }

        // GET: Descarte/Excluir/5
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null)
                return NotFound();

            var descarte = await _context.Descartes
                .Include(d => d.Equipamento)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (descarte == null)
                return NotFound();

            // 🧠 Detecta se veio via AJAX e força o retorno da Partial
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ExcluirDescartePartial", descarte);
            }

            // Se for navegação direta, exibe a página completa
            return View(descarte);
        }

        // POST: Descarte/Excluir/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var descarte = await _context.Descartes
                .Include(d => d.Equipamento)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (descarte == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Registro não encontrado." });
                return NotFound();
            }

            try
            {
                // Reverte status do equipamento, se necessário
                if (descarte.Equipamento != null && descarte.Equipamento.Status == "Descartado")
                {
                    descarte.Equipamento.Status = "Ativo";
                    _context.Update(descarte.Equipamento);
                }

                _context.Descartes.Remove(descarte);
                await _context.SaveChangesAsync();

                // Auditoria (em background)
                var userId = await GetCurrentUserId();
                if (userId.HasValue)
                {
                    _ = Task.Run(async () => {
                        await _auditService.RegistrarAcao(userId.Value, "Excluiu Descarte",
                            $"Registro ID={descarte.Id}, Equipamento={descarte.Equipamento?.Descricao}");
                    });
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = $"Registro de descarte ({descarte.Id}) excluído com sucesso!" });

                TempData["SuccessMessage"] = "Registo excluído com sucesso!";
                return RedirectToAction(nameof(Consulta));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Erro ao excluir o registro." });
                throw;
            }
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

