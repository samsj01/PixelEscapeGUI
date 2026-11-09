namespace PixelEscape
{
    // Aumenta la velocidad del jugador.
    internal class PowerUpVelocidad : PowerUpTemporal
    {
        const int VelocidadConPowerUp = 2;

        public PowerUpVelocidad(int x, int y, int duracion) : base(x, y, duracion)
        {
        }

        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.Velocidad = VelocidadConPowerUp;
        }
    }
}
