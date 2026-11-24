using Microsoft.Data.Sqlite;
using PixelEsGUI.Persistencia;

namespace PixelEsGUI
{
    // Manejo de errores del formulario principal: protege el bucle de juego, los temporizadores y el
    // teclado para que un fallo inesperado nunca cierre el juego, y devuelve al jugador a un estado seguro.
    public partial class Escenario
    {
        bool manejandoErrorCritico;

        /// <summary>Errores esperables al leer o escribir datos (guardados y clasificación).</summary>
        static bool EsErrorDePersistencia(Exception ex) =>
            ex is PersistenciaException or SqliteException or IOException or UnauthorizedAccessException
               or ArgumentException or InvalidDataException;

        /// <summary>Envuelve un manejador de eventos para que sus excepciones se traten en vez de cerrar el juego.</summary>
        EventHandler Proteger(string contexto, EventHandler accion) => (s, e) =>
        {
            try { accion(s, e); }
            catch (Exception ex) when (ex is not OutOfMemoryException) { ManejarErrorCritico(contexto, ex); }
        };

        /// <summary>Igual que <see cref="Proteger"/> para eventos de teclado.</summary>
        KeyEventHandler ProtegerTecla(string contexto, KeyEventHandler accion) => (s, e) =>
        {
            try { accion(s, e); }
            catch (Exception ex) when (ex is not OutOfMemoryException) { ManejarErrorCritico(contexto, ex); }
        };

        /// <summary>
        /// Error inesperado durante la partida: lo registra, detiene el juego, explica qué ocurrió y
        /// vuelve a la pantalla de inicio (desde allí se puede empezar de nuevo o continuar el último guardado).
        /// </summary>
        void ManejarErrorCritico(string contexto, Exception ex)
        {
            RegistroErrores.Registrar("Error en " + contexto, ex);
            // Si el fallo se repite mientras se muestra el aviso, no se apilan más diálogos.
            if (manejandoErrorCritico) return;
            manejandoErrorCritico = true;
            try
            {
                DetenerJuegoPorError();
                MessageBox.Show(this,
                    "Ocurrió un error en " + contexto + ".\n\n" + RegistroErrores.Describir(ex) +
                    "\n\nLa partida se detuvo y volverás al inicio. Si tenías una partida guardada, " +
                    "podrás recuperarla con CONTINUAR PARTIDA.\n\nDetalles técnicos: " + RegistroErrores.RutaArchivo,
                    "Pixel Escape - Error inesperado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MostrarPantallaInicio();
            }
            catch (Exception secundario) when (secundario is not OutOfMemoryException)
            {
                // Último recurso: se anota y se deja que el jugador cierre la ventana con normalidad.
                RegistroErrores.Registrar("Recuperación tras un error en " + contexto, secundario);
            }
            finally
            {
                manejandoErrorCritico = false;
            }
        }

        /// <summary>Deja el juego en reposo y sin estados a medias (temporizadores, teclas, paneles, música).</summary>
        void DetenerJuegoPorError()
        {
            bucleJuego.Stop();
            timerTransicion.Stop();
            timerRespawn.Stop();
            timerAvisoPowerUp.Stop();
            respawnPendiente = false;
            transicionEnCurso = false;
            pausado = false;
            izquierda = derecha = salto = false;
            relojSalto.Reset();
            panelEstado.Visible = false;
            btnGuardar.Visible = false;
            btnVolverInicio.Visible = false;
            btnClasificacionFin.Visible = false;
            audio.DetenerMusica();
        }

        /// <summary>Aviso no intrusivo (sin diálogo modal) cuando el audio no está disponible.</summary>
        void MostrarAvisoAudio(string mensaje)
        {
            // Puede llegar desde el bucle de juego: un diálogo modal dejaría correr el juego por detrás.
            avisoTemporalPowerUp = "🔇 " + mensaje;
            timerAvisoPowerUp.Stop();
            timerAvisoPowerUp.Start();
        }
    }
}
