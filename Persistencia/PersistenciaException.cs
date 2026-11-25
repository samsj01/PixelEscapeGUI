namespace PixelEsGUI.Persistencia
{
    /// <summary>
    /// Error al leer o escribir los datos del juego (guardados y clasificación).
    /// El mensaje ya está redactado para el jugador; la causa técnica queda en InnerException.
    /// </summary>
    internal sealed class PersistenciaException : Exception
    {
        /// <summary>Inicializa PersistenciaException con los datos proporcionados.</summary>
        /// <param name="mensaje">Mensaje de error para el usuario.</param>
        /// <param name="causa">Excepción técnica original, si existe.</param>
        public PersistenciaException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
    }
}
