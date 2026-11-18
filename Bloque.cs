using Engine2D;

namespace PixelEsGUI
{
    // Bloque de terreno (césped, tierra, piedra) tomado de Resources/tileset.png (recurso "TileSet").
    // ---------- Hoja de sprites ----------
    // 400x368 dividido en cuadros de 16x16 => 25 columnas x 23 filas (575 cuadros).
    // Número de cuadro = fila * 25 + columna (se cuenta de izquierda a derecha, de arriba abajo).
    // Cuadros útiles (fila, columna):
    //   césped arriba: (1,1) (1,3) (1,5)     piedra: (1,7)
    //   tierra:        (3,1) (3,5) (5,1) (5,3) (5,5)
    internal class Bloque : ObjetoInteractivo
    {
        const int ColumnasHoja = 25;
        const int FilasHoja = 23;
        public const int Tamano = 32; // cada cuadro de 16x16 se dibuja al doble

        public Bloque(int x, int y, int fila, int columna)
            : base(x, y, "TileSet", ColumnasHoja, FilasHoja, 1f)
        {
            FijarTamano(Tamano, Tamano);

            // Un solo cuadro fijo de la hoja (sin animación).
            int cuadro = fila * ColumnasHoja + columna;
            EstablecerRangoFrames(cuadro, cuadro, false);
        }
    }
}