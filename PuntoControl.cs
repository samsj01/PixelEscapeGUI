namespace PixelEsGUI
{
    // Lugar al que regresa el jugador tras perder una vida.
    /// <summary>Objeto que señala un punto de reaparición mediante una bandera animada.</summary>
    internal class PuntoControl : ObjetoActivable
    {
        // ---------- Hoja de sprites (Resources/Jungle Ruins Flag Checkpoint Sprite Sheet.png, recurso "PuntoControl") ----------
        // 1536x1024 dividido en 4 columnas x 2 filas (cuadros de 384x512), 8 cuadros:
        //   cuadro 0:      bandera abajo (punto sin activar, fijo)
        //   cuadros 1 a 3: la bandera sube (una sola vez, al activarse)
        //   cuadros 4 a 7: la bandera ondea (en bucle mientras esté activo)
        const int ColumnasHoja = 4;
        const int FilasHoja = 2;
        const int CuadroInactivo = 0;
        const int PrimerCuadroSubida = 1;
        const int UltimoCuadroSubida = 3;
        const int PrimerCuadroOndeo = 4;
        const int UltimoCuadroOndeo = 7;
        const float FpsSubida = 8f;
        const float FpsOndeo = 8f;
        const float AnchoPantalla = 66f; // misma proporción 3:4 del cuadro original
        const float AltoPantalla = 88f;

        enum FaseBandera { Inactiva, Subiendo, Ondeando }
        FaseBandera fase = FaseBandera.Inactiva;

        /// <summary>Inicializa PuntoControl con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        public PuntoControl(int x, int y) : base(x, y, "PuntoControl", ColumnasHoja, FilasHoja, FpsSubida)
        {
            FijarTamano(AnchoPantalla, AltoPantalla);
            EstablecerRangoFrames(CuadroInactivo, CuadroInactivo, false);
        }

        /// <summary>Actualiza el estado del objeto al interactuar con el jugador.</summary>
        /// <param name="jugador">Jugador que interactúa con el objeto.</param>
        /// <returns>Estado de activación resultante.</returns>
        public override bool Activar(Jugador jugador)
        {
            if (!Activo)
            {
                Activo = true;
                fase = FaseBandera.Subiendo;
                SetFPS(FpsSubida);
                EstablecerRangoFrames(PrimerCuadroSubida, UltimoCuadroSubida, false);
            }
            return Activo;
        }

        // Cuando termina la subida, el motor deja de reproducir; ahí pasamos a ondear en bucle.
        /// <summary>Avanza la animación de la bandera y cambia a ondeo cuando termina la subida.</summary>
        /// <param name="deltaTime">Tiempo transcurrido en segundos.</param>
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            if (fase == FaseBandera.Subiendo && !IsPlaying)
            {
                fase = FaseBandera.Ondeando;
                SetFPS(FpsOndeo);
                EstablecerRangoFrames(PrimerCuadroOndeo, UltimoCuadroOndeo, true);
            }
        }
    }
}
