namespace PixelEsGUI
{
    // Clase concreta (no abstracta): se pueden crear trampas con new Trampa(...).
    // Causa daño al jugador (una vez por pasada) cuando pisa su zona; saltándola no hace daño.
    internal class Trampa : ObjetoActivable
    {
        // ---------- Hoja de sprites (Resources/2.png, recurso "Trampa") ----------
        // 192x32: una sola fila de 6 cuadros de 32x32. Las púas salen y se esconden:
        //   cuadro 0: en reposo (apenas asoman)  ->  cuadros 1 a 3: salen  ->  cuadros 4 y 5: se esconden
        const int ColumnasHoja = 6;
        const int FilasHoja = 1;
        const int CuadroReposo = 0;
        const int UltimoCuadro = ColumnasHoja - 1;
        const float Fps = 12f;
        const float TamanoPantalla = 64f; // 2x el tamaño original (32x32)

        bool reproduciendo; // true mientras corre la animación de las púas
        bool jugadorEncima; // true mientras el jugador sigue dentro de la zona de la trampa

        public int Dano { get; }

        public Trampa(int x, int y, int dano) : base(x, y, "Trampa", ColumnasHoja, FilasHoja, Fps)
        {
            Dano = dano;
            FijarTamano(TamanoPantalla, TamanoPantalla);
            EstablecerRangoFrames(CuadroReposo, CuadroReposo, false);
        }

        public override bool Activar(Jugador jugador)
        {
            // El jugador "pisa" la trampa si su centro queda dentro del ancho de la trampa
            // y está en el suelo (si salta por encima, no la activa).
            float centroJugador = jugador.X + jugador.Ancho / 2f;
            bool pisando = jugador.EnElSuelo && centroJugador >= X && centroJugador <= X + Ancho;

            // El daño y las púas se disparan solo al ENTRAR en la zona, no en cada fotograma.
            if (pisando && !jugadorEncima)
            {
                jugador.RecibirDano(Dano);
                if (!reproduciendo)
                {
                    reproduciendo = true;
                    EstablecerRangoFrames(CuadroReposo, UltimoCuadro, false); // las púas salen, una vez
                }
            }
            jugadorEncima = pisando;
            Activo = pisando;
            return Activo;
        }

        // Al terminar la animación, la trampa vuelve al cuadro de reposo.
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            if (reproduciendo && !IsPlaying)
            {
                reproduciendo = false;
                EstablecerRangoFrames(CuadroReposo, CuadroReposo, false);
            }
        }
    }
}
