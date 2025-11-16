using Governança_de_TI.Data;
using Governança_de_TI.Services;
using System.Net;
using System.Text.Json;

namespace Governança_de_TI.Middlewares
{
    public class JsonExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public JsonExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ApplicationDbContext db)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log
                await LogService.Gravar(
                    db,
                    origem: "Middleware",
                    tipo: "Erro",
                    mensagem: ex.Message,
                    detalhes: ex.StackTrace
                );

                // Resposta padronizada
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var result = new
                {
                    success = false,
                    message = "Erro interno no servidor.",
                    error = ex.Message
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(result));
            }
        }
    }
}
