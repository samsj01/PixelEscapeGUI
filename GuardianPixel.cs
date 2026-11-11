namespace PixelEsGUI
{
    // Enemigo básico: velocidad moderada, patrulla y ataca al contacto.
    internal class GuardianPixel : Enemigo
    {
        const int VelocidadGuardian = 1;

        // ---------- Hoja de sprites (Resources/guardian_sheet.png) ----------
        // 10 columnas y 11 filas de cuadros de 96x96:
        //   fila 0: quieto izq (8)    fila 1: quieto der (8)
        //   fila 2: camina izq (6)    fila 3: camina der (6)
        //   fila 4: ataque 1 izq (4)  fila 5: ataque 1 der (4)
        //   fila 6: ataque 2 izq (4)  fila 7: ataque 2 der (4)
        //   fila 8: guardia izq (6)   fila 9: guardia der (6)
        //   fila 10: muerte / polvo (10), sin dirección
        const int FilasHoja = 11;
        const int FilaQuieto = 0;
        const int FilaCamina = 2;
        const int FilaAtaque1 = 4;
        const int FilaAtaque2 = 6;
        const int FilaGuardia = 8;
        const int FilaMuerte = 10;
        const float FpsQuieto = 8f;
        const float FpsCamina = 10f;
        const float FpsAtaque = 12f;
        const float FpsGuardia = 8f;
        const float FpsMuerte = 12f;

        public GuardianPixel(int x, int y) : base(x, y, "GuardianPixel", FilasHoja, FpsQuieto)
        {
            Velocidad = VelocidadGuardian;
            Dano = 1;
            Animar(EstadoAnimacion.Quieto);
        }

        public GuardianPixel() : this(0, 0)
        {
        }

        protected override Animacion ObtenerAnimacion(EstadoAnimacion estado)
        {
            switch (estado)
            {
                case EstadoAnimacion.Camina:
                    return new Animacion(FilaCamina, 6, FpsCamina, true, true);
                case EstadoAnimacion.Ataca:
                    return new Animacion(FilaAtaque1, 4, FpsAtaque, false, true);
                case EstadoAnimacion.AtacaFuerte:
                    return new Animacion(FilaAtaque2, 4, FpsAtaque, false, true);
                case EstadoAnimacion.Defiende:
                    return new Animacion(FilaGuardia, 6, FpsGuardia, true, true);
                case EstadoAnimacion.Muere:
                    return new Animacion(FilaMuerte, 10, FpsMuerte, false, false);
                default:
                    return new Animacion(FilaQuieto, 8, FpsQuieto, true, true);
            }
        }
    }
}
