using Governança_de_TI.Models.Auditoria;
using Governança_de_TI.Models.Gamificacao;
using Governança_de_TI.Models.TecgreenModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace Governança_de_TI.Models.Usuario
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

        // ================================
        // FOTO DE PERFIL
        // ================================

        [Display(Name = "Caminho da Foto de Perfil")]
        [StringLength(255)]
        public string? CaminhoFotoPerfil { get; set; }

        // Campo temporário apenas para upload (não mapeado no banco)
        [NotMapped]
        [Display(Name = "Foto de Perfil")]
        public IFormFile? FotoUpload { get; set; }

        // ================================
        // RELACIONAMENTOS E CAMPOS ADICIONAIS
        // ================================

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
                    new Claim(ClaimTypes.Name, Email ?? string.Empty),
                    new Claim("FullName", Nome ?? string.Empty),
                    new Claim(ClaimTypes.Role, Perfil ?? string.Empty)
                };

                // Armazena apenas o caminho da imagem (sem bytes)
                if (!string.IsNullOrEmpty(CaminhoFotoPerfil))
                    claims.Add(new Claim("FotoPerfilPath", CaminhoFotoPerfil));

                return claims;
            }
        }

        // ============================================================
        // Propriedades de Navegação para Gamificação
        // ============================================================
        public virtual ICollection<UsuarioPremioModel> UsuarioPremios { get; set; } = new List<UsuarioPremioModel>();
        public virtual GamificacaoModel? Gamificacao { get; set; }


        // Usado para exibir logs recentes na tela de detalhes
        [NotMapped]
        public List<AuditLogModel> RecentActivity { get; set; } = new List<AuditLogModel>();
    }
}
