using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace PixelEsGUI.Persistencia
{
    /// <summary>
    /// Persistencia local SQLite: un guardado por alias e historial ilimitado de resultados.
    /// Todo fallo de acceso a datos se informa como <see cref="PersistenciaException"/> con un mensaje claro;
    /// los datos inválidos se rechazan con ArgumentException y los guardados dañados con InvalidDataException.
    /// </summary>
    internal sealed class RepositorioJuegoSqlite
    {
        readonly string rutaCompleta;
        readonly string cadenaConexion;
        readonly object candadoInicio = new();
        volatile bool inicializada;
        static readonly JsonSerializerOptions opcionesJson = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Prepara el repositorio sin tocar el disco: la base de datos se crea la primera vez que se usa.
        /// Así el juego arranca aunque la carpeta de datos no esté disponible, y el error se informa al usarla.
        /// </summary>
        public RepositorioJuegoSqlite(string? ruta = null)
        {
            rutaCompleta = ResolverRuta(ruta);
            cadenaConexion = new SqliteConnectionStringBuilder
            {
                DataSource = rutaCompleta,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
        }

        static string ResolverRuta(string? ruta)
        {
            try
            {
                ruta ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PixelEscapeGUI", "pixelescape.db");
                return Path.GetFullPath(ruta);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException
                or System.Security.SecurityException)
            {
                // Ruta inválida: se usa la carpeta temporal para no impedir que el juego funcione.
                RegistroErrores.Registrar("Ruta de base de datos inválida; se usa la carpeta temporal", ex);
                return Path.Combine(Path.GetTempPath(), "PixelEscapeGUI", "pixelescape.db");
            }
        }

        // ---------------------------------------------------------------------------------
        //  Acceso a SQLite con traducción de errores
        // ---------------------------------------------------------------------------------

        /// <summary>Abre la conexión, creando antes la base de datos si es la primera vez.</summary>
        SqliteConnection Abrir()
        {
            AsegurarInicializada();
            return AbrirConexion();
        }

        SqliteConnection AbrirConexion()
        {
            var conexion = new SqliteConnection(cadenaConexion);
            try
            {
                conexion.Open();
                return conexion;
            }
            catch
            {
                conexion.Dispose(); // no dejar la conexión abierta si falla Open()
                throw;
            }
        }

        void AsegurarInicializada()
        {
            if (inicializada) return;
            lock (candadoInicio)
            {
                if (inicializada) return;
                string carpeta = Path.GetDirectoryName(rutaCompleta) ?? "";
                try
                {
                    if (carpeta.Length > 0) Directory.CreateDirectory(carpeta);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
                {
                    RegistroErrores.Registrar("Crear la carpeta de datos", ex);
                    throw new PersistenciaException(
                        "No se pudo crear la carpeta de datos del juego (" + carpeta + "). " +
                        "Revisa los permisos o el espacio disponible en el disco.", ex);
                }
                CrearEsquema();
                inicializada = true; // solo si todo salió bien: si falla, se reintenta en el siguiente uso
            }
        }

        /// <summary>Ejecuta una operación de base de datos y traduce cualquier fallo técnico a PersistenciaException.</summary>
        /// <param name="operacion">Verbo en infinitivo, p. ej. "guardar la partida" (se usa en el mensaje de error).</param>
        T Ejecutar<T>(string operacion, Func<SqliteConnection, T> accion)
        {
            try
            {
                using var conexion = Abrir();
                return accion(conexion);
            }
            catch (SqliteException ex)
            {
                throw TraducirError(operacion, ex);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                RegistroErrores.Registrar("Archivo de datos al " + operacion, ex);
                throw new PersistenciaException(
                    "No se pudo " + operacion + ": no se puede acceder al archivo de datos (" + ex.Message + ").", ex);
            }
            catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or TypeInitializationException)
            {
                RegistroErrores.Registrar("Componente SQLite al " + operacion, ex);
                throw new PersistenciaException(
                    "No se pudo " + operacion + ": falta el componente de base de datos SQLite o no es compatible con este equipo.", ex);
            }
        }

        void Ejecutar(string operacion, Action<SqliteConnection> accion) =>
            Ejecutar<object?>(operacion, conexion => { accion(conexion); return null; });

        PersistenciaException TraducirError(string operacion, SqliteException ex)
        {
            string causa = ex.SqliteErrorCode switch
            {
                5 or 6 => "La base de datos está siendo usada por otro programa u otra copia del juego. Ciérrala e inténtalo de nuevo.",
                8 => "El archivo de datos es de solo lectura. Revisa los permisos de la carpeta del juego.",
                10 => "Ocurrió un error de lectura o escritura en el disco.",
                11 or 26 => "El archivo de datos está dañado. Puedes borrarlo para que el juego cree uno nuevo: " + rutaCompleta,
                13 => "No hay espacio suficiente en el disco.",
                14 => "No se pudo abrir el archivo de datos en: " + rutaCompleta,
                19 => "La base de datos rechazó datos no válidos.",
                _ => "Error inesperado de la base de datos (código " + ex.SqliteErrorCode + "): " + ex.Message
            };
            RegistroErrores.Registrar("SQLite al " + operacion, ex);
            return new PersistenciaException("No se pudo " + operacion + ". " + causa, ex);
        }

        // ---------------------------------------------------------------------------------
        //  Esquema
        // ---------------------------------------------------------------------------------

        /// <summary>Comprueba que la base de datos está disponible (la crea si no existe). Lanza PersistenciaException si no.</summary>
        public void InicializarBaseDatos() =>
            Ejecutar("preparar la base de datos", _ => { });

        /// <summary>Crea automáticamente las tablas y los índices cuando no existen.</summary>
        void CrearEsquema()
        {
            try
            {
                using var conexion = AbrirConexion();
                using var comando = conexion.CreateCommand();
                comando.CommandText = @"
                    CREATE TABLE IF NOT EXISTS resultados_partida (
                        id_resultado INTEGER PRIMARY KEY AUTOINCREMENT,
                        nombre_jugador TEXT NOT NULL,
                        tiempo_segundos REAL NOT NULL CHECK(tiempo_segundos >= 0),
                        monedas INTEGER NOT NULL CHECK(monedas >= 0),
                        vidas_restantes INTEGER NOT NULL CHECK(vidas_restantes >= 0),
                        puntuacion INTEGER NOT NULL CHECK(puntuacion >= 0),
                        nivel_alcanzado INTEGER NOT NULL DEFAULT 1 CHECK(nivel_alcanzado BETWEEN 1 AND 3),
                        fecha_hora_utc TEXT NOT NULL,
                        resultado TEXT NOT NULL CHECK(resultado IN ('victoria','derrota'))
                    );
                    CREATE INDEX IF NOT EXISTS idx_resultados_puntos
                        ON resultados_partida(puntuacion DESC, tiempo_segundos ASC);
                    CREATE TABLE IF NOT EXISTS partidas_guardadas (
                        nombre_jugador TEXT PRIMARY KEY COLLATE NOCASE,
                        estado_json TEXT NOT NULL,
                        fecha_guardado_utc TEXT NOT NULL
                    );";
                comando.ExecuteNonQuery();
                MigrarColumnaNivel(conexion);
            }
            catch (SqliteException ex)
            {
                throw TraducirError("preparar la base de datos", ex);
            }
        }

        /// <summary>
        /// Bases de datos creadas antes de existir la clasificación por nivel no tienen la columna
        /// nivel_alcanzado: se añade sin perder datos. Las victorias antiguas se marcan como nivel 3.
        /// La transacción garantiza que, si algo falla, la base de datos queda como estaba.
        /// </summary>
        static void MigrarColumnaNivel(SqliteConnection conexion)
        {
            bool existe = false;
            using (var consulta = conexion.CreateCommand())
            {
                consulta.CommandText = "PRAGMA table_info(resultados_partida)";
                using var lector = consulta.ExecuteReader();
                while (lector.Read())
                    if (string.Equals(lector.GetString(1), "nivel_alcanzado", StringComparison.OrdinalIgnoreCase))
                        existe = true;
            }
            if (existe) return;
            using var transaccion = conexion.BeginTransaction();
            try
            {
                using var alterar = conexion.CreateCommand();
                alterar.Transaction = transaccion;
                alterar.CommandText = @"
                    ALTER TABLE resultados_partida
                        ADD COLUMN nivel_alcanzado INTEGER NOT NULL DEFAULT 1 CHECK(nivel_alcanzado BETWEEN 1 AND 3);
                    UPDATE resultados_partida SET nivel_alcanzado = 3 WHERE resultado = 'victoria';";
                alterar.ExecuteNonQuery();
                transaccion.Commit();
            }
            catch
            {
                transaccion.Rollback();
                throw;
            }
        }

        static string ValidarNombre(string nombre)
        {
            string valor = (nombre ?? "").Trim();
            if (valor.Length is < 1 or > 40)
                throw new ArgumentException("El alias debe tener entre 1 y 40 caracteres.", nameof(nombre));
            return valor;
        }

        // ---------------------------------------------------------------------------------
        //  Clasificación
        // ---------------------------------------------------------------------------------

        /// <summary>Inserta un resultado sin sobrescribir los anteriores.</summary>
        public void RegistrarResultado(ResultadoPartida resultado)
        {
            if (resultado is null) throw new ArgumentNullException(nameof(resultado));
            string nombre = ValidarNombre(resultado.NombreJugador);
            if (resultado.Resultado is not ("victoria" or "derrota"))
                throw new ArgumentException("Resultado inválido: debe ser 'victoria' o 'derrota'.");
            if (resultado.NivelAlcanzado is < 1 or > 3)
                throw new ArgumentException("Nivel alcanzado inválido: debe estar entre 1 y 3.");
            int puntuacion = ServicioPuntuacion.Calcular(resultado.Monedas, resultado.VidasRestantes, resultado.TiempoSegundos);
            Ejecutar("registrar la puntuación", conexion =>
            {
                using var comando = conexion.CreateCommand();
                comando.CommandText = @"INSERT INTO resultados_partida
                    (nombre_jugador,tiempo_segundos,monedas,vidas_restantes,puntuacion,nivel_alcanzado,fecha_hora_utc,resultado)
                    VALUES ($nombre,$tiempo,$monedas,$vidas,$puntos,$nivel,$fecha,$resultado)";
                comando.Parameters.AddWithValue("$nombre", nombre);
                comando.Parameters.AddWithValue("$tiempo", resultado.TiempoSegundos);
                comando.Parameters.AddWithValue("$monedas", resultado.Monedas);
                comando.Parameters.AddWithValue("$vidas", resultado.VidasRestantes);
                comando.Parameters.AddWithValue("$puntos", puntuacion);
                comando.Parameters.AddWithValue("$nivel", resultado.NivelAlcanzado);
                comando.Parameters.AddWithValue("$fecha", resultado.FechaHoraUtc.ToUniversalTime().ToString("O"));
                comando.Parameters.AddWithValue("$resultado", resultado.Resultado);
                comando.ExecuteNonQuery();
            });
            resultado.Puntuacion = puntuacion;
        }

        /// <summary>Devuelve el ranking de mayor a menor puntuación, desempatando por tiempo.</summary>
        public List<ResultadoPartida> ObtenerRanking(int limite = 100)
        {
            if (limite < 1 || limite > 1000)
                throw new ArgumentOutOfRangeException(nameof(limite), "El límite del ranking debe estar entre 1 y 1000.");
            return Ejecutar("leer la clasificación", conexion =>
            {
                var lista = new List<ResultadoPartida>();
                using var comando = conexion.CreateCommand();
                // Conserva el historial completo, pero expone solo el mejor intento de cada alias.
                // COLLATE NOCASE evita duplicados por diferencias de mayúsculas/minúsculas.
                comando.CommandText = @"SELECT id_resultado,nombre_jugador,tiempo_segundos,monedas,
                    vidas_restantes,puntuacion,fecha_hora_utc,resultado,nivel_alcanzado
                    FROM (
                        SELECT id_resultado,nombre_jugador,tiempo_segundos,monedas,
                            vidas_restantes,puntuacion,fecha_hora_utc,resultado,nivel_alcanzado,
                            ROW_NUMBER() OVER (
                                PARTITION BY nombre_jugador COLLATE NOCASE
                                ORDER BY puntuacion DESC, tiempo_segundos ASC,
                                         fecha_hora_utc ASC, id_resultado ASC
                            ) AS fila
                        FROM resultados_partida
                    )
                    WHERE fila = 1
                    ORDER BY puntuacion DESC, tiempo_segundos ASC, fecha_hora_utc ASC
                    LIMIT $limite";
                comando.Parameters.AddWithValue("$limite", limite);
                using var lector = comando.ExecuteReader();
                while (lector.Read())
                {
                    // Una fecha ilegible en una fila no debe impedir mostrar toda la clasificación.
                    if (!DateTime.TryParse(lector.GetString(6), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out DateTime fecha))
                        fecha = DateTime.MinValue;
                    lista.Add(new ResultadoPartida
                    {
                        Id = lector.GetInt64(0), NombreJugador = lector.GetString(1),
                        TiempoSegundos = lector.GetDouble(2), Monedas = lector.GetInt32(3),
                        VidasRestantes = lector.GetInt32(4), Puntuacion = lector.GetInt32(5),
                        FechaHoraUtc = fecha,
                        Resultado = lector.GetString(7), NivelAlcanzado = lector.GetInt32(8)
                    });
                }
                return lista;
            });
        }

        // ---------------------------------------------------------------------------------
        //  Partidas guardadas
        // ---------------------------------------------------------------------------------

        /// <summary>Guarda una sola partida por alias, reemplazando la anterior de ese jugador.</summary>
        public void GuardarPartida(EstadoPartida estado)
        {
            if (estado is null) throw new ArgumentNullException(nameof(estado));
            string nombre = ValidarNombre(estado.NombreJugador);
            if (estado.NivelActual is < 1 or > 3)
                throw new ArgumentException("Estado de partida inválido: el nivel debe estar entre 1 y 3.");
            if (estado.VidasRestantes < 0 || estado.MonedasTotales < 0 || estado.MonedasNivel < 0)
                throw new ArgumentException("Estado de partida inválido: vidas y monedas no pueden ser negativas.");
            if (!double.IsFinite(estado.TiempoAcumuladoSegundos) || estado.TiempoAcumuladoSegundos < 0)
                throw new ArgumentException("Estado de partida inválido: el tiempo acumulado no es válido.");
            estado.NombreJugador = nombre;
            estado.FechaGuardadoUtc = DateTime.UtcNow;
            string json;
            try { json = JsonSerializer.Serialize(estado, opcionesJson); }
            catch (NotSupportedException ex)
            {
                RegistroErrores.Registrar("Serializar la partida", ex);
                throw new PersistenciaException("No se pudo guardar la partida: sus datos no se pueden convertir a texto.", ex);
            }
            Ejecutar("guardar la partida", conexion =>
            {
                using var comando = conexion.CreateCommand();
                comando.CommandText = @"INSERT INTO partidas_guardadas
                    (nombre_jugador,estado_json,fecha_guardado_utc)
                    VALUES ($nombre,$json,$fecha)
                    ON CONFLICT(nombre_jugador) DO UPDATE SET
                        estado_json=excluded.estado_json,
                        fecha_guardado_utc=excluded.fecha_guardado_utc";
                comando.Parameters.AddWithValue("$nombre", nombre);
                comando.Parameters.AddWithValue("$json", json);
                comando.Parameters.AddWithValue("$fecha", estado.FechaGuardadoUtc.ToString("O"));
                comando.ExecuteNonQuery();
            });
        }

        /// <summary>
        /// Recupera el único guardado del jugador, o null si no existe.
        /// Lanza InvalidDataException si el guardado está dañado o contiene datos imposibles.
        /// </summary>
        public EstadoPartida? CargarPartida(string nombreJugador)
        {
            string nombre = ValidarNombre(nombreJugador);
            return Ejecutar<EstadoPartida?>("cargar la partida", conexion =>
            {
                using var comando = conexion.CreateCommand();
                comando.CommandText = "SELECT estado_json FROM partidas_guardadas WHERE nombre_jugador=$nombre";
                comando.Parameters.AddWithValue("$nombre", nombre);
                object? valor = comando.ExecuteScalar();
                if (valor is not string json) return null;
                EstadoPartida estado;
                try
                {
                    estado = JsonSerializer.Deserialize<EstadoPartida>(json, opcionesJson)
                        ?? throw new InvalidDataException("El guardado de '" + nombre + "' está vacío.");
                }
                catch (JsonException ex)
                {
                    RegistroErrores.Registrar("Leer el guardado de '" + nombre + "'", ex);
                    throw new InvalidDataException("El guardado de '" + nombre + "' está dañado y no se puede leer.", ex);
                }
                ValidarEstadoCargado(estado, nombre);
                return estado;
            });
        }

        /// <summary>Rechaza guardados con valores imposibles (editados a mano o corruptos) antes de usarlos en el juego.</summary>
        static void ValidarEstadoCargado(EstadoPartida estado, string nombre)
        {
            string prefijo = "El guardado de '" + nombre + "' contiene datos no válidos: ";
            if (estado.NivelActual is < 1 or > 3)
                throw new InvalidDataException(prefijo + "el nivel " + estado.NivelActual + " no existe.");
            if (estado.VidasRestantes is < 0 or > 99)
                throw new InvalidDataException(prefijo + "el número de vidas (" + estado.VidasRestantes + ") es imposible.");
            if (estado.MonedasNivel < 0 || estado.MonedasTotales < 0)
                throw new InvalidDataException(prefijo + "las monedas son negativas.");
            if (!double.IsFinite(estado.TiempoAcumuladoSegundos) || estado.TiempoAcumuladoSegundos < 0)
                throw new InvalidDataException(prefijo + "el tiempo acumulado no es válido.");
            if (estado.PosicionX is < -500 or > 10000 || estado.PosicionY is < -500 or > 3000)
                throw new InvalidDataException(prefijo + "la posición del jugador está fuera del mapa.");
            if (string.IsNullOrWhiteSpace(estado.NombreJugador))
                estado.NombreJugador = nombre;
            estado.IndicesColeccionablesRecogidos ??= new List<int>();
            estado.IndicesColeccionablesCheckpoint ??= new List<int>();
        }

        /// <summary>Elimina el guardado del jugador sin borrar su historial de puntuaciones.</summary>
        public void EliminarGuardado(string nombreJugador)
        {
            string nombre = ValidarNombre(nombreJugador);
            Ejecutar("eliminar el guardado", conexion =>
            {
                using var comando = conexion.CreateCommand();
                comando.CommandText = "DELETE FROM partidas_guardadas WHERE nombre_jugador=$nombre";
                comando.Parameters.AddWithValue("$nombre", nombre);
                comando.ExecuteNonQuery();
            });
        }
    }
}
