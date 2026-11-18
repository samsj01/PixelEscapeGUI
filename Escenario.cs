using Engine2D;

namespace PixelEsGUI
{
    public partial class Escenario : Form
    {
        GraphicsScene scene;
        Jugador jugador;
        CazadorPixel cazador;
        GuardianPixel guardian;

        // Objetos del escenario.
        readonly List<Coleccionables> coleccionables = new List<Coleccionables>();
        readonly List<Bloque> bloques = new List<Bloque>(); // bloques de terreno del nivel actual
        Puerta puerta;
        PuntoControl puntoControl;
        Trampa trampa;

        // Altura del "suelo" donde se apoyan los objetos (la base de cada sprite queda en esta Y).
        const int Suelo = 190;

        // Teclas que están presionadas en este momento.
        bool izquierda, derecha, salto;
        readonly System.Windows.Forms.Timer bucleJuego = new System.Windows.Forms.Timer { Interval = 16 };

        public Escenario()
        {

            InitializeComponent();
            ClientSize = new Size(920, 490); // más ancho para que quepan todos los objetos
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

            guardian = new GuardianPixel(280, 120);
            scene.AddItem(guardian);

            cazador = new CazadorPixel(550, 120);
            scene.AddItem(cazador);

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

        // Crea los objetos del nivel y los agrega a la escena. Cada clase ya sabe qué sprite usa;
        // aquí solo se decide dónde va (la Y se calcula para que la base quede sobre el suelo).
        void CrearObjetos()
        {
            // Decorado: ZValue negativo para que el personaje se dibuje por encima.
            puerta = new Puerta(780, 0);
            puerta.SetPos(780, Suelo - (int)puerta.Alto);
            puerta.ZValue = -1;
            scene.AddItem(puerta);

            puntoControl = new PuntoControl(650, 0);
            puntoControl.SetPos(650, Suelo - (int)puntoControl.Alto);
            puntoControl.ZValue = -1;
            scene.AddItem(puntoControl);

            // 480 = 100 + 5 * 76: con la velocidad actual del jugador, su X coincide con la de la trampa.
            trampa = new Trampa(480, 0, 1);
            trampa.SetPos(480, Suelo - (int)trampa.Alto);
            trampa.ZValue = -1;
            scene.AddItem(trampa);

            // Coleccionables.
            //AgregarColeccionable(new Moneda(200, 0), 200);
            //Ciclo que agrega 20 monedas en línea horizontal, separadas 30 px, empezando en X=100
            for (int i = 0; i < 20; i++)
            {
                int posicionX = 100 + (i * 30);

                AgregarColeccionable(
                    new Moneda(posicionX, 0),
                    posicionX
                );
            }
            AgregarColeccionable(new Llave(255, 0), 255);
            AgregarColeccionable(new PowerUpEscudo(400, 0, 10), 400);
            AgregarColeccionable(new PowerUpVelocidad(450, 0, 10), 450);
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

            int columnas = ClientSize.Width / Bloque.Tamano + 1;
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
        }

        void AgregarColeccionable(Coleccionables item, int x)
        {
            item.SetPos(x, Suelo - (int)item.Alto);
            coleccionables.Add(item);
            scene.AddItem(item);
        }

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

                if (cuerpoJugador.IntersectsWith(CuerpoCazador()))
                {
                    evitoCazador = false;
                }

                if (cuerpoJugador.IntersectsWith(CuerpoGuardian()))
                {
                    evitoGuardian = false;
                }
            }

            // Activar punto de control
            if (jugador.ColisionaCon(puntoControl))
            {
                puntoControl.Activar(jugador);
            }

            // Comprobar si puede abrir la puerta
            ValidarSalida();

            // Activar trampa
            trampa.Activar(jugador);
        }


        // Rectángulos del cuerpo REAL de cada personaje. Los cuadros de las hojas de sprites
        // (96x128 el jugador, 96x96 los enemigos) tienen mucho espacio transparente; colisionar
        // con el cuadro completo hace que "tocar" al enemigo ocurra a ~50 px de distancia.
        Rectangle CuerpoJugador() => new Rectangle(jugador.X + 32, jugador.Y + 36, 34, 52);
        Rectangle CuerpoGuardian() => new Rectangle(guardian.X + 24, guardian.Y + 23, 48, 46);
        Rectangle CuerpoCazador() => new Rectangle(cazador.X + 27, cazador.Y + 23, 42, 45);

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


        //Método para cargar un nuevo nivel, ocultando los objetos del nivel anterior y creando los nuevos objetos según el nivel.
        void CargarNivel(int nuevoNivel)
        {
            nivelActual = nuevoNivel;

            monedasRecogidas = 0;

            evitoCazador = true;
            evitoGuardian = true;

            transicionEnCurso = false;

            // avisoObjetivosMostrado = false;  mensaje

            // Ocultar los objetos del nivel anterior
            foreach (Coleccionables item in coleccionables)
            {
                item.Visible = false;
            }

            coleccionables.Clear();

            // Quitar los bloques del nivel anterior
            foreach (Bloque bloque in bloques)
            {
                bloque.Visible = false;
            }

            bloques.Clear();

            if (puerta != null)
                puerta.Visible = false;

            if (puntoControl != null)
                puntoControl.Visible = false;

            if (trampa != null)
                trampa.Visible = false;


            // Construir el terreno del nivel (el suelo es igual en todos; las plataformas cambian)
            CrearSuelo();

            if (nivelActual >= 2)
            {
                // Una plataforma para cada enemigo (guardián en x=300, cazador en x=520).
                CrearPlataforma(300, Suelo - 64, 3);
                CrearPlataforma(520, Suelo - 64, 3);
            }

            // Reiniciar al jugador
            jugador.ReiniciarEstado(100, 100);
            jugador.Visible = true;

            // Mostrar enemigos solamente en niveles 2 y 3
            bool enemigosActivos = nivelActual >= 2;

            cazador.Visible = enemigosActivos;
            guardian.Visible = enemigosActivos;

            // Los enemigos se paran sobre las plataformas (su borde superior está en Suelo - 64).
            // Los pies quedan a 68-69 px del borde superior del cuadro de 96x96 del sprite.
            guardian.SetPos(300, Suelo - 64 - 69);
            cazador.SetPos(520, Suelo - 64 - 68);

            // Crear la nueva puerta
            puerta = new Puerta(780, 0);
            puerta.SetPos(
                780,
                Suelo - (int)puerta.Alto
            );

            puerta.ZValue = -1;
            scene.AddItem(puerta);

            // Crear punto de control
            puntoControl = new PuntoControl(650, 0);
            puntoControl.SetPos(
                650,
                Suelo - (int)puntoControl.Alto
            );

            puntoControl.ZValue = -1;
            scene.AddItem(puntoControl);

            // Crear trampa
            trampa = new Trampa(480, 0, 1);
            trampa.SetPos(
                480,
                Suelo - (int)trampa.Alto
            );

            trampa.ZValue = -1;
            scene.AddItem(trampa);

            // Nivel 1 Y Nivel 3: crear 20 monedas
            if (nivelActual == 1 || nivelActual == 3)
            {
                for (int i = 0; i < 20; i++)
                {
                    int columna = i % 10;
                    int fila = i / 10;

                    int x = 100 + (columna * 65);
                    int y = Suelo - 40 - (fila * 65);

                    AgregarColeccionable(
                        new Moneda(x, y),
                        x
                    );
                }
            }

            // Nivel 2 Y Nivel 3: crear una llave
            if (nivelActual == 2 || nivelActual == 3)
            {
                AgregarColeccionable(
                    new Llave(350, 0),
                    350
                );
            }
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