namespace PixelEsGUI.Persistencia
{
    /// <summary>Contrato de almacenamiento de partidas y resultados del videojuego.</summary>
    internal interface IRepositorioJuego
    {
        /// <summary>Prepara el almacenamiento para su uso.</summary>
        void InicializarBaseDatos();

        /// <summary>Registra el resultado de una partida finalizada.</summary>
        /// <param name="resultado">Datos del resultado.</param>
        void RegistrarResultado(ResultadoPartida resultado);

        /// <summary>Obtiene la clasificación de jugadores.</summary>
        /// <param name="limite">Cantidad máxima de resultados.</param>
        /// <returns>Resultados ordenados según la clasificación.</returns>
        List<ResultadoPartida> ObtenerRanking(int limite = 100);

        /// <summary>Guarda el estado de una partida.</summary>
        /// <param name="estado">Estado que se desea guardar.</param>
        void GuardarPartida(EstadoPartida estado);

        /// <summary>Recupera el guardado asociado a un jugador.</summary>
        /// <param name="nombreJugador">Alias del jugador.</param>
        /// <returns>Estado guardado o null si no existe.</returns>
        EstadoPartida? CargarPartida(string nombreJugador);

        /// <summary>Elimina el guardado asociado a un jugador.</summary>
        /// <param name="nombreJugador">Alias del jugador.</param>
        void EliminarGuardado(string nombreJugador);
    }
}
