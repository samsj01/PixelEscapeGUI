using System;

namespace PixelEsGUI
{
    // Alex Pixel: el personaje que controla el usuario.
    internal class Jugador : Personaje
    {
        // ---------- Constantes del salto ----------
        const double AnguloVertical = Math.PI / 2;   // 90°: salto recto
        const double AnguloParabolico = Math.PI / 3; // 60°: salto con desplazamiento lateral
        const int Gravedad = 3;
        const int FuerzaSaltoBase = 3;
        const double Tiempo = 0.25;                  // intervalo de simulación por fotograma

        // ---------- Hoja de sprites (Resources/jugador_sheet.png) ----------
        // 8 cuadros por fila y 6 filas. Solo hay animaciones hacia abajo (izquierda y derecha):
        //   fila 0: quieto izq    fila 1: quieto der
        //   fila 2: camina izq    fila 3: camina der
        //   fila 4: salta izq     fila 5: salta der
        const int ColumnasHoja = 8;
        const int FilasHoja = 6;
        const int FilaQuieto = 0;
        const int FilaCamina = 2;
        const int FilaSalta = 4;
        const float FpsQuieto = 6f;
        const float FpsCamina = 12f;
        const float FpsSalta = 12f;

        enum EstadoAnimacion { Ninguno, Quieto, Camina, Salta }
        EstadoAnimacion estadoAnimacion = EstadoAnimacion.Ninguno;
        int direccionAnimacion;

        // ---------- Estado del salto en curso ----------
        double velx;               // velocidad horizontal del salto (negativa = izquierda)
        double angulo;
        int movimientoHorizontal;  // -1 izquierda, 0 quieto, 1 derecha (solo en el fotograma actual)
        int origenX;
        int sueloY;
        double tiempoVuelo;

        // ---------- Propiedades ----------
        public int Puntaje { get; private set; }
        public bool TieneLlave { get; private set; }
        public bool EnElSuelo { get; private set; } = true;
        public bool SaltoParabolico { get; private set; }
        public double VelY { get; private set; }     // velocidad vertical (positiva = subiendo)
        public int FuerzaSalto => FuerzaSaltoBase;
        public double VelSalto { get; protected set; } = 5;

        public Jugador(int x, int y) : base(x, y, "Jugador", ColumnasHoja, FilasHoja, FpsQuieto)
        {
            Vida = 3;
            Dano = 1;
            ActualizarAnimacion(false);
        }

        // ---------- Acciones que disparan los coleccionables ----------
        public void SumarPuntos(int puntos)
        {
            Puntaje += puntos;
        }

        public void RecogerLlave()
        {
            TieneLlave = true;
        }

        // ---------- Movimiento ----------
        // En el aire el desplazamiento lo controla el salto; en el suelo se mueve normal
        // y se recuerda la dirección para decidir el tipo de salto.
        public override bool MoverIzq()
        {
            if (!EnElSuelo)
            {
                return false;
            }
            movimientoHorizontal = -1;
            return base.MoverIzq();
        }

        public override bool MoverDer()
        {
            if (!EnElSuelo)
            {
                return false;
            }
            movimientoHorizontal = 1;
            return base.MoverDer();
        }

        // ---------- Salto ----------
        // Orden esperado en cada fotograma: MoverIzq/MoverDer -> Saltar -> Aterrizar.
        // - Si en este fotograma se movió a un lado: salto parabólico (60°).
        // - Si no se movió: salto vertical (90°, la X no cambia).
        public void Saltar()
        {
            if (!EnElSuelo)
            {
                return;
            }

            SaltoParabolico = movimientoHorizontal != 0;
            angulo = SaltoParabolico ? AnguloParabolico : AnguloVertical;

            double velInicial = VelSalto * FuerzaSalto;
            velx = SaltoParabolico ? velInicial * Math.Cos(angulo) * movimientoHorizontal : 0;
            VelY = velInicial * Math.Sin(angulo);

            origenX = X;
            sueloY = Y;
            tiempoVuelo = 0;
            EnElSuelo = false;
        }

        // Avanza un fotograma: sube y baja por la gravedad y aterriza al volver
        // a la altura de la que despegó. Se llama una vez por fotograma.
        //   x(t) = origenX + velx * t
        //   y(t) = sueloY - (v0y * t - 1/2 * gravedad * t^2)   (el eje Y crece hacia abajo)
        public void Aterrizar()
        {
            if (!EnElSuelo)
            {
                double velInicialY = VelSalto * FuerzaSalto * Math.Sin(angulo);
                double duracionVuelo = 2 * velInicialY / Gravedad;

                tiempoVuelo += Tiempo;
                bool aterriza = tiempoVuelo >= duracionVuelo;
                if (aterriza)
                {
                    tiempoVuelo = duracionVuelo;
                }

                double altura = velInicialY * tiempoVuelo - 0.5 * Gravedad * tiempoVuelo * tiempoVuelo;
                int nuevaX = (int)Math.Round(origenX + velx * tiempoVuelo);
                int nuevaY = aterriza ? sueloY : (int)Math.Round(sueloY - altura);
                SetPos(nuevaX, nuevaY);

                if (aterriza)
                {
                    EnElSuelo = true;
                    SaltoParabolico = false;
                    velx = 0;
                    VelY = 0;
                }
                else
                {
                    VelY = velInicialY - Gravedad * tiempoVuelo;
                }
            }

            // Fin del fotograma: se elige el cuadro de animación según lo que pasó en él.
            ActualizarAnimacion(movimientoHorizontal != 0);

            // El movimiento lateral solo cuenta para el salto del mismo fotograma.
            movimientoHorizontal = 0;
        }

        // ---------- Animación ----------
        // Elige la fila de la hoja según el estado y la dirección (-1 izquierda / 1 derecha).
        // Solo reinicia la animación cuando cambia de estado o de dirección; si no,
        // el motor sigue avanzando los cuadros por su cuenta.
        void ActualizarAnimacion(bool caminando)
        {
            EstadoAnimacion estado;
            if (!EnElSuelo)
            {
                estado = EstadoAnimacion.Salta;
            }
            else if (caminando)
            {
                estado = EstadoAnimacion.Camina;
            }
            else
            {
                estado = EstadoAnimacion.Quieto;
            }

            if (estado == estadoAnimacion && Direccion == direccionAnimacion)
            {
                return;
            }
            estadoAnimacion = estado;
            direccionAnimacion = Direccion;

            int columnaDireccion = Direccion < 0 ? 0 : 1; // 0 = izquierda, 1 = derecha
            switch (estado)
            {
                case EstadoAnimacion.Salta:
                    SetFPS(FpsSalta);
                    SeleccionarFila(FilaSalta + columnaDireccion, false); // una sola vez por salto
                    break;
                case EstadoAnimacion.Camina:
                    SetFPS(FpsCamina);
                    SeleccionarFila(FilaCamina + columnaDireccion, true);
                    break;
                default:
                    SetFPS(FpsQuieto);
                    SeleccionarFila(FilaQuieto + columnaDireccion, true);
                    break;
            }
        }
    }
}
