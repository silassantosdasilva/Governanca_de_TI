using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models
{
    public class EquipamentoModel
    {
         [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Código do Item")]
        public int CodigoItem { get; set; }

        [Required(ErrorMessage = "O campo Descrição é obrigatório.")]
        [StringLength(100, ErrorMessage = "A descrição não pode ter mais de 100 caracteres.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        //[Required(ErrorMessage = "O campo Série é obrigatório.")]
        [StringLength(50)]
        [Display(Name = "Série")]
        public string? Serie { get; set; }

        //[Required(ErrorMessage = "O campo Modelo é obrigatório.")]
        [StringLength(50)]
        [Display(Name = "Modelo")]
        public string? Modelo { get; set; }

        //[Required(ErrorMessage = "O campo Data da Compra é obrigatório.")]
        [Display(Name = "Data da Compra")]
        [DataType(DataType.Date)]
        public DateTime? DataCompra { get; set; }

        //[Required(ErrorMessage = "O campo Fim da Garantia é obrigatório.")]
        [Display(Name = "Fim da Garantia")]
        [DataType(DataType.Date)]
        public DateTime? DataFimGarantia { get; set; }
        [Display(Name = "Data de Cadastro")]
        [DataType(DataType.Date)]
        public DateTime? DataDeCadastro { get; set; }

        [Display(Name = "Última Manutenção")]
        [DataType(DataType.Date)]
        public DateTime? DataUltimaManutencao { get; set; }

        //[Required(ErrorMessage = "O campo Status é obrigatório.")]
        [StringLength(20)]
        public string? Status { get; set; }

        [Required(ErrorMessage = "O campo Vida Útil é obrigatório.")]
        [Display(Name = "Vida Útil")]
        [DataType(DataType.Date)]
        public DateTime? VidaUtil { get; set; }

        [Required(ErrorMessage = "O campo Categoria é obrigatório.")]
        [StringLength(50)]
        public string TipoEquipamento { get; set; } // Ex: Notebook, Servidor, Monitor


        // 🔹 Chave estrangeira
        [Display(Name = "Usuário Responsável")]
        public int? UsuarioId { get; set; }  // nullable se nem todo equipamento precisa de usuário

            // 🔹 Navegação
            [ForeignKey("UsuarioId")]
             public Usuario Usuario { get; set; }


        [Display(Name = "Manutenção")]
        public string? FrequenciaManutencao { get; set; } // Mensal, Trimestral, Anual

        [Display(Name = "Avisar antes de quando dias?")]
        public int? DiasAlertaManutencao { get; set; }

        [Display(Name = "Enviar Email")]
        public bool EnviarEmailAlerta { get; set; }

        [Display(Name = "Imagem do Produto")]
        public string? ImagemUrl { get; set; }

        [Display(Name = "Anexo")]
        public string? AnexoUrl { get; set; }

        // Propriedades para lidar com o upload de ficheiros
        // [NotMapped] diz ao Entity Framework para NÃO criar estas colunas na base de dados.
        [NotMapped]
        [Display(Name = "Imagem")]
        public IFormFile ImagemUpload { get; set; }

        [NotMapped]
        [Display(Name = "Anexo")]
        public IFormFile AnexoUpload { get; set; }

    }
}
