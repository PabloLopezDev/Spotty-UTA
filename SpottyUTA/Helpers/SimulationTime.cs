using System;

namespace SpottyUTA.Helpers
{
    public static class SimulationTime
    {
        public static bool IsEnabled { get; set; } = false;
        
        public static DateTime Now => DateTime.Now;
        public static DateTime Today => DateTime.Today;
    }
}
