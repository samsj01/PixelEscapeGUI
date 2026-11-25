namespace PixelEsGUI
{
    // Salida del nivel: se abre si el jugador ya recogió la llave.
    /// <summary>Salida animada del nivel que puede abrirse al cumplir las condiciones.</summary>
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

        /// <summary>Inicializa Puerta con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        public Puerta(int x, int y) : base(x, y, "Puerta", ColumnasHoja, FilasHoja, FpsAbrir)
        {
            FijarTamano(AnchoPantalla, AltoPantalla);
            EstablecerRangoFrames(0, 0, false); // cerrada: cuadro fijo
        }

        /// <summary>Abre la puerta si se cumplen las condiciones de la sobrecarga utilizada.</summary>
        /// <param name="jugador">Jugador que interactúa con el objeto.</param>
        /// <returns>True si la puerta queda abierta.</returns>
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

        // Método para abrir la puerta sin necesidad de un jugador (por ejemplo, para pruebas o eventos especiales)
        //Abrir(Jugador jugador): mantiene la comprobación original de la llave.
        //Abrir() : permite abrir la puerta cuando el sistema ya verificó los objetivos
        /// <summary>Abre la puerta si se cumplen las condiciones de la sobrecarga utilizada.</summary>
        /// <returns>True si la puerta queda abierta.</returns>
        public bool Abrir()
        {
            if (!EstadoPuerta)
            {
                EstadoPuerta = true;
                Activo = true;

                EstablecerRangoFrames(
                    0,
                    TotalCuadros - 1,
                    false
                );
            }

            return EstadoPuerta;
        }

    }
}
