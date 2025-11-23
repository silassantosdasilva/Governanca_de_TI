// Services/Interfaces/IExtratoService.cs

using Governança_de_TI.DTOs;
using System.Threading.Tasks;

public interface IExtratoService
{
    // 5.6. Endpoint principal para consulta de extrato
    Task<ExtratoResponseDTO> FiltrarExtratoAsync(ExtratoRequestDTO request);
}