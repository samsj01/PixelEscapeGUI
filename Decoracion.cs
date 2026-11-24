namespace PixelEsGUI
{
    // Sprite decorativo individual de Resources.resx. No tiene colisión ni altera la lógica.
    internal class Decoracion : ObjetoInteractivo
    {
        public Decoracion(int x, int y, string recurso, int ancho, int alto)
            : base(x, y, recurso, 1, 1, 1f)
        {
            FijarTamano(ancho, alto);
            EstablecerRangoFrames(0, 0, false);
        }

        // Decoración tomada de una hoja de sprites: se dibuja solo la celda 'cuadro'.
        public Decoracion(int x, int y, string recurso, int columnas, int filas, int cuadro, int ancho, int alto)
            : base(x, y, recurso, columnas, filas, 1f)
        {
            FijarTamano(ancho, alto);
            EstablecerRangoFrames(cuadro, cuadro, false);
        }
    }
}
