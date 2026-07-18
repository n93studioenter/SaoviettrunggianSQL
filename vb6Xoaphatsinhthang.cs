using DevExpress.XtraEditors;
using SaovietTax.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaovietTax
{
    public partial class vb6Xoaphatsinhthang : DevExpress.XtraEditors.XtraForm
    {
        public vb6Xoaphatsinhthang()
        {
            InitializeComponent();
        }

        private void vb6Xoaphatsinhthang_Load(object sender, EventArgs e)
        {
            string appPath = Assembly.GetExecutingAssembly().Location;
            string directoryPath = Path.GetDirectoryName(appPath);
            string rootDirectory = Path.GetFullPath(Path.Combine(directoryPath, @"..\.."));
            string filePath = Path.Combine(rootDirectory, "Hoadon", "invoice.txt");
            string _content = File.ReadAllText(filePath);
            if (XtraMessageBox.Show(
               $"Bạn có chắc muốn xóa hóa toàn bộ đơn tháng {_content} không?",
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
                         

                            // Lấy danh sách MaCT từ file


                            // Lấy danh sách ChungTu cần xóa
                            // Lọc trực tiếp trên database
                            var chungTuToDelete = db.ChungTus.ToList()
                                .Where(ct => ct.ThangCT == int.Parse(_content))
                                .ToList();
                            var tbimp = db.tbimports.ToList().Where(m =>chungTuToDelete.Any(n=>n.SoHieu==m.SHDon && n.NgayCT.Value.Date==m.NLap.Value.Date)).ToList();
                            foreach (var it in tbimp)
                            {
                                it.Status = 0;
                               
                            }
                            var getone = chungTuToDelete.FirstOrDefault();
                            var getgv = chungTuToDelete.Where(m => m.SoHieu.Contains(getone.SoHieu) && m.NgayCT.Value.Date == getone.NgayCT.Value.Date && m.SoHieu.Contains("GV")).ToList();
                            db.ChungTus.RemoveRange(getgv);
                            db.SaveChanges();
                            if (chungTuToDelete.Any())
                            {
                                // Lấy danh sách MaSo từ ChungTu
                                var listMaSo = chungTuToDelete.Select(ct => ct.MaSo).ToList();

                                // Lấy danh sách HoaDon cần xóa
                                var hoaDonToDelete = db.HoaDons
                                    .Where(hd => listMaSo.Contains(hd.MaSo))
                                    .ToList();

                                // Xóa HoaDon trước (nếu có)
                                if (hoaDonToDelete.Any())
                                {
                                    db.HoaDons.RemoveRange(hoaDonToDelete);
                                }

                                // Xóa ChungTu
                                db.ChungTus.RemoveRange(chungTuToDelete);

                                // Lưu thay đổi
                                db.SaveChanges();

                                // Commit transaction
                                transaction.Commit();

                                XtraMessageBox.Show(
                                    $"Đã xóa thành công !",
                                    "Thành công",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            }
                            else
                            {
                                XtraMessageBox.Show(
                                    "Không tìm thấy hóa đơn nào để xóa!",
                                    "Thông báo",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                            }
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
                    }
                }
            }
            this.Close();
        }
    }
}