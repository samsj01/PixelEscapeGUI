namespace PixelEsGUI
{
    // Suma puntos al jugador.
    internal class Moneda : Coleccionables
    {
        // ---------- Hoja de sprites (Resources/monedaaa.png, recurso "Moneda") ----------
        // 798x312 dividido en 5 columnas x 2 filas (cuadros de ~159x156), 10 casillas:
        //   la casilla 0 (arriba a la izquierda) está vacía; la moneda gira en las casillas 1 a 9
        //   (de frente -> de canto -> de frente otra vez).
        const int ColumnasHoja = 5;
        const int FilasHoja = 2;
        const int PrimerCuadro = 1;
        const int UltimoCuadro = 9;
        const float Fps = 12f;
        const float TamanoPantalla = 40f; // px en pantalla (cuadrado)

        public int Valor { get; }

        public Moneda(int x, int y, int valor = 100) : base(x, y, "Moneda", ColumnasHoja, FilasHoja, Fps)
        {
            Valor = valor;
            FijarTamano(TamanoPantalla, TamanoPantalla);
            EstablecerRangoFrames(PrimerCuadro, UltimoCuadro, true); // gira en bucle
        }

        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.SumarPuntos(Valor);
        }
    }
}
