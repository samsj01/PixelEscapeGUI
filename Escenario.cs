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
        readonly Dictionary<Enemigo, (int Min, int Max)> limitesPatrulla = new Dictionary<Enemigo, (int Min, int Max)>();

        // Objetos del escenario.
        readonly List<Coleccionables> coleccionables = new List<Coleccionables>();
        readonly List<Obstaculo> obstaculos = new List<Obstaculo>(); // tiles de terreno
        readonly List<Decoracion> decoraciones = new List<Decoracion>();
        readonly List<ObstaculoSprite> obstaculosSprite = new List<ObstaculoSprite>();
        Puerta puerta;
        PuntoControl puntoControl;

        // Altura del "suelo" donde se apoyan los objetos (la base de cada sprite queda en esta Y).
        const int Suelo = 300;

        // ---------- Cámara ----------
        // Ancho TOTAL de cada nivel en píxeles (posición 0 = nivel 1). La ventana solo muestra
        // scene.Width (920 px) de ese ancho; la cámara (scene.CamaraX) decide qué parte se ve.
        readonly int[] anchoNiveles = { 1840, 2760, 3680 };
        int AnchoNivel => anchoNiveles[nivelActual - 1];

        // Teclas que están presionadas en este momento.
        bool izquierda, derecha, salto;
        bool pausado = false;
        readonly System.Windows.Forms.Timer bucleJuego = new System.Windows.Forms.Timer { Interval = 16 };
        readonly System.Windows.Forms.Timer timerRespawn = new System.Windows.Forms.Timer { Interval = 520 };
        bool respawnPendiente = false;

        // Interfaz superpuesta: vidas y paneles de pausa/fin de partida.
        readonly Label lblVidas = new Label();
        readonly Label lblPowerUps = new Label();
        readonly Label lblMonedas = new Label(); // contador de monedas, debajo de las vidas
        string textoMonedas = "";
        readonly Panel panelEstado = new Panel();
        readonly Label lblTituloEstado = new Label();
        readonly Label lblMensajeEstado = new Label();
        readonly Button btnEstado = new Button();
        bool escudoAnterior;
        bool velocidadAnterior;
        string avisoTemporalPowerUp = "";
        readonly System.Windows.Forms.Timer timerAvisoPowerUp = new System.Windows.Forms.Timer { Interval = 1800 };

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

            ConfigurarInterfaz();

            jugador = new Jugador(100, Suelo - 88);
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
                    MostrarVictoria();
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
                if (e.KeyCode == Keys.P)
                {
                    AlternarPausa();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.R && !juegoTerminado && !respawnPendiente)
                {
                    ReiniciarNivel();
                }
                else if (!pausado && !juegoTerminado)
                {
                    CambiarTecla(e.KeyCode, true);
                }
            };
            KeyUp += (s, e) => CambiarTecla(e.KeyCode, false);

            // Un paso de lógica por fotograma: mover -> saltar -> aterrizar (ver Jugador.Saltar).
            bucleJuego.Tick += (s, e) =>
            {
                PasoDeJuego();
                ActualizarAvisosPowerUp();
            };
            timerAvisoPowerUp.Tick += (s, e) =>
            {
                timerAvisoPowerUp.Stop();
                avisoTemporalPowerUp = "";
                ActualizarAvisosPowerUp();
            };
            timerRespawn.Tick += (s, e) =>
            {
                timerRespawn.Stop();
                respawnPendiente = false;
                if (!juegoTerminado)
                {
                    CargarNivel(nivelActual, puntoControlGuardado);
                    ActualizarIndicadorVidas();
                }
            };

            this.Shown += (s, e) =>
            {
                Visor.Start();
                bucleJuego.Start();
            };
        }

        void ConfigurarInterfaz()
        {
            // El visor es el padre de estos controles: BackColor=Transparent permite
            // que el fondo del juego se vea detrás del contador.
            lblVidas.AutoSize = true;
            lblVidas.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            lblVidas.ForeColor = Color.Crimson;
            lblVidas.BackColor = Color.Transparent;
            lblVidas.Padding = new Padding(2);
            lblVidas.Location = new Point(12, 8);
            lblVidas.Text = "♥ ♥ ♥";
            Visor.Controls.Add(lblVidas);

            // Contador de monedas: justo debajo de los corazones.
            lblMonedas.AutoSize = true;
            lblMonedas.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblMonedas.ForeColor = Color.Gold;
            lblMonedas.BackColor = Color.Transparent; // igual que las vidas
            lblMonedas.Padding = new Padding(2);
            lblMonedas.Location = new Point(12, 48);
            lblMonedas.Text = "Coins: 0 / 20";
            Visor.Controls.Add(lblMonedas);

            lblPowerUps.AutoSize = false;
            lblPowerUps.Size = new Size(260, 58);
            lblPowerUps.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblPowerUps.ForeColor = Color.White;
            lblPowerUps.BackColor = Color.FromArgb(150, 25, 35, 45);
            lblPowerUps.Padding = new Padding(8, 5, 8, 5);
            lblPowerUps.TextAlign = ContentAlignment.MiddleLeft;
            lblPowerUps.Location = new Point(14, 92); // debajo del contador de monedas
            lblPowerUps.Visible = false;
            Visor.Controls.Add(lblPowerUps);

            // Panel amplio y con título/mensaje separados para evitar que el texto se recorte.
            panelEstado.Size = new Size(520, 250);
            panelEstado.BackColor = Color.Gainsboro;
            panelEstado.BorderStyle = BorderStyle.FixedSingle;
            panelEstado.Location = new Point((ClientSize.Width - panelEstado.Width) / 2,
                                             (ClientSize.Height - panelEstado.Height) / 2);
            panelEstado.Visible = false;

            lblTituloEstado.Dock = DockStyle.Top;
            lblTituloEstado.Height = 66;
            lblTituloEstado.TextAlign = ContentAlignment.MiddleCenter;
            lblTituloEstado.ForeColor = Color.Firebrick;
            lblTituloEstado.Font = new Font("Segoe UI", 23, FontStyle.Bold);
            lblTituloEstado.Padding = new Padding(12, 8, 12, 2);

            lblMensajeEstado.Dock = DockStyle.Top;
            lblMensajeEstado.Height = 105;
            lblMensajeEstado.TextAlign = ContentAlignment.MiddleCenter;
            lblMensajeEstado.ForeColor = Color.FromArgb(35, 35, 35);
            lblMensajeEstado.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblMensajeEstado.Padding = new Padding(24, 4, 24, 4);
            lblMensajeEstado.AutoEllipsis = true;

            btnEstado.Text = "Volver a jugar";
            btnEstado.Size = new Size(190, 44);
            btnEstado.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnEstado.Location = new Point((panelEstado.Width - btnEstado.Width) / 2, 190);
            btnEstado.BackColor = Color.White;
            btnEstado.ForeColor = Color.FromArgb(30, 30, 30);
            btnEstado.FlatStyle = FlatStyle.Flat;
            btnEstado.Click += (s, e) =>
            {
                if (pausado) BtnReanudar_Click(s, e);
                else ReiniciarPartidaCompleta();
            };

            panelEstado.Controls.Add(lblMensajeEstado);
            panelEstado.Controls.Add(lblTituloEstado);
            panelEstado.Controls.Add(btnEstado);
            Controls.Add(panelEstado);
            panelEstado.BringToFront();
            lblVidas.BringToFront();
            lblMonedas.BringToFront();
            lblPowerUps.BringToFront();
        }

        // Monedas que exige cada nivel (el nivel 2 no tiene monedas: se oculta el contador).
        int MetaMonedas => nivelActual == 2 ? 0 : 20;

        void ActualizarContadorMonedas()
        {
            if (lblMonedas.IsDisposed) return;
            int meta = MetaMonedas;
            lblMonedas.Visible = meta > 0;
            string texto = "Coins: " + monedasRecogidas + " / " + meta;
            if (texto != textoMonedas)
            {
                textoMonedas = texto;
                lblMonedas.Text = texto;
            }
            lblMonedas.ForeColor = monedasRecogidas >= meta ? Color.LimeGreen : Color.Gold;
        }

        void ActualizarIndicadorVidas()
        {
            if (jugador == null) return;
            int vidas = Math.Max(0, Math.Min(3, jugador.Vida));
            lblVidas.Text = string.Join(" ", Enumerable.Repeat("♥", vidas));
            if (vidas == 0) lblVidas.Text = "";
        }

        void ActualizarAvisosPowerUp()
        {
            if (jugador == null || lblPowerUps.IsDisposed) return;

            bool escudo = jugador.TieneEscudo;
            bool velocidad = jugador.TieneVelocidad;

            if (escudo && !escudoAnterior)
            {
                avisoTemporalPowerUp = "🛡 ¡Escudo activado! Absorbe un impacto.";
                timerAvisoPowerUp.Stop();
                timerAvisoPowerUp.Start();
            }
            else if (!escudo && escudoAnterior)
            {
                avisoTemporalPowerUp = "🛡 Escudo perdido: protegió un impacto.";
                timerAvisoPowerUp.Stop();
                timerAvisoPowerUp.Start();
            }

            if (velocidad && !velocidadAnterior)
            {
                avisoTemporalPowerUp = "⚡ ¡Velocidad aumentada!";
                timerAvisoPowerUp.Stop();
                timerAvisoPowerUp.Start();
            }
            else if (!velocidad && velocidadAnterior)
            {
                avisoTemporalPowerUp = "⚡ La mejora de velocidad terminó.";
                timerAvisoPowerUp.Stop();
                timerAvisoPowerUp.Start();
            }

            escudoAnterior = escudo;
            velocidadAnterior = velocidad;

            var estados = new List<string>();
            if (escudo) estados.Add("🛡 Escudo activo");
            if (velocidad) estados.Add("⚡ Velocidad activa");
            if (estados.Count > 0)
            {
                lblPowerUps.Text = string.Join("\n", estados);
                lblPowerUps.Height = estados.Count == 2 ? 58 : 34;
                lblPowerUps.Visible = true;
            }
            else if (!string.IsNullOrWhiteSpace(avisoTemporalPowerUp))
            {
                lblPowerUps.Text = avisoTemporalPowerUp;
                lblPowerUps.Height = 42;
                lblPowerUps.Visible = true;
            }
            else
            {
                lblPowerUps.Visible = false;
            }
            lblPowerUps.BringToFront();
        }

        void MostrarPanelEstado(string titulo, string mensaje, string textoBoton)
        {
            lblTituloEstado.Text = titulo;
            lblMensajeEstado.Text = mensaje;
            btnEstado.Text = textoBoton;
            panelEstado.Visible = true;
            panelEstado.BringToFront();
            lblVidas.BringToFront();
            lblMonedas.BringToFront();
            lblPowerUps.BringToFront();
        }

        void MostrarVictoria()
        {
            juegoTerminado = true;
            pausado = false;
            bucleJuego.Stop();
            MostrarPanelEstado("¡VICTORIA!", "¡Felicitaciones! Completaste los tres niveles de Pixel Escape.", "Jugar nuevamente");
        }

        void ReiniciarPartidaCompleta()
        {
            timerTransicion.Stop();
            timerRespawn.Stop();
            respawnPendiente = false;
            bucleJuego.Stop();
            izquierda = derecha = salto = false;
            pausado = false;
            juegoTerminado = false;
            transicionEnCurso = false;
            panelEstado.Visible = false;
            jugador.ReiniciarPartida();
            escudoAnterior = false;
            velocidadAnterior = false;
            avisoTemporalPowerUp = "";
            lblPowerUps.Visible = false;
            puntoControlGuardado = false;
            indicesRecogidos.Clear();
            puntajeCheckpoint = 0;
            nivelActual = 1;
            CargarNivel(1, false);
            ActualizarIndicadorVidas();
            bucleJuego.Start();
            Visor.Focus();
        }

        void AlternarPausa()
        {
            if (juegoTerminado || transicionEnCurso) return;
            pausado = !pausado;
            izquierda = derecha = salto = false;
            if (pausado)
            {
                bucleJuego.Stop();
                MostrarPanelEstado("JUEGO EN PAUSA", "Pulsa P o el botón para continuar.", "Reanudar");
            }
            else
            {
                panelEstado.Visible = false;
                bucleJuego.Start();
            }
        }

        void BtnReanudar_Click(object? sender, EventArgs e)
        {
            pausado = false;
            panelEstado.Visible = false;
            bucleJuego.Start();
            Visor.Focus();
        }

        // ---------- Obstáculos de terreno (tileset.png) ----------
        // Crea UN obstáculo: (fila, columna) es el cuadro de 16x16 dentro de la hoja "TileSet".
        void AgregarObstaculo(int x, int y, int fila, int columna)
        {
            Obstaculo obstaculo = new Obstaculo(x, y, fila, columna);
            obstaculo.SetPos(x, y);
            obstaculo.ZValue = -2; // detrás de puerta/trampa (-1) y del personaje
            obstaculos.Add(obstaculo);
            scene.AddItem(obstaculo);
        }

        // Recursos individuales existentes
        void AgregarDecoracion(string recurso, int x, int y, int ancho, int alto, int z = -5)
        {
            Decoracion item = new Decoracion(x, y, recurso, ancho, alto);
            item.ZValue = z;
            decoraciones.Add(item);
            scene.AddItem(item);
        }

        // Obstáculos con imagen individual y colisión sólida. Pueden bloquear lateralmente
        // y también servir como superficie si el jugador salta encima.
        void AgregarObstaculoSprite(string recurso, int x, int y, int ancho, int alto, Rectangle colision)
        {
            ObstaculoSprite item = new ObstaculoSprite(x, y, recurso, ancho, alto, colision);
            item.ZValue = -1;
            obstaculosSprite.Add(item);
            jugador.Plataformas.Add(colision);
            scene.AddItem(item);
        }

        void DecorarNivel1()
        {
            // Fondo panorámico: solapa 2 px entre copias para evitar líneas claras por redondeo.
            AgregarDecoracion("FondoMontañas", 0, 0, 922, 490, -20);
            AgregarDecoracion("FondoMontañas", 918, 0, 922, 490, -20);

            // Nubes en una capa distante
            // Se ubican en la franja alta
            AgregarDecoracion("Nube1", 110, 6, 106, 32, -18);
            AgregarDecoracion("Nube2", 490, 14, 90, 32, -18);
            AgregarDecoracion("Nube2", 790, 20, 90, 32, -18);
            AgregarDecoracion("Nube1", 1110, 10, 106, 32, -18);
            AgregarDecoracion("Nube2", 1580, 2, 90, 32, -18);

            // Pinos (Pino1 130x190, Pino2 120x191). El PNG deja ~5 px transparentes abajo,
            // por eso se bajan 6 px para que la base toque el césped.
            AgregarDecoracion("Pino1", 34, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 215, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 390, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 520, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 700, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 850, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 1012, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 1215, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 1380, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 1520, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 1618, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 1730, Suelo - 120, 105, 130, -8);

            // Enredadera (recurso "Enredadera1" = EnredaderasSprite6.png) sobre los tiles de tierra,
            // colgando desde el borde del césped. Z = -1: por delante de los tiles (-2).
            AgregarDecoracion("Enredadera1", 50, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 200, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 350, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 500, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 650, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 800, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 950, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 1050, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 1200, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 1350, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 1500, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 1650, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 1800, Suelo + 10, 26, 126, -1);

            // Arbustos variados de la colección nueva, todos apoyados en la superficie.
            AgregarDecoracion("Arbusto1", 140, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto2", 310, Suelo - 43, 76, 52, -1);
            AgregarDecoracion("Arbusto4", 630, Suelo - 39, 76, 48, -1);
            AgregarDecoracion("Arbusto 3", 940, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto1", 1180, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto2", 1345, Suelo - 43, 76, 52, -1);
            AgregarDecoracion("Arbusto4", 1495, Suelo - 39, 76, 48, -1);
            // Rocas de tamaños alternos y flores pequeñas para dar detalle sin bloquear saltos.
            AgregarDecoracion("Roca2", 15, Suelo - 27, 48, 28, -3);
            AgregarDecoracion("Roca1", 103, Suelo - 25, 55, 30, -3);
            AgregarDecoracion("Roca3", 276, Suelo - 25, 45, 30, -3);
            AgregarDecoracion("Roca3", 840, Suelo - 25, 45, 30, -3);
            AgregarDecoracion("Roca1", 492, Suelo - 25, 55, 30, -3);
            AgregarDecoracion("Roca2", 1000, Suelo - 27, 48, 28, -3);
            AgregarDecoracion("Roca3", 1132, Suelo - 25, 42, 26, -3);
            AgregarDecoracion("Roca1", 1617, Suelo - 25, 55, 30, -3);
            AgregarDecoracion("Roca1", 1800, Suelo - 25, 55, 30, -3);

            AgregarDecoracion("Flor1", 195, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor2", 383, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor3", 815, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor4", 995, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor1", 1115, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor3", 1165, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor2", 1640, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor4", 1790, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor2", 1775, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor2", 1750, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor3", 204, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor1", 820, Suelo - 107, 30, 32, -1);
            AgregarDecoracion("Flor2", 948, Suelo - 107, 30, 32, -1);
        }

        void CrearObstaculosNivel1()
        {
            // Cajas (32x32) en grupo: dos en el suelo, una al lado de la otra, y una tercera
            // encima de la segunda. Cada una con su propia colisión.
            AgregarGrupoCajas(430);

            // El PNG del tronco tiene márgenes transparentes; el dibujo se alinea por su alpha
            // inferior y la colisión cubre únicamente la zona sólida visible.
            AgregarObstaculoSprite("Tronco", 690, Suelo - 50, 140, 76,
                new Rectangle(704, Suelo - 30, 112, 30));

            // El PNG deja margen transparente abajo: se baja 4 px para que apoye en el césped.
            AgregarObstaculoSprite("TroncoPartido", 1280, Suelo - 50, 96, 72,
                new Rectangle(1288, Suelo - 48, 80, 48));

            AgregarGrupoCajas(1550);

            // Obstáculos adicionales
            AgregarObstaculoSprite("Caja", 160, Suelo - 32, 32, 32,
                new Rectangle(160, Suelo - 32, 32, 32));
            AgregarObstaculoSprite("Tronco", 1110, Suelo - 50, 140, 76,
                new Rectangle(1110, Suelo - 30, 112, 30));
            AgregarObstaculoSprite("Caja", 875, Suelo - 115, 32, 32,
                new Rectangle(875, Suelo - 115, 32, 32));
        }

        // Dos cajas en el suelo (x, x + 32) y una encima de la segunda (x + 32).
        void AgregarGrupoCajas(int x)
        {
            AgregarObstaculoSprite("Caja", x, Suelo - 32, 32, 32,
                new Rectangle(x, Suelo - 32, 32, 32));
            AgregarObstaculoSprite("Caja", x + 32, Suelo - 32, 32, 32,
                new Rectangle(x + 32, Suelo - 32, 32, 32));
            AgregarObstaculoSprite("Caja", x + 32, Suelo - 64, 32, 32,
                new Rectangle(x + 32, Suelo - 64, 32, 32));
        }

        // Suelo completo: una fila de césped en Y = Suelo y tierra hacia abajo hasta el borde de la ventana.
        void CrearSuelo()
        {
            // En el nivel 1 se utiliza una sola textura de superficie y una de tierra:
            // esto elimina cambios de diseño y hace que los bloques se vean uniformes.
            // Los niveles 2 y 3 conservan su variedad original de texturas.
            int[] columnasCesped = { 0, 2, 4, 5 };
            int[,] tierra = { { 1, 1 }, { 1, 2 }, { 1, 3 }, { 1, 4 } };
            int columnas = AnchoNivel / Obstaculo.Tamano + 1;
            int filas = (ClientSize.Height - Suelo) / Obstaculo.Tamano + 1;
            for (int c = 0; c < columnas; c++)
            {
                int x = c * Obstaculo.Tamano;
                for (int f = 0; f < filas; f++)
                {
                    int y = Suelo + f * Obstaculo.Tamano;
                    if (nivelActual == 1)
                    {
                        AgregarObstaculo(x, y, f == 0 ? 0 : 1, 0);
                    }
                    else if (f == 0)
                    {
                        AgregarObstaculo(x, y, 0, columnasCesped[c % columnasCesped.Length]);
                    }
                    else
                    {
                        int t = (c * 7 + f * 3) % tierra.GetLength(0);
                        AgregarObstaculo(x, y, tierra[t, 0], tierra[t, 1]);
                    }
                }
            }
        }

        // Plataforma flotante de 'cantidad' bloques de piedra, con la esquina superior izquierda en (x, y).
        void CrearPlataforma(int x, int y, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                AgregarObstaculo(x + i * Obstaculo.Tamano, y, 0, 0);
            }

            // Se registra como superficie sólida: el jugador se para encima y no la atraviesa.
            jugador.Plataformas.Add(new Rectangle(x, y, cantidad * Obstaculo.Tamano, Obstaculo.Tamano));
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
        const int YEnemigo = Suelo - 69;       // los enemigos quedan con los pies sobre el suelo
        const int YMonedaSuelo = Suelo - 100; // moneda pequeña, elevada para exigir salto
        const int YMonedaPlataforma = YPlataforma - 30;

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
            int[] alturas = { Suelo - 125, Suelo - 145, Suelo - 155, Suelo - 145, Suelo - 125 };
            for (int i = 0; i < 5; i++)
            {
                AgregarMonedaEn(xTrampa + 12 + desfase[i], alturas[i]);
            }
        }

        void AgregarTrampa(int x)
        {
            Trampa t = new Trampa(x, 0, 1);
            t.SetPos(x, Suelo - 65);
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
            limitesPatrulla[g] = (Math.Max(0, x - 70), x + 70);
            scene.AddItem(g);
            CrearPlataforma(x - 16, YPlataforma, 4);
        }

        // Cazador en el suelo con una plataforma encima.
        // (Cuerpo del cazador = x+27..x+69; la plataforma lo cubre con 3 bloques.)
        void AgregarCazador(int x)
        {
            CazadorPixel c = new CazadorPixel(x, YEnemigo);
            cazadores.Add(c);
            limitesPatrulla[c] = (Math.Max(0, x - 90), x + 90);
            scene.AddItem(c);
            CrearPlataforma(x, YPlataforma, 3);
        }

        // Guardián SIN plataforma: hay que saltarlo (cuerpo = x+24..x+72).
        void AgregarGuardianLibre(int x)
        {
            GuardianPixel g = new GuardianPixel(x, YEnemigo);
            guardianes.Add(g);
            limitesPatrulla[g] = (Math.Max(0, x - 70), x + 70);
            scene.AddItem(g);
        }

        // Cazador SIN plataforma: hay que saltarlo (cuerpo = x+27..x+69).
        void AgregarCazadorLibre(int x)
        {
            CazadorPixel c = new CazadorPixel(x, YEnemigo);
            cazadores.Add(c);
            limitesPatrulla[c] = (Math.Max(0, x - 90), x + 90);
            scene.AddItem(c);
        }

        // Llave apoyada sobre una plataforma baja que empieza en xPlataforma y mide 'bloques' bloques.
        void AgregarLlaveSobrePlataforma(int xPlataforma, int bloques)
        {
            CrearPlataforma(xPlataforma, YPlataforma, bloques);
            Llave llave = new Llave(0, 0);
            int x = xPlataforma + (bloques * Obstaculo.Tamano - (int)llave.Ancho) / 2; // centrada
            AgregarColeccionable(llave, x, YPlataforma - (int)llave.Alto);
        }

        // Posición X del punto de control de cada nivel (a mitad del recorrido).
        readonly int[] xPuntoControl = { 1060, 1400, 1800 };

        void ActualizarPatrullas()
        {
            foreach (var entrada in limitesPatrulla)
            {
                Enemigo enemigo = entrada.Key;
                if (!enemigo.EstaVivo) continue;
                int minimo = entrada.Value.Min;
                int maximo = entrada.Value.Max;
                if (enemigo.X <= minimo) enemigo.CambiarDireccionPatrulla(1);
                else if (enemigo.X >= maximo) enemigo.CambiarDireccionPatrulla(-1);
                if (enemigo.DireccionActual < 0) enemigo.MoverIzq();
                else enemigo.MoverDer();
                enemigo.Animar(Enemigo.EstadoAnimacion.Camina);
            }
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
            if (transicionEnCurso || juegoTerminado || pausado)
                return;

            if (respawnPendiente)
            {
                jugador.Aterrizar(); // mantiene la animación de daño mientras espera la reaparición
                return;
            }

            ActualizarIndicadorVidas();

            // Los enemigos patrullan continuamente dentro de su zona.
            ActualizarPatrullas();

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
                Rectangle rectItem = new Rectangle(item.X, item.Y, (int)item.Ancho, (int)item.Alto);
                bool colisionColeccionable = (item is Moneda || item is Llave)
                    ? jugador.Cuerpo.IntersectsWith(rectItem)
                    : jugador.ColisionaCon(item);
                if (!item.Recogido && colisionColeccionable)
                {
                    item.Recoger(jugador);

                    if (item is Moneda)
                    {
                        monedasRecogidas++;
                    }
                }
            }
            ActualizarContadorMonedas();

            // El contacto con cualquier enemigo descuenta una vida.
            Rectangle cuerpoJugador = CuerpoJugador();
            foreach (CazadorPixel c in cazadores)
            {
                if (cuerpoJugador.IntersectsWith(CuerpoCazador(c)))
                {
                    int vidasAntes = jugador.Vida;
                    jugador.RecibirDano(1);
                    if (jugador.Vida < vidasAntes) { evitoCazador = false; ResolverImpacto(); return; }
                }
            }
            foreach (GuardianPixel g in guardianes)
            {
                if (cuerpoJugador.IntersectsWith(CuerpoGuardian(g)))
                {
                    int vidasAntes = jugador.Vida;
                    jugador.RecibirDano(1);
                    if (jugador.Vida < vidasAntes) { evitoGuardian = false; ResolverImpacto(); return; }
                }
            }

            // Activar punto de control: la primera vez guarda posición y progreso.
            if (!puntoControl.Activo && jugador.ColisionaCon(puntoControl))
            {
                puntoControl.Activar(jugador);
                GuardarPuntoControl();
            }
            ValidarSalida();

            // Las trampas descuentan una vida al entrar en su zona.
            foreach (Trampa t in trampas)
            {
                int vidasAntes = jugador.Vida;
                t.Activar(jugador);
                if (jugador.Vida < vidasAntes) { ResolverImpacto(); return; }
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
        readonly List<int> indicesRecogidos = new List<int>();
        int puntajeCheckpoint;       // coleccionables que ya tenía al activarlo

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
            if (!desdePuntoControl)
            {
                evitoCazador = true;
                evitoGuardian = true;
            }

            transicionEnCurso = false;

            // Quitar los objetos del nivel anterior (si solo se ocultan, la escena acumula objetos en cada reinicio)
            foreach (Coleccionables item in coleccionables)
            {
                item.Visible = false;
                scene.RemoveItem(item);
            }
            coleccionables.Clear();

            foreach (Obstaculo obstaculo in obstaculos)
            {
                obstaculo.Visible = false;
                scene.RemoveItem(obstaculo);
            }
            obstaculos.Clear();
            foreach (Decoracion item in decoraciones)
            {
                item.Visible = false;
                scene.RemoveItem(item);
            }
            decoraciones.Clear();
            foreach (ObstaculoSprite item in obstaculosSprite)
            {
                item.Visible = false;
                scene.RemoveItem(item);
            }
            obstaculosSprite.Clear();
            jugador.Plataformas.Clear();

            foreach (GuardianPixel g in guardianes)
            {
                g.Visible = false;
                scene.RemoveItem(g);
            }
            guardianes.Clear();
            limitesPatrulla.Clear();

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
            jugador.ReiniciarEstado(xInicio, Suelo - 88);
            jugador.Visible = true;
            if (desdePuntoControl)
            {
                RestaurarProgresoGuardado();
            }

            // La cámara vuelve al inicio del nivel
            ActualizarCamara();

            // Puerta cerca del final del nivel
            int xPuerta = AnchoNivel - 140;
            puerta = new Puerta(xPuerta, 0);
            puerta.SetPos(xPuerta, Suelo - (int)puerta.Alto + 11); // +11: el cuadro deja margen transparente abajo
            puerta.ZValue = -1;
            scene.AddItem(puerta);

            // Punto de control a mitad del nivel
            int xControl = xPuntoControl[nivelActual - 1];
            puntoControl = new PuntoControl(xControl, 0);
            puntoControl.SetPos(xControl, Suelo - (int)puntoControl.Alto + 13); // +13: apoya en el césped
            puntoControl.ZValue = -1;
            scene.AddItem(puntoControl);

            if (desdePuntoControl)
            {
                puntoControl.Activar(jugador); // la bandera ya aparece levantada
            }

            ActualizarContadorMonedas();
        }

        // Guarda dónde reaparecer (en la bandera) y qué monedas/llave llevaba el jugador.
        void GuardarPuntoControl()
        {
            puntoControlGuardado = true;
            xRespawn = puntoControl.X - 16; // el cuerpo del jugador queda sobre la bandera

            puntajeCheckpoint = jugador.Puntaje;
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
            jugador.EstablecerPuntaje(puntajeCheckpoint);
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
            jugador.EstablecerPuntaje(puntajeCheckpoint);
            ActualizarContadorMonedas();
        }

        // Derrota (tocar un enemigo o quedarse sin vidas): el nivel se reinicia por completo
        // (enemigos, trampas, monedas y llave posteriores al punto de control, vida y objetivos).
        // Si ya se activó el punto de control, el jugador reaparece ahí; si no, al inicio.
        void ResolverImpacto()
        {
            izquierda = derecha = salto = false;
            ActualizarIndicadorVidas();

            if (!jugador.EstaVivo)
            {
                // La partida queda detenida hasta que el usuario pulse el botón de reinicio.
                juegoTerminado = true;
                bucleJuego.Stop();
                MostrarPanelEstado("GAME OVER", "Has perdido tus tres vidas.", "Volver a jugar");
            }
            else
            {
                // Se conserva visible la reacción de daño antes de reaparecer.
                respawnPendiente = true;
                bucleJuego.Start();
                timerRespawn.Stop();
                timerRespawn.Start();
            }
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
            DecorarNivel1();
            CrearObstaculosNivel1();

            AgregarMonedasLinea(230, 4);              // 4 monedas: calentamiento (x 230-405)
            AgregarTrampa(260);                       // trampa extra: se salta al recoger las monedas

            AgregarTrampa(540);                       // trampa 1 (x 540-604)
            AgregarMonedasArcoSobreTrampa(540);       // 5 monedas: se recogen saltando la trampa

            CrearPlataforma(800, YPlataforma, 6);     // plataforma (x 800-992)
            // 3 monedas encima; la central sube para que la caja de la plataforma no la tape.
            AgregarMonedaEn(830, YMonedaPlataforma);
            AgregarMonedaEn(880, YMonedaPlataforma - 44);
            AgregarMonedaEn(930, YMonedaPlataforma);

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
            AgregarColeccionable(new PowerUpVelocidad(2070, 0, 10), 2070); // velocidad temporal antes del último tramo

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