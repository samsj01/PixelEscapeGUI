using Engine2D;

namespace PixelEsGUI
{
    public partial class Escenario : Form
    {
        GraphicsScene scene;
        Jugador jugador;
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

            this.Shown += (s, e) => Visor.Start();
        }
    }
}
