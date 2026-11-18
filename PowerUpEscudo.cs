namespace PixelEsGUI
{
    // Protege al jugador de UN impacto (trampa o enemigo): no pierde vida ni vuelve al punto de control.
    internal class PowerUpEscudo : PowerUpTemporal
    {
        const int CuadroEscudo = 0; // burbuja con escudo

        public PowerUpEscudo(int x, int y, int duracion) : base(x, y, duracion, CuadroEscudo)
        {
        }

        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.ActivarEscudo();
        }
    }
}