using Engine2D;

namespace PixelEsGUI
{
    // Base de todos los personajes (jugador y enemigos).
    // Reúne lo que antes estaba repartido en las interfaces IMovible, IDanable e IAtacante.
    // Hereda de AnimatedItem (y no de GraphicsItem) para que el motor dibuje UN fotograma
    // de la hoja de sprites a la vez, en lugar de estirar la imagen completa.
    internal abstract class Personaje : AnimatedItem
    {
        // ---------- Posición ----------
        public new int X { get; protected set; }
        public new int Y { get; protected set; }

        // ---------- Estado ----------
        public int Vida { get; protected set; } = 1;
        public int Velocidad { get; set; } = 1;
        public int Dano { get; protected set; } = 1;
        public int Direccion { get; protected set; } = 1; // -1 izquierda, 1 derecha
        public bool EstaVivo => Vida > 0;
        protected string Nombre { get; set; }

        // ---------- Constructores ----------
        // Sin hoja de sprites (enemigos por ahora): se dibuja el cuadro de color de 32x32 de siempre.
        protected Personaje(int x, int y, string nombre) : this(x, y, nombre, 1, 1, 1f)
        {
        }

        // Con hoja de sprites: 'columnas' x 'filas' cuadros; el recurso se busca por 'nombre'.
        protected Personaje(int x, int y, string nombre, int columnas, int filas, float fps)
            : base(x, y, columnas, filas, fps, nombre)
        {
            X = x;
            Y = y;
            Nombre = nombre;

            if (Imagen == null)
            {
                Ancho = 32;
                Alto = 32;
            }
        }

        protected Personaje() : this(0, 0, "Personaje")
        {
        }

        // ---------- Movimiento (antes IMovible) ----------
        // Además de guardar X/Y, mueve el sprite en el motor (si no, se dibujaría siempre en el origen).
        public void SetPos(int x, int y)
        {
            X = x;
            Y = y;
            base.SetPos(x, y);
        }

        // Devuelve true si el personaje se movió. Las subclases pueden restringirlo.
        public virtual bool MoverIzq()
        {
            SetPos(X - Velocidad, Y);
            Direccion = -1;
            return true;
        }

        public virtual bool MoverDer()
        {
            SetPos(X + Velocidad, Y);
            Direccion = 1;
            return true;
        }

        // ---------- Vida (antes IDanable) ----------
        public virtual void RecibirDano(int dano)
        {
            Vida -= dano;
            if (Vida < 0)
            {
                Vida = 0;
            }
        }

        public void GanarVida(int cantidad)
        {
            Vida += cantidad;
        }

        public bool EstaMuerto()
        {
            return !EstaVivo;
        }

        // ---------- Ataque (antes IAtacante) ----------
        // Plantilla del ataque: las subclases solo cambian la condición (PuedeAtacar).
        // Devuelve true si el ataque hizo daño.
        public bool Atacar(Personaje objetivo, bool colision)
        {
            if (!PuedeAtacar(objetivo, colision))
            {
                return false;
            }
            objetivo.RecibirDano(Dano);
            return true;
        }

        protected virtual bool PuedeAtacar(Personaje objetivo, bool colision)
        {
            return colision;
        }
    }
}
