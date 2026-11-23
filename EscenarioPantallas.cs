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
        readonly TextBox txtAlias = new TextBox();
        readonly Label lblErrorAlias = new Label();
        readonly Button btnVolverAlias = new Button();
        bool cargandoAlias;

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
            btnContinuar.Size = new Size(220, 52);
            btnContinuar.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            btnContinuar.BackColor = Color.White;
            btnContinuar.ForeColor = Color.FromArgb(30, 30, 30);
            btnContinuar.FlatStyle = FlatStyle.Flat;
            btnContinuar.FlatAppearance.BorderSize = 0;
            btnContinuar.Location = new Point((panelPantalla.Width - btnContinuar.Width) / 2, 448);
            btnContinuar.Visible = false;
            btnContinuar.Click += (s, e) => {
                panelPantalla.Visible = false;
                pantallaActiva = false;
                SolicitarContinuacion();
            };
            txtAlias.SetBounds(285, 280, 350, 46);
            txtAlias.Font = new Font("Segoe UI", 17, FontStyle.Bold);
            txtAlias.MaxLength = 40;
            txtAlias.TextAlign = HorizontalAlignment.Center;
            txtAlias.BorderStyle = BorderStyle.FixedSingle;
            txtAlias.Visible = false;
            txtAlias.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ConfirmarAlias(); }
            };

            lblErrorAlias.SetBounds(175, 333, 570, 30);
            lblErrorAlias.TextAlign = ContentAlignment.MiddleCenter;
            lblErrorAlias.ForeColor = Color.Salmon;
            lblErrorAlias.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblErrorAlias.Visible = false;

            btnVolverAlias.Text = "VOLVER";
            btnVolverAlias.Size = new Size(170, 38);
            btnVolverAlias.Location = new Point((panelPantalla.Width - btnVolverAlias.Width) / 2, 441);
            btnVolverAlias.BackColor = Color.FromArgb(65, 75, 95);
            btnVolverAlias.ForeColor = Color.White;
            btnVolverAlias.FlatStyle = FlatStyle.Flat;
            btnVolverAlias.Visible = false;
            btnVolverAlias.Click += (s, e) => MostrarPantallaInicio();

            panelPantalla.Controls.Add(txtAlias);
            panelPantalla.Controls.Add(lblErrorAlias);
            panelPantalla.Controls.Add(btnVolverAlias);
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
            lblPantallaTexto.Height = 160;
            btnPantalla.Location = new Point((panelPantalla.Width - btnPantalla.Width) / 2, 385);
            txtAlias.Visible = false;
            lblErrorAlias.Visible = false;
            btnVolverAlias.Visible = false;
            panelPantalla.Visible = true;
            panelPantalla.BringToFront();
            ActiveControl = btnPantalla;
            btnPantalla.Focus();
        }

        /// <summary>Muestra un formulario de alias integrado en la pantalla del juego.</summary>
        void MostrarPantallaAlias(bool continuar)
        {
            cargandoAlias = continuar;
            MostrarPantalla(continuar ? "CONTINUAR" : "NUEVA PARTIDA",
                continuar ? "Recupera tu aventura" : "¿Cómo quieres que te llamemos?",
                continuar ? "Escribe el alias con el que guardaste la partida." :
                            "Elige un nombre de jugador de hasta 40 caracteres.",
                continuar ? "CARGAR PARTIDA" : "COMENZAR",
                ConfirmarAlias);
            lblPantallaTexto.Height = 65;
            txtAlias.Text = "";
            txtAlias.Visible = true;
            lblErrorAlias.Text = "";
            lblErrorAlias.Visible = true;
            btnVolverAlias.Visible = true;
            btnPantalla.Location = new Point((panelPantalla.Width - btnPantalla.Width) / 2, 381);
            txtAlias.BringToFront();
            lblErrorAlias.BringToFront();
            btnVolverAlias.BringToFront();
            txtAlias.Focus();
        }

        void ConfirmarAlias()
        {
            string alias = txtAlias.Text.Trim();
            if (alias.Length == 0)
            {
                lblErrorAlias.Text = "Escribe un nombre para continuar.";
                txtAlias.Focus();
                // El botón de la pantalla desactiva la acción antes de invocarla.
                accionPantalla = ConfirmarAlias;
                pantallaActiva = true;
                panelPantalla.Visible = true;
                panelPantalla.BringToFront();
                return;
            }
            lblPantallaTexto.Height = 160;
            btnPantalla.Location = new Point((panelPantalla.Width - btnPantalla.Width) / 2, 385);
            if (cargandoAlias) ContinuarPartida(alias);
            else IniciarPartidaConAlias(alias);
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
