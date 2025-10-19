using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Governança_de_TI.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Envia um e-mail.
        /// </summary>
        /// <param name="destinatario">O e-mail do destinatário.</param>
        /// <param name="assunto">O assunto do e-mail.</param>
        /// <param name="corpo">O corpo do e-mail (pode ser HTML).</param>
        /// <param name="anexo">Um ficheiro opcional a ser anexado ao e-mail.</param>
        Task EnviarEmailAsync(string destinatario, string assunto, string corpo, IFormFile anexo = null);
    }
}

