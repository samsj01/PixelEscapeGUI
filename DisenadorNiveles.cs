using Engine2D;

namespace PixelEsGUI
{
    /// <summary>Construye el terreno y distribuye los objetos de cada nivel.</summary>
    internal sealed class DisenadorNiveles
    {
        readonly GraphicsScene scene;
        readonly List<CazadorPixel> cazadores;
        readonly List<GuardianPixel> guardianes;
        readonly List<Trampa> trampas;
        readonly Dictionary<Enemigo, (int Min, int Max)> limitesPatrulla;
        readonly List<Coleccionables> coleccionables;
        readonly List<Obstaculo> obstaculos;
        readonly List<Decoracion> decoraciones;
        readonly List<ObstaculoSprite> obstaculosSprite;
        readonly ICollection<Rectangle> plataformas;
        readonly int nivelActual, AnchoNivel, altoVentana, Suelo;

        /// <summary>Inicializa DisenadorNiveles con los datos proporcionados.</summary>
        /// <param name="scene">Escena donde se agregan los elementos.</param>
        /// <param name="nivelActual">Número del nivel que se construye.</param>
        /// <param name="anchoNivel">Ancho del nivel en píxeles.</param>
        /// <param name="altoVentana">Altura del área visible.</param>
        /// <param name="suelo">Coordenada vertical de referencia del suelo.</param>
        /// <param name="plataformas">Colección de rectángulos de colisión.</param>
        /// <param name="cazadores">Lista de cazadores del nivel.</param>
        /// <param name="guardianes">Lista de guardianes del nivel.</param>
        /// <param name="trampas">Lista de trampas del nivel.</param>
        /// <param name="limitesPatrulla">Límites horizontales de patrulla por enemigo.</param>
        /// <param name="coleccionables">Lista de objetos recogibles.</param>
        /// <param name="obstaculos">Lista de tiles del terreno.</param>
        /// <param name="decoraciones">Lista de elementos decorativos.</param>
        /// <param name="obstaculosSprite">Lista de obstáculos con sprite y colisión.</param>
        internal DisenadorNiveles(GraphicsScene scene, int nivelActual, int anchoNivel,
            int altoVentana, int suelo, ICollection<Rectangle> plataformas,
            List<CazadorPixel> cazadores, List<GuardianPixel> guardianes,
            List<Trampa> trampas, Dictionary<Enemigo, (int Min, int Max)> limitesPatrulla,
            List<Coleccionables> coleccionables, List<Obstaculo> obstaculos,
            List<Decoracion> decoraciones, List<ObstaculoSprite> obstaculosSprite)
        {
            this.scene = scene;
            this.nivelActual = nivelActual;
            AnchoNivel = anchoNivel;
            this.altoVentana = altoVentana;
            Suelo = suelo;
            this.plataformas = plataformas;
            this.cazadores = cazadores;
            this.guardianes = guardianes;
            this.trampas = trampas;
            this.limitesPatrulla = limitesPatrulla;
            this.coleccionables = coleccionables;
            this.obstaculos = obstaculos;
            this.decoraciones = decoraciones;
            this.obstaculosSprite = obstaculosSprite;
        }

        /// <summary>Crea el suelo y distribuye los elementos del nivel seleccionado.</summary>
        internal void Construir()
        {
            CrearSuelo();
            CrearDisposicionNivel();
        }

        // Crea UN obstáculo: (fila, columna)
        void AgregarObstaculo(int x, int y, int fila, int columna)
        {
            Obstaculo obstaculo = new Obstaculo(x, y, fila, columna);
            obstaculo.SetPos(x, y);
            obstaculo.ZValue = -2;
            obstaculos.Add(obstaculo);
            scene.AddItem(obstaculo);
        }

        // Tile del nivel 2
        void AgregarTileNivel2(int x, int y, int cuadro)
        {
            Obstaculo obstaculo = new Obstaculo(x, y, "Nivel2Hoja", 8, 3, cuadro);
            obstaculo.SetPos(x, y);
            obstaculo.ZValue = -2;
            obstaculos.Add(obstaculo);
            scene.AddItem(obstaculo);
        }

        // Tile del nivel 3 (ruinas)
        void AgregarTileNivel3(int x, int y, int cuadro)
        {
            Obstaculo obstaculo = new Obstaculo(x, y, "Nivel3Hoja", 8, 2, cuadro);
            obstaculo.AgrandarBorde(1); // solape de 1 px: evita líneas claras entre tiles por redondeo
            obstaculo.SetPos(x, y);
            obstaculo.ZValue = -2;
            obstaculos.Add(obstaculo);
            scene.AddItem(obstaculo);
        }

