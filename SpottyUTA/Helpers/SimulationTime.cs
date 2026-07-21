using System;

namespace SpottyUTA.Helpers
{
    /// <summary>
    /// Proveedor auxiliar para la gestión unificada de fecha y hora del sistema.
    /// </summary>
    public static class SimulationTime
    {
        /// <summary>
        /// Obtiene o establece si el modo de simulación está activo.
        /// </summary>
        public static bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Obtiene la fecha y hora actual del sistema.
        /// </summary>
        public static DateTime Now => DateTime.Now;

        /// <summary>
        /// Obtiene la fecha de hoy a las 00:00:00.
        /// </summary>
        public static DateTime Today => DateTime.Today;
    }
}
