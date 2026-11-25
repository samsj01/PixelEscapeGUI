using System.Drawing;

namespace PixelEsGUI
{
    // Proyectil del cazador: se mueve horizontalmente y desaparece al impactar o expirar.
    /// <summary>Proyectil del cazador con desplazamiento y tiempo de vida limitados.</summary>
    internal sealed class FlechaCazador : ObjetoInteractivo
    {
        const float VelocidadFlecha = 460f;
        const double DuracionMaxima = 3.2;
        readonly int direccion;
        double tiempoRestante = DuracionMaxima;

        public bool Expired => tiempoRestante <= 0;
        public Rectangle RectanguloColision => new Rectangle(X + 3, Y + 2, 36, 7);

        /// <summary>Inicializa FlechaCazador con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="direccion">Dirección horizontal del movimiento.</param>
        public FlechaCazador(int x, int y, int direccion)
            : base(x, y, direccion < 0 ? "FlechaCazadorIzquierda" : "FlechaCazador", 1, 1, 1f)
        {
            this.direccion = direccion < 0 ? -1 : 1;
            FijarTamano(42, 11);
            EstablecerRangoFrames(0, 0, false);
        }

        /// <summary>Avanza el desplazamiento y el tiempo de vida del proyectil.</summary>
        /// <param name="deltaTime">Tiempo transcurrido en segundos.</param>
        public void Actualizar(double deltaTime)
        {
            double dt = Math.Clamp(deltaTime, 0, 0.05);
            tiempoRestante -= dt;
            SetPos(X + (int)Math.Round(direccion * VelocidadFlecha * dt), Y);
        }
    }
}
