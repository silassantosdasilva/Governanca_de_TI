namespace Governança_de_TI.Models
{
    /// <summary>
    /// Modelo usado para transportar informações de erro para a View de Erro.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// O identificador único do pedido (request) que gerou o erro.
        /// Ajuda a rastrear o erro em logs do servidor.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Propriedade de conveniência que indica se o RequestId deve ser exibido.
        /// Geralmente, só é exibido em ambiente de desenvolvimento.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
