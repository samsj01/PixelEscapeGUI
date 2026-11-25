using Engine2D;

namespace PixelEsGUI
{
    // Base de los objetos del escenario con los que el jugador interactúa.
    // Hereda de AnimatedItem para dibujar su sprite: cada objeto indica el recurso
    // (hoja de sprites) y cómo está dividida (columnas x filas).
    /// <summary>Base de elementos animados del escenario con posición y estado de activación.</summary>
    internal abstract class ObjetoInteractivo : AnimatedItem
    {
        public new int X { get; protected set; }
        public new int Y { get; protected set; }
        public bool Activo { get; protected set; }

        /// <summary>Inicializa ObjetoInteractivo con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="recurso">Nombre del recurso gráfico.</param>
        /// <param name="columnas">Número de columnas de la hoja de sprites.</param>
        /// <param name="filas">Número de filas de la hoja de sprites.</param>
        /// <param name="fps">Fotogramas por segundo.</param>
        protected ObjetoInteractivo(int x, int y, string recurso, int columnas, int filas, float fps)
            : base(x, y, columnas, filas, fps, recurso)
        {
            X = x;
            Y = y;
            Activo = false;
        }

        // Además de guardar X/Y, mueve el sprite en el motor.
        /// <summary>Actualiza la posición lógica y gráfica del objeto.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        public void SetPos(int x, int y)
        {
            X = x;
            Y = y;
            base.SetPos(x, y);
        }

        // Tamaño con el que se dibuja en pantalla (el motor reduce/amplía el cuadro a este tamaño).
        // Si el recurso no se cargó, esto también deja visible un cuadro de color para detectar el error.
        /// <summary>Establece las dimensiones de representación del sprite.</summary>
        /// <param name="ancho">Ancho de representación en píxeles.</param>
        /// <param name="alto">Alto de representación en píxeles.</param>
        protected void FijarTamano(float ancho, float alto)
        {
            Ancho = ancho;
            Alto = alto;
        }
    }
}
