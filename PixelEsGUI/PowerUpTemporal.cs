namespace PixelEscape
{
    // Base común de los power-ups que duran un tiempo.
    internal abstract class PowerUpTemporal : Coleccionables
    {
        public int Duracion { get; }

        protected PowerUpTemporal(int x, int y, int duracion) : base(x, y)
        {
            Duracion = duracion;
        }
    }
}
