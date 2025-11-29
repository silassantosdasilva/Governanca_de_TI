using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.Financeiro.TpLancamento
{
    [Table("SubCategoria")]
    public class SubCategoriaModel
    {
        // PK (Chave Primária)
        [Key]
        public Guid IdSubcategoria { get; set; } = Guid.NewGuid();

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSubcategoriaInt { get; set; } // Atual idSubcategoria

        [Required(ErrorMessage = "O nome da subcategoria é obrigatório.")]
        [MaxLength(100)]
        public string Nome { get; set; } // Nome da Subcategoria (Ex: Uber, Mercado, Aluguel)

        // FK (Chave Estrangeira) para a Categoria Pai
        [Required]
        public Guid IdTipo { get; set; }

        // --- RELACIONAMENTO (N : 1) ---
        // Uma Subcategoria pertence a apenas uma Categoria
        [ForeignKey("IdTipo")]
        public virtual TipoLancamentoModel Categoria { get; set; }
    }
}