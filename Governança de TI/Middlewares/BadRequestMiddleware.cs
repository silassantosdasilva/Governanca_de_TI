using System.Net;
using System.Text.Json;

public class BadRequestMiddleware
{
    private readonly RequestDelegate _next;

    public BadRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var originalBody = context.Response.Body;

        using var newBody = new MemoryStream();
        context.Response.Body = newBody;

        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status400BadRequest)
        {
            context.Response.ContentType = "application/json";

            // Rewind
            newBody.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(newBody).ReadToEndAsync();

            object response;

            // Se já for JSON, só envia
            try
            {
                var json = JsonSerializer.Deserialize<object>(text);
                response = json;
            }
            catch
            {
                // Texto → converte para JSON
                response = new
                {
                    success = false,
                    message = text
                };
            }

            var jsonResult = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResult);
        }
        else
        {
            newBody.Seek(0, SeekOrigin.Begin);
            await newBody.CopyToAsync(originalBody);
        }
    }
}
