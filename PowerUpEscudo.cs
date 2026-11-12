namespace PixelEsGUI
{
    // Protege al jugador de un impacto (le da una vida extra).
    internal class PowerUpEscudo : PowerUpTemporal
    {
        const int CuadroEscudo = 0; // burbuja con escudo

        public PowerUpEscudo(int x, int y, int duracion) : base(x, y, duracion, CuadroEscudo)
        {
        }

        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.GanarVida(1);
        }
    }
}
