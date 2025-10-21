using System.ComponentModel.DataAnnotations;

namespace Governança_de_TI.Models
{
    // === [NOVA REGRA] ===
    // Modelo responsável por representar departamentos do sistema.
    // Cada usuário poderá estar vinculado a um departamento.
    public class DepartamentoModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do departamento é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }
    }
}
