using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.Financeiro
{
    [Table("SaldoDiario")]
    public class SaldoDiarioModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // PK da tabela de saldo (pode ser int auto-incremento)

        [Required]
        public Guid IdConta { get; set; } // <--- CORRIGIDO PARA GUID

        [ForeignKey("IdConta")]
        public virtual ContaBancariaModel ContaBancaria { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoFinal { get; set; }
    }
}