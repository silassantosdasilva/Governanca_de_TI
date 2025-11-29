using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Governança_de_TI.Models.Financeiro.TpLancamento
{
    [Table("TipoLancamento")]
    public class TipoLancamentoModel
    {
        // PK (Chave Primária)
        [Key]
        public Guid IdTipo { get; set; } = Guid.NewGuid();

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdTipoLancamento { get; set; } // Atual idTipoLancamento

        [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
        [MaxLength(100)]
        public string Nome { get; set; } // Nome da Categoria (Ex: Alimentação, Transporte)

        // 1 = Despesa, 2 = Receita
        public int Tipo { get; set; }

        // --- RELACIONAMENTO (1 : N) ---
        // Uma Categoria pode ter várias Subcategorias
        public virtual ICollection<SubCategoriaModel> SubCategorias { get; set; }
    }
}