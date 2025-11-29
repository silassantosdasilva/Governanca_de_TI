using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.TecgreenModels
{
    /// <summary>
    /// Representa um registo de consumo de energia para um determinado mês/ano.
    /// </summary>
    public class ConsumoEnergiaModel
    {
        /// <summary>
        /// Chave primária do registo.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// O mês e o ano a que este registo de consumo se refere.
        /// O dia será sempre guardado como 1 para padronização.
        /// </summary>
        [Required(ErrorMessage = "O campo Mês/Ano de Referência é obrigatório.")]
        [Display(Name = "Mês/Ano de Referência")]
        [DataType(DataType.Date)]
        public DateTime DataReferencia { get; set; }

        /// <summary>
        /// O valor total de energia consumida nesse período, em kWh.
        /// </summary>
        [Required(ErrorMessage = "O campo Valor (kWh) é obrigatório.")]
        [Display(Name = "Valor Consumido (kWh)")]
        [Range(0, double.MaxValue, ErrorMessage = "O valor de consumo deve ser um número positivo.")]
        [DisplayFormat(DataFormatString = "{0:N2}")] // Formata o número com 2 casas decimais
        public decimal ValorKwh { get; set; }
    }
}

