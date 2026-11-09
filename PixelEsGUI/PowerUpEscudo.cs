namespace PixelEscape
{
    // Protege al jugador de un impacto (le da una vida extra).
    internal class PowerUpEscudo : PowerUpTemporal
    {
        public PowerUpEscudo(int x, int y, int duracion) : base(x, y, duracion)
        {
        }

        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.GanarVida(1);
        }
    }
}
