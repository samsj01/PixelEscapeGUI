using System;
using System.Collections.Generic;

namespace PixelEsGUI.Persistencia
{
    /// <summary>Resultado histórico de una partida finalizada.</summary>
    internal sealed class ResultadoPartida
    {
        public long Id { get; set; }
        public string NombreJugador { get; set; } = "";
        public double TiempoSegundos { get; set; }
        public int Monedas { get; set; }
        public int VidasRestantes { get; set; }
        public int Puntuacion { get; set; }
        public string Resultado { get; set; } = "victoria";
        public DateTime FechaHoraUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Instantánea serializable del estado del juego para continuar una partida.</summary>
    internal sealed class EstadoPartida
    {
        public string NombreJugador { get; set; } = "";
        public int NivelActual { get; set; } = 1;
        public int PosicionX { get; set; }
        public int PosicionY { get; set; }
        public int VidasRestantes { get; set; } = 3;
        public int PuntuacionAcumulada { get; set; }
        public int MonedasNivel { get; set; }
        public int MonedasTotales { get; set; }
        public double TiempoAcumuladoSegundos { get; set; }
        public bool TieneLlave { get; set; }
        public bool TieneEscudo { get; set; }
        public bool TieneVelocidad { get; set; }
        public bool PuntoControlGuardado { get; set; }
        public int PosicionRespawnX { get; set; }
        public int PuntajeCheckpoint { get; set; }
        public bool EvitoCazador { get; set; } = true;
        public bool EvitoGuardian { get; set; } = true;
        public List<int> IndicesColeccionablesRecogidos { get; set; } = new();
        public List<int> IndicesColeccionablesCheckpoint { get; set; } = new();
        public DateTime FechaGuardadoUtc { get; set; } = DateTime.UtcNow;
    }
}
