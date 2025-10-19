using System.ComponentModel.DataAnnotations;

namespace Governança_de_TI.Models
{
    /// <summary>
    /// Representa um utilizador do sistema.
    /// </summary>
    public class UsuarioModel
    {
        /// <summary>
        /// Chave primária do utilizador.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nome completo do utilizador.
        /// </summary>
        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        /// <summary>
        /// Endereço de e-mail do utilizador, que será usado para login.
        /// </summary>
        [Required(ErrorMessage = "O campo E-mail é obrigatório.")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; }

        /// <summary>
        /// Palavra-passe do utilizador.
        /// </summary>
        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; }

        /// <summary>
        /// Perfil de acesso do utilizador (Ex: "Admin", "Usuario").
        /// Define as permissões do utilizador no sistema.
        /// </summary>
        [Required(ErrorMessage = "O campo Perfil é obrigatório.")]
        [StringLength(50)]
        public string Perfil { get; set; }
    }
}
