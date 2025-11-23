// Services/Interfaces/ILancamentoService.cs

using Governança_de_TI.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILancamentoService
{
    // Métodos CRUD
    Task<LancamentoDTO> GetByIdAsync(Guid id);
    Task<IEnumerable<LancamentoDTO>> GetAllAsync(); // Usado para consultas simples
    Task<LancamentoDTO> CreateAsync(LancamentoDTO dto, int userId); // Recebe UserId para Auditoria
    Task<LancamentoDTO> UpdateAsync(Guid id, LancamentoDTO dto, int userId);
    Task DeleteAsync(Guid id, int userId);

    // Métodos de Ação (Endpoints PATCH)
    Task<LancamentoParcelaDTO> PagarParcelaAsync(Guid idParcela, BaixaDTO baixaDto, int userId);
    Task CancelarLancamentoAsync(Guid idLancamento, int userId);
}

// DTO para Baixa de Lançamento (usado no método PagarParcelaAsync)
public class BaixaDTO
{
    public Guid IdContaBaixa { get; set; } // Conta que efetuou o pagamento/recebimento
    public decimal ValorPago { get; set; } // Valor real
    public DateTime DataPagamento { get; set; } // Data efetiva
}