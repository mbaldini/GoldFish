namespace GoldFishPet
{
    partial class FishForm
    {
        private System.ComponentModel.IContainer components = null;
        
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FishForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(211, 113);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FishForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "FishForm";
            this.ResumeLayout(false);

        }

        #endregion
    }
}