using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Governança_de_TI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        EquipamentoModel Objequipamento = new EquipamentoModel();
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration; // Para ler o e-mail de suporte do appsettings


        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        //Tela de Equipamentos
        public async Task<IActionResult> Consulta()
        {
            var listaDeEquipamentos = await _context.Equipamentos
                .Include(e => e.Usuario)
                .ToListAsync();

            return View("~/Views/Equipamentos/Consulta.cshtml", listaDeEquipamentos);
        }

        public async Task<IActionResult> Criar()
        {
     

            return View("~/Views/Equipamentos/Criar.cshtml");
        }

        // Ela é pública e não requer login.
        public IActionResult Landing()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Ajuda()
        {
            return View();
        }

        /// <summary>
        /// Action que processa o formulário de Ajuda e envia o e-mail para o suporte.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ajuda(AjudaViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Lê o e-mail de suporte definido no seu ficheiro appsettings.json
                    var emailSuporte = _configuration["SmtpSettings:SupportEmail"];
                    if (string.IsNullOrEmpty(emailSuporte))
                    {
                        ModelState.AddModelError("", "O e-mail de suporte não está configurado.");
                        return View(model);
                    }

                    var assunto = $"Pedido de Suporte de: {model.Nome}";
                    var corpo = $@"
                        <p><strong>Nome:</strong> {model.Nome}</p>
                        <p><strong>E-mail de Contato:</strong> {model.Email}</p>
                        <hr>
                        <p><strong>Mensagem:</strong></p>
                        <p>{model.Observacao}</p>";

                    // Chama o serviço de e-mail, passando o anexo (imagem) se existir
                    await _emailService.EnviarEmailAsync(emailSuporte, assunto, corpo, model.ImagemUpload);

                    TempData["SuccessMessage"] = "A sua mensagem foi enviada com sucesso! A nossa equipa entrará em contacto em breve.";
                    // Limpa o modelo para que o formulário apareça vazio após o envio
                    return View(new AjudaViewModel());
                }
                catch (System.Exception ex)
                {
                    // Em caso de erro no envio, exibe uma mensagem na tela
                    ModelState.AddModelError("", $"Ocorreu um erro ao enviar a sua mensagem: {ex.Message}");
                }
            }
            // Se o modelo for inválido, retorna para a mesma tela para exibir os erros
            return View(model);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            // Aqui você poderia adicionar lógica para logar o erro detalhado
            // usando um serviço de log (ex: Serilog, NLog) se ainda não o fizer.
            // Ex: _logger.LogError($"Erro não tratado. Request ID: {errorViewModel.RequestId}", HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error);

            return View(errorViewModel);
        }
    }
}
