using Governança_de_TI.Models.Usuario;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.Gamificacao
{
    /// <summary>
    /// Armazena a pontuação e o nível de gamificação de um usuário.
    /// Relação One-to-One (ou One-to-Zero) com UsuarioModel.
    /// </summary>
    public class GamificacaoModel
    {
        [Key]
        [ForeignKey("Usuario")] // Chave primária é também a chave estrangeira
        public int? UsuarioId { get; set; }

        [Required]
        [Display(Name = "Pontos ESG")]
        public int Pontos { get; set; } = 0; // Começa com 0 pontos

        [Required]
        [StringLength(50)]
        [Display(Name = "Nível Atual")]
        public string Nivel { get; set; } = "Iniciante"; // Nível inicial

        // Propriedade de navegação para o usuário (opcional, mas útil)
        public virtual UsuarioModel Usuario { get; set; }

        // Poderia adicionar campos como DataUltimaAtualizacao, etc.
    }
}
