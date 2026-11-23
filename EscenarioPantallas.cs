namespace PixelEsGUI
{
    // Pantallas a ventana completa (BORRADOR funcional, sin diseño visual todavía):
    //   inicio -> controles -> "Nivel 1" -> juego ... -> "Nivel 2" -> juego ... -> "Nivel 3" -> juego -> victoria
    // Una sola pantalla reutilizable (panelPantalla) que cambia de textos y de acción del botón.
    public partial class Escenario
    {
        readonly Panel panelPantalla = new Panel();
        readonly Label lblPantallaTitulo = new Label();
        readonly Label lblPantallaSubtitulo = new Label();
        readonly Label lblPantallaTexto = new Label();
        readonly Button btnPantalla = new Button();
        readonly Button btnContinuar = new Button();

        // Mientras hay una pantalla visible el juego está detenido y se ignoran P / R / movimiento.
        bool pantallaActiva = false;
        Action? accionPantalla;
        DateTime instantePantalla = DateTime.MinValue;

        void ConfigurarPantallas()
        {
            panelPantalla.SetBounds(0, 0, ClientSize.Width, ClientSize.Height);
            panelPantalla.BackColor = Color.FromArgb(20, 24, 40);
            panelPantalla.Visible = false;

            lblPantallaTitulo.SetBounds(0, 55, panelPantalla.Width, 90);
            lblPantallaTitulo.TextAlign = ContentAlignment.MiddleCenter;
            lblPantallaTitulo.ForeColor = Color.White;
            lblPantallaTitulo.Font = new Font("Segoe UI", 38, FontStyle.Bold);

            lblPantallaSubtitulo.SetBounds(0, 145, panelPantalla.Width, 40);
            lblPantallaSubtitulo.TextAlign = ContentAlignment.MiddleCenter;
            lblPantallaSubtitulo.ForeColor = Color.Gold;
            lblPantallaSubtitulo.Font = new Font("Segoe UI", 18, FontStyle.Italic);

            lblPantallaTexto.SetBounds(110, 200, panelPantalla.Width - 220, 160);
            lblPantallaTexto.TextAlign = ContentAlignment.MiddleCenter;
            lblPantallaTexto.ForeColor = Color.Gainsboro;
            lblPantallaTexto.Font = new Font("Segoe UI", 15, FontStyle.Bold);

            btnPantalla.Size = new Size(220, 52);
            btnPantalla.Location = new Point((panelPantalla.Width - btnPantalla.Width) / 2, 385);
            btnPantalla.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            btnPantalla.BackColor = Color.White;
            btnPantalla.ForeColor = Color.FromArgb(30, 30, 30);
            btnPantalla.FlatStyle = FlatStyle.Flat;
            btnPantalla.Click += (s, e) =>
            {
                // Evita doble disparo (Enter mantenido / Enter + clic) al encadenar pantallas.
                if ((DateTime.Now - instantePantalla).TotalMilliseconds < 300) return;
                Action? accion = accionPantalla;
                if (accion == null) return;
                accionPantalla = null;
                pantallaActiva = false;
                panelPantalla.Visible = false;
                accion();
            };

            btnContinuar.Text = "CONTINUAR PARTIDA";
            btnContinuar.Size = new Size(220, 40);
            btnContinuar.Location = new Point((panelPantalla.Width - btnContinuar.Width) / 2, 440);
            btnContinuar.Visible = false;
            btnContinuar.Click += (s, e) => {
                panelPantalla.Visible = false;
                pantallaActiva = false;
                SolicitarContinuacion();
            };
            panelPantalla.Controls.Add(btnContinuar);
            panelPantalla.Controls.Add(lblPantallaTitulo);
            panelPantalla.Controls.Add(lblPantallaSubtitulo);
            panelPantalla.Controls.Add(lblPantallaTexto);
            panelPantalla.Controls.Add(btnPantalla);
            Controls.Add(panelPantalla);
            panelPantalla.BringToFront();
        }

        // Muestra la pantalla, detiene el juego y ejecuta 'alContinuar' al pulsar el botón (clic, Enter o Espacio).
        void MostrarPantalla(string titulo, string subtitulo, string texto, string textoBoton, Action alContinuar)
        {
            izquierda = derecha = salto = false;
            bucleJuego.Stop();

            lblPantallaTitulo.Text = titulo;
            lblPantallaSubtitulo.Text = subtitulo;
            lblPantallaTexto.Text = texto;
            btnPantalla.Text = textoBoton;
            accionPantalla = alContinuar;
            instantePantalla = DateTime.Now;
            pantallaActiva = true;

            btnContinuar.Visible = titulo == "PIXEL ESCAPE";
            panelPantalla.Visible = true;
            panelPantalla.BringToFront();
            ActiveControl = btnPantalla;
            btnPantalla.Focus();
        }

        // 1) Pantalla de inicio: nombre del juego y botón de empezar.
        void MostrarPantallaInicio()
        {
            MostrarPantalla("PIXEL ESCAPE", "Escapa a través de tres niveles", "", "EMPEZAR",
                ComenzarNuevaPartida);
        }

        // 2) Pantalla de controles.
        void MostrarPantallaControles()
        {
            MostrarPantalla("CONTROLES", "",
                "← →  /  A  D :  moverse\n" +
                "Espacio  /  ↑  /  W :  saltar\n" +
                "P :  pausar y reanudar\n" +
                "R :  reiniciar el nivel",
                "CONTINUAR", () => MostrarPantallaNivel(1));
        }

        // 3) Pantallas de cambio de nivel (una por nivel): se muestran antes de empezar cada uno.
        void MostrarPantallaNivel(int nivel)
        {
            string subtitulo;
            string objetivo;
            switch (nivel)
            {
                case 1:
                    subtitulo = "El bosque";
                    objetivo = "Recoge las 20 monedas y llega a la puerta.";
                    break;
                case 2:
                    subtitulo = "El bosque nocturno";
                    objetivo = "Recoge la llave, evita al cazador y al guardián y llega a la puerta.";
                    break;
                default:
                    subtitulo = "Las ruinas de la selva";
                    objetivo = "Recoge las 20 monedas y la llave, evita al cazador y al guardián y llega a la puerta.";
                    break;
            }

            MostrarPantalla("NIVEL " + nivel, subtitulo, objetivo, "COMENZAR", () =>
            {
                this.Text = "Pixel Escape - Nivel " + nivel;
                bucleJuego.Start();
                Visor.Focus();
            });
        }
    }
}
