using System.Runtime.CompilerServices;

namespace PixelEsGUI
{
    internal static class Program
    {
        // Control de avalancha: si los errores se repiten sin parar se cierra de forma ordenada.
        const int MaxErroresSeguidos = 5;
        static readonly TimeSpan VentanaErrores = TimeSpan.FromSeconds(10);
        static int erroresSeguidos;
        static DateTime inicioVentana = DateTime.MinValue;
        static bool mostrandoAviso;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();

                // Red de seguridad: las excepciones que no trate el propio juego no lo cierran.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (_, e) => ManejarErrorDeInterfaz(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                    ManejarErrorFatal(e.ExceptionObject as Exception
                        ?? new InvalidOperationException("Error no controlado: " + e.ExceptionObject));
                TaskScheduler.UnobservedTaskException += (_, e) =>
                {
                    RegistroErrores.Registrar("Tarea en segundo plano con error", e.Exception);
                    e.SetObserved();
                };

                EjecutarJuego();
            }
            catch (Exception ex)
            {
                // El juego no pudo ni arrancar (p. ej. falta Engine2D.dll o un recurso esencial).
                ManejarErrorFatal(ex);
                Environment.ExitCode = 1;
            }
        }

        // Método aparte y sin inlining: si falta una librería (p. ej. Engine2D.dll), el error se produce
        // al ejecutar esto, dentro del try de Main, y no al compilar Main justo antes de entrar en él.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void EjecutarJuego() => Application.Run(new Escenario(new Persistencia.RepositorioJuegoSqlite()));

        /// <summary>Error en un evento de la interfaz que nadie trató: se informa y el juego continúa.</summary>
        static void ManejarErrorDeInterfaz(Exception ex)
        {
            RegistroErrores.Registrar("Error no controlado en la interfaz", ex);

            DateTime ahora = DateTime.UtcNow;
            if (ahora - inicioVentana > VentanaErrores) { inicioVentana = ahora; erroresSeguidos = 0; }
            erroresSeguidos++;

            if (erroresSeguidos > MaxErroresSeguidos)
            {
                MostrarAviso("Pixel Escape - Demasiados errores",
                    "Se produjeron demasiados errores seguidos, así que el juego se cerrará para evitar problemas.\n\n" +
                    "Último error: " + RegistroErrores.Describir(ex) + "\n\nDetalles técnicos: " + RegistroErrores.RutaArchivo,
                    MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            MostrarAviso("Pixel Escape - Error inesperado",
                "Ocurrió un error inesperado.\n\n" + RegistroErrores.Describir(ex) +
                "\n\nEl juego intentará continuar. Si el problema se repite, reinícialo.\n\n" +
                "Detalles técnicos: " + RegistroErrores.RutaArchivo,
                MessageBoxIcon.Warning);
        }

        /// <summary>Error que no se puede recuperar (arranque fallido o hilo secundario): se informa antes de terminar.</summary>
        static void ManejarErrorFatal(Exception ex)
        {
            RegistroErrores.Registrar("Error fatal", ex);
            string causa = ex switch
            {
                FileNotFoundException or DllNotFoundException or BadImageFormatException =>
                    RegistroErrores.Describir(ex) +
                    "\nVerifica que todos los archivos del juego (como Engine2D.dll) estén junto al ejecutable.",
                _ => RegistroErrores.Describir(ex)
            };
            MostrarAviso("Pixel Escape - No se pudo continuar",
                "Pixel Escape tuvo un error grave y debe cerrarse.\n\n" + causa +
                "\n\nDetalles técnicos: " + RegistroErrores.RutaArchivo,
                MessageBoxIcon.Error);
        }

        static void MostrarAviso(string titulo, string mensaje, MessageBoxIcon icono)
        {
            if (mostrandoAviso) return; // un solo aviso a la vez
            mostrandoAviso = true;
            try
            {
                MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, icono);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // Sin posibilidad de mostrar ventanas, el detalle ya quedó en el registro.
                RegistroErrores.Registrar("No se pudo mostrar el aviso de error", ex);
            }
            finally
            {
                mostrandoAviso = false;
            }
        }
    }
}
