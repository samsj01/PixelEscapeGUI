using Engine2D;

namespace PixelEsGUI
{
    // Tile de terreno tomado del recurso BosqueTiles (192x112 = 12x7 cuadros de 16x16).
    // Los cuadros se dibujan al doble: 32x32 px. No se generan ni recortan sprites.
    /// <summary>Elemento gráfico del terreno construido a partir de una hoja de sprites.</summary>
    internal class Obstaculo : ObjetoInteractivo
    {
        const int ColumnasHoja = 12;
        const int FilasHoja = 7;
        public const int Tamano = 32;

        /// <summary>Inicializa Obstaculo con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="fila">Fila del tile en la hoja de sprites.</param>
        /// <param name="columna">Columna del tile en la hoja de sprites.</param>
        public Obstaculo(int x, int y, int fila, int columna)
            : base(x, y, "BosqueTiles", ColumnasHoja, FilasHoja, 1f)
        {
            FijarTamano(Tamano, Tamano);
            int cuadro = fila * ColumnasHoja + columna;
            EstablecerRangoFrames(cuadro, cuadro, false);
        }

        // Tile de otra hoja (nivel 2): celdas uniformes de columnas x filas, dibujadas a 32x32.
        /// <summary>Inicializa Obstaculo con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="recurso">Nombre del recurso gráfico.</param>
        /// <param name="columnas">Número de columnas de la hoja de sprites.</param>
        /// <param name="filas">Número de filas de la hoja de sprites.</param>
        /// <param name="cuadro">Índice del fotograma seleccionado.</param>
        public Obstaculo(int x, int y, string recurso, int columnas, int filas, int cuadro)
            : base(x, y, recurso, columnas, filas, 1f)
        {
            FijarTamano(Tamano, Tamano);
            EstablecerRangoFrames(cuadro, cuadro, false);
        }

        // Dibuja el tile 'px' píxeles más grande (hacia derecha y abajo) para que dos tiles vecinos
        // se solapen y no quede una línea fina por donde se vea el fondo. No cambia la colisión.
        /// <summary>Amplía el tamaño gráfico del obstáculo para cubrir separaciones entre tiles.</summary>
        /// <param name="px">Píxeles adicionales del borde.</param>
        public void AgrandarBorde(int px)
        {
            FijarTamano(Tamano + px, Tamano + px);
        }
    }
}