        // Decoración tomada de una hoja de sprites (una celda).
        void AgregarDecoracionHoja(string recurso, int columnas, int filas, int cuadro,
                                   int x, int y, int ancho, int alto, int z)
        {
            Decoracion item = new Decoracion(x, y, recurso, columnas, filas, cuadro, ancho, alto);
            item.ZValue = z;
            decoraciones.Add(item);
            scene.AddItem(item);
        }

        // Recursos individuales existentes
        void AgregarDecoracion(string recurso, int x, int y, int ancho, int alto, int z = -5)
        {
            Decoracion item = new Decoracion(x, y, recurso, ancho, alto);
            item.ZValue = z;
            decoraciones.Add(item);
            scene.AddItem(item);
        }

        // Obstáculos con imagen individual y colisión sólida. Pueden bloquear lateralmente
        // y también servir como superficie si el jugador salta encima.
        void AgregarObstaculoSprite(string recurso, int x, int y, int ancho, int alto, Rectangle colision)
        {
            // Cajas del nivel 2: se bajan para apoyarse en el césped (suelo y plataformas).
            if (recurso == "Caja")
            {
                y += AjusteSuelo;
                colision.Offset(0, AjusteSuelo);
            }
            ObstaculoSprite item = new ObstaculoSprite(x, y, recurso, ancho, alto, colision);
            item.ZValue = -1;
            obstaculosSprite.Add(item);
            plataformas.Add(colision);
            scene.AddItem(item);
        }

