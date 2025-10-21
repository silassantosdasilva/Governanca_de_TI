using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

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

        public byte[]? FotoPerfil { get; set; }

        // Relacionamento opcional com Departamento
        public int? DepartamentoId { get; set; }

        [ForeignKey("DepartamentoId")]
        public DepartamentoModel? Departamento { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Ativo";

        [Display(Name = "Último Login")]
        public DateTime? DataUltimoLogin { get; set; }

        [Display(Name = "Data de Cadastro")]
        public DateTime DataDeCadastro { get; set; } = DateTime.Now;

        // === [NOVA PROPRIEDADE - GERAÇÃO DE CLAIMS] ===
        // Essa propriedade é apenas lógica e não deve ser mapeada pelo EF.
        [NotMapped]
        public IEnumerable<Claim> Claims
        {
            get
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Email),
                    new Claim("FullName", Nome),
                    new Claim(ClaimTypes.Role, Perfil ?? string.Empty)
                };

                if (FotoPerfil != null && FotoPerfil.Length > 0)
                    claims.Add(new Claim("FotoPerfil", Convert.ToBase64String(FotoPerfil)));

                return claims;
            }
        }
    }
}
