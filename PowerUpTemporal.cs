namespace PixelEsGUI
{
    // Base común de los power-ups que duran un tiempo.
    // Todos comparten el recurso "PowerUps" (Resources/powerups_sheet.png): 2 cuadros de 128x128,
    // sin animación; cada power-up muestra el cuadro que le corresponde.
    internal abstract class PowerUpTemporal : Coleccionables
    {
        const int ColumnasHoja = 2;
        const int FilasHoja = 1;
        const float TamanoPantalla = 44f; // px en pantalla (cuadrado)

        public int Duracion { get; }

        protected PowerUpTemporal(int x, int y, int duracion, int cuadro)
            : base(x, y, "PowerUps", ColumnasHoja, FilasHoja, 1f)
        {
            Duracion = duracion;
            FijarTamano(TamanoPantalla, TamanoPantalla);
            EstablecerRangoFrames(cuadro, cuadro, false); // imagen fija
        }
    }
}