        void DecorarNivel1()
        {
            // Fondo
            AgregarDecoracion("FondoMontañas", 0, 0, 922, 490, -20);
            AgregarDecoracion("FondoMontañas", 918, 0, 922, 490, -20);

            // Nubes
            AgregarDecoracion("Nube1", 110, 6, 106, 32, -18);
            AgregarDecoracion("Nube2", 490, 14, 90, 32, -18);
            AgregarDecoracion("Nube2", 790, 20, 90, 32, -18);
            AgregarDecoracion("Nube1", 1110, 10, 106, 32, -18);
            AgregarDecoracion("Nube2", 1580, 2, 90, 32, -18);

            // Pinos
            AgregarDecoracion("Pino1", 34, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 215, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 390, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 520, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 700, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 850, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 1012, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 1215, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 1380, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 1520, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 1618, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 1730, Suelo - 120, 105, 130, -8);

            // Enredadera
            AgregarDecoracion("Enredadera1", 50, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 200, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 350, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 500, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 650, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 800, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 950, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 1050, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 1200, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 1350, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 1500, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 1650, Suelo + 10, 26, 112, -1);
            AgregarDecoracion("Enredadera1", 1800, Suelo + 10, 26, 126, -1);

            // Arbustos
            AgregarDecoracion("Arbusto1", 140, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto2", 310, Suelo - 43, 76, 52, -1);
            AgregarDecoracion("Arbusto4", 630, Suelo - 39, 76, 48, -1);
            AgregarDecoracion("Arbusto 3", 940, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto1", 1180, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto2", 1345, Suelo - 43, 76, 52, -1);
            AgregarDecoracion("Arbusto4", 1495, Suelo - 39, 76, 48, -1);

            // Rocas y Flores
            AgregarDecoracion("Roca2", 15, Suelo - 27, 48, 28, -3);
            AgregarDecoracion("Roca1", 103, Suelo - 25, 55, 30, -3);
            AgregarDecoracion("Roca3", 276, Suelo - 25, 45, 30, -3);
            AgregarDecoracion("Roca3", 840, Suelo - 25, 45, 30, -3);
            AgregarDecoracion("Roca1", 492, Suelo - 25, 55, 30, -3);
            AgregarDecoracion("Roca2", 1000, Suelo - 27, 48, 28, -3);
            AgregarDecoracion("Roca3", 1132, Suelo - 25, 42, 26, -3);
            AgregarDecoracion("Roca1", 1617, Suelo - 25, 55, 30, -3);
            AgregarDecoracion("Roca1", 1800, Suelo - 25, 55, 30, -3);

            AgregarDecoracion("Flor1", 195, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor2", 383, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor3", 815, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor4", 995, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor1", 1115, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor3", 1165, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor2", 1640, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor4", 1790, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor2", 1775, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor2", 1750, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor3", 204, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor1", 820, Suelo - 107, 30, 32, -1);
            AgregarDecoracion("Flor2", 948, Suelo - 107, 30, 32, -1);
        }

        void CrearObstaculosNivel1()
        {
            // Cajas y troncos
            AgregarGrupoCajas(430);

            AgregarObstaculoSprite("Tronco", 690, Suelo - 50, 140, 76,
                new Rectangle(704, Suelo - 30, 112, 30));

            AgregarObstaculoSprite("TroncoPartido", 1280, Suelo - 50, 96, 72,
                new Rectangle(1288, Suelo - 48, 80, 48));

            AgregarGrupoCajas(1550);

            AgregarObstaculoSprite("Caja", 160, Suelo - 32, 32, 32,
                new Rectangle(160, Suelo - 32, 32, 32));
            AgregarObstaculoSprite("Tronco", 1110, Suelo - 50, 140, 76,
                new Rectangle(1110, Suelo - 30, 112, 30));
            AgregarObstaculoSprite("Caja", 875, Suelo - 115, 32, 32,
                new Rectangle(875, Suelo - 115, 32, 32));
        }

        void AgregarGrupoCajas(int x)
        {
            AgregarObstaculoSprite("Caja", x, Suelo - 32, 32, 32,
                new Rectangle(x, Suelo - 32, 32, 32));
            AgregarObstaculoSprite("Caja", x + 32, Suelo - 32, 32, 32,
                new Rectangle(x + 32, Suelo - 32, 32, 32));
            AgregarObstaculoSprite("Caja", x + 32, Suelo - 64, 32, 32,
                new Rectangle(x + 32, Suelo - 64, 32, 32));
        }

        // Suelo completo
        void CrearSuelo()
        {
            int columnas = AnchoNivel / Obstaculo.Tamano + 1;
            int filas = (altoVentana - Suelo) / Obstaculo.Tamano + 1;
            for (int c = 0; c < columnas; c++)
            {
                int x = c * Obstaculo.Tamano;
                for (int f = 0; f < filas; f++)
                {
                    int y = Suelo + f * Obstaculo.Tamano;
                    if (nivelActual == 2)
                    {
                        // Nivel 2
                        int cuadro = f == 0
                            ? (c % 11 == 5 ? 3 : (c % 4 == 2 ? 1 : 0))
                            : 4 + (c * 7 + f * 3) % 4;
                        AgregarTileNivel2(x, y, cuadro);
                    }
                    else if (nivelActual == 1)
                    {
                        AgregarObstaculo(x, y, f == 0 ? 0 : 1, 0);
                    }
                    else
                    {
                        // Nivel 3
                        int cuadro = f == 0
                            ? (c * 5 + c / 3) % 4
                            : (f < 3 ? 4 : f < 5 ? 8 : 12) + (c * 7 + f * 3 + c / 5) % 4;
                        AgregarTileNivel3(x, y, cuadro);
                    }
                }
            }
        }

        // Plataforma flotante
        void CrearPlataforma(int x, int y, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                if (nivelActual == 2)
                    AgregarTileNivel2(x + i * Obstaculo.Tamano, y, 0);
                else if (nivelActual == 3)
                    AgregarTileNivel3(x + i * Obstaculo.Tamano, y, ((x / Obstaculo.Tamano) * 3 + i) % 4);
                else
                    AgregarObstaculo(x + i * Obstaculo.Tamano, y, 0, 0);
            }

            if (nivelActual == 3)
            {
                // Detalles de la plataforma
                AgregarDecoracion("Flor1", x + 4, y - 22, 20, 22, -1);
                AgregarDecoracion("Flor3", x + cantidad * Obstaculo.Tamano - 26, y - 22, 20, 22, -1);
                AgregarDecoracion("Enredadera1", x + 6, y + Obstaculo.Tamano, 18, 50, -1);
                AgregarDecoracion("Enredadera1", x + cantidad * Obstaculo.Tamano - 24, y + Obstaculo.Tamano, 18, 50, -1);
            }

            // Se registra como superficie sólida: el jugador se para encima y no la atraviesa.
            plataformas.Add(new Rectangle(x, y, cantidad * Obstaculo.Tamano, Obstaculo.Tamano));
        }

        void AgregarColeccionable(Coleccionables item, int x)
        {
            AgregarColeccionable(item, x, Suelo - (int)item.Alto);
        }

        void AgregarColeccionable(Coleccionables item, int x, int y)
        {
            item.SetPos(x, y);
            coleccionables.Add(item);
            scene.AddItem(item);
        }

        // ---------- Ayudantes para distribuir objetos por el mapa ----------
        const int AjusteSueloNivel2 = 6;
        int AjusteSuelo => nivelActual == 2 ? AjusteSueloNivel2 : 0;

        int YPlataforma => Suelo - 86;
        int YEnemigo => Suelo - 69;       // los enemigos quedan con los pies sobre el suelo
        int YMonedaSuelo => Suelo - 100; // moneda pequeña, elevada para exigir salto
        int YMonedaPlataforma => YPlataforma - 30;

        // Moneda suelta en (x, y).
        void AgregarMonedaEn(int x, int y)
        {
            AgregarColeccionable(new Moneda(x, y), x, y);
        }

        // Fila de monedas sobre el suelo.
        void AgregarMonedasLinea(int xInicial, int cantidad, int separacion = 45)
        {
            for (int i = 0; i < cantidad; i++)
            {
                AgregarMonedaEn(xInicial + i * separacion, YMonedaSuelo);
            }
        }

        // Fila de monedas encima de una plataforma baja.
        void AgregarMonedasPlataforma(int xInicial, int cantidad, int separacion = 40)
        {
            for (int i = 0; i < cantidad; i++)
            {
                AgregarMonedaEn(xInicial + i * separacion, YMonedaPlataforma);
            }
        }

        // 5 monedas en arco sobre una trampa: el jugador las recoge al saltarla.
        void AgregarMonedasArcoSobreTrampa(int xTrampa)
        {
            int[] desfase = { -100, -50, 0, 50, 100 };
            int[] alturas = { Suelo - 125, Suelo - 145, Suelo - 155, Suelo - 145, Suelo - 125 };
            for (int i = 0; i < 5; i++)
            {
                AgregarMonedaEn(xTrampa + 12 + desfase[i], alturas[i]);
            }
        }

        void AgregarTrampa(int x)
        {
            Trampa t = new Trampa(x, 0, 1);
            t.SetPos(x, Suelo - 65 + AjusteSuelo);
            t.ZValue = -1;
            trampas.Add(t);
            scene.AddItem(t);
        }

        // Guardián en el suelo con una plataforma encima
        void AgregarGuardian(int x)
        {
            GuardianPixel g = new GuardianPixel(x, YEnemigo);
            guardianes.Add(g);
            limitesPatrulla[g] = (Math.Max(0, x - 70), x + 70);
            scene.AddItem(g);
            CrearPlataforma(x - 16, YPlataforma, 4);
        }

        // Cazador en el suelo con una plataforma encima.
        void AgregarCazador(int x)
        {
            CazadorPixel c = new CazadorPixel(x, YEnemigo);
            cazadores.Add(c);
            limitesPatrulla[c] = (Math.Max(0, x - 90), x + 90);
            scene.AddItem(c);
            CrearPlataforma(x, YPlataforma, 3);
        }

        // Guardián sin plataforma
        void AgregarGuardianLibre(int x)
        {
            GuardianPixel g = new GuardianPixel(x, YEnemigo);
            guardianes.Add(g);
            limitesPatrulla[g] = (Math.Max(0, x - 70), x + 70);
            scene.AddItem(g);
        }

        // Cazador sin plataforma.
        void AgregarCazadorLibre(int x)
        {
            CazadorPixel c = new CazadorPixel(x, YEnemigo);
            cazadores.Add(c);
            limitesPatrulla[c] = (Math.Max(0, x - 90), x + 90);
            scene.AddItem(c);
        }

        // Llave apoyada sobre una plataforma
        void AgregarLlaveSobrePlataforma(int xPlataforma, int bloques)
        {
            CrearPlataforma(xPlataforma, YPlataforma, bloques);
            Llave llave = new Llave(0, 0);
            int x = xPlataforma + (bloques * Obstaculo.Tamano - (int)llave.Ancho) / 2; // centrada
            AgregarColeccionable(llave, x, YPlataforma - (int)llave.Alto);
        }
        // =====================================================================================
        //  DISTRIBUCIÓN DE OBJETOS POR NIVEL 
        // =====================================================================================
        void CrearDisposicionNivel()
        {
            switch (nivelActual)
            {
                case 1:
                    CrearNivel1();
                    break;
                case 2:
                    CrearNivel2();
                    break;
                default:
                    CrearNivel3();
                    break;
            }
        }

        // NIVEL 1 (1840 px) - objetivo: 20 monedas. Sin enemigos ni llave: es el nivel de aprendizaje.
        void CrearNivel1()
        {
            DecorarNivel1();
            CrearObstaculosNivel1();

            AgregarMonedasLinea(230, 4);
            AgregarTrampa(260);

            AgregarTrampa(540);
            AgregarMonedasArcoSobreTrampa(540);
            CrearPlataforma(800, YPlataforma, 6);
            // 3 monedas encima
            AgregarMonedaEn(830, YMonedaPlataforma);
            AgregarMonedaEn(880, YMonedaPlataforma - 44);
            AgregarMonedaEn(930, YMonedaPlataforma);

            // Punto de control en x = 1060

            AgregarMonedasLinea(1160, 3);

            AgregarTrampa(1400);
            AgregarMonedasArcoSobreTrampa(1400);
        }

        // Decoración del NIVEL 2:
        void DecorarNivel2()
        {
            // Fondo
            for (int i = 0; i < 8; i++)
            {
                AgregarDecoracion("BgNivel2", i * 383, 0, 385, 383, -20);
            }

            // Pinos oscuros
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 0, 40, Suelo - 132, 87, 138, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 1, 195, Suelo - 140, 92, 146, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 0, 345, Suelo - 116, 77, 122, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 1, 520, Suelo - 132, 87, 138, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 0, 645, Suelo - 124, 82, 130, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 1, 795, Suelo - 116, 77, 122, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 0, 1020, Suelo - 140, 92, 146, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 1, 1170, Suelo - 124, 82, 130, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 0, 1320, Suelo - 140, 92, 146, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 1, 1470, Suelo - 116, 77, 122, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 0, 1645, Suelo - 112, 77, 122, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 1, 1857, Suelo - 116, 77, 122, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 0, 2045, Suelo - 113, 77, 122, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 1, 2258, Suelo - 130, 87, 138, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 0, 2445, Suelo - 124, 82, 130, -8);
            AgregarDecoracionHoja("Nivel2Pinos", 2, 1, 1, 2595, Suelo - 132, 87, 138, -8);

            // Maticas
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 14, 120, Suelo - 48, 52, 52, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 14, 150, Suelo - 52, 56, 56, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 14, 200, Suelo - 48, 52, 52, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 14, 350, Suelo - 52, 56, 56, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 12, 400, Suelo - 60, 64, 64, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 16, 550, Suelo - 52, 56, 56, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 14, 1128, Suelo - 48, 52, 52, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 14, 2456, Suelo - 52, 56, 56, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 12, 2508, Suelo - 60, 64, 64, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 16, 2706, Suelo - 52, 56, 56, -1);

