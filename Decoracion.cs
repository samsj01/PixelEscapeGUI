namespace PixelEsGUI
{
    // Sprite decorativo individual de Resources.resx. No tiene colisión ni altera la lógica.
    /// <summary>Elemento visual del escenario sin lógica de colisión propia.</summary>
    internal class Decoracion : ObjetoInteractivo
    {
        /// <summary>Inicializa Decoracion con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="recurso">Nombre del recurso gráfico.</param>
        /// <param name="ancho">Ancho de representación en píxeles.</param>
        /// <param name="alto">Alto de representación en píxeles.</param>
        public Decoracion(int x, int y, string recurso, int ancho, int alto)
            : base(x, y, recurso, 1, 1, 1f)
        {
            FijarTamano(ancho, alto);
            EstablecerRangoFrames(0, 0, false);
        }

        // Decoración tomada de una hoja de sprites: se dibuja solo la celda 'cuadro'.
        /// <summary>Inicializa Decoracion con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="recurso">Nombre del recurso gráfico.</param>
        /// <param name="columnas">Número de columnas de la hoja de sprites.</param>
        /// <param name="filas">Número de filas de la hoja de sprites.</param>
        /// <param name="cuadro">Índice del fotograma seleccionado.</param>
        /// <param name="ancho">Ancho de representación en píxeles.</param>
        /// <param name="alto">Alto de representación en píxeles.</param>
        public Decoracion(int x, int y, string recurso, int columnas, int filas, int cuadro, int ancho, int alto)
            : base(x, y, recurso, columnas, filas, 1f)
        {
            FijarTamano(ancho, alto);
            EstablecerRangoFrames(cuadro, cuadro, false);
        }
    }
}
