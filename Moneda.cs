namespace PixelEscape
{
    // Suma puntos al jugador.
    internal class Moneda : Coleccionables
    {
        public int Valor { get; }

        public Moneda(int x, int y, int valor = 100) : base(x, y)
        {
            Valor = valor;
        }

        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.SumarPuntos(Valor);
        }
    }
}