            // Flores en el suelo
            AgregarDecoracion("Flor1", 160, Suelo - 31, 30, 34, -1);
            AgregarDecoracion("Flor2", 325, Suelo - 27, 34, 30, -1);
            AgregarDecoracion("Flor3", 570, Suelo - 25, 36, 28, -1);
            AgregarDecoracion("Flor4", 735, Suelo - 27, 30, 30, -1);
            AgregarDecoracion("Flor1", 965, Suelo - 31, 30, 34, -1);
            AgregarDecoracion("Flor2", 1100, Suelo - 27, 34, 30, -1);
            AgregarDecoracion("Flor3", 1185, Suelo - 25, 36, 28, -1);
            AgregarDecoracion("Flor1", 1790, Suelo - 31, 30, 34, -1);
            AgregarDecoracion("Flor2", 1975, Suelo - 27, 34, 30, -1);
            AgregarDecoracion("Flor3", 2150, Suelo - 25, 36, 28, -1);
            AgregarDecoracion("Flor4", 2340, Suelo - 27, 30, 30, -1);
            AgregarDecoracion("Flor1", 2570, Suelo - 31, 30, 34, -1);

            // Flores sobre las plataformas
            AgregarDecoracion("Flor1", 612, YPlataforma - 28, 30, 34, -1);
            AgregarDecoracion("Flor2", 655, YPlataforma - 24, 34, 30, -1);
            AgregarDecoracion("Flor3", 1002, YPlataforma - 22, 36, 28, -1);
            AgregarDecoracion("Flor4", 1032, YPlataforma - 24, 30, 30, -1);
            AgregarDecoracion("Flor1", 1658, YPlataforma - 28, 30, 34, -1);
            AgregarDecoracion("Flor2", 1704, YPlataforma - 24, 34, 30, -1);
            AgregarDecoracion("Flor3", 1892, YPlataforma - 22, 36, 28, -1);
            AgregarDecoracion("Flor4", 1936, YPlataforma - 24, 30, 30, -1);
            AgregarDecoracion("Flor1", 2200, YPlataforma - 28, 30, 34, -1);
            AgregarDecoracion("Flor2", 2230, YPlataforma - 24, 34, 30, -1);

