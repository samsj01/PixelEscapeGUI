namespace PixelEsGUI
{
    // Pantallas a ventana completa (BORRADOR funcional, sin diseño visual todavía):
    public partial class Escenario
    {
        // Paleta pastel y tipografías compartidas por todas las pantallas previas al nivel.
        static readonly Color ColorFondoPantalla = Color.FromArgb(190, 225, 247); // azul claro
        static readonly Color ColorTituloPantalla = Color.FromArgb(111, 62, 161); // morado
        static readonly Color ColorTextoPantalla = Color.FromArgb(35, 76, 115); // azul profundo
        static readonly Color ColorBotonPantalla = Color.FromArgb(246, 185, 211); // rosa pastel
        static readonly Color ColorBotonHover = Color.FromArgb(255, 205, 226);
        static readonly Color ColorBotonPresionado = Color.FromArgb(231, 151, 188);
        static readonly Font FuenteTituloPantalla = new Font("Ravie", 36, FontStyle.Regular);
        static readonly Font FuenteSubtituloPantalla = new Font("Bradley Hand ITC", 17, FontStyle.Bold);
        static readonly Font FuenteTextoPantalla = new Font("Bradley Hand ITC", 13, FontStyle.Bold);
        static readonly Font FuenteBotonPantalla = new Font("Bauhaus 93", 12, FontStyle.Regular);

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
            panelPantalla.BackColor = ColorFondoPantalla;
            panelPantalla.Visible = false;

            lblPantallaTitulo.SetBounds(0, 55, panelPantalla.Width, 90);
            lblPantallaTitulo.TextAlign = ContentAlignment.MiddleCenter;
            lblPantallaTitulo.ForeColor = ColorTituloPantalla;
            lblPantallaTitulo.Font = FuenteTituloPantalla;

            lblPantallaSubtitulo.SetBounds(0, 145, panelPantalla.Width, 40);
            lblPantallaSubtitulo.TextAlign = ContentAlignment.MiddleCenter;
            lblPantallaSubtitulo.ForeColor = ColorTextoPantalla;
            lblPantallaSubtitulo.Font = FuenteSubtituloPantalla;

            lblPantallaTexto.SetBounds(110, 200, panelPantalla.Width - 220, 160);
            lblPantallaTexto.TextAlign = ContentAlignment.MiddleCenter;
            lblPantallaTexto.ForeColor = ColorTextoPantalla;
            lblPantallaTexto.Font = FuenteTextoPantalla;

            btnPantalla.Size = new Size(280, 58);
            btnPantalla.MinimumSize = new Size(280, 58);
            btnPantalla.MaximumSize = new Size(280, 58);
            btnPantalla.Location = new Point((panelPantalla.Width - btnPantalla.Width) / 2, 330);
            btnPantalla.Font = FuenteBotonPantalla;
            btnPantalla.TextAlign = ContentAlignment.MiddleCenter;
            btnPantalla.AutoSize = false;
            btnPantalla.UseVisualStyleBackColor = false;
            btnPantalla.Padding = Padding.Empty;
            btnPantalla.BackColor = ColorBotonPantalla;
            btnPantalla.ForeColor = Color.Black;
            btnPantalla.FlatStyle = FlatStyle.Flat;
            btnPantalla.FlatAppearance.BorderSize = 0;
            btnPantalla.FlatAppearance.MouseOverBackColor = ColorBotonHover;
            btnPantalla.FlatAppearance.MouseDownBackColor = ColorBotonPresionado;
            btnPantalla.Click += (s, e) =>
            {
                // Evita doble disparo (Enter mantenido / Enter + clic) al encadenar pantallas.
                if ((DateTime.Now - instantePantalla).TotalMilliseconds < 300) return;
                Action? accion = accionPantalla;
                if (accion == null) return;
                audio.Efecto("button");
                accionPantalla = null;
                pantallaActiva = false;
                panelPantalla.Visible = false;
                accion();
            };

            btnContinuar.Text = "CONTINUAR PARTIDA";
            btnContinuar.Size = new Size(280, 58);
            btnContinuar.MinimumSize = new Size(280, 58);
            btnContinuar.MaximumSize = new Size(280, 58);
            btnContinuar.Font = FuenteBotonPantalla;
            btnContinuar.TextAlign = ContentAlignment.MiddleCenter;
            btnContinuar.AutoSize = false;
            btnContinuar.UseVisualStyleBackColor = false;
            btnContinuar.Padding = Padding.Empty;
            btnContinuar.BackColor = ColorBotonPantalla;
            btnContinuar.ForeColor = Color.Black;
            btnContinuar.FlatStyle = FlatStyle.Flat;
            btnContinuar.FlatAppearance.BorderSize = 0;
            btnContinuar.FlatAppearance.MouseOverBackColor = ColorBotonHover;
            btnContinuar.FlatAppearance.MouseDownBackColor = ColorBotonPresionado;
            btnContinuar.Location = new Point((panelPantalla.Width - btnContinuar.Width) / 2, 380);
            btnContinuar.Visible = false;
            btnContinuar.Click += (s, e) => {
                audio.Efecto("button");
                panelPantalla.Visible = false;
                pantallaActiva = false;
                SolicitarContinuacion();
            };
            txtAlias.SetBounds(285, 280, 350, 46);
            txtAlias.Font = new Font("Bradley Hand ITC", 15, FontStyle.Bold);
            txtAlias.BackColor = Color.White;
            txtAlias.ForeColor = ColorTextoPantalla;
            txtAlias.MaxLength = 40;
            txtAlias.TextAlign = HorizontalAlignment.Center;
            txtAlias.BorderStyle = BorderStyle.FixedSingle;
            txtAlias.Visible = false;
            txtAlias.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ConfirmarAlias(); }
            };

            lblErrorAlias.SetBounds(175, 333, 570, 30);
            lblErrorAlias.TextAlign = ContentAlignment.MiddleCenter;
            lblErrorAlias.ForeColor = Color.FromArgb(173, 52, 91);
            lblErrorAlias.Font = new Font("Bradley Hand ITC", 10, FontStyle.Bold);
            lblErrorAlias.Visible = false;

            btnVolverAlias.Text = "VOLVER";
            btnVolverAlias.Size = new Size(170, 38);
            btnVolverAlias.Location = new Point((panelPantalla.Width - btnVolverAlias.Width) / 2, 441);
            btnVolverAlias.BackColor = ColorBotonPantalla;
            btnVolverAlias.ForeColor = Color.Black;
            btnVolverAlias.Font = FuenteBotonPantalla;
            btnVolverAlias.FlatStyle = FlatStyle.Flat;
            btnVolverAlias.FlatAppearance.BorderSize = 0;
            btnVolverAlias.FlatAppearance.MouseOverBackColor = ColorBotonHover;
            btnVolverAlias.FlatAppearance.MouseDownBackColor = ColorBotonPresionado;
            btnVolverAlias.Visible = false;
            btnVolverAlias.Click += (s, e) => { audio.Efecto("button"); MostrarPantallaInicio(); };

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
            // Distribución basada en la altura real del panel: evita solapamientos
            // y garantiza que los dos botones de inicio queden completos y visibles.
            int yBotonPrincipal = titulo == "PIXEL ESCAPE"
                ? Math.Max(280, panelPantalla.Height - 165)
                : Math.Max(300, panelPantalla.Height - 105);
            int yBotonContinuar = panelPantalla.Height - 92;
            btnPantalla.Size = new Size(280, 58);
            btnContinuar.Size = new Size(280, 58);
            btnPantalla.Location = new Point((panelPantalla.Width - btnPantalla.Width) / 2, yBotonPrincipal);
            btnContinuar.Location = new Point((panelPantalla.Width - btnContinuar.Width) / 2, yBotonContinuar);
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
            btnPantalla.Location = new Point((panelPantalla.Width - btnPantalla.Width) / 2, 365);
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
            btnPantalla.Location = new Point((panelPantalla.Width - btnPantalla.Width) / 2, 370);
            if (cargandoAlias) ContinuarPartida(alias);
            else IniciarPartidaConAlias(alias);
        }

        // 1) Pantalla de inicio: nombre del juego y botón de empezar.
        void MostrarPantallaInicio()
        {
            audio.DetenerMusica();
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
                audio.IniciarMusica();
                bucleJuego.Start();
                Visor.Focus();
            });
        }
    }
}
