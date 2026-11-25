using Engine2D;
using System.Diagnostics;
using PixelEsGUI.Persistencia;

namespace PixelEsGUI
{
    public partial class Escenario : Form
    {
        GraphicsScene scene;
        Jugador jugador;
        // Cada nivel puede tener varios cazadores, guardianes y trampas.
        readonly List<CazadorPixel> cazadores = new List<CazadorPixel>();
        readonly List<FlechaCazador> flechasCazador = new List<FlechaCazador>();
        readonly List<GuardianPixel> guardianes = new List<GuardianPixel>();
        readonly List<Trampa> trampas = new List<Trampa>();
        readonly Dictionary<Enemigo, (int Min, int Max)> limitesPatrulla = new Dictionary<Enemigo, (int Min, int Max)>();

        // Objetos del escenario.
        readonly List<Coleccionables> coleccionables = new List<Coleccionables>();
        readonly List<Obstaculo> obstaculos = new List<Obstaculo>(); // tiles de terreno
        readonly List<Decoracion> decoraciones = new List<Decoracion>();
        readonly List<ObstaculoSprite> obstaculosSprite = new List<ObstaculoSprite>();
        Puerta puerta = null!; // Se crea al cargar el primer nivel en el constructor.
        PuntoControl puntoControl = null!; // Se crea al cargar el primer nivel en el constructor.

        // Altura del "suelo" donde se apoyan los objetos
        const int Suelo = 300;

        // ---------- Cámara ----------
        readonly int[] anchoNiveles = { 1840, 2760, 3680 };
        int AnchoNivel => anchoNiveles[nivelActual - 1];

        // Teclas que están presionadas en este momento.
        bool izquierda, derecha, salto;
        bool pausado = false;
        readonly System.Windows.Forms.Timer bucleJuego = new System.Windows.Forms.Timer { Interval = 16 };
        readonly Stopwatch relojSalto = new Stopwatch();
        readonly IRepositorioJuego repositorio;
        readonly GestorAudio audio = new();
        bool saltoPresionadoAnterior;
        readonly Button btnGuardar = new Button();
        readonly Button btnVolverInicio = new Button();
        readonly Button btnClasificacionFin = new Button(); // en el panel de victoria / game over
        string nombreJugador = "";
        double tiempoPartidaSegundos;
        int monedasTotales;
        bool resultadoRegistrado;
        readonly System.Windows.Forms.Timer timerRespawn = new System.Windows.Forms.Timer { Interval = 520 };
        bool respawnPendiente = false;

        // Interfaz superpuesta: vidas y paneles de pausa/fin de partida.
        readonly Label lblVidas = new Label();
        readonly Label lblPowerUps = new Label();
        readonly Label lblMonedas = new Label(); // contador de monedas, debajo de las vidas
        string textoMonedas = "";
        int vidasIndicador = -1;
        Color colorMonedasIndicador = Color.Empty;
        readonly Panel panelEstado = new Panel();
        readonly Label lblTituloEstado = new Label();
        readonly Label lblMensajeEstado = new Label();
        readonly Button btnEstado = new Button();
        bool escudoAnterior;
        bool velocidadAnterior;
        string avisoTemporalPowerUp = "";
        readonly System.Windows.Forms.Timer timerAvisoPowerUp = new System.Windows.Forms.Timer { Interval = 1800 };

        public Escenario() : this(new RepositorioJuegoSqlite())
        {
        }

        internal Escenario(IRepositorioJuego repositorio)
        {
            this.repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));

            InitializeComponent();
            ClientSize = new Size(920, 490); // tamaño de la ventana = lo que ve la cámara
            FormBorderStyle = FormBorderStyle.FixedSingle; // scene.Width debe coincidir con la ventana,
            MaximizeBox = false;
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
            ConfigurarBotonesPausa();
            ConfigurarPantallas(); // pantallas de inicio, controles y cambio de nivel (EscenarioPantallas.cs)
            audio.ErrorReproduccion += MostrarAvisoAudio; // sin sonido, el juego continúa y se avisa en pantalla
            FormClosed += (_, _) => audio.Dispose();

            jugador = new Jugador(100, Suelo - 88);
            scene.AddItem(jugador);

            // Cargar el primer nivel
            CargarNivel(1);

