namespace PixelEsGUI
{
    // Salida del nivel: se abre si el jugador ya recogió la llave.
    internal class Puerta : ObjetoInteractivo
    {
        // ---------- Hoja de sprites (Resources/Mossy Ruin Door Animation Sheet.png, recurso "Puerta") ----------
        // 1536x1024 dividido en 4 columnas x 2 filas (cuadros de 384x512), 8 cuadros:
        //   cuadro 0: cerrada ... cuadro 7: completamente abierta (se reproduce una sola vez).
        const int ColumnasHoja = 4;
        const int FilasHoja = 2;
        const int TotalCuadros = ColumnasHoja * FilasHoja;
        const float FpsAbrir = 8f;
        const float AnchoPantalla = 75f; // misma proporción 3:4 del cuadro original
        const float AltoPantalla = 100f;

        public bool EstadoPuerta { get; private set; } = false; // true = abierta

        public Puerta(int x, int y) : base(x, y, "Puerta", ColumnasHoja, FilasHoja, FpsAbrir)
        {
            FijarTamano(AnchoPantalla, AltoPantalla);
            EstablecerRangoFrames(0, 0, false); // cerrada: cuadro fijo
        }

        public bool Abrir(Jugador jugador)
        {
            if (jugador.TieneLlave && !EstadoPuerta)
            {
                EstadoPuerta = true;
                Activo = true;
                EstablecerRangoFrames(0, TotalCuadros - 1, false); // animación de apertura, una vez
            }
            return EstadoPuerta;
        }


        //Este método se puede usar para abrir la puerta si se cumplen los objetivos del nivel, pero no es necesario ya que el método Abrir() ya verifica si el jugador tiene la llave.
        //El método abrir la puerta se puede llamar desde el Escenario.cs cuando se cumplan los objetivos del nivel, y no es necesario pasar un parámetro booleano.
        //El método abrir por si solo se cumpl si solo se tiene la llave, este permita abrir la puerta cuando se hayan cumplido los requisitos del nivel
        /*public bool AbrirSiCumpleObjetivos(bool cumple)
        {
        if (cumple && !EstadoPuerta)
        {
            EstadoPuerta = true;
            Activo = true;

            EstablecerRangoFrames(0,TotalCuadros - 1,false);
        }

        return EstadoPuerta;
        }
          */
    }
}
