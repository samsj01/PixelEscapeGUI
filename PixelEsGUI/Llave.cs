namespace PixelEscape
{
    // Le da al jugador la llave que abre la puerta de salida.
    internal class Llave : Coleccionables
    {
        public Llave(int x, int y) : base(x, y)
        {
        }

        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.RecogerLlave();
        }
    }
}
