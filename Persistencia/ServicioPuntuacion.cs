using System;

namespace PixelEsGUI.Persistencia
{
    /// <summary>Calcula puntuaciones sin depender de la interfaz ni de SQLite.</summary>
    internal static class ServicioPuntuacion
    {
        /// <summary>Calcula los puntos usando monedas, vidas y tiempo de juego.</summary>
        public static int Calcular(int monedas, int vidas, double segundos)
        {
            if (monedas < 0 || vidas < 0 || !double.IsFinite(segundos) || segundos < 0)
                throw new ArgumentOutOfRangeException(nameof(segundos), "Los valores no pueden ser negativos ni infinitos.");
            double puntos = Math.Max(0, 1000.0 + 100.0 * monedas + 500.0 * vidas - 5.0 * segundos);
            return (int)Math.Min(int.MaxValue, Math.Round(puntos, MidpointRounding.AwayFromZero));
        }
    }
}