            // Arbustos
            AgregarDecoracion("ArbustoNivel2", 0, Suelo - 50, 88, 80, -1);
            AgregarDecoracion("Arbusto2Nivel2", 472, Suelo - 60, 84, 80, -1);
            AgregarDecoracion("ArbustoNivel2", 722, Suelo - 46, 78, 71, -1);
            AgregarDecoracion("Arbusto2Nivel2", 922, Suelo - 60, 84, 80, -1);
            AgregarDecoracion("ArbustoNivel2", 1111, Suelo - 50, 88, 80, -1);
            AgregarDecoracion("Arbusto2Nivel2", 1293, Suelo - 60, 84, 80, -1);
            AgregarDecoracion("Arbusto2Nivel2", 1522, Suelo - 74, 100, 96, -1);
            AgregarDecoracion("ArbustoNivel2", 1750, Suelo - 65, 104, 94, -1);
            AgregarDecoracion("Arbusto2Nivel2", 1990, Suelo - 56, 84, 80, -1);
            AgregarDecoracion("Arbusto2Nivel2", 2188, Suelo - 74, 100, 96, -1);
            AgregarDecoracion("Arbusto2Nivel2", 3010, Suelo - 60, 84, 80, -1);

            // Rocas
            AgregarDecoracion("Roca2", 110, Suelo - 30, 47, 39, -3);
            AgregarDecoracion("Roca3", 270, Suelo - 25, 48, 33, -3);
            AgregarDecoracion("Roca2", 434, Suelo - 30, 47, 39, -3);
            AgregarDecoracion("Roca3", 610, Suelo - 25, 48, 33, -3);
            AgregarDecoracion("Roca2", 855, Suelo - 30, 47, 39, -3);
            AgregarDecoracion("Roca3", 990, Suelo - 25, 48, 33, -3);
            AgregarDecoracion("Roca2", 1244, Suelo - 30, 47, 39, -3);
            AgregarDecoracion("Roca3", 1428, Suelo - 25, 48, 33, -3);
            AgregarDecoracion("Roca2", 1715, Suelo - 30, 47, 39, -3);
            AgregarDecoracion("Roca3", 1925, Suelo - 25, 48, 33, -3);
            AgregarDecoracion("Roca2", 2100, Suelo - 30, 47, 39, -3);
            AgregarDecoracion("Roca3", 2400, Suelo - 25, 48, 33, -3);

