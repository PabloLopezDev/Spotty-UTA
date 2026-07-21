using System;

namespace SpottyUTA.Helpers
{
    public static class SimulationTime
    {
        public static bool IsEnabled { get; set; } = false;
        
        // Simular un lunes a las 10:00 AM (horario laboral estándar de la biblioteca)
        public static DateTime SimulatedDateTime { get; set; } = new DateTime(2026, 7, 20, 10, 0, 0);

        public static DateTime Now => IsEnabled ? SimulatedDateTime : DateTime.Now;
        public static DateTime Today => IsEnabled ? SimulatedDateTime.Date : DateTime.Today;
    }
}
