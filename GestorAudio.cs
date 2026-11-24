using NAudio.Wave;
using PixelEsGUI.Properties;

namespace PixelEsGUI;

/// <summary>Reproduce recursos MP3 incrustados sin depender de rutas locales.</summary>
internal sealed class GestorAudio : IDisposable
{
    private readonly List<(WaveOutEvent Salida, Mp3FileReader Lector, Stream Datos)> efectos = new();
    private WaveOutEvent? salidaMusica;
    private Mp3FileReader? lectorMusica;
    private Stream? datosMusica;
    private bool cerrado;
    private bool falloNotificado;

    /// <summary>
    /// Se dispara una sola vez por sesión con un texto corto cuando el audio falla (sin dispositivo,
    /// recurso ausente o archivo dañado). El juego sigue sin sonido; el detalle queda en errores.log.
    /// </summary>
    public event Action<string>? ErrorReproduccion;

    public float VolumenMusica { get; set; } = 0.35f;
    public float VolumenEfectos { get; set; } = 0.75f;

    private static Stream Cargar(string nombre)
    {
        using Stream origen = Resources.ResourceManager.GetStream(nombre)
            ?? throw new InvalidOperationException("Recurso de audio no encontrado: '" + nombre + "'.");
        var memoria = new MemoryStream();
        origen.CopyTo(memoria);
        memoria.Position = 0;
        return memoria;
    }

    public void IniciarMusica()
    {
        if (cerrado) return;
        DetenerMusica();
        try
        {
            datosMusica = Cargar("BackgroundSound");
            lectorMusica = new Mp3FileReader(datosMusica);
            salidaMusica = new WaveOutEvent { Volume = VolumenMusica };
            salidaMusica.Init(new Repetidor(lectorMusica));
            salidaMusica.Play();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // El juego debe poder ejecutarse aunque no haya dispositivo de audio.
            DetenerMusica();
            ReportarFallo("iniciar la música", ex);
        }
    }

    public void PausarMusica()
    {
        try
        {
            if (!cerrado && salidaMusica?.PlaybackState == PlaybackState.Playing)
                salidaMusica.Pause();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ReportarFallo("pausar la música", ex);
        }
    }

    public void ReanudarMusica()
    {
        try
        {
            if (!cerrado && salidaMusica?.PlaybackState == PlaybackState.Paused)
                salidaMusica.Play();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ReportarFallo("reanudar la música", ex);
        }
    }

    public void DetenerMusica()
    {
        // Cada recurso se libera por separado: si uno falla, los demás igualmente se liberan.
        Liberar("detener la salida de audio", () => { salidaMusica?.Stop(); salidaMusica?.Dispose(); });
        salidaMusica = null;
        Liberar("cerrar el lector de música", () => lectorMusica?.Dispose());
        lectorMusica = null;
        Liberar("cerrar los datos de música", () => datosMusica?.Dispose());
        datosMusica = null;
    }

    public void Efecto(string nombre)
    {
        if (cerrado) return;
        Stream? datos = null;
        Mp3FileReader? lector = null;
        WaveOutEvent? salida = null;
        (WaveOutEvent Salida, Mp3FileReader Lector, Stream Datos) efecto = default;
        try
        {
            datos = Cargar(nombre);
            lector = new Mp3FileReader(datos);
            salida = new WaveOutEvent { Volume = VolumenEfectos };
            salida.Init(lector);
            efecto = (Salida: salida, Lector: lector, Datos: datos);
            efectos.Add(efecto);
            var salidaLocal = salida;
            salida.PlaybackStopped += (_, _) =>
            {
                try
                {
                    if (efectos.Remove(efecto))
                    {
                        salidaLocal.Dispose();
                        efecto.Lector.Dispose();
                        efecto.Datos.Dispose();
                    }
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    RegistroErrores.Registrar("Audio: liberar un efecto terminado", ex);
                }
            };
            salida.Play();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Un efecto ausente o un dispositivo sin audio no detiene el juego; se liberan los recursos a medias.
            if (efecto.Salida != null) efectos.Remove(efecto);
            Liberar("liberar el efecto '" + nombre + "'", () => { salida?.Dispose(); lector?.Dispose(); datos?.Dispose(); });
            ReportarFallo("reproducir el efecto '" + nombre + "'", ex);
        }
    }

    public void Dispose()
    {
        if (cerrado) return;
        cerrado = true;
        DetenerMusica();
        foreach (var (salida, lector, datos) in efectos.ToArray())
        {
            Liberar("cerrar un efecto de sonido", () =>
            {
                salida.Stop();
                salida.Dispose();
                lector.Dispose();
                datos.Dispose();
            });
        }
        efectos.Clear();
    }

    /// <summary>Ejecuta una liberación de recursos sin que un fallo impida liberar el resto ni cerrar el juego.</summary>
    private static void Liberar(string descripcion, Action accion)
    {
        try { accion(); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            RegistroErrores.Registrar("Audio: no se pudo " + descripcion, ex);
        }
    }

    /// <summary>Registra el fallo y avisa una sola vez, con un texto corto y específico según la causa.</summary>
    private void ReportarFallo(string operacion, Exception ex)
    {
        RegistroErrores.Registrar("Audio: no se pudo " + operacion, ex);
        if (falloNotificado) return;
        falloNotificado = true;
        try { ErrorReproduccion?.Invoke(DescribirErrorAudio(ex)); }
        catch (Exception manejador) when (manejador is not OutOfMemoryException)
        {
            RegistroErrores.Registrar("Audio: avisar al jugador del fallo", manejador);
        }
    }

    private static string DescribirErrorAudio(Exception ex)
    {
        // MmException (NAudio) indica que Windows no pudo abrir el dispositivo de salida.
        if (ex.GetType().Name == "MmException") return "Sin dispositivo de audio.";
        return ex switch
        {
            InvalidOperationException => "Falta un recurso de audio.",
            InvalidDataException or FormatException => "Archivo de audio dañado.",
            _ => "Error de audio."
        };
    }

    private sealed class Repetidor(WaveStream fuente) : WaveStream
    {
        public override WaveFormat WaveFormat => fuente.WaveFormat;
        public override long Length => fuente.Length;
        public override long Position { get => fuente.Position; set => fuente.Position = value; }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int leidos = fuente.Read(buffer, offset + total, count - total);
                if (leidos == 0)
                {
                    if (fuente.Position == 0) break;
                    fuente.Position = 0;
                    continue;
                }
                total += leidos;
            }
            return total;
        }
    }
}
