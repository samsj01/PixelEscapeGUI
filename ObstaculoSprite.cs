using System.Drawing;

namespace PixelEsGUI
{
    // Obstáculo con imagen individual y una zona sólida registrada por Escenario en Plataformas.
    /// <summary>Obstáculo gráfico con un rectángulo de colisión independiente.</summary>
    internal class ObstaculoSprite : ObjetoInteractivo
    {
        public Rectangle Colision { get; }

        /// <summary>Inicializa ObstaculoSprite con los datos proporcionados.</summary>
        /// <param name="x">Coordenada horizontal inicial o de destino.</param>
        /// <param name="y">Coordenada vertical inicial o de destino.</param>
        /// <param name="recurso">Nombre del recurso gráfico.</param>
        /// <param name="ancho">Ancho gráfico en píxeles.</param>
        /// <param name="alto">Alto gráfico en píxeles.</param>
        /// <param name="colision">Rectángulo de colisión del obstáculo.</param>
        public ObstaculoSprite(int x, int y, string recurso, int ancho, int alto, Rectangle colision)
            : base(x, y, recurso, 1, 1, 1f)
        {
            FijarTamano(ancho, alto);
            EstablecerRangoFrames(0, 0, false);
            Colision = colision;
        }
    }
}
