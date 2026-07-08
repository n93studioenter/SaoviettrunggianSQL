namespace SaovietTax
{
    partial class frmThongkeImport
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
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.lblTongso = new DevExpress.XtraEditors.LabelControl();
            this.lblTongsoTC = new DevExpress.XtraEditors.LabelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.lblTongsoTB = new DevExpress.XtraEditors.LabelControl();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl1
            // 
            this.gridControl1.Location = new System.Drawing.Point(12, 48);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(1037, 506);
            this.gridControl1.TabIndex = 0;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(12, 12);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(105, 16);
            this.labelControl1.TabIndex = 1;
            this.labelControl1.Text = "Tổng số hoá đơn :";
            // 
            // lblTongso
            // 
            this.lblTongso.Location = new System.Drawing.Point(126, 12);
            this.lblTongso.Name = "lblTongso";
            this.lblTongso.Size = new System.Drawing.Size(55, 16);
            this.lblTongso.TabIndex = 2;
            this.lblTongso.Text = "lblTongso";
            // 
            // lblTongsoTC
            // 
            this.lblTongsoTC.Location = new System.Drawing.Point(461, 12);
            this.lblTongsoTC.Name = "lblTongsoTC";
            this.lblTongsoTC.Size = new System.Drawing.Size(75, 16);
            this.lblTongsoTC.TabIndex = 4;
            this.lblTongsoTC.Text = "labelControl2";
            // 
            // labelControl3
            // 
            this.labelControl3.Location = new System.Drawing.Point(329, 12);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(117, 16);
            this.labelControl3.TabIndex = 3;
            this.labelControl3.Text = "Impoer thành công :";
            // 
            // lblTongsoTB
            // 
            this.lblTongsoTB.Location = new System.Drawing.Point(803, 12);
            this.lblTongsoTB.Name = "lblTongsoTB";
            this.lblTongsoTB.Size = new System.Drawing.Size(75, 16);
            this.lblTongsoTB.TabIndex = 6;
            this.lblTongsoTB.Text = "labelControl4";
            // 
            // labelControl5
            // 
            this.labelControl5.Location = new System.Drawing.Point(689, 12);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(94, 16);
            this.labelControl5.TabIndex = 5;
            this.labelControl5.Text = "Import thất bại :";
            // 
            // frmThongkeImport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1061, 566);
            this.Controls.Add(this.lblTongsoTB);
            this.Controls.Add(this.labelControl5);
            this.Controls.Add(this.lblTongsoTC);
            this.Controls.Add(this.labelControl3);
            this.Controls.Add(this.lblTongso);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.gridControl1);
            this.Name = "frmThongkeImport";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmThongkeImport";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmThongkeImport_FormClosed);
            this.Load += new System.EventHandler(this.frmThongkeImport_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl lblTongso;
        private DevExpress.XtraEditors.LabelControl lblTongsoTC;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl lblTongsoTB;
        private DevExpress.XtraEditors.LabelControl labelControl5;
    }
}