using System.ComponentModel.DataAnnotations;

namespace Governança_de_TI.Models
{
    /// <summary>
    /// Representa os dados necessários para que um utilizador faça login no sistema.
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// O endereço de e-mail do utilizador.
        /// </summary>
        [Required(ErrorMessage = "O campo E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; }

        /// <summary>
        /// A senha do utilizador.
        /// </summary>
        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; }

        /// <summary>
        /// Opção para o utilizador escolher se quer manter a sessão ativa ("Lembrar-me").
        /// </summary>
        [Display(Name = "Lembrar-me")]
        public bool LembrarMe { get; set; }
    }
}

