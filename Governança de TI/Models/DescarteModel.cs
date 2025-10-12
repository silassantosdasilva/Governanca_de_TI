using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Governança_de_TI.Models
{
    public class DescarteModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "É obrigatório selecionar um item.")]
        [Display(Name = "Item")]
        public int EquipamentoId { get; set; }

        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [ForeignKey("EquipamentoId")]
        public virtual EquipamentoModel Equipamento { get; set; }

        [Required(ErrorMessage = "O campo Quantidade é obrigatório.")]
        public int Quantidade { get; set; }

        [Required(ErrorMessage = "O campo Data é obrigatório.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data da Coleta")]
        public DateTime DataColeta { get; set; }

        [Required(ErrorMessage = "O campo Empresa Coletora é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Empresa Coletora")]
        public string EmpresaColetora { get; set; }

        [StringLength(18)]
        [Display(Name = "CNPJ")]
        public string? CnpjEmpresa { get; set; }

        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        [StringLength(100)]
        [Display(Name = "E-mail")]
        public string? EmailEmpresa { get; set; }

        [StringLength(100)]
        [Display(Name = "Pessoa que vai Coletar")]
        public string PessoaResponsavelColeta { get; set; }

        public string? CertificadoUrl { get; set; }

        [NotMapped]
        [Display(Name = "Certificado de Coleta")]
        public IFormFile CertificadoUpload { get; set; }

        [Display(Name = "Enviar E-mail Automático")]
        public bool EnviarEmail { get; set; }

        [Display(Name = "Data de Cadastro")]
        public DateTime DataDeCadastro { get; set; }

  
        [StringLength(20)]
        public string? Status { get; set; }

        [Display(Name = "Usuário Responsável")]
        public int? UsuarioId { get; set; }  // nullable se nem todo equipamento precisa de usuário

        // 🔹 Navegação
        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }



    }
}
