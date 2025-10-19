using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services; // Adiciona o namespace dos seus serviços
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Governança_de_TI.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService; // Injeta o serviço de e-mail

        public UsuarioController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Usuario
        // OBSERVAÇÃO: Action para exibir a lista de todos os utilizadores.
        public async Task<IActionResult> Index()
        {
            return View(await _context.Usuarios.OrderBy(u => u.Nome).ToListAsync());
        }

        // GET: Usuario/Detalhes/5
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.Id == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        // GET: Usuario/Criar
        public IActionResult Criar()
        {
            return View();
        }

        // POST: Usuario/Criar
        // POST: Usuario/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        // OBSERVAÇÃO: O atributo [Bind] foi atualizado para incluir os novos campos
        // 'Departamento' e 'Status', que vêm do formulário.
        public async Task<IActionResult> Criar([Bind("Nome,Email,Perfil,Departamento,Status")] UsuarioModel usuario)
        {
            // Remove a validação da senha, pois ela será gerada automaticamente.
            ModelState.Remove("Senha");
            if (ModelState.IsValid)
            {
                // Adicionada validação para verificar se o e-mail já existe no banco de dados.
                var existingUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == usuario.Email);
            if (existingUser != null)
            {
                // Se o e-mail já existir, adiciona um erro ao modelo e retorna para a view.
                ModelState.AddModelError("Email", "Este e-mail já está a ser utilizado por outra conta.");
                return View(usuario);
            }

          
                string senhaAleatoria = GerarSenhaAleatoria();
                usuario.Senha = HashPassword(senhaAleatoria);

                // Define a data de cadastro no momento da criação
                usuario.DataDeCadastro = DateTime.Now;

                _context.Add(usuario);
                await _context.SaveChangesAsync();

                try
                {
                    var corpoEmail = $"<p>Olá {usuario.Nome},</p><p>A sua conta foi criada com sucesso. A sua senha de acesso temporária é: <strong>{senhaAleatoria}</strong></p>";
                    await _emailService.EnviarEmailAsync(usuario.Email, "Bem-vindo ao Sistema", corpoEmail);
                    TempData["SuccessMessage"] = $"Utilizador {usuario.Nome} criado! Uma senha foi enviada para o e-mail {usuario.Email}.";
                }
                catch (Exception ex)
                {
                    TempData["WarningMessage"] = $"Utilizador criado, mas falha ao enviar e-mail: {ex.Message}";
                }

                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        // GET: Usuario/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        // POST: Usuario/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("Id,Nome,Email,Perfil,Departamento,Status")] UsuarioModel usuarioModel)
        {
            if (id != usuarioModel.Id)
            {
                return NotFound();
            }

            // Remove a validação da senha, pois ela não é alterada nesta tela.
            ModelState.Remove("Senha");

            if (ModelState.IsValid)
            {
                try
                {
                    // Busca o registo original para preservar campos não editáveis.
                    var usuarioOriginal = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                    if (usuarioOriginal != null)
                    {
                        // Preserva a senha e a data de cadastro originais.
                        usuarioModel.Senha = usuarioOriginal.Senha;
                        usuarioModel.DataDeCadastro = usuarioOriginal.DataDeCadastro;
                    }

                    _context.Update(usuarioModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuarioModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["SuccessMessage"] = "Utilizador atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            // Se o modelo for inválido, retorna para a mesma view para exibir os erros.
            return View(usuarioModel);
        }
        // OBSERVAÇÃO: Nova Action para redefinir a senha a partir da tela de edição.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RedefinirSenha(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Utilizador não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            string novaSenha = GerarSenhaAleatoria();
            usuario.Senha = HashPassword(novaSenha);

            _context.Update(usuario);
            await _context.SaveChangesAsync();

            try
            {
                var corpoEmail = $"<p>Olá {usuario.Nome},</p><p>A sua senha foi redefinida. A sua nova senha de acesso temporária é: <strong>{novaSenha}</strong></p>";
                await _emailService.EnviarEmailAsync(usuario.Email, "Redefinição de Senha", corpoEmail);
                TempData["SuccessMessage"] = $"Uma nova senha foi gerada e enviada para o e-mail {usuario.Email}.";
            }
            catch (Exception ex)
            {
                TempData["WarningMessage"] = $"Senha do utilizador {usuario.Nome} foi redefinida, mas falha ao enviar e-mail: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Usuario/Excluir/5
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.Id == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        // POST: Usuario/Excluir/5
        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Utilizador excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        #region Métodos Auxiliares
        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }

        private string GerarSenhaAleatoria(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        #endregion
    }
}

