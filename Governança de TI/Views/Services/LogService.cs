using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Governança_de_TI.Data;
using Governança_de_TI.Models;
using Governança_de_TI.Models.Auditoria;

namespace Governança_de_TI.Services
{
    // ============================================================
    // 📘 SERVIÇO CENTRAL DE LOGS DO SISTEMA
    // ============================================================
    //
    // Este serviço grava logs de forma padronizada em banco de dados.
    // Ele é chamado automaticamente por qualquer parte do sistema
    // (controllers, serviços, consultas, dashboards, etc.).
    //
    // Cada log contém:
    //   - Data e hora
    //   - Origem (ex: Dashboard, Financeiro, Login)
    //   - Tipo (Info, Aviso, Erro)
    //   - Mensagem
    //   - Detalhes técnicos (stacktrace, payload, etc.)
    //
    // ============================================================
    public static class LogService
    {
        // ============================================================
        // 🔹 MÉTODO: Gravar()
        // ============================================================
        //
        // Método principal de registro de log.
        // Pode ser chamado de qualquer camada da aplicação.
        //
        // Exemplo:
        // await LogService.Gravar(_context, "Dashboard", "Erro", "Falha ao carregar widget", ex.ToString());
        //
        // ============================================================
        public static async Task Gravar(
            ApplicationDbContext _context,
            string origem,
            string tipo,
            string mensagem,
            string? detalhes = null)
        {
            try
            {
                var log = new LogModel
                {
                    DataRegistro = DateTime.Now,
                    Origem = origem,
                    Tipo = tipo,
                    Mensagem = mensagem,
                    Detalhes = detalhes ?? string.Empty
                };

                await _context.Logs.AddAsync(log);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[LOG] ({tipo}) {origem} → {mensagem}");
            }
            catch (Exception ex)
            {
                // ⚠️ Caso ocorra erro ao gravar log, registra fallback no console
                Console.Error.WriteLine($"[ERRO][LogService]: Falha ao gravar log → {ex.Message}");
            }
        }

        // ============================================================
        // 🔹 MÉTODO: Limpar()
        // ============================================================
        //
        // Remove todos os registros de log.
        // Usado apenas pela tela administrativa.
        //
        // ============================================================
        public static async Task Limpar(ApplicationDbContext _context)
        {
            try
            {
                _context.Logs.RemoveRange(_context.Logs);
                await _context.SaveChangesAsync();
                Console.WriteLine("[LOG] Todos os registros foram apagados pelo administrador.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO][LogService/Limpar]: {ex.Message}");
            }
        }
    }
}
