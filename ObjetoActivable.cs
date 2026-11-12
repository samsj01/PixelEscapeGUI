namespace PixelEsGUI
{
    // Objeto que reacciona cuando el jugador lo activa (trampa, punto de control).
    internal abstract class ObjetoActivable : ObjetoInteractivo
    {
        protected ObjetoActivable(int x, int y, string recurso, int columnas, int filas, float fps)
            : base(x, y, recurso, columnas, filas, fps)
        {
        }

        public abstract bool Activar(Jugador jugador);
    }
}
