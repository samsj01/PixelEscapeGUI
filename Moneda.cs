namespace PixelEsGUI
{
    // Suma puntos al jugador.
    /// <summary>Coleccionable que incrementa el puntaje.</summary>
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
        const float TamanoPantalla = 24f; // px en pantalla (cuadrado)

        public int Valor { get; }

        /// <summary>Inicializa Moneda con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="valor">Puntos otorgados por la moneda.</param>
        public Moneda(int x, int y, int valor = 100) : base(x, y, "Moneda", ColumnasHoja, FilasHoja, Fps)
        {
            Valor = valor;
            FijarTamano(TamanoPantalla, TamanoPantalla);
            EstablecerRangoFrames(PrimerCuadro, UltimoCuadro, true); // gira en bucle
        }

        /// <summary>Aplica al jugador el efecto específico del coleccionable.</summary>
        /// <param name="jugador">Jugador que interactúa con el objeto.</param>
        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.SumarPuntos(Valor);
        }
    }
}
