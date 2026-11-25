using System;

namespace PixelEsGUI.Persistencia
{
    /// <summary>Calcula puntuaciones sin depender de la interfaz ni de SQLite.</summary>
    internal static class ServicioPuntuacion
    {
        /// <summary>Calcula los puntos usando monedas, vidas y tiempo de juego.</summary>
        public static int Calcular(int monedas, int vidas, double segundos)
        {
            if (monedas < 0)
                throw new ArgumentOutOfRangeException(nameof(monedas), "Las monedas no pueden ser negativas.");
            if (vidas < 0)
                throw new ArgumentOutOfRangeException(nameof(vidas), "Las vidas no pueden ser negativas.");
            if (!double.IsFinite(segundos) || segundos < 0)
                throw new ArgumentOutOfRangeException(nameof(segundos), "El tiempo debe ser finito y no negativo.");
            double puntos = Math.Max(0, 1000.0 + 100.0 * monedas + 500.0 * vidas - 5.0 * segundos);
            return (int)Math.Min(int.MaxValue, Math.Round(puntos, MidpointRounding.AwayFromZero));
        }
    }
}
