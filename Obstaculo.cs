namespace PixelEscape
{
    // Bloquea el paso del jugador.
    internal class Obstaculo
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public string Tipo { get; }

        public Obstaculo(int x, int y, string tipo)
        {
            X = x;
            Y = y;
            Tipo = tipo;
        }
    }
}
