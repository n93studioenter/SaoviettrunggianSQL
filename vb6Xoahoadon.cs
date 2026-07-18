using DevExpress.XtraEditors;
using SaovietTax.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SaovietTax
{
    public partial class vb6Xoahoadon : DevExpress.XtraEditors.XtraForm
    {
        public vb6Xoahoadon()
        {
            InitializeComponent();
        }

        private void vb6Xoahoadon_Load(object sender, EventArgs e)
        {
            if (XtraMessageBox.Show(
                "Bạn có chắc muốn xóa hóa đơn này không?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                using (var db = new DatablankEntities())
                {
                    // Bắt đầu transaction
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            string appPath = Assembly.GetExecutingAssembly().Location;
                            string directoryPath = Path.GetDirectoryName(appPath);
                            string rootDirectory = Path.GetFullPath(Path.Combine(directoryPath, @"..\.."));
                            string filePath = Path.Combine(rootDirectory, "Hoadon", "invoice.txt");
                            string _content = File.ReadAllText(filePath);

                            // Lấy danh sách MaCT từ file
                            var getsplit = _content.Split(',')
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Select(s => int.Parse(s))
                                .ToList();
                            foreach(var item in getsplit)
                            {
                                var getfirstCt=db.ChungTus.Where(m=>m.MaCT.Value==item).FirstOrDefault();
                                var getremain = db.ChungTus.ToList().Where(m => m.SoHieu.Contains(getfirstCt.SoHieu) && m.NgayCT.Value.Date == getfirstCt.NgayCT.Value.Date).ToList();
                                List<SaovietTax.Models.ChungTu> lastordefault = getremain.Where(m=>m.SoPS2No==0 && m.SoPS2Co==0).ToList();
                                if (getfirstCt.MaLoai == 0)
                                {
                                    lastordefault = lastordefault.Skip(1).ToList();
                                }
                                var tbimp = db.tbimports.ToList().Where(m => m.SHDon == getfirstCt.SoHieu && m.NLap.Value.Date == getfirstCt.NgayCT.Value.Date).FirstOrDefault();
                                if (tbimp != null)
                                {
                                    tbimp.Status = 0;
                                }
                              
                                foreach (var hd in lastordefault)
                                {
                                    var hoadon=db.HoaDons.Where(m=>m.MaSo==hd.MaSo).FirstOrDefault();
                                    db.HoaDons.Remove(hoadon);
                                }
                                db.ChungTus.RemoveRange(getremain);
                            }
                            db.SaveChanges();
                            transaction.Commit();
                            
                        }
                        catch (Exception ex)
                        {
                            // Rollback khi có lỗi
                            transaction.Rollback();
                            XtraMessageBox.Show(
                                $"Lỗi khi xóa hóa đơn: {ex.Message}",
                                "Lỗi",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                        finally
                        {
                                            int rowsAffected = db.Database.ExecuteSqlCommand(
                       "UPDATE tbResponse SET Status = 1",
                       1000, 131); 
                        }
                    }
                }
            }
            this.Close();
        }
    }
}