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

            CrearObjetos();

            jugador = new Jugador(100, 100);
            scene.AddItem(jugador);

            guardian = new GuardianPixel(280, 120);
            scene.AddItem(guardian);

            cazador = new CazadorPixel(550, 120);
            scene.AddItem(cazador);

            // Teclado: el formulario recibe las teclas antes que el visor.
            KeyPreview = true;
            KeyDown += (s, e) => CambiarTecla(e.KeyCode, true);
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
            AgregarColeccionable(new Moneda(200, 0), 200);
            AgregarColeccionable(new Llave(255, 0), 255);
            AgregarColeccionable(new PowerUpEscudo(400, 0, 10), 400);
            AgregarColeccionable(new PowerUpVelocidad(450, 0, 10), 450);
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
            // Si se presionan ambas direcciones, gana la derecha.
            if (derecha)
            {
                jugador.MoverDer();
            }
            else if (izquierda)
            {
                jugador.MoverIzq();
            }

            if (salto)
            {
                jugador.Saltar();
            }

            jugador.Aterrizar();

            // Interacción con los objetos al tocarlos.
            foreach (Coleccionables item in coleccionables)
            {
                if (!item.Recogido && jugador.ColisionaCon(item))
                {
                    item.Recoger(jugador);
                }
            }
            if (jugador.ColisionaCon(puntoControl))
            {
                puntoControl.Activar(jugador);
            }
            if (jugador.ColisionaCon(puerta))
            {
                puerta.Abrir(jugador); // solo se abre si ya recogió la llave
            }
            trampa.Activar(jugador);
        }
    }
}
