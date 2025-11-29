using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.TecgreenModels
{
    // === [NOVA REGRA] ===
    // Modelo responsável por representar departamentos do sistema.
    // Cada usuário poderá estar vinculado a um departamento.
    public class DepartamentoModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do departamento é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode ter mais de 100 caracteres.")]
        [Display(Name = "Nome do Departamento")]
        public string Nome { get; set; }
    }
}
