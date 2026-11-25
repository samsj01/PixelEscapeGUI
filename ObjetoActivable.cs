namespace PixelEsGUI
{
    // Objeto que reacciona cuando el jugador lo activa (trampa, punto de control).
    /// <summary>Base de objetos que reaccionan a la activación del jugador.</summary>
    internal abstract class ObjetoActivable : ObjetoInteractivo
    {
        /// <summary>Inicializa ObjetoActivable con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="recurso">Nombre del recurso gráfico.</param>
        /// <param name="columnas">Número de columnas de la hoja de sprites.</param>
        /// <param name="filas">Número de filas de la hoja de sprites.</param>
        /// <param name="fps">Fotogramas por segundo.</param>
        protected ObjetoActivable(int x, int y, string recurso, int columnas, int filas, float fps)
            : base(x, y, recurso, columnas, filas, fps)
        {
        }

        /// <summary>Ejecuta la reacción del objeto ante el jugador.</summary>
        /// <param name="jugador">Jugador que interactúa con el objeto.</param>
        /// <returns>Estado de activación resultante.</returns>
        public abstract bool Activar(Jugador jugador);
    }
}
