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
            Visor.Location = new Point(21, 47);
            Visor.Name = "Visor";
            Visor.Scene = null;
            Visor.Size = new Size(800, 550);
            Visor.TabIndex = 0;
            // 
            // Escenario
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(862, 653);
            Controls.Add(Visor);
            Name = "Escenario";
            Text = "Escenario";
            ResumeLayout(false);
        }

        #endregion

        private Engine2D.GraphicsView Visor;
    }
}
