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

                            // Lấy danh sách ChungTu cần xóa
                            var chungTuToDelete = db.ChungTus
                                .Where(ct => getsplit.Contains(ct.MaCT.Value))
                                .ToList();

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