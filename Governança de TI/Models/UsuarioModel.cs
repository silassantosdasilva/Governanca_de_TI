using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models
{
    /// <summary>
    /// Representa um utilizador do sistema (Responsável).
    /// </summary>
    public class UsuarioModel
    {
      
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O campo E-mail é obrigatório.")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; }

    
        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        [StringLength(256)] // Aumentado para acomodar hashes mais longos como SHA256
        [DataType(DataType.Password)]
        public string Senha { get; set; }

        [Required(ErrorMessage = "O campo Perfil é obrigatório.")]
        [StringLength(50)]
        public string Perfil { get; set; }

   
        [StringLength(100)]
        public string Departamento { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Ativo"; // Define "Ativo" como valor padrão para novos utilizadores

        [Display(Name = "Último Login")]
        public DateTime? DataUltimoLogin { get; set; }
     
        [Display(Name = "Data de Cadastro")]
        public DateTime DataDeCadastro { get; set; } = DateTime.Now; // Define a data atual como valor padrão
    }
}

