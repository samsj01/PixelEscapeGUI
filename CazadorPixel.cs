using System;

namespace PixelEsGUI
{
    // Enemigo del Nivel 2: más rápido que el Guardián y solo ataca dentro de su rango.
    internal class CazadorPixel : Enemigo
    {
        const int VelocidadCazador = 1;
        const double RangoPorDefecto = 360;
        const double TiempoRecargaDisparo = 1.9;
        const double DuracionAnimacionDisparo = 0.62;
        double recargaRestante;
        double animacionAtaqueRestante;
        bool estadoActualAtaque;

        public bool PuedeDisparar => recargaRestante <= 0;
        public bool EstaEnAnimacionAtaque => animacionAtaqueRestante > 0;

        // ---------- Hoja de sprites (Resources/cazador_sheet.png) ----------
        const int FilasHoja = 7;
        const int FilaQuieto = 0;
        const int FilaCamina = 2;
        const int FilaDispara = 4;
        const int FilaMuerte = 6;
        const float FpsQuieto = 6f;
        const float FpsCamina = 10f;
        const float FpsDispara = 12f;
        const float FpsMuerte = 12f;

        public double RangoDeteccion { get; private set; } = RangoPorDefecto;

        public CazadorPixel(int x, int y) : base(x, y, "CazadorPixel", FilasHoja, FpsQuieto)
        {
            Velocidad = VelocidadCazador;
            Dano = 1;
            Animar(EstadoAnimacion.Quieto);
        }

        public CazadorPixel() : this(0, 0)
        {
        }

        protected override Animacion ObtenerAnimacion(EstadoAnimacion estado)
        {
            switch (estado)
            {
                case EstadoAnimacion.Camina:
                    return new Animacion(FilaCamina, 4, FpsCamina, true, true);
                case EstadoAnimacion.Ataca:
                case EstadoAnimacion.AtacaFuerte:
                    return new Animacion(FilaDispara, 8, FpsDispara, false, true);
                case EstadoAnimacion.Muere:
                    return new Animacion(FilaMuerte, 10, FpsMuerte, false, false);
                default: // Quieto (el Cazador no tiene animación de defensa)
                    return new Animacion(FilaQuieto, 6, FpsQuieto, true, true);
            }
        }

        // true si el objetivo está dentro del rango de detección.
        public void ActualizarTemporizadores(double deltaTime)
        {
            double dt = Math.Clamp(deltaTime, 0, 0.05);
            recargaRestante = Math.Max(0, recargaRestante - dt);
            animacionAtaqueRestante = Math.Max(0, animacionAtaqueRestante - dt);
            if (animacionAtaqueRestante <= 0 && estadoActualAtaque)
            {
                estadoActualAtaque = false;
                Animar(EstadoAnimacion.Quieto);
            }
        }

        public void IniciarAtaque()
        {
            recargaRestante = TiempoRecargaDisparo;
            animacionAtaqueRestante = DuracionAnimacionDisparo;
            estadoActualAtaque = true;
            Animar(EstadoAnimacion.Ataca);
        }

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
