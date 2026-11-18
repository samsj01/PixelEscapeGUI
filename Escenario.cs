using Engine2D;

namespace PixelEsGUI
{
    public partial class Escenario : Form
    {
        GraphicsScene scene;
        Jugador jugador;
        // Cada nivel puede tener varios cazadores, guardianes y trampas.
        readonly List<CazadorPixel> cazadores = new List<CazadorPixel>();
        readonly List<GuardianPixel> guardianes = new List<GuardianPixel>();
        readonly List<Trampa> trampas = new List<Trampa>();

        // Objetos del escenario.
        readonly List<Coleccionables> coleccionables = new List<Coleccionables>();
        readonly List<Bloque> bloques = new List<Bloque>(); // bloques de terreno del nivel actual
        Puerta puerta;
        PuntoControl puntoControl;

        // Altura del "suelo" donde se apoyan los objetos (la base de cada sprite queda en esta Y).
        const int Suelo = 190;

        // ---------- Cámara ----------
        // Ancho TOTAL de cada nivel en píxeles (posición 0 = nivel 1). La ventana solo muestra
        // scene.Width (920 px) de ese ancho; la cámara (scene.CamaraX) decide qué parte se ve.
        readonly int[] anchoNiveles = { 1840, 2760, 3680 };
        int AnchoNivel => anchoNiveles[nivelActual - 1];

        // Teclas que están presionadas en este momento.
        bool izquierda, derecha, salto;
        readonly System.Windows.Forms.Timer bucleJuego = new System.Windows.Forms.Timer { Interval = 16 };

        public Escenario()
        {

            InitializeComponent();
            ClientSize = new Size(920, 490); // tamaño de la ventana = lo que ve la cámara
            FormBorderStyle = FormBorderStyle.FixedSingle; // scene.Width debe coincidir con la ventana,
            MaximizeBox = false;                           // por eso no se deja redimensionar
            StartPosition = FormStartPosition.CenterScreen;
            scene = new GraphicsScene
            {
                Origin = CoordinateOrigin.TopLeft,
                Width = ClientSize.Width, // el fondo se estira a todo el ancho de la ventana
                BackgroundColor = Color.FromArgb(30, 30, 30)
            };
            scene.SetBackgroundImage("background");
            Visor.Scene = scene;
            Visor.Dock = DockStyle.Fill;



            jugador = new Jugador(100, 100);
            scene.AddItem(jugador);

            // Cargar el primer nivel
            CargarNivel(1);

            // Temporizador para cambiar de nivel
            timerTransicion.Tick += (s, e) =>
            {
                timerTransicion.Stop();
                transicionEnCurso = false;

                if (nivelActual == 3)
                {
                    juegoTerminado = true;
                    bucleJuego.Stop();

                }
                else
                {
                    CargarNivel(nivelActual + 1);


                }
            };

            //

            void ReiniciarNivel()
            {
                if (transicionEnCurso || juegoTerminado)
                    return;

                // Detener cualquier transición pendiente
                timerTransicion.Stop();

                // Limpiar teclas
                izquierda = false;
                derecha = false;
                salto = false;

                // Reiniciar el nivel actual
                CargarNivel(nivelActual);

                // Asegurar que el jugador siga visible
                jugador.Visible = true;

                // Actualizar título
                this.Text = "Pixel Escape - Nivel " + nivelActual;
            }




            // Teclado: el formulario recibe las teclas antes que el visor.
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.R)
                {
                    ReiniciarNivel();
                }
                else
                {
                    CambiarTecla(e.KeyCode, true);
                }
            };
            KeyUp += (s, e) => CambiarTecla(e.KeyCode, false);

            // Un paso de lógica por fotograma: mover -> saltar -> aterrizar (ver Jugador.Saltar).
            bucleJuego.Tick += (s, e) => PasoDeJuego();

