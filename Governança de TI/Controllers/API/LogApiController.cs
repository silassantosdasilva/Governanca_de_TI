using Governança_de_TI.Data;
using Governança_de_TI.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/log")]
public class LogApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LogApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] LogRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Origem) || string.IsNullOrWhiteSpace(request.Tipo))
                return BadRequest(new { message = "Campos 'origem' e 'tipo' são obrigatórios." });

            await LogService.Gravar(_context, request.Origem, request.Tipo, request.Mensagem, request.Detalhes);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERRO][LogApiController]: {ex.Message}");
            return StatusCode(500, new { message = "Erro ao registrar log.", detalhe = ex.Message });
        }
    }

    public class LogRequest
    {
        public string Origem { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string? Mensagem { get; set; }
        public string? Detalhes { get; set; }
    }
}
