using System;
using System.Collections.Generic;
using System.Drawing;

namespace PixelEsGUI
{
    // Alex Pixel: el personaje que controla el usuario.
    internal class Jugador : Personaje
    {
        // ---------- Constantes del salto ----------
        const double AnguloVertical = Math.PI / 2;   // 90°: salto recto
        const double AnguloParabolico = Math.PI / 3; // 60°: salto con desplazamiento lateral
        const int Gravedad = 4;
        const int FuerzaSaltoBase = 4;
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
        double sueloY;             // altura (Y) de la que parte la parábola actual
        int sueloBaseY;            // Y del jugador cuando está parado en el suelo principal
        double tiempoVuelo;

        // ---------- Propiedades ----------
        public int Puntaje { get; private set; }
        public bool TieneLlave { get; private set; }
        public bool TieneEscudo { get; private set; }

        // Tras romperse el escudo el jugador es invulnerable un instante (60 fotogramas ~ 1 s),
        // para que un enemigo que lo sigue tocando no lo mate en el fotograma siguiente.
        const int FotogramasInvulnerable = 60;
        int fotogramasInvulnerable;
        public bool Invulnerable => fotogramasInvulnerable > 0;
        public bool EnElSuelo { get; private set; } = true;
        public bool SaltoParabolico { get; private set; }
        public double VelY { get; private set; }     // velocidad vertical (positiva = subiendo)
        public int FuerzaSalto => FuerzaSaltoBase;
        public double VelSalto { get; protected set; } = 7; // 7 * 4 = 28 -> sube ~98 px (antes 5 -> ~50 px)

        public Jugador(int x, int y) : base(x, y, "Jugador", ColumnasHoja, FilasHoja, FpsQuieto)
        {
            Vida = 3;
            Dano = 1;
            sueloBaseY = y;
            ActualizarAnimacion(false);
        }

        // ---------- Acciones que disparan los coleccionables ----------
        public void EstablecerPuntaje(int puntos)
        {
            Puntaje = puntos;
        }

        public void ReiniciarPartida()
        {
            Vida = 3;
            Puntaje = 0;
            TieneLlave = false;
            TieneEscudo = false;
            fotogramasInvulnerable = 0;
        }

        public void SumarPuntos(int puntos)
        {
            Puntaje += puntos;
        }

        public void RecogerLlave()
        {
            TieneLlave = true;
        }

        // El escudo absorbe UN impacto: no se pierde vida y el nivel no se reinicia.
        public void ActivarEscudo()
        {
            TieneEscudo = true;
        }

        // true si el impacto queda absorbido (invulnerable o escudo). El escudo se gasta al absorber.
        public bool AbsorbeImpacto()
        {
            if (Invulnerable)
            {
                return true;
            }
            if (TieneEscudo)
            {
                TieneEscudo = false;
                fotogramasInvulnerable = FotogramasInvulnerable;
                return true;
            }
            return false;
        }

        public override void RecibirDano(int dano)
        {
            if (AbsorbeImpacto())
            {
                return; // la vida queda igual que antes del impacto
            }
            base.RecibirDano(dano);
        }

        // Una vez por fotograma: cuenta la invulnerabilidad y hace parpadear al jugador.
        void ActualizarInvulnerabilidad()
        {
            if (fotogramasInvulnerable > 0)
            {
                fotogramasInvulnerable--;
                Visible = fotogramasInvulnerable == 0 || (fotogramasInvulnerable / 4) % 2 == 0;
            }
        }

        //Para perder la llave cuando el jugador muere, se reinicia el juego y pierde la llave que tenía, tambien para cuando inicia un nuevo nivel, se reinicia el juego y pierde la llave que tenía

        public void ReiniciarEstado(int x, int y)
        {
            // Reiniciar llave y escudo
            TieneLlave = false;
            TieneEscudo = false;
            fotogramasInvulnerable = 0;

            // Reiniciar salto
            EnElSuelo = true;
            SaltoParabolico = false;
            VelY = 0;

            velx = 0;
            tiempoVuelo = 0;
            movimientoHorizontal = 0;

            // Reiniciar posición
            sueloY = y;
            sueloBaseY = y;
            SetPos(x, y);

            // Volver a mostrar el personaje
            Visible = true;

            // Actualizar animación
            estadoAnimacion = EstadoAnimacion.Ninguno;
            ActualizarAnimacion(false);
        }


        // ---------- Movimiento ----------
        // En el aire el desplazamiento lo controla el salto; en el suelo se mueve normal
        // y se recuerda la dirección para decidir el tipo de salto.
        // Si el paso lo metería dentro de una plataforma, se deshace (los bloques no se atraviesan de lado).
        public override bool MoverIzq()
        {
            movimientoHorizontal = 3;
            int xAntes = X;
            bool seMovio = base.MoverIzq();
            if (ChocaConPlataforma())
            {
                SetPos(xAntes, Y);
            }
            return seMovio;
        }

        public override bool MoverDer()
        {
            movimientoHorizontal = 3;
            int xAntes = X;
            bool seMovio = base.MoverDer();
            if (ChocaConPlataforma())
            {
                SetPos(xAntes, Y);
            }
            return seMovio;
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

            // Calculamos únicamente la velocidad vertical inicial (Impulso en Y)
            // Se usa AnguloVertical (o directamente la velocidad del salto) para el impulso vertical.
            double velInicial = VelSalto * FuerzaSalto;
            VelY = velInicial * Math.Sin(AnguloVertical);

            sueloY = Y;
            tiempoVuelo = 0;
            EnElSuelo = false;
        }

