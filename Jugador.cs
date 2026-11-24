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
        const int FuerzaSaltoBase = 4;
        const double AlturaSalto = 98.0;
        const double TiempoSubida = 0.38;
        const double TiempoCaida = 0.27;
        const double GravedadSubida = 2 * AlturaSalto / (TiempoSubida * TiempoSubida);
        const double GravedadCaida = 2 * AlturaSalto / (TiempoCaida * TiempoCaida);
        const double ImpulsoSalto = GravedadSubida * TiempoSubida;

        // ---------- Hoja de sprites (Resources/jugador_sheet.png) ----------
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
        int movimientoHorizontal;  // -1 izquierda, 0 quieto, 1 derecha (solo en el fotograma actual)
        double sueloY;             // altura (Y) de la que parte la parábola actual
        int sueloBaseY;            // Y del jugador cuando está parado en el suelo principal
        double posicionVertical; // conserva fracciones de píxel entre actualizaciones
        double posicionHorizontalAire;
        const double VelocidadHorizontalSuelo = 225.0;
        const int SolapeMinimoApoyo = 8;
        // Velocidad aérea fija: no se duplica con el power-up de velocidad.
        // 225 px/s * 0.65 s = ~146 px de alcance en terreno plano.
        const double VelocidadHorizontalAire = 225.0;

        // ---------- Propiedades ----------
        public int Puntaje { get; private set; }
        public bool TieneLlave { get; private set; }
        public bool TieneEscudo { get; private set; }
        public bool TieneVelocidad => fotogramasVelocidad > 0;
        const int VelocidadNormal = 5;
        int velocidadAnterior = VelocidadNormal;
        int fotogramasVelocidad;

        // Tras romperse el escudo el jugador es invulnerable un instante (60 fotogramas ~ 1 s),
        // para que un enemigo que lo sigue tocando no lo mate en el fotograma siguiente.
        const int FotogramasInvulnerable = 60;
        int fotogramasInvulnerable;
        int fotogramasReaccionDano;
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
            posicionVertical = y;
            posicionHorizontalAire = x;
            ActualizarAnimacion(false);
        }

        // ---------- Acciones que disparan los coleccionables ----------
        public void EstablecerPuntaje(int puntos)
        {
            Puntaje = puntos;
        }

        /// <summary>Restituye vidas e inventario desde un guardado verificado.</summary>
        public void RestaurarDatos(int vidas, int puntos, bool llave, bool escudo, bool velocidad)
        {
            Vida = Math.Max(0, vidas);
            Puntaje = Math.Max(0, puntos);
            TieneLlave = llave;
            TieneEscudo = escudo;
            fotogramasVelocidad = 0;
            Velocidad = VelocidadNormal;
            if (velocidad) ActivarVelocidadTemporal(5);
        }

        public void ReiniciarPartida()
        {
            Vida = 3;
            Puntaje = 0;
            TieneLlave = false;
            TieneEscudo = false;
            fotogramasInvulnerable = 0;
            fotogramasVelocidad = 0;
            Velocidad = VelocidadNormal;
        }

        public void ActivarVelocidadTemporal(int duracionSegundos)
        {
            if (fotogramasVelocidad <= 0)
                velocidadAnterior = Velocidad;
            Velocidad = 8;
            fotogramasVelocidad = Math.Max(1, duracionSegundos) * 60;
        }

        void ActualizarVelocidadTemporal()
        {
            if (fotogramasVelocidad > 0)
            {
                fotogramasVelocidad--;
                if (fotogramasVelocidad == 0)
                    Velocidad = velocidadAnterior;
            }
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

            int vidasAntes = Vida;
            base.RecibirDano(dano);
            if (Vida < vidasAntes)
            {
                // Reacción visual: pequeño retroceso y parpadeo durante la invulnerabilidad.
                fotogramasReaccionDano = 8;
                fotogramasInvulnerable = FotogramasInvulnerable;
                Visible = true;
            }
        }

        // Una vez por fotograma: cuenta la invulnerabilidad y hace parpadear al jugador.
        void ActualizarInvulnerabilidad()
        {
            if (fotogramasReaccionDano > 0)
            {
                SetPos(X - Direccion * 2, Y); // retrocede brevemente en sentido contrario al movimiento
                fotogramasReaccionDano--;
            }

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
            fotogramasReaccionDano = 0;
            fotogramasVelocidad = 0;
            Velocidad = VelocidadNormal;

            // Reiniciar salto
            EnElSuelo = true;
            SaltoParabolico = false;
            VelY = 0;
            posicionVertical = y;
            posicionHorizontalAire = x;
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

        // Caminar y saltar usan la misma velocidad base y el mismo deltaTime.
        // El power-up duplica solo la velocidad al caminar.
        public void MoverHorizontal(int direccion, double deltaTime)
        {
            if (direccion == 0) return;

            movimientoHorizontal = 3;
            Direccion = direccion < 0 ? -1 : 1;
            double velocidad = EnElSuelo
                ? VelocidadHorizontalSuelo * (TieneVelocidad ? 2.0 : 1.0)
                : VelocidadHorizontalAire;
            double anterior = posicionHorizontalAire;
            int xAnterior = X;
            posicionHorizontalAire += Direccion * velocidad * Math.Max(0, Math.Min(deltaTime, 0.05));
            SetPos((int)Math.Round(posicionHorizontalAire), Y);

            // Solo bloquear al entrar lateralmente desde fuera del obstáculo.
            // No devolver al personaje por una intersección que ya existía al caer.
            Rectangle cuerpoAnterior = new Rectangle(xAnterior + CuerpoX, Y + CuerpoY, CuerpoAncho, CuerpoAlto);
            Rectangle cuerpoNuevo = Cuerpo;
            foreach (Rectangle plataforma in Plataformas)
            {
                if (!cuerpoNuevo.IntersectsWith(plataforma)) continue;
                bool entraDesdeIzquierda = Direccion > 0 && cuerpoAnterior.Right <= plataforma.Left;
                bool entraDesdeDerecha = Direccion < 0 && cuerpoAnterior.Left >= plataforma.Right;
                if (entraDesdeIzquierda || entraDesdeDerecha)
                {
                    posicionHorizontalAire = anterior;
                    SetPos(xAnterior, Y);
                    break;
                }
            }
        }

        // Se llama cuando otra parte del escenario corrige X (por ejemplo, límites del nivel).
        public void SincronizarPosicionHorizontal() => posicionHorizontalAire = X;

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

            VelY = ImpulsoSalto;
            posicionHorizontalAire = X;
            posicionVertical = Y;
            EnElSuelo = false;
        }

        // Avanza la física vertical usando segundos reales; la velocidad positiva indica subida.
        // Se parte el paso cuando alcanza la cima para mantener las dos gravedades exactas.
        public void Aterrizar(double deltaTime)
        {
            ActualizarInvulnerabilidad();
            ActualizarVelocidadTemporal();

            if (EnElSuelo && !HaySueloBajoLosPies())
                EmpezarCaida();

            if (!EnElSuelo)
            {
                // Limitar retrasos del temporizador evita atravesar plataformas tras un bloqueo.
                double restante = Math.Max(0, Math.Min(deltaTime, 0.05));
                while (restante > 0.0000001 && !EnElSuelo)
                {
                    double gravedad = VelY > 0 ? GravedadSubida : GravedadCaida;
                    double paso = restante;
                    if (VelY > 0)
                        paso = Math.Min(paso, VelY / gravedad);

                    int piesAntes = PiesY;
                    int cabezaAntes = Y + CuerpoY;
                    double velocidadAntes = VelY;
                    double velocidadDespues = VelY - gravedad * paso;
                    posicionVertical -= (velocidadAntes + velocidadDespues) * 0.5 * paso;
                    VelY = velocidadDespues;
                    int nuevaY = (int)Math.Round(posicionVertical);

                    if (velocidadAntes > 0)
                    {
                        int techo = BuscarTecho(cabezaAntes, nuevaY + CuerpoY);
                        if (techo != SinSuperficie)
                        {
                            SetPos(X, techo - CuerpoY);
                            EmpezarCaida();
                        }
                        else
                            SetPos(X, nuevaY);
                    }
                    else
                    {
                        int superficie = BuscarSuperficie(piesAntes, nuevaY + PiesOffset);
                        if (superficie != SinSuperficie)
                        {
                            SetPos(X, superficie - PiesOffset);
                            posicionVertical = Y;
                            EnElSuelo = true;
                            posicionHorizontalAire = X;
                            SaltoParabolico = false;
                            VelY = 0;
                        }
                        else
                            SetPos(X, nuevaY);
                    }

                    restante -= paso;
                }
            }

            ActualizarAnimacion(movimientoHorizontal != 0);
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
            int solape = Math.Min(derecha, plataforma.Right) - Math.Max(izquierda, plataforma.Left);
            return solape >= SolapeMinimoApoyo;
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

        // Caída desde un borde o al chocar con un techo, sin impulso hacia arriba.
        void EmpezarCaida()
        {
            posicionVertical = Y;
            VelY = 0;
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