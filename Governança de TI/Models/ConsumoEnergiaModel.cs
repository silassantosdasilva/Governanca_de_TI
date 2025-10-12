using System;
using System.ComponentModel.DataAnnotations;

namespace Governança_de_TI.Models // Certifique-se de que o namespace está correto
{
    public class ConsumoEnergiaModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O campo Mês/Ano de Referência é obrigatório.")]
        [Display(Name = "Mês/Ano de Referência")]
        [DataType(DataType.Date)]
        public DateTime DataReferencia { get; set; }

        [Required(ErrorMessage = "O campo Valor (kWh) é obrigatório.")]
        [Display(Name = "Valor Consumido (kWh)")]
        [Range(0, double.MaxValue, ErrorMessage = "O valor deve ser positivo.")]
        public decimal ValorKwh { get; set; }
    }
}
