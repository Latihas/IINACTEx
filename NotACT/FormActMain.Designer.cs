using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Advanced_Combat_Tracker {
    partial class FormActMain {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.SuspendLayout();
            // 
            // FormActMain
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(345, 250);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormActMain";
            this.Text = "Super Advanced Combat Tracker";
            this.ResumeLayout(false);

        }

        #endregion
    }
}