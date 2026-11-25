namespace PixelEsGUI
{
    // Le da al jugador la llave que abre la puerta de salida.
    /// <summary>Coleccionable que permite abrir la salida.</summary>
    internal class Llave : Coleccionables
    {
        // ---------- Hoja de sprites (Resources/Key 13 - GOLD - Spritesheet.png, recurso "Llave") ----------
        // 250x27: una sola fila de 25 cuadros de 10x27 (el brillo cian recorre la llave).
        const int ColumnasHoja = 25;
        const int FilasHoja = 1;
        const float Fps = 12f;
        const float AnchoPantalla = 14f; // 2x el tamaño original (10x27)
        const float AltoPantalla = 38f;

        /// <summary>Inicializa Llave con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        public Llave(int x, int y) : base(x, y, "Llave", ColumnasHoja, FilasHoja, Fps)
        {
            FijarTamano(AnchoPantalla, AltoPantalla);
            EstablecerRangoFrames(0, ColumnasHoja - 1, true); // brillo en bucle
        }

        /// <summary>Aplica al jugador el efecto específico del coleccionable.</summary>
        /// <param name="jugador">Jugador que interactúa con el objeto.</param>
        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.RecogerLlave();
        }
    }
}
