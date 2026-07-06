using DevExpress.XtraEditors;
using SaovietTax.Models;
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
    public partial class AutoSumTonkho : DevExpress.XtraEditors.XtraForm
    {
        public AutoSumTonkho()
        {
            InitializeComponent();
        }
        DatablankEntities db = new DatablankEntities();
        private void AutoSumTonkho_Load(object sender, EventArgs e)
        {
            SQL_TinhTonKho();
        }
        /// <summary>
        /// Hàm chính: Cập nhật tồn kho bằng SQL (Đã sửa lỗi cộng nhầm xuất)
        /// </summary>
        private void SQL_TinhTonKho()
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. INSERT vật tư mới (chưa có trong TonKho)
                    string sqlInsert = @"
                INSERT INTO TonKho (MaVatTu, MaSoKho, MaTaiKhoan, Luong_0, Tien_0)
                SELECT 
                    vt.MaSo,
                    2 AS MaSoKho,
                    39 AS MaTaiKhoan,
                    0 AS Luong_0,  -- 👈 Tồn đầu kỳ mặc định = 0
                    0 AS Tien_0    -- 👈 Tiền tồn đầu kỳ mặc định = 0
                FROM Vattu vt
                WHERE vt.MaSo IN (SELECT DISTINCT MaVattu FROM ChungTu)
                  AND NOT EXISTS (SELECT 1 FROM TonKho t WHERE t.MaVatTu = vt.MaSo)";

                    db.Database.ExecuteSqlCommand(sqlInsert);

                    // 2. RESET các cột tháng 1-12
                    string sqlReset = @"
                UPDATE TonKho SET
                    Tien_Nhap_1 = 0, Luong_Nhap_1 = 0, Tien_Xuat_1 = 0, Luong_Xuat_1 = 0, Luong_1 = 0, Tien_1 = 0,
                    Tien_Nhap_2 = 0, Luong_Nhap_2 = 0, Tien_Xuat_2 = 0, Luong_Xuat_2 = 0, Luong_2 = 0, Tien_2 = 0,
                    Tien_Nhap_3 = 0, Luong_Nhap_3 = 0, Tien_Xuat_3 = 0, Luong_Xuat_3 = 0, Luong_3 = 0, Tien_3 = 0,
                    Tien_Nhap_4 = 0, Luong_Nhap_4 = 0, Tien_Xuat_4 = 0, Luong_Xuat_4 = 0, Luong_4 = 0, Tien_4 = 0,
                    Tien_Nhap_5 = 0, Luong_Nhap_5 = 0, Tien_Xuat_5 = 0, Luong_Xuat_5 = 0, Luong_5 = 0, Tien_5 = 0,
                    Tien_Nhap_6 = 0, Luong_Nhap_6 = 0, Tien_Xuat_6 = 0, Luong_Xuat_6 = 0, Luong_6 = 0, Tien_6 = 0,
                    Tien_Nhap_7 = 0, Luong_Nhap_7 = 0, Tien_Xuat_7 = 0, Luong_Xuat_7 = 0, Luong_7 = 0, Tien_7 = 0,
                    Tien_Nhap_8 = 0, Luong_Nhap_8 = 0, Tien_Xuat_8 = 0, Luong_Xuat_8 = 0, Luong_8 = 0, Tien_8 = 0,
                    Tien_Nhap_9 = 0, Luong_Nhap_9 = 0, Tien_Xuat_9 = 0, Luong_Xuat_9 = 0, Luong_9 = 0, Tien_9 = 0,
                    Tien_Nhap_10 = 0, Luong_Nhap_10 = 0, Tien_Xuat_10 = 0, Luong_Xuat_10 = 0, Luong_10 = 0, Tien_10 = 0,
                    Tien_Nhap_11 = 0, Luong_Nhap_11 = 0, Tien_Xuat_11 = 0, Luong_Xuat_11 = 0, Luong_11 = 0, Tien_11 = 0,
                    Tien_Nhap_12 = 0, Luong_Nhap_12 = 0, Tien_Xuat_12 = 0, Luong_Xuat_12 = 0, Luong_12 = 0, Tien_12 = 0
                WHERE EXISTS (SELECT 1 FROM ChungTu WHERE MaVattu = TonKho.MaVatTu)";

                    db.Database.ExecuteSqlCommand(sqlReset);

                    // 3. Cập nhật tồn kho
                    string sqlUpdate = SQL_BuildTonKhoUpdate();
                    db.Database.ExecuteSqlCommand(sqlUpdate);

                    transaction.Commit();
                     XtraMessageBox.Show("✅ Cập nhật tồn kho thành công!");
                    this.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    XtraMessageBox.Show($"❌ Lỗi: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Xây dựng câu SQL UPDATE tồn kho (Đã sửa tất cả lỗi)
        /// </summary>
        private string SQL_BuildTonKhoUpdate()
        {
            var sb = new StringBuilder();

            sb.Append(@"
        UPDATE TonKho SET
            -- ===== THÁNG 1 =====
            Tien_Nhap_1 = ISNULL(t1.NhapTien, 0),
            Luong_Nhap_1 = ISNULL(t1.NhapLuong, 0),
            Tien_Xuat_1 = ROUND(ISNULL(t1.XuatLuong, 0) * 
                (ISNULL(t.Tien_0, 0) + ISNULL(t1.NhapTien, 0)) / 
                NULLIF((ISNULL(t.Luong_0, 0) + ISNULL(t1.NhapLuong, 0)), 0), 0),
            Luong_Xuat_1 = ISNULL(t1.XuatLuong, 0),
            
            -- ✅ SỬA: Tồn = Tồn đầu + Nhập - Xuất
            Luong_1 = ISNULL(t.Luong_0, 0) + ISNULL(t1.NhapLuong, 0) - ISNULL(t1.XuatLuong, 0),
            Tien_1 = ROUND(ISNULL(t.Tien_0, 0) + ISNULL(t1.NhapTien, 0) - 
                ISNULL(t1.XuatLuong, 0) * 
                (ISNULL(t.Tien_0, 0) + ISNULL(t1.NhapTien, 0)) / 
                NULLIF((ISNULL(t.Luong_0, 0) + ISNULL(t1.NhapLuong, 0)), 0), 0)");

            for (int thang = 2; thang <= 12; thang++)
            {
                string tongLuong = SQL_BuildTongLuong(thang);
                string tongTien = SQL_BuildTongTien(thang);
                string tongXuatLuong = SQL_BuildTongXuatLuong(thang);

                sb.Append($@",
            -- ===== THÁNG {thang} =====
            Tien_Nhap_{thang} = ISNULL(t{thang}.NhapTien, 0),
            Luong_Nhap_{thang} = ISNULL(t{thang}.NhapLuong, 0),
            Tien_Xuat_{thang} = ROUND(ISNULL(t{thang}.XuatLuong, 0) * 
                ({tongTien}) / NULLIF({tongLuong}, 0), 0),
            Luong_Xuat_{thang} = ISNULL(t{thang}.XuatLuong, 0),
            
            -- ✅ SỬA: Tồn = Tồn đầu + Nhập - Xuất (có ngoặc đảm bảo trừ)
            Luong_{thang} = ({tongLuong}) - ({tongXuatLuong}),
            Tien_{thang} = ROUND({tongTien} - 
                ISNULL(t{thang}.XuatLuong, 0) * 
                ({tongTien}) / NULLIF({tongLuong}, 0), 0)");
            }

            sb.Append(@"
        FROM TonKho t
        OUTER APPLY (
            SELECT 
                SUM(CASE WHEN MaLoai = 1 THEN SoPS2No ELSE 0 END) AS NhapLuong,
                SUM(CASE WHEN MaLoai = 1 THEN SoPS ELSE 0 END) AS NhapTien,
                SUM(CASE WHEN MaLoai = 2 AND SoHieu LIKE '%GV%' THEN SoPS2Co ELSE 0 END) AS XuatLuong
            FROM ChungTu
            WHERE MaVattu = t.MaVatTu AND ThangCT = 1
        ) t1");

            for (int thang = 2; thang <= 12; thang++)
            {
                sb.Append($@"
        OUTER APPLY (
            SELECT 
                SUM(CASE WHEN MaLoai = 1 THEN SoPS2No ELSE 0 END) AS NhapLuong,
                SUM(CASE WHEN MaLoai = 1 THEN SoPS ELSE 0 END) AS NhapTien,
                SUM(CASE WHEN MaLoai = 2 AND SoHieu LIKE '%GV%' THEN SoPS2Co ELSE 0 END) AS XuatLuong
            FROM ChungTu
            WHERE MaVattu = t.MaVatTu AND ThangCT = {thang}
        ) t{thang}");
            }

            sb.Append(" WHERE EXISTS (SELECT 1 FROM ChungTu WHERE MaVattu = t.MaVatTu)");

            return sb.ToString();
        }

        /// <summary>
        /// Xây dựng biểu thức Tổng số lượng (Tồn đầu + Nhập các tháng)
        /// </summary>
        private string SQL_BuildTongLuong(int thang)
        {
            string result = "ISNULL(t.Luong_0, 0)";
            for (int i = 1; i <= thang; i++)
            {
                result += $" + ISNULL(t{i}.NhapLuong, 0)";
            }
            return result;
        }

        /// <summary>
        /// Xây dựng biểu thức Tổng tiền (Tồn đầu + Nhập các tháng)
        /// </summary>
        private string SQL_BuildTongTien(int thang)
        {
            string result = "ISNULL(t.Tien_0, 0)";
            for (int i = 1; i <= thang; i++)
            {
                result += $" + ISNULL(t{i}.NhapTien, 0)";
            }
            return result;
        }

        /// <summary>
        /// Xây dựng biểu thức Tổng xuất số lượng (Có ngoặc đảm bảo trừ đúng)
        /// </summary>
        private string SQL_BuildTongXuatLuong(int thang)
        {
            string result = "";
            for (int i = 1; i <= thang; i++)
            {
                if (i > 1) result += " + ";
                result += $"ISNULL(t{i}.XuatLuong, 0)";
            }
            return result;
        }

        /// <summary>
        /// Hàm debug: So sánh EF vs SQL cho 1 vật tư
        /// </summary>
    }
}