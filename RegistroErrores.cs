using System.Diagnostics;
using System.Text;
using PixelEsGUI.Persistencia;

namespace PixelEsGUI
{
    /// <summary>
    /// Registro de errores en disco y textos claros para el jugador.
    /// Registrar() nunca lanza excepciones: un fallo al escribir el log no debe provocar otro error.
    /// </summary>
    internal static class RegistroErrores
    {
        static readonly object candado = new();
        const long TamanoMaximoBytes = 512 * 1024; // al superarlo se reinicia el archivo

        /// <summary>Ruta del archivo errores.log (se muestra al jugador junto a los errores graves).</summary>
        public static string RutaArchivo { get; } = CalcularRuta();

        static string CalcularRuta()
        {
            try
            {
                string carpeta = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(carpeta)) carpeta = Path.GetTempPath();
                return Path.Combine(carpeta, "PixelEscapeGUI", "errores.log");
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or System.Security.SecurityException)
            {
                return Path.Combine(Path.GetTempPath(), "PixelEscapeGUI-errores.log");
            }
        }

        /// <summary>Anota fecha, contexto, tipo, mensaje y pila de la excepción (y de sus causas internas).</summary>
        /// <param name="contexto">Contexto en el que ocurrió el fallo.</param>
        /// <param name="ex">Excepción que se registra o describe.</param>
        public static void Registrar(string contexto, Exception ex)
        {
            Debug.WriteLine($"[Pixel Escape] {contexto}: {ex.GetType().Name}: {ex.Message}");
            try
            {
                lock (candado)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo)!);
                    var archivo = new FileInfo(RutaArchivo);
                    if (archivo.Exists && archivo.Length > TamanoMaximoBytes)
                        archivo.Delete();
                    var texto = new StringBuilder();
                    texto.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {contexto}");
                    texto.AppendLine(ex.ToString()); // incluye la pila y las excepciones internas
                    texto.AppendLine(new string('-', 70));
                    File.AppendAllText(RutaArchivo, texto.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception fallo) when (fallo is IOException or UnauthorizedAccessException
                or System.Security.SecurityException or NotSupportedException or ArgumentException)
            {
                Debug.WriteLine("[Pixel Escape] No se pudo escribir el registro de errores: " + fallo.Message);
            }
        }

        /// <summary>Convierte una excepción en una explicación comprensible para el jugador.</summary>
        /// <param name="ex">Excepción que se registra o describe.</param>
        /// <returns>Descripción del error.</returns>
        public static string Describir(Exception ex) => ex switch
        {
            PersistenciaException or InvalidDataException => ex.Message, // ya redactadas para el jugador
            OutOfMemoryException => "El equipo se quedó sin memoria disponible. Cierra otras aplicaciones e inténtalo de nuevo.",
            FileNotFoundException f => "Falta un archivo necesario para el juego: " + (f.FileName ?? f.Message) + ".",
            DirectoryNotFoundException => "No se encontró una carpeta necesaria para el juego.",
            UnauthorizedAccessException => "El juego no tiene permisos para acceder a un archivo o carpeta.",
            IOException => "Error de lectura o escritura de archivos: " + ex.Message,
            DllNotFoundException or BadImageFormatException =>
                "Falta un componente del sistema o es incompatible con tu equipo: " + ex.Message,
            NullReferenceException => "El juego intentó usar un elemento que no existe (referencia nula).",
            IndexOutOfRangeException or ArgumentOutOfRangeException => "El juego accedió a una posición fuera de rango.",
            ArgumentException => "Se recibió un dato no válido: " + ex.Message,
            InvalidOperationException => "Se intentó una operación no permitida en el estado actual del juego: " + ex.Message,
            DivideByZeroException => "Se produjo una división entre cero.",
            OverflowException => "Un valor numérico superó el límite permitido.",
            _ => $"Error inesperado ({ex.GetType().Name}): {ex.Message}"
        };
    }
}
