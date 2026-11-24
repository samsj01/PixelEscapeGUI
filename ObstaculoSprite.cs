using System.Drawing;

namespace PixelEsGUI
{
    // Obstáculo con imagen individual y una zona sólida registrada por Escenario en Plataformas.
    internal class ObstaculoSprite : ObjetoInteractivo
    {
        public Rectangle Colision { get; }

        public ObstaculoSprite(int x, int y, string recurso, int ancho, int alto, Rectangle colision)
            : base(x, y, recurso, 1, 1, 1f)
        {
            FijarTamano(ancho, alto);
            EstablecerRangoFrames(0, 0, false);
            Colision = colision;
        }
    }
}
