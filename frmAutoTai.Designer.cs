namespace SaovietTax
{
    partial class frmAutoTai
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.progressPanel1 = new DevExpress.XtraWaitForm.ProgressPanel();
            this.chkDauvao = new DevExpress.XtraEditors.CheckEdit();
            this.chkdaura = new DevExpress.XtraEditors.CheckEdit();
            ((System.ComponentModel.ISupportInitialize)(this.chkDauvao.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkdaura.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // progressPanel1
            // 
            this.progressPanel1.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.progressPanel1.Appearance.Options.UseBackColor = true;
            this.progressPanel1.Location = new System.Drawing.Point(176, 103);
            this.progressPanel1.Name = "progressPanel1";
            this.progressPanel1.Size = new System.Drawing.Size(246, 66);
            this.progressPanel1.TabIndex = 0;
            this.progressPanel1.Text = "progressPanel1";
            // 
            // chkDauvao
            // 
            this.chkDauvao.Location = new System.Drawing.Point(154, 51);
            this.chkDauvao.Name = "chkDauvao";
            this.chkDauvao.Properties.Caption = "checkEdit1";
            this.chkDauvao.Size = new System.Drawing.Size(94, 20);
            this.chkDauvao.TabIndex = 1;
            // 
            // chkdaura
            // 
            this.chkdaura.Location = new System.Drawing.Point(317, 51);
            this.chkdaura.Name = "chkdaura";
            this.chkdaura.Properties.Caption = "checkEdit1";
            this.chkdaura.Size = new System.Drawing.Size(94, 20);
            this.chkdaura.TabIndex = 2;
            // 
            // frmAutoTai
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(567, 391);
            this.Controls.Add(this.chkdaura);
            this.Controls.Add(this.chkDauvao);
            this.Controls.Add(this.progressPanel1);
            this.Name = "frmAutoTai";
            this.Text = "frmAutoTai";
            this.Load += new System.EventHandler(this.frmAutoTai_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chkDauvao.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkdaura.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraWaitForm.ProgressPanel progressPanel1;
        private DevExpress.XtraEditors.CheckEdit chkDauvao;
        private DevExpress.XtraEditors.CheckEdit chkdaura;
    }
}