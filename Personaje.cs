namespace PixelEscape
{
    // Base de todos los personajes (jugador y enemigos).
    // Reúne lo que antes estaba repartido en las interfaces IMovible, IDanable e IAtacante.
    internal abstract class Personaje
    {
        // ---------- Posición ----------
        public int X { get; protected set; }
        public int Y { get; protected set; }

        // ---------- Estado ----------
        public int Vida { get; protected set; } = 1;
        public int Velocidad { get; set; } = 1;
        public int Dano { get; protected set; } = 1;
        public int Direccion { get; protected set; } = 1; // -1 izquierda, 1 derecha
        public bool EstaVivo => Vida > 0;

        // ---------- Constructores ----------
        protected Personaje(int x, int y)
        {
            X = x;
            Y = y;
        }

        protected Personaje() : this(0, 0)
        {
        }

        // ---------- Movimiento (antes IMovible) ----------
        public void SetPos(int x, int y)
        {
            X = x;
            Y = y;
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
