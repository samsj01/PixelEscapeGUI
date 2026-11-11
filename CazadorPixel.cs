using System;

namespace PixelEsGUI
{
    // Enemigo del Nivel 2: más rápido que el Guardián y solo ataca dentro de su rango.
    internal class CazadorPixel : Enemigo
    {
        const int VelocidadCazador = 3;
        const double RangoPorDefecto = 50;

        // ---------- Hoja de sprites (Resources/cazador_sheet.png) ----------
        // 10 columnas y 7 filas de cuadros de 96x96:
        //   fila 0: quieto izq (6)    fila 1: quieto der (6)
        //   fila 2: camina izq (4)    fila 3: camina der (4)
        //   fila 4: dispara izq (8)   fila 5: dispara der (8)
        //   fila 6: muerte / polvo (10), sin dirección
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