            // Temporizador para cambiar de nivel
            timerTransicion.Tick += Proteger("el cambio de nivel", (s, e) =>
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
                    MostrarPantallaNivel(nivelActual); // pantalla "NIVEL 2" / "NIVEL 3"
                }
            });

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
                audio.DetenerMusica();
                audio.IniciarMusica();
                CargarNivel(nivelActual);

                // Asegurar que el jugador siga visible
                jugador.Visible = true;

                // Actualizar título
                this.Text = "Pixel Escape - Nivel " + nivelActual;
            }

            // Teclado: el formulario recibe las teclas antes que el visor.
            KeyPreview = true;
            KeyDown += ProtegerTecla("el teclado", (s, e) =>
            {
                if (pantallaActiva) return; // con una pantalla visible no se mueve, pausa ni reinicia

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
            });
            KeyUp += ProtegerTecla("el teclado", (s, e) => CambiarTecla(e.KeyCode, false));

            //mover -> saltar -> aterrizar (ver Jugador.Saltar).
            bucleJuego.Tick += Proteger("el bucle de juego", (s, e) =>
            {
                double deltaTime = relojSalto.IsRunning ? relojSalto.Elapsed.TotalSeconds : 1.0 / 60.0;
                relojSalto.Restart();
                if (!pantallaActiva && !pausado && !juegoTerminado && !transicionEnCurso && !respawnPendiente)
                    tiempoPartidaSegundos += Math.Max(0, Math.Min(deltaTime, 0.05));
                PasoDeJuego(deltaTime);
                ActualizarAvisosPowerUp();
            });
            timerAvisoPowerUp.Tick += Proteger("el aviso de mejoras", (s, e) =>
            {
                timerAvisoPowerUp.Stop();
                avisoTemporalPowerUp = "";
                ActualizarAvisosPowerUp();
            });
            timerRespawn.Tick += Proteger("la reaparición del jugador", (s, e) =>
            {
                timerRespawn.Stop();
                respawnPendiente = false;
                if (!juegoTerminado)
                {
                    CargarNivel(nivelActual, puntoControlGuardado);
                    ActualizarIndicadorVidas();
                }
            });

            this.Shown += Proteger("el arranque del juego", (s, e) =>
            {
                Visor.Start();
                MostrarPantallaInicio(); // el juego empieza al pulsar EMPEZAR (el bucle arranca al cerrar las pantallas)
            });
        }


        /// <summary>Registra una sola vez la puntuación al terminar la partida.</summary>
        void RegistrarResultadoFinal(string tipo)
        {
            if (resultadoRegistrado || string.IsNullOrWhiteSpace(nombreJugador)) return;
            try
            {
                repositorio.RegistrarResultado(new ResultadoPartida {
                    NombreJugador = nombreJugador,
                    TiempoSegundos = tiempoPartidaSegundos,
                    Monedas = monedasTotales,
                    VidasRestantes = jugador.Vida,
                    NivelAlcanzado = nivelActual,
                    Resultado = tipo
                });
                resultadoRegistrado = true;
                // Al perder se conserva el último guardado para poder reintentarlo con CONTINUAR PARTIDA.
                // Solo se borra al completar el juego, cuando ya no queda nada que continuar.
                if (tipo == "victoria")
                    repositorio.EliminarGuardado(nombreJugador);
            }
            catch (Exception ex) when (EsErrorDePersistencia(ex))
            {
                RegistroErrores.Registrar("Registrar la puntuación final", ex);
                MessageBox.Show(this, "No se pudo registrar la puntuación de esta partida.\n\n" + RegistroErrores.Describir(ex),
                    "Pixel Escape - Clasificación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void ConfigurarBotonesPausa()
        {
            btnGuardar.Text = "Guardar partida";
            btnGuardar.Size = new Size(260, 42);
            btnGuardar.Location = new Point((panelEstado.Width - btnGuardar.Width) / 2, 190);
            btnGuardar.Font = FuenteBotonPantalla;
            btnGuardar.BackColor = ColorBotonPantalla;
            btnGuardar.ForeColor = Color.Black;
            btnGuardar.FlatStyle = FlatStyle.Flat;
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.FlatAppearance.MouseOverBackColor = ColorBotonHover;
            btnGuardar.FlatAppearance.MouseDownBackColor = ColorBotonPresionado;
            btnGuardar.Visible = false;
            btnVolverInicio.Visible = false;
            btnGuardar.Click += (s, ev) => { audio.Efecto("button"); GuardarPartidaActual(); };
            panelEstado.Controls.Add(btnGuardar);
            btnGuardar.BringToFront();

            btnVolverInicio.Text = "VOLVER AL INICIO";
            btnVolverInicio.Size = new Size(260, 42);
            btnVolverInicio.Location = new Point((panelEstado.Width - btnVolverInicio.Width) / 2, 242);
            btnVolverInicio.Font = FuenteBotonPantalla;
            btnVolverInicio.BackColor = ColorBotonPantalla;
            btnVolverInicio.ForeColor = Color.Black;
            btnVolverInicio.FlatStyle = FlatStyle.Flat;
            btnVolverInicio.FlatAppearance.BorderSize = 0;
            btnVolverInicio.FlatAppearance.MouseOverBackColor = ColorBotonHover;
            btnVolverInicio.FlatAppearance.MouseDownBackColor = ColorBotonPresionado;
            btnVolverInicio.Visible = false;
            btnVolverInicio.Click += (s, ev) =>
            {
                // Si falla el guardado, permanece en pausa para no perder progreso.
                audio.Efecto("button");
                // Tras perder o ganar no hay nada que guardar (el último guardado se conserva).
                if (!juegoTerminado && !GuardarPartidaActual(false)) return;
                audio.DetenerMusica();
                pausado = false;
                izquierda = derecha = salto = false;
                panelEstado.Visible = false;
                btnGuardar.Visible = false;
            btnVolverInicio.Visible = false;
                btnVolverInicio.Visible = false;
                bucleJuego.Stop();
                relojSalto.Reset();
                MostrarPantallaInicio();
            };
            panelEstado.Controls.Add(btnVolverInicio);
            btnVolverInicio.BringToFront();

            // Acceso a la clasificación al terminar la partida (ocupa el hueco de "Guardar partida",
            // que solo se ve en pausa).
            btnClasificacionFin.Text = "CLASIFICACIÓN";
            btnClasificacionFin.Size = new Size(260, 42);
            btnClasificacionFin.Location = new Point((panelEstado.Width - btnClasificacionFin.Width) / 2, 190);
            btnClasificacionFin.Font = FuenteBotonPantalla;
            btnClasificacionFin.BackColor = ColorBotonPantalla;
            btnClasificacionFin.ForeColor = Color.Black;
            btnClasificacionFin.FlatStyle = FlatStyle.Flat;
            btnClasificacionFin.FlatAppearance.BorderSize = 0;
            btnClasificacionFin.FlatAppearance.MouseOverBackColor = ColorBotonHover;
            btnClasificacionFin.FlatAppearance.MouseDownBackColor = ColorBotonPresionado;
            btnClasificacionFin.Visible = false;
            btnClasificacionFin.Click += (s, ev) =>
            {
                audio.Efecto("button");
                panelEstado.Visible = false;
                MostrarPantallaClasificacion(() =>
                {
                    panelEstado.Visible = true;
                    panelEstado.BringToFront();
                    lblVidas.BringToFront();
                    lblMonedas.BringToFront();
                    lblPowerUps.BringToFront();
                });
            };
            panelEstado.Controls.Add(btnClasificacionFin);
            btnClasificacionFin.BringToFront();
        }

        /// <summary>Instantánea del estado actual de la partida (compartida por guardado manual y automático).</summary>
        EstadoPartida CrearEstadoActual()
        {
            var recogidos = new List<int>();
            for (int i = 0; i < coleccionables.Count; i++)
                if (coleccionables[i].Recogido) recogidos.Add(i);
            return new EstadoPartida {
                    NombreJugador = nombreJugador,
                    NivelActual = nivelActual,
                    PosicionX = jugador.X, PosicionY = jugador.Y,
                    VidasRestantes = jugador.Vida,
                    PuntuacionAcumulada = jugador.Puntaje,
                    MonedasNivel = monedasRecogidas, MonedasTotales = monedasTotales,
                    TiempoAcumuladoSegundos = tiempoPartidaSegundos,
                    TieneLlave = jugador.TieneLlave, TieneEscudo = jugador.TieneEscudo,
                    TieneVelocidad = jugador.TieneVelocidad,
                    PuntoControlGuardado = puntoControlGuardado,
                    PosicionRespawnX = xRespawn, PuntajeCheckpoint = puntajeCheckpoint,
                    EvitoCazador = evitoCazador, EvitoGuardian = evitoGuardian,
                    IndicesColeccionablesRecogidos = recogidos,
                    IndicesColeccionablesCheckpoint = new List<int>(indicesRecogidos)
            };
        }

        /// <summary>
        /// Guarda automáticamente al empezar cada nivel, para que CONTINUAR PARTIDA siempre tenga algo
        /// que cargar aunque el jugador no haya guardado a mano. Nunca pisa un guardado de un nivel
        /// igual o más avanzado (p. ej. uno manual hecho a mitad del nivel).
        /// </summary>
        void AutoGuardarInicioNivel()
        {
            if (string.IsNullOrWhiteSpace(nombreJugador)) return;
            try
            {
                EstadoPartida? existente = null;
                try { existente = repositorio.CargarPartida(nombreJugador); }
                catch (System.IO.InvalidDataException) { /* guardado dañado: se reemplaza */ }
                if (existente != null && existente.NivelActual >= nivelActual) return;
                repositorio.GuardarPartida(CrearEstadoActual());
            }
            catch (Exception ex) when (EsErrorDePersistencia(ex))
            {
                // No interrumpe el juego: se anota y el jugador aún puede guardar a mano desde la pausa.
                RegistroErrores.Registrar("Autoguardado al empezar el nivel " + nivelActual, ex);
            }
        }

        bool GuardarPartidaActual(bool mostrarConfirmacion = true)
        {
            if (!pausado || string.IsNullOrWhiteSpace(nombreJugador)) return false;
            try
            {
                repositorio.GuardarPartida(CrearEstadoActual());
                if (mostrarConfirmacion)
                    MessageBox.Show("Partida guardada correctamente.", "Pixel Escape");
                return true;
            }
            catch (Exception ex) when (EsErrorDePersistencia(ex))
            {
                RegistroErrores.Registrar("Guardar la partida manualmente", ex);
                MessageBox.Show(this, "No se pudo guardar la partida.\n\n" + RegistroErrores.Describir(ex),
                    "Pixel Escape - Guardado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        /// <summary>Restituye jugador, coleccionables y progreso desde SQLite.</summary>
        void ContinuarPartida(string alias)
        {
            try
            {
                EstadoPartida? estado = repositorio.CargarPartida(alias);
                if (estado == null)
                {
                    MostrarErrorContinuacion("No existe una partida guardada con ese alias.");
                    return;
                }
                if (estado.NivelActual is < 1 or > 3)
                    throw new InvalidDataException("El guardado indica un nivel que no existe (" + estado.NivelActual + ").");
                if (estado.VidasRestantes <= 0)
                    throw new InvalidDataException("El guardado corresponde a una partida ya terminada (sin vidas restantes).");
                nombreJugador = estado.NombreJugador;
                timerTransicion.Stop();
                timerRespawn.Stop();
                respawnPendiente = false;
                resultadoRegistrado = false;
                pausado = false;
                juegoTerminado = false;
                jugador.ReiniciarPartida();
                CargarNivel(estado.NivelActual);
                jugador.SetPos(estado.PosicionX, estado.PosicionY);
                jugador.SincronizarPosicionHorizontal();
                jugador.RestaurarDatos(estado.VidasRestantes, estado.PuntuacionAcumulada,
                    estado.TieneLlave, estado.TieneEscudo, estado.TieneVelocidad);
                foreach (int indice in estado.IndicesColeccionablesRecogidos ?? new List<int>())
                    if (indice >= 0 && indice < coleccionables.Count)
                        coleccionables[indice].Recoger(jugador);
                // Recoger aplica efectos: restaurar los valores originales después.
                jugador.RestaurarDatos(estado.VidasRestantes, estado.PuntuacionAcumulada,
                    estado.TieneLlave, estado.TieneEscudo, estado.TieneVelocidad);
                monedasRecogidas = estado.MonedasNivel;
                monedasTotales = estado.MonedasTotales;
                tiempoPartidaSegundos = estado.TiempoAcumuladoSegundos;
                puntoControlGuardado = estado.PuntoControlGuardado;
                xRespawn = estado.PosicionRespawnX;
                puntajeCheckpoint = estado.PuntajeCheckpoint;
                indicesRecogidos.Clear();
                indicesRecogidos.AddRange(estado.IndicesColeccionablesCheckpoint ?? new List<int>());
                evitoCazador = estado.EvitoCazador;
                evitoGuardian = estado.EvitoGuardian;
                if (puntoControlGuardado) puntoControl.Activar(jugador);
                ActualizarContadorMonedas();
                ActualizarIndicadorVidas();
                panelPantalla.Visible = false;
                pantallaActiva = false;
                panelEstado.Visible = false;
                btnGuardar.Visible = false;
            btnVolverInicio.Visible = false;
                relojSalto.Restart();
                audio.IniciarMusica();
                bucleJuego.Start();
                Visor.Focus();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // Datos ilegibles, base de datos inaccesible o fallo al reconstruir el nivel: el detalle
                // va en un cuadro de diálogo (es largo) y la pantalla de alias queda lista para reintentar.
                RegistroErrores.Registrar("Continuar la partida de '" + alias + "'", ex);
                DetenerJuegoPorError();
                MessageBox.Show(this, "No se pudo cargar la partida de '" + alias + "'.\n\n" + RegistroErrores.Describir(ex),
                    "Pixel Escape - Cargar partida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MostrarErrorContinuacion("No se pudo cargar la partida. Revisa el mensaje.");
            }
        }

        /// <summary>Pide un alias para iniciar o continuar una partida.</summary>
        void ComenzarNuevaPartida()
        {
            MostrarPantallaAlias(false);
        }

        void SolicitarContinuacion()
        {
            MostrarPantallaAlias(true);
        }

        /// <summary>Comienza una partida nueva con el alias confirmado en la pantalla.</summary>
        void IniciarPartidaConAlias(string alias)
        {
            // Se comprueba la base de datos al empezar: mejor avisar ahora que perder la puntuación al terminar.
            try
            {
                repositorio.InicializarBaseDatos();
            }
            catch (Exception ex) when (EsErrorDePersistencia(ex))
            {
                RegistroErrores.Registrar("Comprobar la base de datos al iniciar una partida", ex);
                DialogResult respuesta = MessageBox.Show(this,
                    RegistroErrores.Describir(ex) +
                    "\n\nSi continúas, no se guardará tu progreso ni tu puntuación.\n¿Quieres jugar de todas formas?",
                    "Pixel Escape - Base de datos no disponible", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (respuesta != DialogResult.Yes)
                {
                    MostrarErrorContinuacion("No se pudo acceder a la base de datos.");
                    return;
                }
            }
            nombreJugador = alias;
            ReiniciarPartidaCompleta();
            MostrarPantallaControles();
        }

        void ConfigurarInterfaz()
        {
            lblVidas.AutoSize = true;
            lblVidas.Font = new Font("Bradley Hand ITC", 22, FontStyle.Bold);
            lblVidas.ForeColor = Color.Crimson;
            lblVidas.BackColor = Color.Transparent;
            lblVidas.Padding = new Padding(2);
            lblVidas.Location = new Point(12, 8);
            lblVidas.Text = "♥ ♥ ♥";
            Visor.Controls.Add(lblVidas);

            // Contador de monedas
            lblMonedas.AutoSize = true;
            lblMonedas.Font = new Font("Bradley Hand ITC", 16, FontStyle.Bold);
            lblMonedas.ForeColor = Color.Gold;
            lblMonedas.BackColor = Color.Transparent;
            lblMonedas.Padding = new Padding(2);
            lblMonedas.Location = new Point(12, 48);
            lblMonedas.Text = "Coins: 0 / 20";
            Visor.Controls.Add(lblMonedas);

            lblPowerUps.AutoSize = false;
            lblPowerUps.Size = new Size(260, 58);
            lblPowerUps.Font = new Font("Bradley Hand ITC", 10, FontStyle.Bold);
            lblPowerUps.ForeColor = Color.White;
            lblPowerUps.BackColor = Color.FromArgb(150, 25, 35, 45);
            lblPowerUps.Padding = new Padding(8, 5, 8, 5);
            lblPowerUps.TextAlign = ContentAlignment.MiddleLeft;
            lblPowerUps.Location = new Point(14, 92);
            lblPowerUps.Visible = false;
            Visor.Controls.Add(lblPowerUps);

            // Panel amplio
            panelEstado.Size = new Size(520, 320);
            panelEstado.BackColor = ColorFondoPantalla;
            panelEstado.BorderStyle = BorderStyle.FixedSingle;
            panelEstado.Location = new Point((ClientSize.Width - panelEstado.Width) / 2,
                                             (ClientSize.Height - panelEstado.Height) / 2);
            panelEstado.Visible = false;

            lblTituloEstado.Dock = DockStyle.Top;
            lblTituloEstado.Height = 66;
            lblTituloEstado.TextAlign = ContentAlignment.MiddleCenter;
            lblTituloEstado.ForeColor = ColorTituloPantalla;
            lblTituloEstado.Font = new Font("Ravie", 24, FontStyle.Regular);
            lblTituloEstado.Padding = new Padding(12, 8, 12, 2);

            lblMensajeEstado.Dock = DockStyle.Top;
            lblMensajeEstado.Height = 65;
            lblMensajeEstado.TextAlign = ContentAlignment.MiddleCenter;
            lblMensajeEstado.ForeColor = ColorTextoPantalla;
            lblMensajeEstado.Font = FuenteTextoPantalla;
            lblMensajeEstado.Padding = new Padding(24, 4, 24, 4);
            lblMensajeEstado.AutoEllipsis = true;

            btnEstado.Text = "Volver a jugar";
            btnEstado.Size = new Size(260, 44);
            btnEstado.Font = FuenteBotonPantalla;
            btnEstado.Location = new Point((panelEstado.Width - btnEstado.Width) / 2, 138);
            btnEstado.BackColor = ColorBotonPantalla;
            btnEstado.ForeColor = Color.Black;
            btnEstado.FlatStyle = FlatStyle.Flat;
            btnEstado.FlatAppearance.BorderSize = 0;
            btnEstado.FlatAppearance.MouseOverBackColor = ColorBotonHover;
            btnEstado.FlatAppearance.MouseDownBackColor = ColorBotonPresionado;
            btnEstado.Click += Proteger("el botón del panel de estado", (s, e) =>
            {
                if (pausado) BtnReanudar_Click(s, e);
                else { audio.Efecto("button"); ReiniciarPartidaCompleta(); }
            });

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
            Color color = monedasRecogidas >= meta ? Color.LimeGreen : Color.Gold;
            if (colorMonedasIndicador != color)
            {
                colorMonedasIndicador = color;
                lblMonedas.ForeColor = color;
            }
        }

        void ActualizarIndicadorVidas()
        {
            if (jugador == null) return;
            int vidas = Math.Max(0, Math.Min(3, jugador.Vida));
            if (vidas == vidasIndicador) return;
            vidasIndicador = vidas;
            lblVidas.Text = vidas == 0 ? "" : string.Join(" ", Enumerable.Repeat("♥", vidas));
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
            btnClasificacionFin.Visible = juegoTerminado; // solo al ganar o perder, no en pausa
            btnVolverInicio.Visible = juegoTerminado;     // en pausa lo activa AlternarPausa
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
            relojSalto.Reset();
            audio.DetenerMusica();
            audio.Efecto("victory");
            RegistrarResultadoFinal("victoria");
            MostrarPanelEstado("¡VICTORIA!", "¡Felicitaciones! Completaste los tres niveles de Pixel Escape.", "Jugar nuevamente");
        }

        void ReiniciarPartidaCompleta()
        {
            audio.DetenerMusica();
            timerTransicion.Stop();
            timerRespawn.Stop();
            respawnPendiente = false;
            bucleJuego.Stop();
            izquierda = derecha = salto = false;
            pausado = false;
            juegoTerminado = false;
            transicionEnCurso = false;
            panelEstado.Visible = false;
            btnGuardar.Visible = false;
            btnVolverInicio.Visible = false;
            jugador.ReiniciarPartida();
            tiempoPartidaSegundos = 0;
            monedasTotales = 0;
            resultadoRegistrado = false;
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
            MostrarPantallaNivel(1); // vuelve a mostrar "NIVEL 1"; el bucle arranca al pulsar COMENZAR
        }

        void AlternarPausa()
        {
            if (juegoTerminado || transicionEnCurso) return;
            pausado = !pausado;
            izquierda = derecha = salto = false;
            if (pausado)
            {
                bucleJuego.Stop();
                audio.PausarMusica();
                relojSalto.Reset();
                MostrarPanelEstado("JUEGO EN PAUSA", "Pulsa P o el botón para continuar.", "Reanudar");
                btnGuardar.Visible = true;
                btnVolverInicio.Visible = true;
                btnGuardar.BringToFront();
                btnVolverInicio.BringToFront();
            }
            else
            {
                panelEstado.Visible = false;
                btnGuardar.Visible = false;
            btnVolverInicio.Visible = false;
                relojSalto.Restart();
                audio.ReanudarMusica();
                bucleJuego.Start();
            }
        }

        void BtnReanudar_Click(object? sender, EventArgs e)
        {
            audio.Efecto("button");
            audio.ReanudarMusica();
            pausado = false;
            panelEstado.Visible = false;
            btnGuardar.Visible = false;
            btnVolverInicio.Visible = false;
            relojSalto.Restart();
                bucleJuego.Start();
            Visor.Focus();
        }

        const int AjusteSueloNivel2 = 6;
        int AjusteSuelo => nivelActual == 2 ? AjusteSueloNivel2 : 0;


        // Posición X del punto de control de cada nivel
        readonly int[] xPuntoControl = { 1060, 1400, 1800 };

        // Culling visual: los objetos fuera del área de cámara (con margen) no se dibujan.
        // No se eliminan del escenario ni se altera su posición o colisión.
        const int MargenVisibilidad = 128;

        bool EnZonaVisible(int x, int y, float ancho, float alto)
        {
            float izquierda = scene.CamaraX - MargenVisibilidad;
            float derecha = scene.CamaraX + scene.Width + MargenVisibilidad;
            float arriba = -MargenVisibilidad;
            float abajo = scene.Height + MargenVisibilidad;
            return x + ancho >= izquierda && x <= derecha && y + alto >= arriba && y <= abajo;
        }

        void ActualizarVisibilidadCamara()
        {
            foreach (var item in decoraciones)
            {
                bool visible = EnZonaVisible(item.X, item.Y, item.Ancho, item.Alto);
                if (item.Visible != visible) item.Visible = visible;
            }
            foreach (var item in obstaculos)
            {
                bool visible = EnZonaVisible(item.X, item.Y, item.Ancho, item.Alto);
                if (item.Visible != visible) item.Visible = visible;
            }
            foreach (var item in obstaculosSprite)
            {
                bool visible = EnZonaVisible(item.X, item.Y, item.Ancho, item.Alto);
                if (item.Visible != visible) item.Visible = visible;
            }
            foreach (var item in coleccionables)
            {
                bool visible = !item.Recogido && EnZonaVisible(item.X, item.Y, item.Ancho, item.Alto);
                if (item.Visible != visible) item.Visible = visible;
            }
            foreach (var item in trampas)
            {
                bool visible = EnZonaVisible(item.X, item.Y, item.Ancho, item.Alto);
                if (item.Visible != visible) item.Visible = visible;
            }
            foreach (var item in cazadores)
            {
                bool visible = EnZonaVisible(item.X, item.Y, item.Ancho, item.Alto);
                if (item.Visible != visible) item.Visible = visible;
            }
            foreach (var item in guardianes)
            {
                bool visible = EnZonaVisible(item.X, item.Y, item.Ancho, item.Alto);
                if (item.Visible != visible) item.Visible = visible;
            }
            foreach (var item in flechasCazador)
            {
                bool visible = EnZonaVisible(item.X, item.Y, item.Ancho, item.Alto);
                if (item.Visible != visible) item.Visible = visible;
            }
            if (puerta != null)
            {
                bool visible = EnZonaVisible(puerta.X, puerta.Y, puerta.Ancho, puerta.Alto);
                if (puerta.Visible != visible) puerta.Visible = visible;
            }
            if (puntoControl != null)
            {
                bool visible = EnZonaVisible(puntoControl.X, puntoControl.Y, puntoControl.Ancho, puntoControl.Alto);
                if (puntoControl.Visible != visible) puntoControl.Visible = visible;
            }
        }

        void ActualizarPatrullas(double deltaTime)
        {
            foreach (var entrada in limitesPatrulla)
            {
                Enemigo enemigo = entrada.Key;
                if (!enemigo.EstaVivo || !EnZonaVisible(enemigo.X, enemigo.Y, enemigo.Ancho, enemigo.Alto)) continue;
                if (enemigo is CazadorPixel cazador && ActualizarCazador(cazador, deltaTime)) continue;
                int minimo = entrada.Value.Min;
                int maximo = entrada.Value.Max;
                if (enemigo.X <= minimo) enemigo.CambiarDireccionPatrulla(1);
                else if (enemigo.X >= maximo) enemigo.CambiarDireccionPatrulla(-1);
                if (enemigo.DireccionActual < 0) enemigo.MoverIzq();
                else enemigo.MoverDer();
                enemigo.Animar(Enemigo.EstadoAnimacion.Camina);
            }
        }

        // El cazador deja de patrullar al detectar al jugador y dispara una flecha a intervalos.
        // Devuelve true mientras está en modo de ataque/alerta para no aplicar patrulla ese frame.
        bool ActualizarCazador(CazadorPixel cazador, double deltaTime)
        {
            cazador.ActualizarTemporizadores(deltaTime);
            int deltaX = jugador.X - cazador.X;
            bool detectado = cazador.Detecta(jugador);
            if (!detectado) return false;

            cazador.CambiarDireccionPatrulla(deltaX < 0 ? -1 : 1);
            if (cazador.EstaEnAnimacionAtaque)
            {
                cazador.Animar(Enemigo.EstadoAnimacion.Ataca);
                return true;
            }

            if (cazador.PuedeDisparar)
            {
                cazador.IniciarAtaque();
                CrearFlechaCazador(cazador);
            }
            else
            {
                cazador.Animar(Enemigo.EstadoAnimacion.Quieto);
            }
            return true;
        }

        void CrearFlechaCazador(CazadorPixel cazador)
        {
            int direccion = cazador.DireccionActual;
            int x = direccion > 0 ? cazador.X + 70 : cazador.X + 8;
            int y = cazador.Y + 45;
            FlechaCazador flecha = new FlechaCazador(x, y, direccion);
            flechasCazador.Add(flecha);
            scene.AddItem(flecha);
        }

        void ActualizarFlechasCazador(double deltaTime)
        {
            double dt = Math.Clamp(deltaTime, 0, 0.05);
            List<FlechaCazador> eliminar = new List<FlechaCazador>();
            bool impactoConDano = false;
            foreach (FlechaCazador flecha in flechasCazador)
            {
                flecha.Actualizar(dt);
                if (flecha.Expired || flecha.X < -80 || flecha.X > AnchoNivel + 80)
                {
                    eliminar.Add(flecha);
                    continue;
                }

                if (flecha.RectanguloColision.IntersectsWith(CuerpoJugador()))
                {
                    int vidasAntes = jugador.Vida;
                    jugador.RecibirDano(1);
                    eliminar.Add(flecha);
                    if (jugador.Vida < vidasAntes)
                    {
                        evitoCazador = false;
                        impactoConDano = true;
                        break;
                    }
                }
            }
            foreach (FlechaCazador flecha in eliminar)
            {
                scene.RemoveItem(flecha);
                flecha.Visible = false;
                flechasCazador.Remove(flecha);
            }
            if (impactoConDano) ResolverImpacto();
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

        void PasoDeJuego(double deltaTime)
        {
            // No continuar mientras se cambia de nivel
            if (transicionEnCurso || juegoTerminado || pausado)
                return;

            if (respawnPendiente)
            {
                jugador.Aterrizar(deltaTime); // mantiene la animación de daño mientras espera la reaparición
                return;
            }

            ActualizarIndicadorVidas();

            // Los enemigos patrullan continuamente dentro de su zona.
            ActualizarPatrullas(deltaTime);

            // Impulso y movimiento horizontal usan el mismo tiempo real.
            if (salto)
            {
                if (!saltoPresionadoAnterior) audio.Efecto("jump");
                jugador.Saltar();
            }
            saltoPresionadoAnterior = salto;

            int direccionJugador = derecha ? 1 : izquierda ? -1 : 0;
            jugador.MoverHorizontal(direccionJugador, deltaTime);
            jugador.Aterrizar(deltaTime);

            // La cámara sigue al jugador y este no se sale de los bordes del nivel
            LimitarJugadorAlNivel();
            ActualizarCamara();
            ActualizarVisibilidadCamara();
            ActualizarFlechasCazador(deltaTime);
            if (respawnPendiente || juegoTerminado) return;

            // Recoger monedas y otros coleccionables
            foreach (Coleccionables item in coleccionables)
            {
                if (item.Recogido || Math.Abs(item.X - jugador.X) > 160 || Math.Abs(item.Y - jugador.Y) > 120) continue;
                Rectangle rectItem = new Rectangle(item.X, item.Y, (int)item.Ancho, (int)item.Alto);
                bool colisionColeccionable = (item is Moneda || item is Llave)
                    ? jugador.Cuerpo.IntersectsWith(rectItem)
                    : jugador.ColisionaCon(item);
                if (!item.Recogido && colisionColeccionable)
                {
                    item.Recoger(jugador);
                    if (item is Moneda) audio.Efecto("coin");
                    else audio.Efecto("powerUpKey");

                    if (item is Moneda)
                    {
                        monedasRecogidas++;
                        monedasTotales++;
                    }
                }
            }
            ActualizarContadorMonedas();

            // El contacto con cualquier enemigo descuenta una vida.
            Rectangle cuerpoJugador = CuerpoJugador();
            foreach (CazadorPixel c in cazadores)
            {
                if (Math.Abs(c.X - jugador.X) > 150 || Math.Abs(c.Y - jugador.Y) > 120) continue;
                if (cuerpoJugador.IntersectsWith(CuerpoCazador(c)))
                {
                    int vidasAntes = jugador.Vida;
                    jugador.RecibirDano(1);
                    if (jugador.Vida < vidasAntes) { evitoCazador = false; ResolverImpacto(); return; }
                }
            }
            foreach (GuardianPixel g in guardianes)
            {
                if (Math.Abs(g.X - jugador.X) > 150 || Math.Abs(g.Y - jugador.Y) > 120) continue;
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
            jugador.SincronizarPosicionHorizontal();
        }

        // Centra la cámara en el jugador, sin mostrar nada fuera del nivel
        void ActualizarCamara()
        {
            Rectangle cuerpo = jugador.Cuerpo;
            float centroJugador = cuerpo.X + cuerpo.Width / 2f;

            float maxCamara = Math.Max(0f, AnchoNivel - scene.Width);
            float camara = Math.Clamp(centroJugador - scene.Width / 2f, 0f, maxCamara);

            scene.CamaraX = (float)Math.Round(camara); // entero: evita líneas finas entre bloques
        }

        // Rectángulos del cuerpo real de cada personaje
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
        bool puntoControlGuardado;
        int xRespawn;
        readonly List<int> indicesRecogidos = new List<int>();
        int puntajeCheckpoint;

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


        //Método para cargar un nuevo nivel
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

            // Quitar los objetos del nivel anterior
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
            foreach (FlechaCazador flecha in flechasCazador)
            {
                flecha.Visible = false;
                scene.RemoveItem(flecha);
            }
            flechasCazador.Clear();

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

            // Terreno
            new DisenadorNiveles(scene, nivelActual, AnchoNivel, ClientSize.Height, Suelo,
                jugador.Plataformas, cazadores, guardianes, trampas, limitesPatrulla,
                coleccionables, obstaculos, decoraciones, obstaculosSprite).Construir();

            // Reiniciar al jugador
            int xInicio = desdePuntoControl ? xRespawn : 100;
            jugador.ReiniciarEstado(xInicio, Suelo - 88);
            jugador.Visible = true;
            if (desdePuntoControl)
            {
                RestaurarProgresoGuardado();
            }

            // La cámara vuelve al inicio del nivel
            ActualizarCamara();
            ActualizarVisibilidadCamara();

            // Puerta cerca del final del nivel
            int xPuerta = AnchoNivel - 140;
            puerta = new Puerta(xPuerta, 0);
            puerta.SetPos(xPuerta, Suelo - (int)puerta.Alto + 11 + AjusteSuelo);
            puerta.ZValue = -1;
            scene.AddItem(puerta);

            // Punto de control a mitad del nivel
            int xControl = xPuntoControl[nivelActual - 1];
            puntoControl = new PuntoControl(xControl, 0);
            puntoControl.SetPos(xControl, Suelo - (int)puntoControl.Alto + 13 + AjusteSuelo);
            puntoControl.ZValue = -1;
            scene.AddItem(puntoControl);

            if (desdePuntoControl)
            {
                puntoControl.Activar(jugador);
            }

            ActualizarContadorMonedas();
        }

        // Guarda dónde reaparecer (en la bandera) y qué monedas/llave llevaba el jugador.
        void GuardarPuntoControl()
        {
            puntoControlGuardado = true;
            xRespawn = puntoControl.X - 16;

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

        // Derrota
        void ResolverImpacto()
        {
            izquierda = derecha = salto = false;
            audio.Efecto("die");
            ActualizarIndicadorVidas();

            if (!jugador.EstaVivo)
            {
                // La partida queda detenida hasta que el usuario pulse el botón de reinicio.
                juegoTerminado = true;
                bucleJuego.Stop();
                audio.DetenerMusica();
                audio.Efecto("gameOver");
                RegistrarResultadoFinal("derrota");
                MostrarPanelEstado("GAME OVER", "Has perdido tus tres vidas.", "Volver a jugar");
            }
            else
            {
                // Se conserva visible la reacción de daño antes de reaparecer.
                respawnPendiente = true;
                relojSalto.Restart();
                bucleJuego.Start();
                timerRespawn.Stop();
                timerRespawn.Start();
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
                    audio.DetenerMusica();
                    audio.Efecto("door");

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

                        relojSalto.Restart();
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