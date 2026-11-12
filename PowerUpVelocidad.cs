namespace PixelEsGUI
{
    // Aumenta la velocidad del jugador.
    internal class PowerUpVelocidad : PowerUpTemporal
    {
        const int VelocidadConPowerUp = 10;
        const int CuadroVelocidad = 1; // burbuja con la bota alada

        public PowerUpVelocidad(int x, int y, int duracion) : base(x, y, duracion, CuadroVelocidad)
        {
        }

        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.Velocidad = VelocidadConPowerUp;
        }
    }
}
