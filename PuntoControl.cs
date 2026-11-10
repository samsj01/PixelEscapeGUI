namespace PixelEscape
{
    // Lugar al que regresa el jugador tras perder una vida.
    internal class PuntoControl : ObjetoActivable
    {
        public PuntoControl(int x, int y) : base(x, y)
        {
        }

        public override bool Activar(Jugador jugador)
        {
            Activo = true;
            return Activo;
        }
    }
}
