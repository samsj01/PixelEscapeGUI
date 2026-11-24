using Engine2D;

namespace PixelEsGUI
{
    // Base de todo lo que el jugador puede recoger.
    // Reúne lo que antes estaba en la interfaz IRecolectable.
    // Hereda de AnimatedItem para dibujar su sprite: cada coleccionable indica el recurso
    // (hoja de sprites) y cómo está dividida (columnas x filas).
    internal abstract class Coleccionables : AnimatedItem
    {
        public new int X { get; protected set; }
        public new int Y { get; protected set; }
        public bool Recogido { get; private set; } = false;

        protected Coleccionables(int x, int y, string recurso, int columnas, int filas, float fps)
            : base(x, y, columnas, filas, fps, recurso)
        {
            X = x;
            Y = y;
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

        // Método plantilla: garantiza que se recoja una sola vez y delega el efecto a cada subclase.
        public void Recoger(Jugador jugador)
        {
            if (Recogido)
            {
                return;
            }
            Recogido = true;
            Visible = false; // desaparece de la escena
            AplicarEfecto(jugador);
        }

        protected abstract void AplicarEfecto(Jugador jugador);
    }
}
