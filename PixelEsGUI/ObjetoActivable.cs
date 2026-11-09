namespace PixelEscape
{
    // Objeto que reacciona cuando el jugador lo activa (trampa, punto de control).
    internal abstract class ObjetoActivable : ObjetoInteractivo
    {
        protected ObjetoActivable(int x, int y) : base(x, y)
        {
        }

        public abstract bool Activar(Jugador jugador);
    }
}
