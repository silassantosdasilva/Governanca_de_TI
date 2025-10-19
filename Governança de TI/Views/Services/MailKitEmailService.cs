using Governança_de_TI.Services;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.IO;
using System.Threading.Tasks;

namespace Governança_de_TI.Services
{
    /// <summary>
    /// Implementação do serviço de e-mail utilizando a biblioteca MailKit.
    /// </summary>
    public class MailKitEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public MailKitEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Envia um e-mail de forma assíncrona, com suporte para anexos.
        /// </summary>
        public async Task EnviarEmailAsync(string destinatario, string assunto, string corpo, IFormFile anexo = null)
        {
            var emailMessage = new MimeMessage();
            var settings = _configuration.GetSection("SmtpSettings");

            emailMessage.From.Add(new MailboxAddress(settings["SenderName"], settings["SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress("", destinatario));
            emailMessage.Subject = assunto;

            // O BodyBuilder é a forma correta de criar um corpo de e-mail
            // que pode conter tanto HTML como anexos.
            var bodyBuilder = new BodyBuilder { HtmlBody = corpo };

            // Verifica se um ficheiro de anexo foi fornecido.
            if (anexo != null && anexo.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Copia o conteúdo do ficheiro de upload para a memória.
                    await anexo.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Adiciona o anexo ao e-mail, especificando o nome do ficheiro e o tipo de conteúdo.
                    bodyBuilder.Attachments.Add(anexo.FileName, memoryStream.ToArray(), ContentType.Parse(anexo.ContentType));
                }
            }

            // Atribui o corpo construído (com ou sem anexo) à mensagem de e-mail.
            emailMessage.Body = bodyBuilder.ToMessageBody();

            // Usa o cliente SMTP do MailKit para se conectar ao servidor e enviar o e-mail.
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                await client.ConnectAsync(settings["Server"], int.Parse(settings["Port"]), MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(settings["Username"], settings["Password"]);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}

