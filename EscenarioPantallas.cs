namespace PixelEsGUI
{
    // Pantallas a ventana completa (BORRADOR funcional, sin diseño visual todavía):
    public partial class Escenario
    {
        // Paleta retro y tipografías compartidas por todas las pantallas previas al nivel.
        static readonly Color ColorFondoPantalla = Color.FromArgb(49, 27, 91); // violeta retro // azul claro
        static readonly Color ColorTituloPantalla = Color.FromArgb(255, 244, 206); // marfil // morado
        static readonly Color ColorTextoPantalla = Color.FromArgb(238, 229, 255); // azul profundo
        static readonly Color ColorBotonPantalla = Color.FromArgb(255, 207, 64); // rosa pastel
        static readonly Color ColorBotonHover = Color.FromArgb(255, 225, 111);
        static readonly Color ColorBotonPresionado = Color.FromArgb(226, 170, 42);
        static readonly Font FuenteTituloPantalla = new Font("Ravie", 30, FontStyle.Regular);
        static readonly Font FuenteSubtituloPantalla = new Font("Consolas", 16, FontStyle.Bold);
        static readonly Font FuenteTextoPantalla = new Font("Consolas", 13, FontStyle.Bold);
        static readonly Font FuenteBotonPantalla = new Font("Consolas", 13, FontStyle.Bold);

        readonly Panel panelPantalla = new Panel();
        readonly PictureBox spriteMenu = new PictureBox();
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
            panelPantalla.Paint += DibujarDecoracionMenu;
            panelPantalla.Resize += (s, e) => { ActualizarDisenoPantalla(); panelPantalla.Invalidate(); };
            spriteMenu.SizeMode = PictureBoxSizeMode.Zoom;
            spriteMenu.BackColor = Color.Transparent;
            spriteMenu.Size = new Size(112, 112);
            spriteMenu.Image = Properties.Resources.CazadorPixel;
            spriteMenu.Visible = true;

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
            btnPantalla.FlatAppearance.BorderSize = 3;
            btnPantalla.FlatAppearance.BorderColor = Color.FromArgb(33, 21, 58);
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
            btnContinuar.FlatAppearance.BorderSize = 3;
            btnContinuar.FlatAppearance.BorderColor = Color.FromArgb(33, 21, 58);
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
            txtAlias.Font = new Font("Consolas", 15, FontStyle.Bold);
            txtAlias.BackColor = Color.FromArgb(255, 250, 226);
            txtAlias.ForeColor = Color.FromArgb(49, 27, 91);
            txtAlias.MaxLength = 40;
            txtAlias.TextAlign = HorizontalAlignment.Center;
            txtAlias.BorderStyle = BorderStyle.FixedSingle;
            txtAlias.Visible = false;
            txtAlias.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ConfirmarAlias(); }
            };

            lblErrorAlias.SetBounds(175, 333, 570, 30);
            lblErrorAlias.TextAlign = ContentAlignment.MiddleCenter;
            lblErrorAlias.ForeColor = Color.FromArgb(255, 184, 122);
            lblErrorAlias.Font = new Font("Consolas", 10, FontStyle.Bold);
            lblErrorAlias.Visible = false;

            btnVolverAlias.Text = "VOLVER";
            btnVolverAlias.Size = new Size(170, 38);
            btnVolverAlias.Location = new Point((panelPantalla.Width - btnVolverAlias.Width) / 2, 441);
            btnVolverAlias.BackColor = ColorBotonPantalla;
            btnVolverAlias.ForeColor = Color.Black;
            btnVolverAlias.Font = FuenteBotonPantalla;
            btnVolverAlias.FlatStyle = FlatStyle.Flat;
            btnVolverAlias.FlatAppearance.BorderSize = 3;
            btnVolverAlias.FlatAppearance.BorderColor = Color.FromArgb(33, 21, 58);
            btnVolverAlias.FlatAppearance.MouseOverBackColor = ColorBotonHover;
            btnVolverAlias.FlatAppearance.MouseDownBackColor = ColorBotonPresionado;
            btnVolverAlias.Visible = false;
            btnVolverAlias.Click += (s, e) => { audio.Efecto("button"); MostrarPantallaInicio(); };

            panelPantalla.Controls.Add(spriteMenu);
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


        // Disposición proporcional: los botones nunca se superponen, ni siquiera al redimensionar.
        void ActualizarDisenoPantalla()
        {
            int w = panelPantalla.Width, h = panelPantalla.Height;
            if (w < 1 || h < 1) return;
            bool esInicio = lblPantallaTitulo.Text == "PIXEL ESCAPE";
            bool esAlias = txtAlias.Visible;
            lblPantallaTitulo.SetBounds(25, Math.Max(22, h / 9), Math.Max(1, w - 50), 78);
            lblPantallaSubtitulo.SetBounds(35, Math.Max(95, h / 9 + 78), Math.Max(1, w - 70), 44);
            lblPantallaTexto.SetBounds(80, esAlias ? Math.Max(140, h / 3 - 15) : Math.Max(154, h / 3 - 2), Math.Max(1, w - 160), esAlias ? 65 : 145);
            int ancho = Math.Min(320, Math.Max(200, w - 100));
            int x = (w - ancho) / 2;
            btnPantalla.Size = new Size(ancho, 58);
            btnContinuar.Size = new Size(ancho, 58);
            btnPantalla.MinimumSize = btnContinuar.MinimumSize = Size.Empty;
            btnPantalla.MaximumSize = btnContinuar.MaximumSize = Size.Empty;
            if (esInicio)
            {
                btnPantalla.Location = new Point(x, h - 164);
                btnContinuar.Location = new Point(x, h - 90);
            }
            else if (esAlias)
            {
                txtAlias.SetBounds((w - Math.Min(350, w - 100)) / 2, h - 220, Math.Min(350, w - 100), 42);
                lblErrorAlias.SetBounds(50, h - 177, Math.Max(1, w - 100), 30);
                btnPantalla.Location = new Point(x, h - 143);
                btnVolverAlias.Location = new Point((w - btnVolverAlias.Width) / 2, h - 73);
            }
            else
                btnPantalla.Location = new Point(x, h - 110);
            spriteMenu.SetBounds(Math.Max(10, w - 154), Math.Max(162, h / 2 - 15), 104, 104);
            spriteMenu.Visible = !esAlias;
            panelPantalla.Invalidate();
        }

        // Decoración pixel-art sencilla sin añadir archivos externos ni alterar el motor 2D.
        void DibujarDecoracionMenu(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            int w = panelPantalla.Width, h = panelPantalla.Height;
            using var borde = new Pen(Color.FromArgb(128, 92, 181), 6);
            g.DrawRectangle(borde, 15, 15, Math.Max(1, w - 30), Math.Max(1, h - 30));
            using var sombra = new SolidBrush(Color.FromArgb(37, 22, 73));
            using var brillo = new SolidBrush(Color.FromArgb(255, 207, 64));
            for (int i = 0; i < 7; i++)
            {
                int px = 44 + i * 48;
                int py = h - 30 - (i % 3) * 7;
                g.FillRectangle(sombra, px, py, 32, 16);
            }
            for (int i = 0; i < 5; i++)
            {
                int px = 65 + i * Math.Max(55, (w - 130) / 5);
                g.FillRectangle(brillo, px, 34 + (i % 2) * 14, 7, 7);
            }
            // Flecha retro junto al personaje en el lateral.
            using var linea = new Pen(brillo, 5);
            int fx = Math.Max(25, w - 190), fy = h / 2 + 42;
            g.DrawLine(linea, fx, fy, fx + 33, fy);
            g.DrawLine(linea, fx + 21, fy - 12, fx + 33, fy);
            g.DrawLine(linea, fx + 21, fy + 12, fx + 33, fy);
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
            ActualizarDisenoPantalla();
            txtAlias.Visible = false;
            lblErrorAlias.Visible = false;
            btnVolverAlias.Visible = false;
            panelPantalla.Visible = true;
            panelPantalla.BringToFront();
            ActualizarDisenoPantalla();
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
            ActualizarDisenoPantalla();
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
            ActualizarDisenoPantalla();
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
