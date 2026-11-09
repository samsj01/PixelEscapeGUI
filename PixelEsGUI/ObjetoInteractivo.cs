namespace PixelEscape
{
    // Base de los objetos del escenario con los que el jugador interactúa.
    internal abstract class ObjetoInteractivo
    {
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public bool Activo { get; protected set; }

        protected ObjetoInteractivo(int x, int y)
        {
            X = x;
            Y = y;
            Activo = false;
        }
    }
}
