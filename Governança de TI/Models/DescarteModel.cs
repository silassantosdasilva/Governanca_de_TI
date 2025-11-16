using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models
{
    /// <summary>
    /// Representa um registo de descarte de um ou mais equipamentos.
    /// </summary>
    public class DescarteModel
    {
        /// <summary>
        /// Chave primária do registo de descarte.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Chave estrangeira que referencia o equipamento a ser descartado.
        /// </summary>
        [Required(ErrorMessage = "É obrigatório selecionar um item.")]
        [Display(Name = "Item")]
        public int EquipamentoId { get; set; }

        /// <summary>
        /// Propriedade de navegação para o equipamento relacionado.
        /// O Entity Framework usa isto para carregar os dados do equipamento (join).
        /// </summary>
        [ForeignKey("EquipamentoId")]
        public virtual EquipamentoModel Equipamento { get; set; }

        /// <summary>
        /// Descrição do equipamento, preenchida automaticamente ao selecionar o item.
        /// </summary>
        [Display(Name = "Descrição do Item")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O campo Quantidade é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser de pelo menos 1.")]
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
        [Display(Name = "CNPJ / CPF")]
        public string? CnpjEmpresa { get; set; }

        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        [StringLength(100)]
        [Display(Name = "E-mail")]
        public string? EmailEmpresa { get; set; }

        [StringLength(100)]
        [Display(Name = "Pessoa que vai Coletar")]
        public string PessoaResponsavelColeta { get; set; }

        /// <summary>
        /// Caminho relativo para o ficheiro do certificado guardado no servidor.
        /// </summary>
        public string? CertificadoUrl { get; set; }

        /// <summary>
        /// Propriedade temporária para receber o ficheiro de upload do certificado.
        /// Não é mapeada para uma coluna no banco de dados.
        /// </summary>
        [NotMapped]
        [Display(Name = "Certificado de Coleta")]
        public IFormFile? CertificadoUpload { get; set; }

        [Display(Name = "URL Imagem Equip.")] // Nome curto para exibição
        public string? ImagemEquipamentoUrl { get; set; }

        [Display(Name = "Enviar E-mail Automático")]
        public bool EnviarEmail { get; set; }

        [Display(Name = "Data de Cadastro")]
        public DateTime DataDeCadastro { get; set; }

        [StringLength(500)]
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [StringLength(50)]
        public string Status { get; set; }
    }
}

