using System.ComponentModel.DataAnnotations;

namespace Governança_de_TI.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "O campo E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        [Display(Name = "E-mail de Cadastro")]
        public string Email { get; set; }
    }
}
