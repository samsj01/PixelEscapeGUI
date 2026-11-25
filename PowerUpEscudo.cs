namespace PixelEsGUI
{
    // Protege al jugador de UN impacto (trampa o enemigo): no pierde vida ni vuelve al punto de control.
    /// <summary>Coleccionable que protege al jugador de un impacto.</summary>
    internal class PowerUpEscudo : PowerUpTemporal
    {
        const int CuadroEscudo = 0; // burbuja con escudo

        /// <summary>Inicializa PowerUpEscudo con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="duracion">Valor de duración asociado al power-up.</param>
        public PowerUpEscudo(int x, int y, int duracion) : base(x, y, duracion, CuadroEscudo)
        {
        }

        /// <summary>Aplica el efecto del power-up al jugador.</summary>
        /// <param name="jugador">Jugador que interactúa con el objeto.</param>
        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.ActivarEscudo();
        }
    }
}