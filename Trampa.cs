namespace PixelEsGUI
{
    // Pinchos estáticos: recurso individual "Pinchos". Daña una vez al entrar en su zona
    // cuando el jugador está apoyado en el suelo; saltarlos no activa el daño.
    internal class Trampa : ObjetoActivable
    {
        const float TamanoSprite = 100f;
        bool jugadorEncima;
        public int Dano { get; }

        public Trampa(int x, int y, int dano)
            : base(x, y, "Pinchos", 1, 1, 1f)
        {
            Dano = dano;
            FijarTamano(TamanoSprite, TamanoSprite);
            EstablecerRangoFrames(0, 0, false);
        }

        public override bool Activar(Jugador jugador)
        {
            // El PNG tiene margen transparente. La zona activa se aproxima al ancho visible.
            float centroJugador = jugador.X + jugador.Ancho / 2f;
            bool pisando = jugador.EnElSuelo && centroJugador >= X + 7 && centroJugador <= X + 93;
            if (pisando && !jugadorEncima)
                jugador.RecibirDano(Dano);

            jugadorEncima = pisando;
            Activo = pisando;
            return Activo;
        }
    }
}