        // Avanza un fotograma del salto o de la caída. Se llama una vez por fotograma.
        //   y(t) = sueloY - (v0y * t - 1/2 * gravedad * t^2)   (el eje Y crece hacia abajo)
        // - Subiendo: si la cabeza llega al borde de abajo de una plataforma, rebota y empieza a caer.
        // - Bajando: aterriza en lo primero que cruce con los pies (tope de una plataforma o suelo principal).
        // - Parado en el suelo: si ya no hay nada bajo los pies (se salió del borde), empieza a caer.
        public void Aterrizar()
        {
            ActualizarInvulnerabilidad();

            if (EnElSuelo && !HaySueloBajoLosPies())
            {
                EmpezarCaida();
            }

            if (!EnElSuelo)
            {
                double velInicialY = VelSalto * FuerzaSalto;
                int piesAntes = PiesY;
                int cabezaAntes = Y + CuerpoY;

                tiempoVuelo += Tiempo;
                double altura = velInicialY * tiempoVuelo - 0.5 * Gravedad * tiempoVuelo * tiempoVuelo;
                int nuevaY = (int)Math.Round(sueloY - altura);
                VelY = velInicialY - Gravedad * tiempoVuelo;

                if (VelY > 0)
                {
                    // Subiendo: ¿choca la cabeza con el borde inferior de una plataforma?
                    int techo = BuscarTecho(cabezaAntes, nuevaY + CuerpoY);
                    if (techo != SinSuperficie)
                    {
                        SetPos(X, techo - CuerpoY);
                        EmpezarCaida();
                        VelY = 0;
                    }
                    else
                    {
                        SetPos(X, nuevaY);
                    }
                }
                else
                {
                    // Bajando: ¿cruzan los pies el tope de una plataforma o el suelo?
                    int superficie = BuscarSuperficie(piesAntes, nuevaY + PiesOffset);
                    if (superficie != SinSuperficie)
                    {
                        SetPos(X, superficie - PiesOffset);
                        EnElSuelo = true;
                        SaltoParabolico = false;
                        velx = 0;
                        VelY = 0;
                    }
                    else
                    {
                        SetPos(X, nuevaY);
                    }
                }
            }

            // Fin del fotograma: actualiza la animación según la dirección actual
            ActualizarAnimacion(movimientoHorizontal != 0);

            // Reiniciamos el registro de entrada horizontal del fotograma
            movimientoHorizontal = 0;
        }

        // ---------- Plataformas (bloques sólidos) ----------
        // Cuerpo REAL del jugador dentro de su cuadro de 96x128 (el resto es espacio transparente).
        const int CuerpoX = 32;
        const int CuerpoY = 36;
        const int CuerpoAncho = 34;
        const int CuerpoAlto = 52;
        const int PiesOffset = CuerpoY + CuerpoAlto; // distancia del borde superior del cuadro a los pies (88)
        const int SinSuperficie = int.MinValue;

        public Rectangle Cuerpo => new Rectangle(X + CuerpoX, Y + CuerpoY, CuerpoAncho, CuerpoAlto);
        public int PiesY => Y + PiesOffset;

        // Rectángulos de las plataformas del nivel (los llena Escenario). El suelo principal no va aquí.
        public List<Rectangle> Plataformas { get; } = new List<Rectangle>();

        bool ChocaConPlataforma()
        {
            Rectangle cuerpo = Cuerpo;
            foreach (Rectangle plataforma in Plataformas)
            {
                if (cuerpo.IntersectsWith(plataforma))
                {
                    return true;
                }
            }
            return false;
        }

        // true si el cuerpo está (aunque sea un poco) encima o debajo de la plataforma, en horizontal.
        bool SolapaEnX(Rectangle plataforma)
        {
            int izquierda = X + CuerpoX;
            int derecha = izquierda + CuerpoAncho;
            return izquierda < plataforma.Right && derecha > plataforma.Left;
        }

        bool HaySueloBajoLosPies()
        {
            if (PiesY >= sueloBaseY + PiesOffset)
            {
                return true; // suelo principal
            }
            foreach (Rectangle plataforma in Plataformas)
            {
                if (plataforma.Top == PiesY && SolapaEnX(plataforma))
                {
                    return true;
                }
            }
            return false;
        }

        // Devuelve la Y (de los pies) de la superficie más alta que los pies cruzaron al bajar, o SinSuperficie.
        int BuscarSuperficie(int piesAntes, int piesNuevos)
        {
            int mejor = int.MaxValue;
            int sueloPrincipal = sueloBaseY + PiesOffset;
            if (piesAntes <= sueloPrincipal && piesNuevos >= sueloPrincipal)
            {
                mejor = sueloPrincipal;
            }
            foreach (Rectangle plataforma in Plataformas)
            {
                if (SolapaEnX(plataforma) && piesAntes <= plataforma.Top && piesNuevos >= plataforma.Top && plataforma.Top < mejor)
                {
                    mejor = plataforma.Top;
                }
            }
            return mejor == int.MaxValue ? SinSuperficie : mejor;
        }

        // Devuelve la Y del borde inferior de la plataforma que la cabeza cruzó al subir, o SinSuperficie.
        int BuscarTecho(int cabezaAntes, int cabezaNueva)
        {
            int mejor = SinSuperficie;
            foreach (Rectangle plataforma in Plataformas)
            {
                if (SolapaEnX(plataforma) && cabezaAntes >= plataforma.Bottom && cabezaNueva < plataforma.Bottom && plataforma.Bottom > mejor)
                {
                    mejor = plataforma.Bottom;
                }
            }
            return mejor;
        }

        // Caída libre: se coloca la parábola de modo que su cima (velocidad vertical 0) quede justo aquí.
        void EmpezarCaida()
        {
            double velInicialY = VelSalto * FuerzaSalto;
            sueloY = Y + velInicialY * velInicialY / (2 * Gravedad);
            tiempoVuelo = velInicialY / Gravedad;
            EnElSuelo = false;
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