            // Enredaderas
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 60, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 60, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 225, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 225, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 415, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 415, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 555, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 555, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 695, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 695, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 885, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 885, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 1050, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 1050, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 1215, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 1215, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 1545, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 1545, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 1685, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 1685, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 1850, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 1850, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 2015, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 2015, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 2180, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 2180, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 2320, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 2320, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 2510, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 2510, Suelo + 86, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 21, 2700, Suelo + 16, 68, 68, -1);
            AgregarDecoracionHoja("Nivel2Hoja", 8, 3, 22, 2700, Suelo + 86, 68, 68, -1);
        }

        // Obstáculos del nivel 2
        void CrearObstaculosNivel2()
        {
            AgregarGrupoCajas(215);                       

            AgregarObstaculoSprite("Tronco", 795, Suelo - 40, 140, 76,
                new Rectangle(809, Suelo - 20, 112, 30));

            AgregarObstaculoSprite("TroncoPartido", 1530, Suelo - 48, 96, 72,
                new Rectangle(1538, Suelo - 46, 80, 48));

            // Cajas sobre las plataformas flotantes
            AgregarObstaculoSprite("Caja", 700, YPlataforma - 32, 32, 32,
                new Rectangle(700, YPlataforma - 32, 32, 32));    // guardián 1 (604-732)
            AgregarObstaculoSprite("Caja", 1064, YPlataforma - 32, 32, 32,
                new Rectangle(1064, YPlataforma - 32, 32, 32));   // cazador 1 (1000-1096)
            AgregarObstaculoSprite("Caja", 1980, YPlataforma - 32, 32, 32,
                new Rectangle(1980, YPlataforma - 32, 32, 32));   // guardián 2 (1884-2012)
            AgregarObstaculoSprite("Caja", 2264, YPlataforma - 32, 32, 32,
                new Rectangle(2264, YPlataforma - 32, 32, 32));   // cazador 2 (2200-2296)
        }

        // NIVEL 2 (2760 px) - objetivo: llave + evitar al cazador y al guardián (sin monedas).
        void CrearNivel2()
        {
            DecorarNivel2();
            CrearObstaculosNivel2();

            AgregarTrampa(380);                       // trampa 1: bienvenida

            AgregarGuardian(620);                     // guardián 1

            AgregarCazador(1000);                     // cazador 1

            AgregarTrampa(1240);                      // trampa 2

            // Punto de control en x = 1400
            AgregarColeccionable(new PowerUpEscudo(1480, 0, 10), 1480); // escudo tras el punto de control

            AgregarLlaveSobrePlataforma(1650, 3);     // LLAVE sobre plataforma

            AgregarGuardian(1900);                    // guardián 2
            AgregarColeccionable(new PowerUpVelocidad(289, 0, 7), 289); // velocidad temporal

            AgregarCazador(2200);                     // cazador 2

            AgregarTrampa(2440);                      // trampa 3
        }

        // Decoración del NIVEL 3
        void DecorarNivel3()
        {
            // Fondo
            AgregarDecoracion("Nivel3Fondo", 0, 0, 1442, 330, -20);
            AgregarDecoracion("Nivel3Fondo", 1440, 0, 1442, 330, -20);
            AgregarDecoracion("Nivel3Fondo", 2880, 0, 1442, 330, -20);

            // Pinos
            AgregarDecoracion("Pino1", 30, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 578, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 847, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 1114, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 1389, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 1674, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 2141, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 2416, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 2686, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino2", 2976, Suelo - 120, 105, 130, -8);
            AgregarDecoracion("Pino1", 3264, Suelo - 120, 105, 130, -8);

            // Arbustos
            AgregarDecoracion("Arbusto2", 882, Suelo - 43, 76, 52, -1);
            AgregarDecoracion("Arbusto4", 1007, Suelo - 39, 76, 48, -1);
            AgregarDecoracion("Arbusto 3", 1177, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto1", 1354, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto4", 1615, Suelo - 39, 76, 48, -1);
            AgregarDecoracion("Arbusto2", 2057, Suelo - 43, 76, 52, -1);
            AgregarDecoracion("Arbusto 3", 2325, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto1", 2475, Suelo - 39, 54, 41, -1);
            AgregarDecoracion("Arbusto2", 2598, Suelo - 43, 76, 52, -1);
            AgregarDecoracion("Arbusto4", 2777, Suelo - 39, 76, 48, -1);
            AgregarDecoracion("Arbusto 3", 2938, Suelo - 39, 54, 41, -1);

            // Rocas.
            AgregarDecoracion("Roca2", 813, Suelo - 27, 48, 28, -3);
            AgregarDecoracion("Roca1", 1024, Suelo - 25, 55, 30, -3);
            AgregarDecoracion("Roca3", 1331, Suelo - 25, 45, 30, -3);
            AgregarDecoracion("Roca2", 1592, Suelo - 27, 48, 28, -3);
            AgregarDecoracion("Roca3", 2097, Suelo - 25, 45, 30, -3);
            AgregarDecoracion("Roca2", 2352, Suelo - 27, 48, 28, -3);
            AgregarDecoracion("Roca1", 2622, Suelo - 25, 55, 30, -3);
            AgregarDecoracion("Roca3", 2870, Suelo - 25, 45, 30, -3);
            AgregarDecoracion("Roca1", 3454, Suelo - 25, 55, 30, -3);

            // Flores pequeñas.
            AgregarDecoracion("Flor1", 808, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor2", 892, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor3", 956, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor4", 1017, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor1", 1131, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor2", 1234, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor3", 1294, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor4", 1367, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor3", 1677, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor4", 1740, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor4", 2104, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor3", 2350, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor4", 2450, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor1", 2522, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor2", 2631, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor3", 2734, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor4", 2811, Suelo - 22, 20, 22, -1);
            AgregarDecoracion("Flor1", 2892, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor2", 2957, Suelo - 22, 30, 32, -1);
            AgregarDecoracion("Flor3", 3371, Suelo - 22, 30, 32, -1);

            // Enredaderas
            AgregarDecoracion("Enredadera1", 55, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 192, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 324, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 451, Suelo + 10, 26, 118, -1);
            AgregarDecoracion("Enredadera1", 633, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 777, Suelo + 10, 26, 118, -1);
            AgregarDecoracion("Enredadera1", 962, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 1098, Suelo + 10, 26, 118, -1);
            AgregarDecoracion("Enredadera1", 1267, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 1437, Suelo + 10, 26, 118, -1);
            AgregarDecoracion("Enredadera1", 1584, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 1738, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 1896, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 2042, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 2212, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 2344, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 2482, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 2658, Suelo + 10, 26, 118, -1);
            AgregarDecoracion("Enredadera1", 2779, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 2941, Suelo + 10, 26, 118, -1);
            AgregarDecoracion("Enredadera1", 3110, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 3239, Suelo + 10, 26, 104, -1);
            AgregarDecoracion("Enredadera1", 3385, Suelo + 10, 26, 126, -1);
            AgregarDecoracion("Enredadera1", 3536, Suelo + 10, 26, 104, -1);
        }

        // Obstáculos
        void CrearObstaculosNivel3()
        {
            // --- Inicio
            AgregarObstaculoSprite("Nivel3Columna", 185, Suelo - 78, 56, 78,
                new Rectangle(189, Suelo - 66, 48, 66));
            AgregarObstaculoSprite("Caja", 290, Suelo - 32, 32, 32,
                new Rectangle(290, Suelo - 32, 32, 32));
            // Vasija
            AgregarObstaculoSprite("Nivel3Vasija", 365, Suelo - 54, 38, 54,
                new Rectangle(368, Suelo - 50, 32, 50));

            // --- Tramo 3
            AgregarObstaculoSprite("Caja", 1945, Suelo - 32, 32, 32,
                new Rectangle(1945, Suelo - 32, 32, 32));
            AgregarObstaculoSprite("Nivel3Columna", 1985, Suelo - 78, 56, 78,
                new Rectangle(1989, Suelo - 66, 48, 66));

            // --- Sobre la plataforma del guardián 3
            AgregarObstaculoSprite("Caja", 2700, YPlataforma - 32, 32, 32,
                new Rectangle(2700, YPlataforma - 32, 32, 32));
            AgregarObstaculoSprite("Nivel3Vasija", 2768, YPlataforma - 54, 38, 54,
                new Rectangle(2771, YPlataforma - 50, 32, 50));
        }

        // NIVEL 3
        void CrearNivel3()
        {
            DecorarNivel3();
            CrearObstaculosNivel3();

            // --- Tramo 1 (0-920)
            AgregarMonedasLinea(240, 3);              // 3 monedas
            AgregarTrampa(480);                       // trampa 1
            AgregarMonedasArcoSobreTrampa(480);       // 5 monedas en arco
            AgregarTrampa(640);                       // trampa 2
            AgregarGuardianLibre(820);                // guardián 1

            // --- Tramo 2 (920-1840)
            AgregarGuardian(1040);                    // guardián 2
            AgregarMonedasPlataforma(1032, 3);        // 3 monedas sobre su plataforma
            AgregarCazadorLibre(1300);                // cazador 1
            AgregarTrampa(1460);                      // trampa 3
            AgregarCazador(1640);                     // cazador 2 
            AgregarMonedasPlataforma(1652, 2);        // 2 monedas

            // Punto de control en x = 1800
            AgregarColeccionable(new PowerUpEscudo(1900, 0, 10), 1900); // único power-up del nivel

            // --- Tramo 3 (1840-2760)
            AgregarMonedasLinea(1960, 3);             // 3 monedas
            AgregarTrampa(2160);                      // trampa 4
            AgregarCazadorLibre(2330);                // cazador 3
            AgregarLlaveSobrePlataforma(2470, 4);     // LLAVE
            AgregarMonedaEn(2482, YMonedaPlataforma); // 2 monedas junto a la llave
            AgregarMonedaEn(2552, YMonedaPlataforma);

            // --- Tramo 4 (2760-3680)
            AgregarGuardian(2700);                    // guardián 3
            AgregarCazador(2900);                     // cazador 4
            AgregarMonedasPlataforma(2912, 2);        // 2 monedas -> total 20
            AgregarTrampa(3060);                      // trampa 5
            AgregarTrampa(3210);                      // trampa 6
            AgregarGuardianLibre(3330);               // guardián 4
        }
    }
}
