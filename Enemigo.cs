namespace PixelEsGUI
{
    // Base de los enemigos con hoja de sprites. Hace lo mismo que Jugador.ActualizarAnimacion,
    // pero una sola vez para todos: cada enemigo solo dice QUÉ fila/cuadros usa cada estado.
    internal abstract class Enemigo : Personaje
    {
        // Todas las hojas de enemigos tienen 10 columnas (el polvo de muerte tiene 10 cuadros)
        // y cuadros de 96x96. Las animaciones más cortas dejan vacías las columnas que sobran.
        protected const int ColumnasHoja = 10;

        public enum EstadoAnimacion { Quieto, Camina, Ataca, AtacaFuerte, Defiende, Muere }

        // Fila: primera fila del bloque. PorDireccion: si hay dos filas (izq, der) o una sola.
        protected readonly record struct Animacion(int Fila, int Cuadros, float Fps, bool Repetir, bool PorDireccion);

        EstadoAnimacion? estadoActual;
        int direccionAnimacion;

        protected Enemigo(int x, int y, string nombre, int filas, float fpsInicial)
            : base(x, y, nombre, ColumnasHoja, filas, fpsInicial)
        {
        }

        // Cada enemigo describe sus animaciones (fila, cuántos cuadros, velocidad).
        protected abstract Animacion ObtenerAnimacion(EstadoAnimacion estado);

        // Duración de la animación de muerte en segundos (para quitar al enemigo de la escena después).
        public double DuracionMuerte
        {
            get
            {
                Animacion muerte = ObtenerAnimacion(EstadoAnimacion.Muere);
                return muerte.Cuadros / muerte.Fps;
            }
        }

        // Igual que en Jugador: solo reinicia la animación si cambió el estado o la dirección;
        // si no, el motor sigue avanzando los cuadros solo. Un enemigo muerto no cambia de estado.
        public void Animar(EstadoAnimacion estado)
        {
            if (!EstaVivo && estadoActual == EstadoAnimacion.Muere)
            {
                return;
            }
            if (!EstaVivo)
            {
                estado = EstadoAnimacion.Muere;
            }
            if (estado == estadoActual && Direccion == direccionAnimacion)
            {
                return;
            }
            estadoActual = estado;
            direccionAnimacion = Direccion;

            Animacion anim = ObtenerAnimacion(estado);
            int columnaDireccion = Direccion < 0 ? 0 : 1; // 0 = izquierda, 1 = derecha
            int fila = anim.Fila + (anim.PorDireccion ? columnaDireccion : 0);

            SetFPS(anim.Fps);
            // La fila tiene menos cuadros que columnas: solo se reproducen los que existen.
            // Se pasa 'Repetir' aquí: el parámetro 'loop' de EstablecerRangoFrames vale true por defecto
            // y, si se omite, convertiría ataques y muerte en animaciones infinitas.
            EstablecerRangoFrames(fila * ColumnasHoja, fila * ColumnasHoja + anim.Cuadros - 1, anim.Repetir);
        }

        // Al llegar a 0 de vida pasa solo a la animación de muerte (polvo).
        public override void RecibirDano(int dano)
        {
            base.RecibirDano(dano);
            if (!EstaVivo)
            {
                Animar(EstadoAnimacion.Muere);
            }
        }
    }
}
