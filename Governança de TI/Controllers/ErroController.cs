using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Governança_de_TI.Controllers
{
    public class ErroController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ErroController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // 🔥 ROTA GLOBAL DE ERROS
        // =========================================================
        [Route("Erro/Geral")]
        public async Task<IActionResult> Geral()
        {
            var exceptionInfo = HttpContext.Features.Get<IExceptionHandlerFeature>();

            if (exceptionInfo != null)
            {
                var erro = exceptionInfo.Error;
                var rota = HttpContext?.Request?.Path.Value;

                await LogService.Gravar(
                    _context,
                    origem: "Sistema",
                    tipo: "Erro",
                    mensagem: erro.Message,
                    detalhes: $"{erro.StackTrace}\nRota: {rota}"
                );
            }

            // 🔥 Sempre envia um Model válido para evitar NullReference
            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel());
        }



    }
}
