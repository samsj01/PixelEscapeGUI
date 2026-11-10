namespace PixelEscape
{
    // Enemigo básico: velocidad moderada, patrulla y ataca al contacto.
    internal class GuardianPixel : Personaje
    {
        const int VelocidadGuardian = 1;

        public GuardianPixel(int x, int y) : base(x, y)
        {
            Velocidad = VelocidadGuardian;
            Dano = 1;
        }

        public GuardianPixel() : this(0, 0)
        {
        }
    }
}
