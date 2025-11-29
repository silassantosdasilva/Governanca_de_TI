using Governança_de_TI.Models.Usuario;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.Gamificacao
{
    /// <summary>
    /// Tabela de junção para a relação Muitos-para-Muitos entre Usuario e Premio.
    /// Registra qual usuário conquistou qual prêmio e quando.
    /// Chave primária composta (UsuarioId, PremioId) definida via Fluent API no DbContext.
    /// </summary>
    public class UsuarioPremioModel
    {
        // Chave primária composta (UsuarioId, PremioId)
        // Definida via Fluent API no ApplicationDbContext.cs
        public int UsuarioId { get; set; }
        public int PremioId { get; set; }

        [Required]
        [Display(Name = "Data da Conquista")]
        public DateTime DataConquista { get; set; } = DateTime.Now;

        // Propriedades de navegação para as entidades relacionadas
        [ForeignKey("UsuarioId")]
        public virtual UsuarioModel Usuario { get; set; }

        [ForeignKey("PremioId")]
        public virtual PremioModel Premio { get; set; }
    }
}

