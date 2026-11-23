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
    public float VolumenMusica { get; set; } = 0.35f;
    public float VolumenEfectos { get; set; } = 0.75f;

    private static Stream Cargar(string nombre)
    {
        using Stream origen = Resources.ResourceManager.GetStream(nombre)
            ?? throw new InvalidOperationException("Recurso de audio no encontrado: " + nombre);
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
        catch
        {
            DetenerMusica();
            // El juego debe poder ejecutarse aunque no haya dispositivo de audio.
        }
    }

    public void PausarMusica()
    {
        if (!cerrado && salidaMusica?.PlaybackState == PlaybackState.Playing)
            salidaMusica.Pause();
    }

    public void ReanudarMusica()
    {
        if (!cerrado && salidaMusica?.PlaybackState == PlaybackState.Paused)
            salidaMusica.Play();
    }

    public void DetenerMusica()
    {
        salidaMusica?.Stop();
        salidaMusica?.Dispose();
        salidaMusica = null;
        lectorMusica?.Dispose();
        lectorMusica = null;
        datosMusica?.Dispose();
        datosMusica = null;
    }

    public void Efecto(string nombre)
    {
        if (cerrado) return;
        try
        {
            var datos = Cargar(nombre);
            var lector = new Mp3FileReader(datos);
            var salida = new WaveOutEvent { Volume = VolumenEfectos };
            salida.Init(lector);
            var efecto = (Salida: salida, Lector: lector, Datos: datos);
            efectos.Add(efecto);
            salida.PlaybackStopped += (_, _) =>
            {
                if (efectos.Remove(efecto))
                {
                    salida.Dispose();
                    lector.Dispose();
                    datos.Dispose();
                }
            };
            salida.Play();
        }
        catch
        {
            // Un efecto ausente o un dispositivo sin audio no detiene el juego.
        }
    }

    public void Dispose()
    {
        if (cerrado) return;
        cerrado = true;
        DetenerMusica();
        foreach (var (salida, lector, datos) in efectos.ToArray())
        {
            salida.Stop();
            salida.Dispose();
            lector.Dispose();
            datos.Dispose();
        }
        efectos.Clear();
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
