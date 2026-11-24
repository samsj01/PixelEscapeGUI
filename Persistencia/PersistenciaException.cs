namespace PixelEsGUI.Persistencia
{
    /// <summary>
    /// Error al leer o escribir los datos del juego (guardados y clasificación).
    /// El mensaje ya está redactado para el jugador; la causa técnica queda en InnerException.
    /// </summary>
    internal sealed class PersistenciaException : Exception
    {
        public PersistenciaException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
    }
}
