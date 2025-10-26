using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models
{
    /// <summary>
    /// Define os prêmios/conquistas disponíveis e os pontos necessários.
    /// </summary>
    public class PremioModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do prêmio é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Nome do Prêmio")]
        public string Nome { get; set; }

        [StringLength(255)]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "Os pontos necessários são obrigatórios.")]
        [Display(Name = "Pontos Necessários")]
        [Range(1, int.MaxValue, ErrorMessage = "Pontos devem ser positivos.")]
        public int PontosNecessarios { get; set; }

        [StringLength(50)]
        [Display(Name = "Ícone Bootstrap")]
        public string IconeBootstrap { get; set; } // Ex: "bi-award", "bi-star-fill"

        // Propriedade de navegação para a relação muitos-para-muitos
        public virtual ICollection<UsuarioPremioModel> UsuariosQueConquistaram { get; set; } = new List<UsuarioPremioModel>();
    }
}
