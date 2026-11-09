namespace PixelEscape
{
    // Base de todo lo que el jugador puede recoger.
    // Reúne lo que antes estaba en la interfaz IRecolectable.
    internal abstract class Coleccionables
    {
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public bool Recogido { get; private set; } = false;

        protected Coleccionables(int x, int y)
        {
            X = x;
            Y = y;
        }

        // Método plantilla: garantiza que se recoja una sola vez y delega el efecto a cada subclase.
        public void Recoger(Jugador jugador)
        {
            if (Recogido)
            {
                return;
            }
            Recogido = true;
            AplicarEfecto(jugador);
        }

        protected abstract void AplicarEfecto(Jugador jugador);
    }
}
