namespace PixelEscape
{
    // Salida del nivel: se abre si el jugador ya recogió la llave.
    internal class Puerta : ObjetoInteractivo
    {
        public bool EstadoPuerta { get; private set; } = false; // true = abierta

        public Puerta(int x, int y) : base(x, y)
        {
        }

        public bool Abrir(Jugador jugador)
        {
            if (jugador.TieneLlave)
            {
                EstadoPuerta = true;
            }
            return EstadoPuerta;
        }
    }
}
