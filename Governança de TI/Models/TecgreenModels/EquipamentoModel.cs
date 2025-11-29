using Governança_de_TI.Models.Usuario;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.TecgreenModels
{
    /// <summary>
    /// Representa um equipamento de TI no sistema.
    /// </summary>
    public class EquipamentoModel
    {
        /// <summary>
        /// Chave primária do equipamento (gerada automaticamente pelo banco de dados).
        /// </summary>
        [Key]
        [BindNever]
        [Display(Name = "Código do Item")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CodigoItem { get; set; }

        [Required(ErrorMessage = "O campo Descrição é obrigatório.")]
        [StringLength(100, ErrorMessage = "A descrição não pode ter mais de 100 caracteres.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        [StringLength(50)]
        public string? Serie { get; set; }

        [StringLength(50)]
        public string? Modelo { get; set; }

        /// <summary>
        /// Chave estrangeira que referencia o tipo de equipamento.
        /// </summary>
        // === [CORREÇÃO DE VALIDAÇÃO] ===
        // Alterado para 'int?' e [Required] adicionado para validar a seleção "Selecione...".
        [Required(ErrorMessage = "O campo Tipo de Equipamento é obrigatório.")]
        [Display(Name = "Tipo de Equipamento")]
        public int? TipoEquipamentoId { get; set; } // Alterado para nulável

        /// <summary>
        /// Propriedade de navegação para o Tipo de Equipamento relacionado.
        /// </summary>
        [ForeignKey("TipoEquipamentoId")]
        public virtual TipoEquipamentoModel TipoEquipamento { get; set; }

        [Display(Name = "Data da Compra")]
        [DataType(DataType.Date)]
        public DateTime? DataCompra { get; set; }

        [Display(Name = "Vida Útil (em anos)")]
        [Range(1, 30, ErrorMessage = "A vida útil deve ser um valor positivo.")]
        public int? VidaUtilAnos { get; set; }

        /// <summary>
        /// Data calculada para o fim da vida útil (DataCompra + VidaUtilAnos).
        /// Pode ser nula se a data de compra ainda não foi definida.
        /// </summary>
        [Display(Name = "Fim da Vida Útil")]
        [DataType(DataType.Date)]
        public DateTime? VidaUtilFim { get; set; }

        [Display(Name = "Fim da Garantia")]
        [DataType(DataType.Date)]
        public DateTime? DataFimGarantia { get; set; }

        [Display(Name = "Última Manutenção")]
        [DataType(DataType.Date)]
        public DateTime? DataUltimaManutencao { get; set; }

        // === [CORREÇÃO DE VALIDAÇÃO] ===
        // [Required] adicionado para validar a seleção "Selecione...".
        [Required(ErrorMessage = "O campo Status é obrigatório.")]
        [StringLength(50)]
        public string Status { get; set; }

        [Display(Name = "Frequência de Manutenção")]
        public string FrequenciaManutencao { get; set; } // Mensal, Trimestral, Anual

        [Display(Name = "Avisar antes de (dias)")]
        public int? DiasAlertaManutencao { get; set; }

        [Display(Name = "Enviar E-mail de Alerta")]
        public bool EnviarEmailAlerta { get; set; }

        /// <summary>
        /// Caminho relativo para a imagem do equipamento guardada no servidor.
        /// </summary>
        public string? ImagemUrl { get; set; }

        /// <summary>
        /// Caminho relativo para o ficheiro de anexo guardado no servidor.
        /// </summary>
        public string? AnexoUrl { get; set; }

        /// <summary>
        /// Propriedade temporária para receber o upload da imagem. Não é guardada no banco.
        /// </summary>
        [NotMapped]
        [Display(Name = "Imagem do Equipamento")]
        public IFormFile? ImagemUpload { get; set; }

        /// <summary>
        /// Propriedade temporária para receber o upload do anexo. Não é guardada no banco.
        /// </summary>
        [NotMapped]
        [Display(Name = "Anexo")]
        public IFormFile? AnexoUpload { get; set; }

        [Display(Name = "Data de Cadastro")]
        public DateTime DataDeCadastro { get; set; }

        [Display(Name = "Usuário Responsável")]
        public int? UsuarioId { get; set; }

        /// <summary>
        /// Propriedade de navegação para o Usuário responsável.
        /// </summary>
        [ForeignKey("UsuarioId")]
        public virtual UsuarioModel Usuario { get; set; }
    }
}