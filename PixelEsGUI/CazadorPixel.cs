using System;

namespace PixelEscape
{
    // Enemigo del Nivel 2: más rápido que el Guardián y solo ataca dentro de su rango.
    internal class CazadorPixel : Personaje
    {
        const int VelocidadCazador = 3;
        const double RangoPorDefecto = 50;

        public double RangoDeteccion { get; private set; } = RangoPorDefecto;

        public CazadorPixel(int x, int y) : base(x, y)
        {
            Velocidad = VelocidadCazador;
            Dano = 1;
        }

        public CazadorPixel() : this(0, 0)
        {
        }

        // true si el objetivo está dentro del rango de detección.
        public bool Detecta(Personaje objetivo)
        {
            double deltaX = X - objetivo.X;
            double deltaY = Y - objetivo.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY) <= RangoDeteccion;
        }

        protected override bool PuedeAtacar(Personaje objetivo, bool colision)
        {
            return colision && Detecta(objetivo);
        }
    }
}
