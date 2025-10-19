using System.ComponentModel.DataAnnotations;

namespace Governança_de_TI.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "O campo Nome completo é obrigatório.")]
        [Display(Name = "Nome completo")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O campo E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; }

       
        // será gerada automaticamente pelo sistema e enviada por e-mail.

        [Display(Name = "Permito Receber e-mail Promocionais e Novidades.")]
        public bool AceitaPromocoes { get; set; }
    }
}

