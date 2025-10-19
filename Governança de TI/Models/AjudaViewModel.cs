using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Governança_de_TI.Models
{
    public class AjudaViewModel
    {
        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        [Display(Name = "Seu Nome")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O campo E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        [Display(Name = "Seu E-mail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo Observação é obrigatório.")]
        [StringLength(500, ErrorMessage = "A observação não pode ter mais de 500 caracteres.")]
        [Display(Name = "Observação")]
        public string Observacao { get; set; }

        [Display(Name = "Anexar Imagem")]
        public IFormFile ImagemUpload { get; set; }
    }
}
