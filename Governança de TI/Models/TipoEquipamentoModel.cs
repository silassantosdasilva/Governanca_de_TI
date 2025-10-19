using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models
{
    /// <summary>
    /// Representa uma categoria ou tipo de equipamento (Ex: Notebook, Servidor, Monitor).
    /// </summary>
    public class TipoEquipamentoModel
    {
        /// <summary>
        /// Chave primária do tipo de equipamento.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// O nome do tipo de equipamento.
        /// </summary>
        [Required(ErrorMessage = "O nome do tipo de equipamento é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Nome do Tipo")]
        public string Nome { get; set; }
    }
}

