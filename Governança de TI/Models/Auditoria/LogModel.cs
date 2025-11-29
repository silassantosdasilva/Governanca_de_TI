using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.Auditoria
{
    // ============================================================
    // 🧩 MODELO DE LOG
    // ============================================================
    //
    // Estrutura padrão dos registros de log gravados no banco.
    // ============================================================
    [Table("Logs")]
    public class LogModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string? Origem { get; set; } = string.Empty; // Ex: "Dashboard", "Financeiro"

        [Required]
        [StringLength(20)]
        public string? Tipo { get; set; } = "Info"; // Info, Aviso, Erro

        [Required]
        [StringLength(500)]
        public string? Mensagem { get; set; } = string.Empty;

        public string? Detalhes { get; set; }

        [Required]
        public DateTime? DataRegistro { get; set; } = DateTime.Now;
    }
}
