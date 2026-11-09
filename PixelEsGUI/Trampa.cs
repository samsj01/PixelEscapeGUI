namespace PixelEscape
{
    // Clase concreta (no abstracta): se pueden crear trampas con new Trampa(...).
    // Causa daño al jugador cuando está en su misma posición X.
    internal class Trampa : ObjetoActivable
    {
        public int Dano { get; }

        public Trampa(int x, int y, int dano) : base(x, y)
        {
            Dano = dano;
        }

        public override bool Activar(Jugador jugador)
        {
            Activo = jugador.X == X;
            if (Activo)
            {
                jugador.RecibirDano(Dano);
            }
            return Activo;
        }
    }
}
