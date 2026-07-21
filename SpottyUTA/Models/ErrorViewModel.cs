namespace SpottyUTA.Models
{
    /// <summary>
    /// Modelo de vista utilizado para la presentación de errores globales.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Identificador de la solicitud HTTP asociada al error.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Determina si el identificador de solicitud debe mostrarse en la vista.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
