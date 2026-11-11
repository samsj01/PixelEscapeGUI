using Engine2D;

namespace PixelEsGUI
{
    public partial class Escenario : Form
    {
        GraphicsScene scene;
        Jugador jugador;

        // Teclas que están presionadas en este momento.
        bool izquierda, derecha, salto;
        readonly System.Windows.Forms.Timer bucleJuego = new System.Windows.Forms.Timer { Interval = 16 };

        public Escenario()
        {
            
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            scene = new GraphicsScene();
            scene.Origin = CoordinateOrigin.TopLeft;
            scene.BackgroundColor = Color.FromArgb(30,30,30);
            scene.SetBackgroundImage("background");
            Visor.Scene = scene;
            Visor.Size = new Size(800, 600);

            jugador = new Jugador(100, 100);
            scene.AddItem(jugador);

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
        }
    }
}
