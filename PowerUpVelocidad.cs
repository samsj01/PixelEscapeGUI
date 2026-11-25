namespace PixelEsGUI
{
    // Aumenta la velocidad del jugador.
    /// <summary>Coleccionable que activa un aumento temporal de velocidad.</summary>
    internal class PowerUpVelocidad : PowerUpTemporal
    {
        const int CuadroVelocidad = 1; // burbuja con la bota alada

        /// <summary>Inicializa PowerUpVelocidad con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="duracion">Valor de duración asociado al power-up.</param>
        public PowerUpVelocidad(int x, int y, int duracion) : base(x, y, duracion, CuadroVelocidad)
        {
        }

        /// <summary>Aplica el efecto del power-up al jugador.</summary>
        /// <param name="jugador">Jugador que interactúa con el objeto.</param>
        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.ActivarVelocidadTemporal(Duracion);
        }
    }
}
