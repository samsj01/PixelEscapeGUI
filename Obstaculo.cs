using Engine2D;

namespace PixelEsGUI
{
    // Tile de terreno tomado del recurso BosqueTiles (192x112 = 12x7 cuadros de 16x16).
    // Los cuadros se dibujan al doble: 32x32 px. No se generan ni recortan sprites.
    internal class Obstaculo : ObjetoInteractivo
    {
        const int ColumnasHoja = 12;
        const int FilasHoja = 7;
        public const int Tamano = 32;

        public Obstaculo(int x, int y, int fila, int columna)
            : base(x, y, "BosqueTiles", ColumnasHoja, FilasHoja, 1f)
        {
            FijarTamano(Tamano, Tamano);
            int cuadro = fila * ColumnasHoja + columna;
            EstablecerRangoFrames(cuadro, cuadro, false);
        }
    }
}
