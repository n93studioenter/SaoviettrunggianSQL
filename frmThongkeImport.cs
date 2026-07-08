using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaovietTax
{
    public partial class frmThongkeImport : DevExpress.XtraEditors.XtraForm
    {
        public frmThongkeImport()
        {
            InitializeComponent();
        }
        public frmMain frmmain { get; set; }
        private void frmThongkeImport_Load(object sender, EventArgs e)
        {
            //XtraMessageBox.Show(frmmain.lstImportResult.Count.ToString());
            lblTongso.Text = frmmain.lstImportResult.Count.ToString();
            lblTongsoTC.Text = frmmain.lstImportResult.Where(m => m.Status == 1).Count().ToString();
            lblTongsoTB.Text = frmmain.lstImportResult.Where(m => m.Status == -1).Count().ToString();
            gridControl1.DataSource = frmmain.lstImportResult;  
        }

        private void frmThongkeImport_FormClosed(object sender, FormClosedEventArgs e)
        {

        }
    }
}