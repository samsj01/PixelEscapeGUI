namespace PixelEsGUI
{
    partial class Escenario
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Visor = new Engine2D.GraphicsView();
            SuspendLayout();
            // 
            // Visor
            // 
            Visor.BackColor = SystemColors.GradientInactiveCaption;
            Visor.Location = new Point(-1, -1);
            Visor.Margin = new Padding(3, 2, 3, 2);
            Visor.Name = "Visor";
            Visor.Scene = null;
            Visor.Size = new Size(755, 494);
            Visor.TabIndex = 0;
            // 
            // Escenario
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(754, 490);
            Controls.Add(Visor);
            Margin = new Padding(3, 2, 3, 2);
            Name = "Escenario";
            Text = "Escenario";
            ResumeLayout(false);
        }

        #endregion

        private Engine2D.GraphicsView Visor;

        //  Niveles
        // Control de niveles
        int nivelActual = 1;

        // Cantidad de monedas recogidas en el nivel actual
        int monedasRecogidas = 0;

        // Control de los enemigos
        bool evitoCazador = true;
        bool evitoGuardian = true;

        // Control de finalización del nivel
        bool juegoTerminado = false;

        //Swicth case para cambiar de nivel 
        bool CumpleObjetivosNivel()
        {
            switch (nivelActual)
            {
                case 1:
                    // Nivel 1: recoger 20 monedas
                    return monedasRecogidas >= 20;

                case 2:
                    // Nivel 2: llave y evitar los dos enemigos
                    return jugador.TieneLlave
                        && evitoCazador
                        && evitoGuardian;

                case 3:
                    // Nivel 3: monedas, llave y evitar enemigos
                    return monedasRecogidas >= 20
                        && jugador.TieneLlave
                        && evitoCazador
                        && evitoGuardian;

                default:
                    return false;
            }
        }



    }
}
