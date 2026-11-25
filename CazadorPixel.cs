using System;

namespace PixelEsGUI
{
    // Enemigo del Nivel 2: más rápido que el Guardián y solo ataca dentro de su rango.
    /// <summary>Enemigo que detecta al jugador en su carril y dispara proyectiles.</summary>
    internal class CazadorPixel : Enemigo
    {
        const int VelocidadCazador = 1;
        const double RangoPorDefecto = 240;
        const double ToleranciaVertical = 38;
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

        /// <summary>Inicializa CazadorPixel con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        public CazadorPixel(int x, int y) : base(x, y, "CazadorPixel", FilasHoja, FpsQuieto)
        {
            Velocidad = VelocidadCazador;
            Dano = 1;
            Animar(EstadoAnimacion.Quieto);
        }

        /// <summary>Inicializa CazadorPixel con los datos proporcionados.</summary>
        public CazadorPixel() : this(0, 0)
        {
        }

        /// <summary>Selecciona los fotogramas y la velocidad correspondientes al estado solicitado.</summary>
        /// <param name="estado">Estado de animación solicitado.</param>
        /// <returns>Configuración de fotogramas, velocidad y repetición.</returns>
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
        /// <summary>Reduce los tiempos de recarga y animación del ataque.</summary>
        /// <param name="deltaTime">Tiempo transcurrido en segundos.</param>
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

        /// <summary>Inicia la recarga y la animación de disparo.</summary>
        public void IniciarAtaque()
        {
            recargaRestante = TiempoRecargaDisparo;
            animacionAtaqueRestante = DuracionAnimacionDisparo;
            estadoActualAtaque = true;
            Animar(EstadoAnimacion.Ataca);
        }

        /// <summary>Comprueba si el objetivo está dentro del rango horizontal y la tolerancia vertical.</summary>
        /// <param name="objetivo">Personaje que se evalúa como objetivo.</param>
        /// <returns>True si el objetivo está dentro del rango de detección.</returns>
        public bool Detecta(Personaje objetivo)
        {
            // Solo detecta al jugador en el mismo carril horizontal.
            // Un jugador en plataformas superiores queda fuera de la línea de visión.
            double deltaX = Math.Abs(X - objetivo.X);
            double deltaY = Math.Abs(Y - objetivo.Y);
            return deltaX <= RangoDeteccion && deltaY <= ToleranciaVertical;
        }

        /// <summary>Determina si el ataque está permitido según la colisión y la detección.</summary>
        /// <param name="objetivo">Personaje que se evalúa como objetivo.</param>
        /// <param name="colision">Indica si existe contacto entre los personajes.</param>
        /// <returns>True si se permite el ataque.</returns>
        protected override bool PuedeAtacar(Personaje objetivo, bool colision)
        {
            return colision && Detecta(objetivo);
        }
    }
}
