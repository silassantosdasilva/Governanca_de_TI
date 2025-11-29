using Governança_de_TI.Models.Usuario;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.Auditoria
{
    public class AuditLogModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual UsuarioModel Usuario { get; set; }

        [Required]
        [StringLength(100)]
        public string Acao { get; set; } // Ex: "Criou Equipamento", "Excluiu Descarte"

        [StringLength(255)]
        public string Detalhes { get; set; } // Ex: "Item ID: 1023 - Notebook Dell"

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