            this.Shown += (s, e) =>
            {
                Visor.Start();
                bucleJuego.Start();
            };
        }

        // ---------- Bloques de terreno (tileset.png) ----------
        // Crea UN bloque: (fila, columna) es el cuadro de 16x16 dentro de la hoja "TileSet".
        void AgregarBloque(int x, int y, int fila, int columna)
        {
            Bloque bloque = new Bloque(x, y, fila, columna);
            bloque.SetPos(x, y);
            bloque.ZValue = -2; // detrás de puerta/trampa (-1) y del personaje
            bloques.Add(bloque);
            scene.AddItem(bloque);
        }

        // Suelo completo: una fila de césped en Y = Suelo y tierra hacia abajo hasta el borde de la ventana.
        void CrearSuelo()
        {
            int[] columnasCesped = { 1, 3, 5 };                                   // 3 variantes de césped (fila 1)
            int[,] tierra = { { 3, 1 }, { 3, 5 }, { 5, 1 }, { 5, 3 }, { 5, 5 } }; // 5 variantes de tierra

            int columnas = AnchoNivel / Bloque.Tamano + 1;
            int filas = (ClientSize.Height - Suelo) / Bloque.Tamano + 1;

            for (int c = 0; c < columnas; c++)
            {
                for (int f = 0; f < filas; f++)
                {
                    int x = c * Bloque.Tamano;
                    int y = Suelo + f * Bloque.Tamano;

                    if (f == 0)
                    {
                        AgregarBloque(x, y, 1, columnasCesped[c % 3]);
                    }
                    else
                    {
                        int t = (c * 7 + f * 3) % 5; // reparto "aleatorio" pero fijo
                        AgregarBloque(x, y, tierra[t, 0], tierra[t, 1]);
                    }
                }
            }
        }

        // Plataforma flotante de 'cantidad' bloques de piedra, con la esquina superior izquierda en (x, y).
        void CrearPlataforma(int x, int y, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                AgregarBloque(x + i * Bloque.Tamano, y, 1, 7);
            }

            // Se registra como superficie sólida: el jugador se para encima y no la atraviesa.
            jugador.Plataformas.Add(new Rectangle(x, y, cantidad * Bloque.Tamano, Bloque.Tamano));
        }

        // En el suelo: la base del objeto queda sobre el suelo.
        void AgregarColeccionable(Coleccionables item, int x)
        {
            AgregarColeccionable(item, x, Suelo - (int)item.Alto);
        }

        // En una posición exacta (para monedas en el aire, sobre plataformas, la llave, etc.).
        void AgregarColeccionable(Coleccionables item, int x, int y)
        {
            item.SetPos(x, y);
            coleccionables.Add(item);
            scene.AddItem(item);
        }

        // ---------- Ayudantes para distribuir objetos por el mapa ----------
        // Y de la superficie de las plataformas bajas: el salto del jugador sube ~98 px, así que
        // 86 px sobre el suelo se alcanza siempre. Su borde de abajo (136) deja pasar caminando por debajo.
        const int YPlataforma = Suelo - 86;
        const int YEnemigo = 120;           // los enemigos quedan con los pies sobre el suelo
        const int YMonedaSuelo = Suelo - 40; // la moneda mide 40 px
        const int YMonedaPlataforma = YPlataforma - 40;

        // Moneda suelta en (x, y).
        void AgregarMonedaEn(int x, int y)
        {
            AgregarColeccionable(new Moneda(x, y), x, y);
        }

        // Fila de monedas sobre el suelo.
        void AgregarMonedasLinea(int xInicial, int cantidad, int separacion = 45)
        {
            for (int i = 0; i < cantidad; i++)
            {
                AgregarMonedaEn(xInicial + i * separacion, YMonedaSuelo);
            }
        }

        // Fila de monedas encima de una plataforma baja.
        void AgregarMonedasPlataforma(int xInicial, int cantidad, int separacion = 40)
        {
            for (int i = 0; i < cantidad; i++)
            {
                AgregarMonedaEn(xInicial + i * separacion, YMonedaPlataforma);
            }
        }

        // 5 monedas en arco sobre una trampa: el jugador las recoge al saltarla.
        void AgregarMonedasArcoSobreTrampa(int xTrampa)
        {
            int[] desfase = { -100, -50, 0, 50, 100 };
            int[] alturas = { 130, 105, 90, 105, 130 };
            for (int i = 0; i < 5; i++)
            {
                AgregarMonedaEn(xTrampa + 12 + desfase[i], alturas[i]);
            }
        }

        void AgregarTrampa(int x)
        {
            Trampa t = new Trampa(x, 0, 1);
            t.SetPos(x, Suelo - (int)t.Alto);
            t.ZValue = -1;
            trampas.Add(t);
            scene.AddItem(t);
        }

        // Guardián en el suelo con una plataforma encima: hay que subirse a ella para pasar.
        // (Cuerpo del guardián = x+24..x+72; la plataforma lo cubre con 4 bloques.)
        void AgregarGuardian(int x)
        {
            GuardianPixel g = new GuardianPixel(x, YEnemigo);
            guardianes.Add(g);
            scene.AddItem(g);
            CrearPlataforma(x - 16, YPlataforma, 4);
        }

        // Cazador en el suelo con una plataforma encima.
        // (Cuerpo del cazador = x+27..x+69; la plataforma lo cubre con 3 bloques.)
        void AgregarCazador(int x)
        {
            CazadorPixel c = new CazadorPixel(x, YEnemigo);
            cazadores.Add(c);
            scene.AddItem(c);
            CrearPlataforma(x, YPlataforma, 3);
        }

        // Guardián SIN plataforma: hay que saltarlo (cuerpo = x+24..x+72).
        void AgregarGuardianLibre(int x)
        {
            GuardianPixel g = new GuardianPixel(x, YEnemigo);
            guardianes.Add(g);
            scene.AddItem(g);
        }

        // Cazador SIN plataforma: hay que saltarlo (cuerpo = x+27..x+69).
        void AgregarCazadorLibre(int x)
        {
            CazadorPixel c = new CazadorPixel(x, YEnemigo);
            cazadores.Add(c);
            scene.AddItem(c);
        }

        // Llave apoyada sobre una plataforma baja que empieza en xPlataforma y mide 'bloques' bloques.
        void AgregarLlaveSobrePlataforma(int xPlataforma, int bloques)
        {
            CrearPlataforma(xPlataforma, YPlataforma, bloques);
            Llave llave = new Llave(0, 0);
            int x = xPlataforma + (bloques * Bloque.Tamano - (int)llave.Ancho) / 2; // centrada
            AgregarColeccionable(llave, x, YPlataforma - (int)llave.Alto);
        }

        // Posición X del punto de control de cada nivel (a mitad del recorrido).
        readonly int[] xPuntoControl = { 1060, 1400, 1800 };

        void CambiarTecla(Keys tecla, bool presionada)
        {
            switch (tecla)
            {
                case Keys.Left:
                case Keys.A:
                    izquierda = presionada;
                    break;
                case Keys.Right:
                case Keys.D:
                    derecha = presionada;
                    break;
                case Keys.Space:
                case Keys.Up:
                case Keys.W:
                    salto = presionada;
                    break;
            }
        }

        void PasoDeJuego()
        {
            // No continuar mientras se cambia de nivel
            if (transicionEnCurso || juegoTerminado)
                return;

            // Movimiento del jugador
            if (derecha)
            {
                jugador.MoverDer();
            }
            else if (izquierda)
            {
                jugador.MoverIzq();
            }

            // Salto
            if (salto)
            {
                jugador.Saltar();
            }

            jugador.Aterrizar();

            // La cámara sigue al jugador y este no se sale de los bordes del nivel
            LimitarJugadorAlNivel();
            ActualizarCamara();

            // Recoger monedas y otros coleccionables
            foreach (Coleccionables item in coleccionables)
            {
                if (!item.Recogido && jugador.ColisionaCon(item))
                {
                    item.Recoger(jugador);

                    if (item is Moneda)
                    {
                        monedasRecogidas++;
                    }
                }
            }

            // Comprobar si evita los enemigos
            if (nivelActual >= 2)
            {
                Rectangle cuerpoJugador = CuerpoJugador();

                foreach (CazadorPixel c in cazadores)
                {
                    if (cuerpoJugador.IntersectsWith(CuerpoCazador(c)))
                    {
                        if (!jugador.AbsorbeImpacto())
                        {
                            evitoCazador = false; // sin escudo: tocarlo hace perder el nivel
                        }
                    }
                }

                foreach (GuardianPixel g in guardianes)
                {
                    if (cuerpoJugador.IntersectsWith(CuerpoGuardian(g)))
                    {
                        if (!jugador.AbsorbeImpacto())
                        {
                            evitoGuardian = false; // sin escudo: tocarlo hace perder el nivel
                        }
                    }
                }
            }

            // Tocar a un enemigo = perder el nivel
            if (nivelActual >= 2 && (!evitoCazador || !evitoGuardian))
            {
                PerderNivel();
                return;
            }

            // Activar punto de control: la primera vez guarda la posición y el progreso
            if (!puntoControl.Activo && jugador.ColisionaCon(puntoControl))
            {
                puntoControl.Activar(jugador);
                GuardarPuntoControl();
            }

            // Comprobar si puede abrir la puerta
            ValidarSalida();

            // Activar trampas
            foreach (Trampa t in trampas)
            {
                t.Activar(jugador);
            }

            // Sin vidas = perder el nivel
            if (!jugador.EstaVivo)
            {
                PerderNivel();
            }
        }


        // Evita que el jugador salga del nivel por la izquierda o por la derecha.
        void LimitarJugadorAlNivel()
        {
            Rectangle cuerpo = jugador.Cuerpo;

            if (cuerpo.Left < 0)
            {
                jugador.SetPos(jugador.X - cuerpo.Left, jugador.Y);
            }
            else if (cuerpo.Right > AnchoNivel)
            {
                jugador.SetPos(jugador.X - (cuerpo.Right - AnchoNivel), jugador.Y);
            }
        }

        // Centra la cámara en el jugador, sin mostrar nada fuera del nivel:
        //   CamaraX = 0                      -> se ve el inicio del nivel
        //   CamaraX = AnchoNivel - scene.Width -> se ve el final del nivel
        // El motor dibuja todo desplazado -CamaraX, así que las posiciones de los objetos
        // siguen siendo las del MUNDO (no hay que convertirlas a coordenadas de pantalla).
        void ActualizarCamara()
        {
            Rectangle cuerpo = jugador.Cuerpo;
            float centroJugador = cuerpo.X + cuerpo.Width / 2f;

            float maxCamara = Math.Max(0f, AnchoNivel - scene.Width);
            float camara = Math.Clamp(centroJugador - scene.Width / 2f, 0f, maxCamara);

            scene.CamaraX = (float)Math.Round(camara); // entero: evita líneas finas entre bloques
        }

        // Rectángulos del cuerpo REAL de cada personaje. Los cuadros de las hojas de sprites
        // (96x128 el jugador, 96x96 los enemigos) tienen mucho espacio transparente; colisionar
        // con el cuadro completo hace que "tocar" al enemigo ocurra a ~50 px de distancia.
        Rectangle CuerpoJugador() => jugador.Cuerpo;
        Rectangle CuerpoGuardian(GuardianPixel g) => new Rectangle(g.X + 24, g.Y + 23, 48, 46);
        Rectangle CuerpoCazador(CazadorPixel c) => new Rectangle(c.X + 27, c.Y + 23, 42, 45);

        bool ColisionPuerta()
        {
            Rectangle jugadorRect = new Rectangle(
                jugador.X,
                jugador.Y,
                (int)jugador.Ancho,
                (int)jugador.Alto
            );

            Rectangle puertaRect = new Rectangle(
                puerta.X,
                puerta.Y,
                (int)puerta.Ancho,
                (int)puerta.Alto
            );

            return jugadorRect.IntersectsWith(puertaRect);
        }

        // ---------- Punto de control ----------
        const int VidaInicial = 3;
        bool puntoControlGuardado;                                   // true si ya se activó en este nivel
        int xRespawn;                                                // X donde reaparece el jugador
        readonly List<int> indicesRecogidos = new List<int>();       // coleccionables que ya tenía al activarlo

        // Control de niveles
        int nivelActual = 1;

        int monedasRecogidas = 0;

        bool evitoCazador = true;
        bool evitoGuardian = true;

        bool transicionEnCurso = false;
        bool juegoTerminado = false;

        // Temporizador para esperar la animación de la puerta
        readonly System.Windows.Forms.Timer timerTransicion =
            new System.Windows.Forms.Timer { Interval = 1100 };


        //Método para cargar un nuevo nivel: quita los objetos del nivel anterior y crea los del nuevo.
        // desdePuntoControl = true: el nivel se reinicia pero el jugador reaparece en el punto de control.
        void CargarNivel(int nuevoNivel, bool desdePuntoControl = false)
        {
            nivelActual = nuevoNivel;

            // Nivel nuevo o reinicio con R: se olvida el punto de control anterior
            if (!desdePuntoControl)
            {
                puntoControlGuardado = false;
                indicesRecogidos.Clear();
            }

            monedasRecogidas = 0;

            evitoCazador = true;
            evitoGuardian = true;

            transicionEnCurso = false;

            // Quitar los objetos del nivel anterior (si solo se ocultan, la escena acumula objetos en cada reinicio)
            foreach (Coleccionables item in coleccionables)
            {
                item.Visible = false;
                scene.RemoveItem(item);
            }
            coleccionables.Clear();

            foreach (Bloque bloque in bloques)
            {
                bloque.Visible = false;
                scene.RemoveItem(bloque);
            }
            bloques.Clear();
            jugador.Plataformas.Clear();

            foreach (GuardianPixel g in guardianes)
            {
                g.Visible = false;
                scene.RemoveItem(g);
            }
            guardianes.Clear();

            foreach (CazadorPixel c in cazadores)
            {
                c.Visible = false;
                scene.RemoveItem(c);
            }
            cazadores.Clear();

            foreach (Trampa t in trampas)
            {
                t.Visible = false;
                scene.RemoveItem(t);
            }
            trampas.Clear();

            if (puerta != null)
            {
                puerta.Visible = false;
                scene.RemoveItem(puerta);
            }

            if (puntoControl != null)
            {
                puntoControl.Visible = false;
                scene.RemoveItem(puntoControl);
            }

            // Terreno (el suelo es igual en todos los niveles) y todo lo que va encima
            CrearSuelo();
            CrearDisposicionNivel();

            // Reiniciar al jugador: en el punto de control si venimos de una derrota, si no en el inicio
            int xInicio = desdePuntoControl ? xRespawn : 100;
            jugador.ReiniciarEstado(xInicio, 100);
            jugador.Visible = true;
            jugador.GanarVida(VidaInicial - jugador.Vida); // vida completa
            if (desdePuntoControl)
            {
                RestaurarProgresoGuardado();
            }

            // La cámara vuelve al inicio del nivel
            ActualizarCamara();

            // Puerta cerca del final del nivel
            int xPuerta = AnchoNivel - 140;
            puerta = new Puerta(xPuerta, 0);
            puerta.SetPos(xPuerta, Suelo - (int)puerta.Alto);
            puerta.ZValue = -1;
            scene.AddItem(puerta);

            // Punto de control a mitad del nivel
            int xControl = xPuntoControl[nivelActual - 1];
            puntoControl = new PuntoControl(xControl, 0);
            puntoControl.SetPos(xControl, Suelo - (int)puntoControl.Alto);
            puntoControl.ZValue = -1;
            scene.AddItem(puntoControl);

            if (desdePuntoControl)
            {
                puntoControl.Activar(jugador); // la bandera ya aparece levantada
            }
        }

        // Guarda dónde reaparecer (en la bandera) y qué monedas/llave llevaba el jugador.
        void GuardarPuntoControl()
        {
            puntoControlGuardado = true;
            xRespawn = puntoControl.X - 16; // el cuerpo del jugador queda sobre la bandera

            indicesRecogidos.Clear();
            for (int i = 0; i < coleccionables.Count; i++)
            {
                if (coleccionables[i].Recogido)
                {
                    indicesRecogidos.Add(i);
                }
            }
        }

        // Tras recargar el nivel, vuelve a marcar como recogido lo que ya tenía al activar el punto de control.
        // (Los objetos se crean siempre en el mismo orden, por eso el índice identifica a cada uno.)
        void RestaurarProgresoGuardado()
        {
            foreach (int i in indicesRecogidos)
            {
                if (i < coleccionables.Count)
                {
                    coleccionables[i].Recoger(jugador);
                    if (coleccionables[i] is Moneda)
                    {
                        monedasRecogidas++;
                    }
                }
            }
        }

        // Derrota (tocar un enemigo o quedarse sin vidas): el nivel se reinicia por completo
        // (enemigos, trampas, monedas y llave posteriores al punto de control, vida y objetivos).
        // Si ya se activó el punto de control, el jugador reaparece ahí; si no, al inicio.
        void PerderNivel()
        {
            CargarNivel(nivelActual, puntoControlGuardado);
        }

        // =====================================================================================
        //  DISTRIBUCIÓN DE OBJETOS POR NIVEL  (todas las X son coordenadas del MUNDO, en píxeles)
        //  El jugador sale en x = 100 y la puerta está en AnchoNivel - 140.
        //  Regla de espaciado: entre dos peligros (trampa / plataforma con enemigo) hay >= 100 px
        //  (niveles 1 y 2); el nivel 3 lo reduce a ~70-90 px a propósito para que sea más difícil.
        // =====================================================================================
        void CrearDisposicionNivel()
        {
            switch (nivelActual)
            {
                case 1:
                    CrearNivel1();
                    break;
                case 2:
                    CrearNivel2();
                    break;
                default:
                    CrearNivel3();
                    break;
            }
        }

        // NIVEL 1 (1840 px) - objetivo: 20 monedas. Sin enemigos ni llave: es el nivel de aprendizaje.
        void CrearNivel1()
        {
            AgregarMonedasLinea(230, 4);              // 4 monedas: calentamiento (x 230-405)

            AgregarTrampa(540);                       // trampa 1 (x 540-604)
            AgregarMonedasArcoSobreTrampa(540);       // 5 monedas: se recogen saltando la trampa

            CrearPlataforma(800, YPlataforma, 6);     // plataforma (x 800-992)
            AgregarMonedasPlataforma(830, 3, 50);     // 3 monedas encima

            // Punto de control en x = 1060

            AgregarMonedasLinea(1160, 3);             // 3 monedas

            AgregarTrampa(1400);                      // trampa 2 (x 1400-1464)
            AgregarMonedasArcoSobreTrampa(1400);      // 5 monedas -> total 20
        }

        // NIVEL 2 (2760 px) - objetivo: llave + evitar al cazador y al guardián (sin monedas).
        void CrearNivel2()
        {
            AgregarTrampa(380);                       // trampa 1: bienvenida

            AgregarGuardian(620);                     // guardián 1 bajo plataforma (x 604-732)

            AgregarCazador(1000);                     // cazador 1 bajo plataforma (x 1000-1096)

            AgregarTrampa(1240);                      // trampa 2

            // Punto de control en x = 1400
            AgregarColeccionable(new PowerUpEscudo(1480, 0, 10), 1480); // escudo tras el punto de control

            AgregarLlaveSobrePlataforma(1650, 3);     // LLAVE sobre plataforma (x 1650-1746): hay que subirse

            AgregarGuardian(1900);                    // guardián 2 (x 1884-2012)

            AgregarCazador(2200);                     // cazador 2 (x 2200-2296)

            AgregarTrampa(2440);                      // trampa 3: último obstáculo antes de la puerta
        }

        // NIVEL 3 (3680 px) - objetivo: 20 monedas + llave + evitar enemigos. VERSIÓN DIFÍCIL:
        //   8 enemigos (4 guardianes + 4 cazadores, la mitad SIN plataforma: hay que saltarlos),
        //   6 trampas (dos pares seguidos), poco espacio para aterrizar entre peligros (~70-90 px),
        //   la llave está detrás de una trampa y un cazador, y casi no hay power-ups.
        void CrearNivel3()
        {
            // --- Tramo 1 (0-920): ya no es un paseo ---
            AgregarMonedasLinea(240, 3);              // 3 monedas (x 240-370)
            AgregarTrampa(480);                       // trampa 1 (x 480-544)
            AgregarMonedasArcoSobreTrampa(480);       // 5 monedas en arco
            AgregarTrampa(640);                       // trampa 2: par de trampas, hay que encadenar saltos
            AgregarGuardianLibre(820);                // guardián 1 sin plataforma (cuerpo x 844-892)

            // --- Tramo 2 (920-1840): enemigos seguidos ---
            AgregarGuardian(1040);                    // guardián 2 bajo plataforma (x 1024-1152)
            AgregarMonedasPlataforma(1032, 3);        // 3 monedas sobre su plataforma
            AgregarCazadorLibre(1300);                // cazador 1 sin plataforma (cuerpo x 1327-1369)
            AgregarTrampa(1460);                      // trampa 3 pegada al cazador (aterrizaje justo)
            AgregarCazador(1640);                     // cazador 2 bajo plataforma (x 1640-1736)
            AgregarMonedasPlataforma(1652, 2);        // 2 monedas sobre su plataforma

            // Punto de control en x = 1800
            AgregarColeccionable(new PowerUpEscudo(1900, 0, 10), 1900); // único power-up del nivel

            // --- Tramo 3 (1840-2760): la llave está bien protegida ---
            AgregarMonedasLinea(1960, 3);             // 3 monedas
            AgregarTrampa(2160);                      // trampa 4
            AgregarCazadorLibre(2330);                // cazador 3 sin plataforma (cuerpo x 2357-2399)
            AgregarLlaveSobrePlataforma(2470, 4);     // LLAVE (plataforma x 2470-2598): saltar el cazador y subir
            AgregarMonedaEn(2482, YMonedaPlataforma); // 2 monedas junto a la llave
            AgregarMonedaEn(2552, YMonedaPlataforma);

            // --- Tramo 4 (2760-3680): zona de peligro hasta la puerta ---
            AgregarGuardian(2700);                    // guardián 3 bajo plataforma (x 2684-2812)
            AgregarCazador(2900);                     // cazador 4 bajo plataforma (x 2900-2996)
            AgregarMonedasPlataforma(2912, 2);        // 2 monedas -> total 20
            AgregarTrampa(3060);                      // trampa 5
            AgregarTrampa(3210);                      // trampa 6: segundo par de trampas
            AgregarGuardianLibre(3330);               // guardián 4 sin plataforma, última barrera (cuerpo x 3354-3402)
        }

        //Swicth case que valida si el jugador cumple los objetivos del nivel actual, y devuelve true si los cumple, false si no los cumple
        bool CumpleObjetivosNivel()
        {
            switch (nivelActual)
            {
                case 1:
                    return monedasRecogidas >= 20;

                case 2:
                    return jugador.TieneLlave
                        && evitoCazador
                        && evitoGuardian;

                case 3:
                    return monedasRecogidas >= 20
                        && jugador.TieneLlave
                        && evitoCazador
                        && evitoGuardian;

                default:
                    return false;
            }
        }


        //Método que revisa los objetivos del nivel actual y devuelve un mensaje con los objetivos que faltan por cumplir, o un mensaje de éxito si todos los objetivos están completos
        string RevisarObjetivos()
        {
            string mensaje = "";

            if (nivelActual == 1 || nivelActual == 3)
            {
                if (monedasRecogidas < 20)
                {
                    mensaje += "Te faltan "
                        + (20 - monedasRecogidas)
                        + " monedas.\n";
                }
            }

            if (nivelActual == 2 || nivelActual == 3)
            {
                if (!jugador.TieneLlave)
                {
                    mensaje += "Debes recoger la llave.\n";
                }

                if (!evitoCazador)
                {
                    mensaje += "No cumpliste el objetivo de evitar al cazador.\n";
                }

                if (!evitoGuardian)
                {
                    mensaje += "No cumpliste el objetivo de evitar al guardian.\n";
                }
            }

            if (mensaje == "")
            {
                mensaje = "¡Todos los objetivos están completos!";
            }

            return mensaje;
        }

        bool avisoObjetivosMostrado = false;
        // Método que valida si el jugador colisiona con la puerta y si cumple los objetivos del nivel para abrirla
        void ValidarSalida()
        {
            if (transicionEnCurso || juegoTerminado)
                return;

            if (ColisionPuerta())
            {
                if (CumpleObjetivosNivel())
                {
                    puerta.Abrir();

                    transicionEnCurso = true;

                    timerTransicion.Start();
                }
                else
                {
                    string mensaje = RevisarObjetivos();

                    // Mostrar solamente una vez mientras
                    // el jugador permanece junto a la puerta.
                    if (!avisoObjetivosMostrado)
                    {
                        // Marcar ANTES de mostrar: MessageBox deja correr el Timer del juego y,
                        // si el aviso se marca después, cada fotograma abre otra ventana.
                        avisoObjetivosMostrado = true;
                        bucleJuego.Stop(); // pausar el juego mientras se lee el aviso

                        MessageBox.Show(
                            mensaje,
                            "Objetivos pendientes"
                        );

                        // Mientras estaba la ventana no llegaron los KeyUp: soltar las teclas.
                        izquierda = false;
                        derecha = false;
                        salto = false;

                        bucleJuego.Start();
                    }
                }
            }
            else
            {
                avisoObjetivosMostrado = false;
            }
        }

    }
}