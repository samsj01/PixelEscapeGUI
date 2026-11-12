namespace PixelEsGUI
{
    // Le da al jugador la llave que abre la puerta de salida.
    internal class Llave : Coleccionables
    {
        // ---------- Hoja de sprites (Resources/Key 13 - GOLD - Spritesheet.png, recurso "Llave") ----------
        // 250x27: una sola fila de 25 cuadros de 10x27 (el brillo cian recorre la llave).
        const int ColumnasHoja = 25;
        const int FilasHoja = 1;
        const float Fps = 12f;
        const float AnchoPantalla = 20f; // 2x el tamaño original (10x27)
        const float AltoPantalla = 54f;

        public Llave(int x, int y) : base(x, y, "Llave", ColumnasHoja, FilasHoja, Fps)
        {
            FijarTamano(AnchoPantalla, AltoPantalla);
            EstablecerRangoFrames(0, ColumnasHoja - 1, true); // brillo en bucle
        }

        protected override void AplicarEfecto(Jugador jugador)
        {
            jugador.RecogerLlave();
        }
    }
}
