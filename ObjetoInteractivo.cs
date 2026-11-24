using Engine2D;

namespace PixelEsGUI
{
    // Base de los objetos del escenario con los que el jugador interactúa.
    // Hereda de AnimatedItem para dibujar su sprite: cada objeto indica el recurso
    // (hoja de sprites) y cómo está dividida (columnas x filas).
    internal abstract class ObjetoInteractivo : AnimatedItem
    {
        public new int X { get; protected set; }
        public new int Y { get; protected set; }
        public bool Activo { get; protected set; }

        protected ObjetoInteractivo(int x, int y, string recurso, int columnas, int filas, float fps)
            : base(x, y, columnas, filas, fps, recurso)
        {
            X = x;
            Y = y;
            Activo = false;
        }

        // Además de guardar X/Y, mueve el sprite en el motor.
        public void SetPos(int x, int y)
        {
            X = x;
            Y = y;
            base.SetPos(x, y);
        }

        // Tamaño con el que se dibuja en pantalla (el motor reduce/amplía el cuadro a este tamaño).
        // Si el recurso no se cargó, esto también deja visible un cuadro de color para detectar el error.
        protected void FijarTamano(float ancho, float alto)
        {
            Ancho = ancho;
            Alto = alto;
        }
    }
}
