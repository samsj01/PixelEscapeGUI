using Engine2D;

namespace PixelEsGUI
{
    // Base de todos los personajes (jugador y enemigos).
    // Reúne lo que antes estaba repartido en las interfaces IMovible, IDanable e IAtacante.
    // Hereda de AnimatedItem (y no de GraphicsItem) para que el motor dibuje UN fotograma
    // de la hoja de sprites a la vez, en lugar de estirar la imagen completa.
    /// <summary>Base animada de personajes con movimiento, vida y ataque.</summary>
    internal abstract class Personaje : AnimatedItem
    {
        // ---------- Posición ----------
        public new int X { get; protected set; }
        public new int Y { get; protected set; }

        // ---------- Estado ----------
        public int Vida { get; protected set; } = 1;
        public int Velocidad { get; set; } = 5;
        public int Dano { get; protected set; } = 1;
        public int Direccion { get; protected set; } = 1; // -1 izquierda, 1 derecha
        public bool EstaVivo => Vida > 0;
        protected string Nombre { get; set; }

        // ---------- Constructores ----------
        // Sin hoja de sprites (enemigos por ahora): se dibuja el cuadro de color de 32x32 de siempre.
        /// <summary>Inicializa Personaje con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="nombre">Nombre del personaje y recurso asociado.</param>
        protected Personaje(int x, int y, string nombre) : this(x, y, nombre, 1, 1, 1f)
        {
        }

        // Con hoja de sprites: 'columnas' x 'filas' cuadros; el recurso se busca por 'nombre'.
        /// <summary>Inicializa Personaje con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="nombre">Nombre del personaje y recurso asociado.</param>
        /// <param name="columnas">Columnas de la hoja de sprites.</param>
        /// <param name="filas">Filas de la hoja de sprites.</param>
        /// <param name="fps">Fotogramas por segundo.</param>
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

        /// <summary>Inicializa Personaje con los datos proporcionados.</summary>
        protected Personaje() : this(0, 0, "Personaje")
        {
        }

        // ---------- Movimiento (antes IMovible) ----------
        // Además de guardar X/Y, mueve el sprite en el motor (si no, se dibujaría siempre en el origen).
        /// <summary>Actualiza las coordenadas lógicas y gráficas del personaje.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        public void SetPos(int x, int y)
        {
            X = x;
            Y = y;
            base.SetPos(x, y);
        }

        // Devuelve true si el personaje se movió. Las subclases pueden restringirlo.
        /// <summary>Desplaza al personaje hacia la izquierda.</summary>
        /// <returns>True al ejecutar el desplazamiento base.</returns>
        public virtual bool MoverIzq()
        {
            SetPos(X - Velocidad, Y);
            Direccion = -1;
            return true;
        }

        /// <summary>Desplaza al personaje hacia la derecha.</summary>
        /// <returns>True al ejecutar el desplazamiento base.</returns>
        public virtual bool MoverDer()
        {
            SetPos(X + Velocidad, Y);
            Direccion = 1;
            return true;
        }

        // ---------- Vida (antes IDanable) ----------
        /// <summary>Reduce la vida sin permitir que quede por debajo de cero.</summary>
        /// <param name="dano">Cantidad de daño.</param>
        public virtual void RecibirDano(int dano)
        {
            Vida -= dano;
            if (Vida < 0)
            {
                Vida = 0;
            }
        }

        /// <summary>Incrementa la vida del personaje.</summary>
        /// <param name="cantidad">Cantidad de vida que se añade.</param>
        public void GanarVida(int cantidad)
        {
            Vida += cantidad;
        }

        /// <summary>Comprueba si el personaje ha agotado su vida.</summary>
        /// <returns>True si la vida se ha agotado.</returns>
        public bool EstaMuerto()
        {
            return !EstaVivo;
        }

        // ---------- Ataque (antes IAtacante) ----------
        // Plantilla del ataque: las subclases solo cambian la condición (PuedeAtacar).
        // Devuelve true si el ataque hizo daño.
        /// <summary>Aplica daño al objetivo cuando se cumplen las condiciones del ataque.</summary>
        /// <param name="objetivo">Personaje objetivo del ataque.</param>
        /// <param name="colision">Indica si existe contacto con el objetivo.</param>
        /// <returns>True si se ejecutó el ataque.</returns>
        public bool Atacar(Personaje objetivo, bool colision)
        {
            if (!PuedeAtacar(objetivo, colision))
            {
                return false;
            }
            objetivo.RecibirDano(Dano);
            return true;
        }

        /// <summary>Evalúa si existe una colisión que permita atacar.</summary>
        /// <param name="objetivo">Personaje objetivo del ataque.</param>
        /// <param name="colision">Indica si existe contacto con el objetivo.</param>
        /// <returns>True si se permite atacar.</returns>
        protected virtual bool PuedeAtacar(Personaje objetivo, bool colision)
        {
            return colision;
        }
    }
}
