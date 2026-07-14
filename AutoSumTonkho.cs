using DevExpress.XtraEditors;
using SaovietTax.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
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

        // Các tham số
        private int _tkVT_ID = 1;
        private int _outCost = 0;
        private int _pTien = 0;
        private int _pGiaUSD = 0;
        private int _maskN = 1;

        private void AutoSumTonkho_Load(object sender, EventArgs e)
        {
            try
            {
                if (db.Database.Connection.State != System.Data.ConnectionState.Open)
                {
                    db.Database.Connection.Open();
                }

                KiemTraVatTu(0);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Hàm chính: Kiểm tra và cập nhật tồn kho vật tư
        /// </summary>
        public void KiemTraVatTu(int ktraxuat = 0)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Chuẩn hóa và cập nhật bảng TonKho
                    ChuanHoaTonKho();

                    // 2. Reset dữ liệu nhập xuất
                    ResetTonKho(); 
                    // 3. Cập nhật số phát sinh nhập
                    CapNhatNhap();

                    // 4. Cập nhật số phát sinh xuất
                    CapNhatXuat();

                    // 5. Cập nhật số tồn
                    CapNhatSoTon();

                    // 6. Xóa các bản ghi không cần thiết
                    XoaBanGhiKhongCanThiet();

                    transaction.Commit();
                    XtraMessageBox.Show("✅ Cập nhật tồn kho thành công!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    XtraMessageBox.Show($"❌ Lỗi: {ex.Message}", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    throw;
                }
            }
        }

        #region Private Methods

        /// <summary>
        /// Chuẩn hóa bảng TonKho
        /// </summary>
        private void ChuanHoaTonKho()
        {
            try
            {
                // 1. Xóa các bản ghi có MaVattu không tồn tại trong bảng Vattu
                db.Database.ExecuteSqlCommand(@"
                    DELETE FROM TonKho 
                    WHERE MaVatTu NOT IN (SELECT MaSo FROM Vattu)");

                // 2. Insert vật tư mới từ bảng Vattu vào TonKho
                db.Database.ExecuteSqlCommand(@"
                    INSERT INTO TonKho (MaVatTu, MaSoKho, MaTaiKhoan, Luong_0, Tien_0)
                    SELECT 
                        vt.MaSo,
                        1 AS MaSoKho,
                        1 AS MaTaiKhoan,
                        0,
                        0
                    FROM Vattu vt
                    WHERE vt.MaSo IN (SELECT DISTINCT MaVattu FROM ChungTu)
                      AND NOT EXISTS (SELECT 1 FROM TonKho t WHERE t.MaVatTu = vt.MaSo)");

                // 3. Xóa các bản ghi trùng lặp
                db.Database.ExecuteSqlCommand(@"
                    DELETE FROM TonKho 
                    WHERE MaSoKho = 0 AND MaTaiKhoan = 0 AND Luong_0 = 0 AND Tien_0 = 0");
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi chuẩn hóa TonKho: " + ex.Message);
            }
        }

        /// <summary>
        /// Reset dữ liệu nhập xuất trong bảng TonKho
        /// </summary>
        private void ResetTonKho()
        {
            try
            {
                string sql = "UPDATE TonKho SET MaSoKho = MaSoKho";
                for (int i = 1; i <= 12; i++)
                {
                    sql += $@"
                        , Luong_Nhap_{i} = 0
                        , Luong_Xuat_{i} = 0
                        , Tien_Nhap_{i} = 0
                        , Luong_{i} = 0
                        , Tien_{i} = 0
                        , Tien_Xuat_{i} = 0";

                    if (_pGiaUSD > 0)
                    {
                        sql += $@"
                        , USDTien_Nhap_{i} = 0
                        , USDTien_Xuat_{i} = 0";
                    }
                }
                db.Database.ExecuteSqlCommand(sql);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi reset TonKho: " + ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật số phát sinh nhập
        /// </summary>
        private void CapNhatNhap()
        {
            try
            {
                // Sử dụng SQL trực tiếp với CAST để xử lý kiểu dữ liệu
                string sql = @"
                    SELECT 
                        MaTkNo,
                        MaKho AS MaSoKho,
                        MaVattu,
                        CAST(SUM(CASE WHEN ThangCT = 1 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_1,
                        CAST(SUM(CASE WHEN ThangCT = 1 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_1,
                        CAST(SUM(CASE WHEN ThangCT = 2 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_2,
                        CAST(SUM(CASE WHEN ThangCT = 2 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_2,
                        CAST(SUM(CASE WHEN ThangCT = 3 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_3,
                        CAST(SUM(CASE WHEN ThangCT = 3 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_3,
                        CAST(SUM(CASE WHEN ThangCT = 4 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_4,
                        CAST(SUM(CASE WHEN ThangCT = 4 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_4,
                        CAST(SUM(CASE WHEN ThangCT = 5 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_5,
                        CAST(SUM(CASE WHEN ThangCT = 5 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_5,
                        CAST(SUM(CASE WHEN ThangCT = 6 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_6,
                        CAST(SUM(CASE WHEN ThangCT = 6 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_6,
                        CAST(SUM(CASE WHEN ThangCT = 7 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_7,
                        CAST(SUM(CASE WHEN ThangCT = 7 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_7,
                        CAST(SUM(CASE WHEN ThangCT = 8 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_8,
                        CAST(SUM(CASE WHEN ThangCT = 8 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_8,
                        CAST(SUM(CASE WHEN ThangCT = 9 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_9,
                        CAST(SUM(CASE WHEN ThangCT = 9 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_9,
                        CAST(SUM(CASE WHEN ThangCT = 10 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_10,
                        CAST(SUM(CASE WHEN ThangCT = 10 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_10,
                        CAST(SUM(CASE WHEN ThangCT = 11 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_11,
                        CAST(SUM(CASE WHEN ThangCT = 11 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_11,
                        CAST(SUM(CASE WHEN ThangCT = 12 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_12,
                        CAST(SUM(CASE WHEN ThangCT = 12 THEN SoPS2No ELSE 0 END) AS decimal(18,2)) AS NTNo_12
                    FROM ChungTu
                    WHERE MaLoai IN (1, 4)
                      AND MaVattu > 0
                      AND MaKho > 0
                    GROUP BY MaTkNo, MaKho, MaVattu";

                // Sử dụng SqlQuery với kiểu dynamic để tránh lỗi cast
                var data = db.Database.SqlQuery<NhapData>(sql).ToList();

                foreach (var item in data)
                {
                    string updateSql = BuildUpdateNhapSQL(item);

                    try
                    {
                        int affected = db.Database.ExecuteSqlCommand(updateSql,
                            item.MaSoKho, item.MaTkNo, item.MaVattu);

                        if (affected == 0)
                        {
                            // Thêm mới nếu không tìm thấy
                            db.Database.ExecuteSqlCommand(@"
                                INSERT INTO TonKho (MaSoKho, MaTaiKhoan, MaVatTu, Luong_0, Tien_0) 
                                VALUES ({0}, {1}, {2}, 0, 0)",
                                item.MaSoKho, item.MaTkNo, item.MaVattu);

                            db.Database.ExecuteSqlCommand(updateSql,
                                item.MaSoKho, item.MaTkNo, item.MaVattu);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi và tiếp tục
                        Console.WriteLine($"Lỗi cập nhật nhập: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi cập nhật nhập: " + ex.Message);
            }
        }
        /// <summary>
        /// Xây dựng SQL UPDATE nhập
        /// </summary>
        private string BuildUpdateNhapSQL(NhapData data)
        {
            var sql = new StringBuilder("UPDATE TonKho SET MaSoKho = MaSoKho");

            for (int i = 1; i <= 12; i++)
            {
                decimal noValue = GetSafeDecimalValue(data, $"No_{i}");
                decimal ntNoValue = GetSafeDecimalValue(data, $"NTNo_{i}");

                sql.Append($@"
                    , Tien_Nhap_{i} = {noValue}
                    , Luong_Nhap_{i} = {ntNoValue}");
            }

            sql.Append(" WHERE MaSoKho = {0} AND MaTaiKhoan = {1} AND MaVatTu = {2}");
            return sql.ToString();
        }
        /// <summary>
        /// Cập nhật số phát sinh xuất - GIỮ NGUYÊN LOGIC VB6 (Có lọc "GV")
        /// </summary>
        private void CapNhatXuat()
        {
            try
            {
                // ⭐ GIỮ NGUYÊN: Lọc "GV" như VB6
                string sql = @"
            SELECT 
                MaTkCo,
                MaKho AS MaSoKho,
                MaVattu,
                CAST(SUM(CASE WHEN ThangCT = 1 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_1,
                CAST(SUM(CASE WHEN ThangCT = 1 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_1,
                CAST(SUM(CASE WHEN ThangCT = 2 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_2,
                CAST(SUM(CASE WHEN ThangCT = 2 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_2,
                CAST(SUM(CASE WHEN ThangCT = 3 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_3,
                CAST(SUM(CASE WHEN ThangCT = 3 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_3,
                CAST(SUM(CASE WHEN ThangCT = 4 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_4,
                CAST(SUM(CASE WHEN ThangCT = 4 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_4,
                CAST(SUM(CASE WHEN ThangCT = 5 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_5,
                CAST(SUM(CASE WHEN ThangCT = 5 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_5,
                CAST(SUM(CASE WHEN ThangCT = 6 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_6,
                CAST(SUM(CASE WHEN ThangCT = 6 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_6,
                CAST(SUM(CASE WHEN ThangCT = 7 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_7,
                CAST(SUM(CASE WHEN ThangCT = 7 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_7,
                CAST(SUM(CASE WHEN ThangCT = 8 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_8,
                CAST(SUM(CASE WHEN ThangCT = 8 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_8,
                CAST(SUM(CASE WHEN ThangCT = 9 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_9,
                CAST(SUM(CASE WHEN ThangCT = 9 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_9,
                CAST(SUM(CASE WHEN ThangCT = 10 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_10,
                CAST(SUM(CASE WHEN ThangCT = 10 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_10,
                CAST(SUM(CASE WHEN ThangCT = 11 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_11,
                CAST(SUM(CASE WHEN ThangCT = 11 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_11,
                CAST(SUM(CASE WHEN ThangCT = 12 THEN SoPS ELSE 0 END) AS decimal(18,2)) AS No_12,
                CAST(SUM(CASE WHEN ThangCT = 12 THEN SoPS2Co ELSE 0 END) AS decimal(18,2)) AS NTNo_12
            FROM ChungTu
            WHERE MaLoai IN (2, 4)  
              AND MaVattu > 0
              AND MaKho > 0
              AND (SoHieu LIKE '%GV%' OR SoHieu LIKE '%gv%') 
                    GROUP BY MaTkCo, MaKho, MaVattu";
        
        var data = db.Database.SqlQuery<XuatData>(sql).ToList();

                foreach (var item in data)
                {
                    // ⭐ GIỮ NGUYÊN: Cập nhật KHÔNG đổi dấu (giữ nguyên dương) như VB6
                    string updateSql = BuildUpdateXuatSQL(item);

                    try
                    {
                        int affected = db.Database.ExecuteSqlCommand(updateSql,
                            item.MaSoKho, item.MaTkCo, item.MaVattu);

                        if (affected == 0)
                        {
                            // ⭐ GIỮ NGUYÊN: Thêm mới nếu chưa tồn tại như VB6
                            db.Database.ExecuteSqlCommand(@"
                        INSERT INTO TonKho (MaSoKho, MaTaiKhoan, MaVatTu, Luong_0, Tien_0) 
                        VALUES ({0}, {1}, {2}, 0, 0)",
                                item.MaSoKho, item.MaTkCo, item.MaVattu);

                            db.Database.ExecuteSqlCommand(updateSql,
                                item.MaSoKho, item.MaTkCo, item.MaVattu);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi cập nhật xuất: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi cập nhật xuất: " + ex.Message);
            }
        }

        /// <summary>
        /// Xây dựng SQL UPDATE xuất - GIỮ NGUYÊN LOGIC VB6
        /// </summary>
        private string BuildUpdateXuatSQL(XuatData data)
        {
            var sql = new StringBuilder("UPDATE TonKho SET MaSoKho = MaSoKho");

            for (int i = 1; i <= 12; i++)
            {
                // ⭐ GIỮ NGUYÊN: Lấy giá trị, KHÔNG đổi dấu (giữ nguyên dương) như VB6
                decimal noValue = GetSafeDecimalValue(data, $"No_{i}");
                decimal ntNoValue = GetSafeDecimalValue(data, $"NTNo_{i}");

                // VB6 dùng DoiDau để đổi dấu, nhưng vì SQL đã lấy giá trị tuyệt đối nên không cần
                sql.Append($@"
            , Tien_Xuat_{i} = {noValue}
            , Luong_Xuat_{i} = {ntNoValue}");
            }

            sql.Append(" WHERE MaSoKho = {0} AND MaTaiKhoan = {1} AND MaVatTu = {2}");
            return sql.ToString();
        }

        /// <summary>
        /// Cập nhật số tồn - Đã sửa lỗi -0.5
        /// </summary>
        private void CapNhatSoTon()
        {
            try
            {
                // Làm tròn số tồn đầu kỳ (Sửa lỗi -0.5)
                LamTronTonKho();

                // Cập nhật số tồn từng tháng
                var sql = new StringBuilder("UPDATE TonKho SET MaVatTu = MaVatTu");

                for (int i = 1; i <= 12; i++)
                {
                    string luongExpr = BuildLuongExpr(i);
                    string tienExpr = BuildTienExpr(i);

                    sql.Append($@"
                        , Luong_{i} = ROUND({luongExpr}, 0)
                        , Tien_{i} = {tienExpr}");
                }

                db.Database.ExecuteSqlCommand(sql.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi cập nhật số tồn: " + ex.Message);
            }
        }

        /// <summary>
        /// Làm tròn tồn kho - Đã sửa lỗi -0.5
        /// </summary>
        private void LamTronTonKho()
        {
            try
            {
                // Sử dụng ROUND chuẩn để tránh lỗi -0.5
                string sql = $@"
                    UPDATE TonKho 
                    SET 
                        Luong_0 = ROUND(Luong_0, 0),
                        Tien_0 = ROUND(Tien_0, 0)
                    WHERE ABS(Luong_0) > 0.0001 OR ABS(Tien_0) > 0.0001";

                db.Database.ExecuteSqlCommand(sql);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi làm tròn tồn kho: " + ex.Message);
            }
        }

        /// <summary>
        /// Xây dựng biểu thức tính tồn lượng
        /// </summary>
        private string BuildLuongExpr(int i)
        {
            string expr = "ISNULL(Luong_0, 0)";
            for (int j = 1; j <= i; j++)
            {
                expr += $" + ISNULL(Luong_Nhap_{j}, 0) - ISNULL(Luong_Xuat_{j}, 0)";
            }
            return expr;
        }

        /// <summary>
        /// Xây dựng biểu thức tính tồn tiền
        /// </summary>
        private string BuildTienExpr(int i)
        {
            string expr = "ISNULL(Tien_0, 0)";
            for (int j = 1; j <= i; j++)
            {
                expr += $" + ISNULL(Tien_Nhap_{j}, 0) - ISNULL(Tien_Xuat_{j}, 0)";
            }
            return expr;
        }

        /// <summary>
        /// Xóa bản ghi không cần thiết
        /// </summary>
        private void XoaBanGhiKhongCanThiet()
        {
            try
            {
                string sql = "DELETE FROM TonKho WHERE Luong_0 = 0 AND Tien_0 = 0";

                for (int i = 1; i <= 12; i++)
                {
                    sql += $@"
                        AND Luong_Nhap_{i} = 0 
                        AND Luong_Xuat_{i} = 0 
                        AND Tien_Nhap_{i} = 0";
                }

                db.Database.ExecuteSqlCommand(sql);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi xóa bản ghi không cần thiết: " + ex.Message);
            }
        }

        #endregion

        #region Helper Methods - Xử lý an toàn kiểu dữ liệu

        /// <summary>
        /// Lấy giá trị Decimal an toàn từ object (Không bị lỗi cast)
        /// </summary>
        private decimal GetSafeDecimalValue(object obj, string propertyName)
        {
            try
            {
                var prop = obj.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    if (value != null && value != DBNull.Value)
                    {
                        // Chuyển đổi an toàn
                        return Convert.ToDecimal(value);
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Lấy giá trị int an toàn
        /// </summary>
        private int GetSafeIntValue(object obj, string propertyName)
        {
            try
            {
                var prop = obj.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    if (value != null && value != DBNull.Value)
                    {
                        return Convert.ToInt32(value);
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Helper Classes - Sử dụng decimal thay vì double

        public class NhapData
        {
            public int MaTkNo { get; set; }
            public int MaSoKho { get; set; }
            public int MaVattu { get; set; }

            // Sử dụng decimal thay vì double để tránh lỗi cast
            public decimal No_1 { get; set; }
            public decimal NTNo_1 { get; set; }
            public decimal No_2 { get; set; }
            public decimal NTNo_2 { get; set; }
            public decimal No_3 { get; set; }
            public decimal NTNo_3 { get; set; }
            public decimal No_4 { get; set; }
            public decimal NTNo_4 { get; set; }
            public decimal No_5 { get; set; }
            public decimal NTNo_5 { get; set; }
            public decimal No_6 { get; set; }
            public decimal NTNo_6 { get; set; }
            public decimal No_7 { get; set; }
            public decimal NTNo_7 { get; set; }
            public decimal No_8 { get; set; }
            public decimal NTNo_8 { get; set; }
            public decimal No_9 { get; set; }
            public decimal NTNo_9 { get; set; }
            public decimal No_10 { get; set; }
            public decimal NTNo_10 { get; set; }
            public decimal No_11 { get; set; }
            public decimal NTNo_11 { get; set; }
            public decimal No_12 { get; set; }
            public decimal NTNo_12 { get; set; }
        }

        public class XuatData
        {
            public int MaTkCo { get; set; }
            public int MaSoKho { get; set; }
            public int MaVattu { get; set; }

            // Sử dụng decimal thay vì double
            public decimal No_1 { get; set; }
            public decimal NTNo_1 { get; set; }
            public decimal No_2 { get; set; }
            public decimal NTNo_2 { get; set; }
            public decimal No_3 { get; set; }
            public decimal NTNo_3 { get; set; }
            public decimal No_4 { get; set; }
            public decimal NTNo_4 { get; set; }
            public decimal No_5 { get; set; }
            public decimal NTNo_5 { get; set; }
            public decimal No_6 { get; set; }
            public decimal NTNo_6 { get; set; }
            public decimal No_7 { get; set; }
            public decimal NTNo_7 { get; set; }
            public decimal No_8 { get; set; }
            public decimal NTNo_8 { get; set; }
            public decimal No_9 { get; set; }
            public decimal NTNo_9 { get; set; }
            public decimal No_10 { get; set; }
            public decimal NTNo_10 { get; set; }
            public decimal No_11 { get; set; }
            public decimal NTNo_11 { get; set; }
            public decimal No_12 { get; set; }
            public decimal NTNo_12 { get; set; }
        }

        #endregion
    }
}