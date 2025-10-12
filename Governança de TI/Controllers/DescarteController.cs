using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers // Certifique-se de que o namespace está correto
{
    public class DescarteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DescarteController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Descarte
        // Agora esta Action mostra a lista de todos os descartes registados.
        public async Task<IActionResult> Index()
        {
            var descartes = await _context.Descartes.Include(d => d.Equipamento).ToListAsync();
            return View(descartes);
        }

        // GET: Descarte/Detalhes/5
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var descarte = await _context.Descartes
                .Include(d => d.Equipamento)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (descarte == null)
            {
                return NotFound();
            }

            return View(descarte);
        }


        public async Task<IActionResult> Consulta(string equipamento, DateTime? dataColeta, string empresaColetora, string cnpj, string responsavel, DateTime? dataCadastro, string observacao,int id, string status)
        {
            var query = _context.Descartes
                .Include(d => d.Equipamento) // Inclui os dados do equipamento relacionado
                .Include(d => d.Usuario)     // Inclui os dados do usuário relacionado
                .AsQueryable();

            if (id > 0)
            {
                query = query.Where(d => d.Id == id);
            }
            if (!string.IsNullOrEmpty(equipamento))
            {
                query = query.Where(d => d.Equipamento.CodigoItem.ToString().Contains(equipamento) || d.Equipamento.Descricao.Contains(equipamento));
            }
            if (dataColeta.HasValue)
            {
                var dataFim = dataColeta.Value.AddDays(1);
                query = query.Where(d => d.DataColeta >= dataColeta.Value && d.DataColeta < dataFim);
            }
            if (!string.IsNullOrEmpty(empresaColetora))
            {
                query = query.Where(d => d.EmpresaColetora.Contains(empresaColetora));
            }
            if (!string.IsNullOrEmpty(cnpj))
            {
                query = query.Where(d => d.CnpjEmpresa == cnpj);
            }
            if (!string.IsNullOrEmpty(responsavel))
            {
                query = query.Where(d => d.PessoaResponsavelColeta.Contains(responsavel));
            }
            if (dataCadastro.HasValue)
            {
                var dataFim = dataCadastro.Value.AddDays(1);
                query = query.Where(d => d.DataDeCadastro >= dataCadastro.Value && d.DataDeCadastro < dataFim);
            }
            if (!string.IsNullOrEmpty(observacao))
            {
                query = query.Where(d => d.Observacao.Contains(observacao));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status.Contains(status));
            }

            var descartes = await query.ToListAsync();
            return View(descartes);
        }

        // GET: Descarte/Criar
        public async Task<IActionResult> Criar()
        {
            ViewData["EquipamentoId"] = new SelectList(await _context.Equipamentos.ToListAsync(), "CodigoItem", "Descricao");
            return View();
        }

        // POST: Descarte/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([Bind("EquipamentoId,Descricao,Quantidade,DataColeta,EmpresaColetora,CnpjEmpresa,EmailEmpresa,PessoaResponsavelColeta,CertificadoUpload,EnviarEmail,UsuarioId,Status")] DescarteModel descarte)
        {
     
                if (descarte.CertificadoUpload != null)
                {
                    string pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "certificados");
                    string nomeFicheiroUnico = Guid.NewGuid().ToString() + "_" + descarte.CertificadoUpload.FileName;
                    string caminhoCompleto = Path.Combine(pastaUploads, nomeFicheiroUnico);

                    if (!Directory.Exists(pastaUploads))
                    {
                        Directory.CreateDirectory(pastaUploads);
                    }

                    using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
                    {
                        await descarte.CertificadoUpload.CopyToAsync(fileStream);
                    }
                    descarte.CertificadoUrl = "/uploads/certificados/" + nomeFicheiroUnico;
                }

                descarte.DataDeCadastro = DateTime.Now;
                _context.Add(descarte);
                await _context.SaveChangesAsync(); // O ID é gerado aqui e atribuído ao objeto 'descarte'

                // CORREÇÃO: A mensagem é criada DEPOIS de salvar, para que o ID esteja disponível.
                TempData["SuccessMessage"] = $"Registo de descarte ({descarte.Id}) criado com sucesso!";

            return RedirectToAction(nameof(Consulta));

        }

        // GET: Descarte/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var descarte = await _context.Descartes.FindAsync(id);
            if (descarte == null)
            {
                return NotFound();
            }
            ViewData["EquipamentoId"] = new SelectList(await _context.Equipamentos.ToListAsync(), "CodigoItem", "Descricao", descarte.EquipamentoId);
            return View(descarte);
        }

        // POST: Descarte/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("Id,EquipamentoId,Observacao,Quantidade,DataColeta,EmpresaColetora,CnpjEmpresa,EmailEmpresa,PessoaResponsavelColeta,CertificadoUpload,EnviarEmail,UsuarioId,DataDeCadastro,CertificadoUrl,Status")] DescarteModel descarte)
        {
            if (id != descarte.Id)
            {
                return NotFound();
            }

            
                try
                {
                    // Lógica para atualizar o certificado, se um novo for enviado
                    if (descarte.CertificadoUpload != null)
                    {
                        string pastaUploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "certificados");
                        string nomeFicheiroUnico = Guid.NewGuid().ToString() + "_" + descarte.CertificadoUpload.FileName;
                        string caminhoCompleto = Path.Combine(pastaUploads, nomeFicheiroUnico);
                        using (var fileStream = new FileStream(caminhoCompleto, FileMode.Create))
                        {
                            await descarte.CertificadoUpload.CopyToAsync(fileStream);
                        }
                        descarte.CertificadoUrl = "/uploads/certificados/" + nomeFicheiroUnico;
                    }

                    _context.Update(descarte);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DescarteExists(descarte.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["SuccessMessage"] = "Registo de descarte atualizado com sucesso!";
                return RedirectToAction(nameof(Consulta));

        }

        // GET: Descarte/Excluir/5
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var descarte = await _context.Descartes
                .Include(d => d.Equipamento)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (descarte == null)
            {
                return NotFound();
            }

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
            TempData["SuccessMessage"] = $"Registo de descarte {descarte.Id} excluído com sucesso!";
            return RedirectToAction(nameof(Consulta));
        }

        private bool DescarteExists(int id)
        {
            return _context.Descartes.Any(e => e.Id == id);
        }
    }
}

