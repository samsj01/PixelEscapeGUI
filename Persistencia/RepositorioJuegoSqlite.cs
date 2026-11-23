using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace PixelEsGUI.Persistencia
{
    /// <summary>Persistencia local SQLite: un guardado por alias e historial ilimitado de resultados.</summary>
    internal sealed class RepositorioJuegoSqlite
    {
        readonly string cadenaConexion;
        static readonly JsonSerializerOptions opcionesJson = new() { PropertyNameCaseInsensitive = true };

        /// <summary>Crea el repositorio y su directorio de datos local.</summary>
        public RepositorioJuegoSqlite(string? ruta = null)
        {
            ruta ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PixelEscapeGUI", "pixelescape.db");
            string rutaCompleta = Path.GetFullPath(ruta);
            Directory.CreateDirectory(Path.GetDirectoryName(rutaCompleta)!);
            cadenaConexion = new SqliteConnectionStringBuilder { DataSource = rutaCompleta, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
            InicializarBaseDatos();
        }

        SqliteConnection Abrir()
        {
            var conexion = new SqliteConnection(cadenaConexion);
            conexion.Open();
            return conexion;
        }

        /// <summary>Crea automáticamente las tablas y los índices cuando no existen.</summary>
        public void InicializarBaseDatos()
        {
            using var conexion = Abrir();
            using var comando = conexion.CreateCommand();
            comando.CommandText = @"
                CREATE TABLE IF NOT EXISTS resultados_partida (
                    id_resultado INTEGER PRIMARY KEY AUTOINCREMENT,
                    nombre_jugador TEXT NOT NULL,
                    tiempo_segundos REAL NOT NULL CHECK(tiempo_segundos >= 0),
                    monedas INTEGER NOT NULL CHECK(monedas >= 0),
                    vidas_restantes INTEGER NOT NULL CHECK(vidas_restantes >= 0),
                    puntuacion INTEGER NOT NULL CHECK(puntuacion >= 0),
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
        }

        static string ValidarNombre(string nombre)
        {
            string valor = (nombre ?? "").Trim();
            if (valor.Length is < 1 or > 40)
                throw new ArgumentException("El alias debe tener entre 1 y 40 caracteres.", nameof(nombre));
            return valor;
        }

        /// <summary>Inserta un resultado sin sobrescribir los anteriores.</summary>
        public void RegistrarResultado(ResultadoPartida resultado)
        {
            if (resultado is null) throw new ArgumentNullException(nameof(resultado));
            string nombre = ValidarNombre(resultado.NombreJugador);
            if (resultado.Resultado is not ("victoria" or "derrota"))
                throw new ArgumentException("Resultado inválido.");
            int puntuacion = ServicioPuntuacion.Calcular(resultado.Monedas, resultado.VidasRestantes, resultado.TiempoSegundos);
            using var conexion = Abrir();
            using var comando = conexion.CreateCommand();
            comando.CommandText = @"INSERT INTO resultados_partida
                (nombre_jugador,tiempo_segundos,monedas,vidas_restantes,puntuacion,fecha_hora_utc,resultado)
                VALUES ($nombre,$tiempo,$monedas,$vidas,$puntos,$fecha,$resultado)";
            comando.Parameters.AddWithValue("$nombre", nombre);
            comando.Parameters.AddWithValue("$tiempo", resultado.TiempoSegundos);
            comando.Parameters.AddWithValue("$monedas", resultado.Monedas);
            comando.Parameters.AddWithValue("$vidas", resultado.VidasRestantes);
            comando.Parameters.AddWithValue("$puntos", puntuacion);
            comando.Parameters.AddWithValue("$fecha", resultado.FechaHoraUtc.ToUniversalTime().ToString("O"));
            comando.Parameters.AddWithValue("$resultado", resultado.Resultado);
            comando.ExecuteNonQuery();
            resultado.Puntuacion = puntuacion;
        }

        /// <summary>Devuelve el ranking de mayor a menor puntuación, desempatando por tiempo.</summary>
        public List<ResultadoPartida> ObtenerRanking(int limite = 100)
        {
            if (limite < 1 || limite > 1000) throw new ArgumentOutOfRangeException(nameof(limite));
            var lista = new List<ResultadoPartida>();
            using var conexion = Abrir();
            using var comando = conexion.CreateCommand();
            // Conserva el historial completo, pero expone solo el mejor intento de cada alias.
            // COLLATE NOCASE evita duplicados por diferencias de mayúsculas/minúsculas.
            comando.CommandText = @"SELECT id_resultado,nombre_jugador,tiempo_segundos,monedas,
                vidas_restantes,puntuacion,fecha_hora_utc,resultado
                FROM (
                    SELECT id_resultado,nombre_jugador,tiempo_segundos,monedas,
                        vidas_restantes,puntuacion,fecha_hora_utc,resultado,
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
                lista.Add(new ResultadoPartida {
                    Id = lector.GetInt64(0), NombreJugador = lector.GetString(1),
                    TiempoSegundos = lector.GetDouble(2), Monedas = lector.GetInt32(3),
                    VidasRestantes = lector.GetInt32(4), Puntuacion = lector.GetInt32(5),
                    FechaHoraUtc = DateTime.Parse(lector.GetString(6), null,
                        System.Globalization.DateTimeStyles.RoundtripKind),
                    Resultado = lector.GetString(7)
                });
            return lista;
        }

        /// <summary>Guarda una sola partida por alias, reemplazando la anterior de ese jugador.</summary>
        public void GuardarPartida(EstadoPartida estado)
        {
            if (estado is null) throw new ArgumentNullException(nameof(estado));
            string nombre = ValidarNombre(estado.NombreJugador);
            if (estado.NivelActual is < 1 or > 3 || estado.VidasRestantes < 0 ||
                estado.MonedasTotales < 0 || estado.MonedasNivel < 0 ||
                !double.IsFinite(estado.TiempoAcumuladoSegundos) || estado.TiempoAcumuladoSegundos < 0)
                throw new ArgumentException("Estado de partida inválido.");
            estado.NombreJugador = nombre;
            estado.FechaGuardadoUtc = DateTime.UtcNow;
            string json = JsonSerializer.Serialize(estado, opcionesJson);
            using var conexion = Abrir();
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
        }

        /// <summary>Recupera el único guardado del jugador, o null si no existe.</summary>
        public EstadoPartida? CargarPartida(string nombreJugador)
        {
            using var conexion = Abrir();
            using var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT estado_json FROM partidas_guardadas WHERE nombre_jugador=$nombre";
            comando.Parameters.AddWithValue("$nombre", ValidarNombre(nombreJugador));
            object? valor = comando.ExecuteScalar();
            if (valor is not string json) return null;
            try { return JsonSerializer.Deserialize<EstadoPartida>(json, opcionesJson)
                ?? throw new InvalidDataException("El guardado está vacío."); }
            catch (JsonException ex) { throw new InvalidDataException("El guardado está dañado.", ex); }
        }

        /// <summary>Elimina el guardado del jugador sin borrar su historial de puntuaciones.</summary>
        public void EliminarGuardado(string nombreJugador)
        {
            using var conexion = Abrir();
            using var comando = conexion.CreateCommand();
            comando.CommandText = "DELETE FROM partidas_guardadas WHERE nombre_jugador=$nombre";
            comando.Parameters.AddWithValue("$nombre", ValidarNombre(nombreJugador));
            comando.ExecuteNonQuery();
        }
    }
}
