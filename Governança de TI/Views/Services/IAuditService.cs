using Governança_de_TI.Data;
using Governança_de_TI.Models;
using System.Threading.Tasks;

namespace Governança_de_TI.Services
{
    public interface IAuditService
    {
        Task RegistrarAcao(int usuarioId, string acao, string detalhes);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarAcao(int usuarioId, string acao, string detalhes)
        {
            var log = new AuditLogModel
            {
                UsuarioId = usuarioId,
                Acao = acao,
                Detalhes = detalhes
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
