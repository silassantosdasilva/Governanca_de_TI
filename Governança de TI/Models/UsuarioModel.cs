using System;
using System.Collections.Generic;
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
        [StringLength(256)] // tamanho suficiente para armazenar hash BCrypt/SHA256
        [DataType(DataType.Password)]
        public string Senha { get; set; }

        [Required(ErrorMessage = "O campo Perfil é obrigatório.")]
        [StringLength(50)]
        public string Perfil { get; set; }

        [Display(Name = "Foto de Perfil")]
        public byte[]? FotoPerfil { get; set; }

        [Display(Name = "Departamento")]
        public int? DepartamentoId { get; set; }

        [ForeignKey("DepartamentoId")]
        public DepartamentoModel? Departamento { get; set; }

        [Required(ErrorMessage = "O campo Status é obrigatório.")]
        [StringLength(50)]
        public string Status { get; set; } = "Ativo";

        [Display(Name = "Último Login")]
        public DateTime? DataUltimoLogin { get; set; }

        [Display(Name = "Data de Cadastro")]
        public DateTime DataDeCadastro { get; set; } = DateTime.Now;

        // ============================================================
        // Propriedade lógica: Claims do usuário (não mapeada no EF)
        // ============================================================
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

                // Armazena imagem de perfil (opcional)
                if (FotoPerfil != null && FotoPerfil.Length > 0)
                    claims.Add(new Claim("FotoPerfil", Convert.ToBase64String(FotoPerfil)));

                return claims;
            }
        }

        // ============================================================
        // Propriedades de Navegação para Gamificação
        // ============================================================

        // Relação Muitos-para-Muitos com Premios (via UsuarioPremioModel)
        public virtual ICollection<UsuarioPremioModel> UsuarioPremios { get; set; } = new List<UsuarioPremioModel>();

        // Relação One-to-One com GamificacaoModel
        public virtual GamificacaoModel Gamificacao { get; set; }

        // Adicionado para busca de logs na view Detalhes.cshtml via @inject (Não mapeado)
        [NotMapped]
        public List<AuditLogModel> RecentActivity { get; set; } = new List<AuditLogModel>();

    }
}

