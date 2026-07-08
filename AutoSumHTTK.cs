using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace SaovietTax
{
    public partial class AutoSumHTTK : DevExpress.XtraEditors.XtraForm
    {
        public AutoSumHTTK()
        {
            InitializeComponent();
        }

        private string _connectionString;

        private void AutoSumHTTK_Load(object sender, EventArgs e)
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;
            CapNhatHeThongTK();
        }

        // ============================================================
        // HÀM CHÍNH: CẬP NHẬT HETHONGTK
        // ============================================================
        public void CapNhatHeThongTK()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // BƯỚC 0: KIỂM TRA TK 156
                        CheckTK156(conn, tran);

                        // BƯỚC 1: Lấy danh sách tài khoản có phát sinh
                        DataTable dtTaiKhoanPS = GetTaiKhoanCoPhatSinh(conn, tran);

                        if (dtTaiKhoanPS.Rows.Count == 0)
                        {
                            XtraMessageBox.Show("Không có chứng từ nào để tính toán!", "Thông báo",
                                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                            return;
                        }

                        // BƯỚC 2: Reset Co_1..12 cho tất cả tài khoản
                        ResetCoThang(conn, tran);

                        // BƯỚC 3: Reset toàn bộ dữ liệu cho tài khoản có phát sinh
                        ResetTaiKhoanCoPhatSinh(conn, tran, dtTaiKhoanPS);

                        // BƯỚC 4: Cập nhật số phát sinh từ chứng từ
                        CapNhatSoPhatSinhTuChungTu(conn, tran);

                        // BƯỚC 5: Tách dư nợ/dư có ban đầu
                        TachDuNoDuCoBanDau(conn, tran);

                        // BƯỚC 6: Tính dư nợ/dư có cho tài khoản cấp con
                        TinhDuNoDuCoTaiKhoanCon(conn, tran);

                        // BƯỚC 7: Tính dư nợ/dư có cho tài khoản cấp cha
                        TinhDuNoDuCoTaiKhoanCha(conn, tran);

                        // BƯỚC 8: Tách dư nợ/dư có lần cuối
                        TachDuNoDuCoLanCuoi(conn, tran);

                        // BƯỚC 9: Cập nhật SoDuKhachHang
                        CapNhatSoDuKhachHang(conn, tran);

                        // BƯỚC 10: Kiểm tra kết quả
                        CheckKetQua(conn, tran);

                        tran.Commit();

                        XtraMessageBox.Show("✅ Cập nhật HeThongTK thành công!", "Thông báo",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        XtraMessageBox.Show($"❌ Lỗi: {ex.Message}", "Lỗi",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                        throw;
                    }
                }
            }
        }

        // ============================================================
        // BƯỚC 0: KIỂM TRA TK 156
        // ============================================================
        private void CheckTK156(SqlConnection conn, SqlTransaction tran)
        {
            Console.WriteLine("\n🔍 KIỂM TRA TK 156:");
            Console.WriteLine("========================================");

            // Tìm MaSo của TK 156
            string sql = "SELECT MaSo FROM HethongTK WHERE SoHieu = '156'";
            int maSo156 = 0;
            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    maSo156 = Convert.ToInt32(result);
                    Console.WriteLine($"✅ TK 156 có MaSo = {maSo156}");
                }
                else
                {
                    Console.WriteLine("❌ KHÔNG tìm thấy TK 156 trong HeThongTK!");
                    return;
                }
            }

            // Kiểm tra chứng từ dùng TK 156
            string sqlCT = $@"
                SELECT COUNT(*) FROM ChungTu 
                WHERE (MaTKNo = {maSo156} OR MaTKCo = {maSo156}) AND SoPS <> 0";

            using (SqlCommand cmd = new SqlCommand(sqlCT, conn, tran))
            {
                object result = cmd.ExecuteScalar();
                int count = 0;
                if (result != null && result != DBNull.Value)
                {
                    count = Convert.ToInt32(result);
                }
                Console.WriteLine($"📄 Có {count} chứng từ sử dụng TK 156 (MaSo={maSo156})");
            }

            // Kiểm tra dữ liệu hiện tại của TK 156
            string sqlData = $@"
                SELECT No_7, Co_7, DuNo_7, DuCo_7 
                FROM HethongTK WHERE MaSo = {maSo156}";

            using (SqlCommand cmd = new SqlCommand(sqlData, conn, tran))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Console.WriteLine($"📊 Dữ liệu hiện tại TK 156:");
                        Console.WriteLine($"   No_7: {reader["No_7"]}, Co_7: {reader["Co_7"]}");
                        Console.WriteLine($"   DuNo_7: {reader["DuNo_7"]}, DuCo_7: {reader["DuCo_7"]}");
                    }
                    reader.Close();
                }
            }
            Console.WriteLine("");
        }

        // ============================================================
        // BƯỚC 1: LẤY DANH SÁCH TÀI KHOẢN CÓ PHÁT SINH (QUAN TRỌNG)
        // ============================================================
        private DataTable GetTaiKhoanCoPhatSinh(SqlConnection conn, SqlTransaction tran)
        {
            // Lấy tất cả MaSo có phát sinh từ chứng từ, BỎ QUA TK GỐC
            string sql = @"
        SELECT DISTINCT MaSo 
        FROM HethongTK 
        WHERE MaSo IN (
            SELECT MaTKNo FROM ChungTu WHERE SoPS <> 0
            UNION
            SELECT MaTKCo FROM ChungTu WHERE SoPS <> 0
        )
        AND MaSo > 0
        AND MaSo NOT IN (1, 2, 6, 7, 20, 30, 40)  -- Bỏ qua TK gốc
        ORDER BY MaSo";

            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            // Tự động thêm các TK cấp cha có TK con (KHÔNG BAO GỒM TK GỐC)
            string sqlGetCha = @"
        SELECT DISTINCT TkCha0 AS MaSo
        FROM HethongTK 
        WHERE TkCha0 > 0
        AND TkCha0 NOT IN (1, 2, 6, 7, 20, 30, 40)";

            DataTable dtCha = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlGetCha, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtCha);
                }
            }

            // Thêm các TK cấp cha vào danh sách (nếu chưa có)
            foreach (DataRow row in dtCha.Rows)
            {
                int maSoCha = Convert.ToInt32(row["MaSo"]);
                bool exists = false;
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["MaSo"]) == maSoCha)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    DataRow newRow = dt.NewRow();
                    newRow["MaSo"] = maSoCha;
                    dt.Rows.Add(newRow);
                    Console.WriteLine($"✅ Đã thêm TK cấp cha (MaSo={maSoCha}) vào danh sách reset");
                }
            }

            Console.WriteLine($"📌 Có {dt.Rows.Count} tài khoản cần reset và tính toán");
            return dt;
        }

        // ============================================================
        // HÀM LẤY MASO TỪ SOHIEU
        // ============================================================
        private int GetMaSoFromSoHieu(SqlConnection conn, SqlTransaction tran, string soHieu)
        {
            string sql = "SELECT MaSo FROM HethongTK WHERE SoHieu = @SoHieu";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@SoHieu", soHieu);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
                return 0;
            }
        }

        // ============================================================
        // HÀM LẤY SOHIEU TỪ MASO
        // ============================================================
        private string GetSoHieuFromMaSo(SqlConnection conn, SqlTransaction tran, int maSo)
        {
            string sql = "SELECT SoHieu FROM HethongTK WHERE MaSo = @MaSo";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@MaSo", maSo);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return result.ToString();
                }
                return "";
            }
        }

        // ============================================================
        // HÀM LẤY TÊN TỪ MASO
        // ============================================================
        private string GetTenFromMaSo(SqlConnection conn, SqlTransaction tran, int maSo)
        {
            string sql = "SELECT Ten FROM HethongTK WHERE MaSo = @MaSo";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@MaSo", maSo);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return result.ToString();
                }
                return "";
            }
        }

        // ============================================================
        // BƯỚC 2: RESET CO_1..12 CHO TẤT CẢ TÀI KHOẢN
        // ============================================================
        private void ResetCoThang(SqlConnection conn, SqlTransaction tran)
        {
            string sql = @"
                UPDATE HethongTK SET 
                    Co_1 = 0, Co_1_NT = 0,
                    Co_2 = 0, Co_2_NT = 0,
                    Co_3 = 0, Co_3_NT = 0,
                    Co_4 = 0, Co_4_NT = 0,
                    Co_5 = 0, Co_5_NT = 0,
                    Co_6 = 0, Co_6_NT = 0,
                    Co_7 = 0, Co_7_NT = 0,
                    Co_8 = 0, Co_8_NT = 0,
                    Co_9 = 0, Co_9_NT = 0,
                    Co_10 = 0, Co_10_NT = 0,
                    Co_11 = 0, Co_11_NT = 0,
                    Co_12 = 0, Co_12_NT = 0
                WHERE MaSo IS NOT NULL";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                int rows = cmd.ExecuteNonQuery();
                Console.WriteLine($"✅ Đã reset Co cho {rows} tài khoản");
            }
        }

        // ============================================================
        // BƯỚC 3: RESET TOÀN BỘ DỮ LIỆU CHO TÀI KHOẢN CÓ PHÁT SINH
        // ============================================================
        private void ResetTaiKhoanCoPhatSinh(SqlConnection conn, SqlTransaction tran, DataTable dtTaiKhoan)
        {
            if (dtTaiKhoan.Rows.Count == 0) return;

            // Lọc bỏ các TK gốc
            DataTable dtFiltered = dtTaiKhoan.Clone();
            foreach (DataRow row in dtTaiKhoan.Rows)
            {
                int maSo = Convert.ToInt32(row["MaSo"]);
                if (maSo != 1 && maSo != 2 && maSo != 6 && maSo != 7 && maSo != 20 && maSo != 30 && maSo != 40)
                {
                    dtFiltered.ImportRow(row);
                }
            }

            if (dtFiltered.Rows.Count == 0) return;

            // Tạo chuỗi IN
            string inClause = "";
            for (int i = 0; i < dtFiltered.Rows.Count; i++)
            {
                if (i > 0) inClause += ",";
                inClause += dtFiltered.Rows[i]["MaSo"].ToString();
            }

            string sql = $@"
        UPDATE HethongTK SET 
            DuNo_0 = 0, DuCo_0 = 0,
            No_1 = 0, Co_1 = 0, No_1_NT = 0, Co_1_NT = 0,
            No_2 = 0, Co_2 = 0, No_2_NT = 0, Co_2_NT = 0,
            No_3 = 0, Co_3 = 0, No_3_NT = 0, Co_3_NT = 0,
            No_4 = 0, Co_4 = 0, No_4_NT = 0, Co_4_NT = 0,
            No_5 = 0, Co_5 = 0, No_5_NT = 0, Co_5_NT = 0,
            No_6 = 0, Co_6 = 0, No_6_NT = 0, Co_6_NT = 0,
            No_7 = 0, Co_7 = 0, No_7_NT = 0, Co_7_NT = 0,
            No_8 = 0, Co_8 = 0, No_8_NT = 0, Co_8_NT = 0,
            No_9 = 0, Co_9 = 0, No_9_NT = 0, Co_9_NT = 0,
            No_10 = 0, Co_10 = 0, No_10_NT = 0, Co_10_NT = 0,
            No_11 = 0, Co_11 = 0, No_11_NT = 0, Co_11_NT = 0,
            No_12 = 0, Co_12 = 0, No_12_NT = 0, Co_12_NT = 0,
            DuNo_1 = 0, DuCo_1 = 0, DuNT_1 = 0,
            DuNo_2 = 0, DuCo_2 = 0, DuNT_2 = 0,
            DuNo_3 = 0, DuCo_3 = 0, DuNT_3 = 0,
            DuNo_4 = 0, DuCo_4 = 0, DuNT_4 = 0,
            DuNo_5 = 0, DuCo_5 = 0, DuNT_5 = 0,
            DuNo_6 = 0, DuCo_6 = 0, DuNT_6 = 0,
            DuNo_7 = 0, DuCo_7 = 0, DuNT_7 = 0,
            DuNo_8 = 0, DuCo_8 = 0, DuNT_8 = 0,
            DuNo_9 = 0, DuCo_9 = 0, DuNT_9 = 0,
            DuNo_10 = 0, DuCo_10 = 0, DuNT_10 = 0,
            DuNo_11 = 0, DuCo_11 = 0, DuNT_11 = 0,
            DuNo_12 = 0, DuCo_12 = 0, DuNT_12 = 0
        WHERE MaSo IN ({inClause})";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                int rows = cmd.ExecuteNonQuery();
                Console.WriteLine($"✅ Đã reset toàn bộ cho {rows} tài khoản có phát sinh (không bao gồm TK gốc)");
            }
        }

        // ============================================================
        // BƯỚC 4: CẬP NHẬT SỐ PHÁT SINH TỪ CHỨNG TỪ
        // ============================================================
        private void CapNhatSoPhatSinhTuChungTu(SqlConnection conn, SqlTransaction tran)
        {
            Console.WriteLine("\n📊 CẬP NHẬT SỐ PHÁT SINH TỪ CHỨNG TỪ:");
            Console.WriteLine("========================================");

            // Lấy tổng hợp số phát sinh Nợ
            string sqlNo = @"
                SELECT 
                    MaTKNo AS MaTaiKhoan,
                    ThangCT,
                    SUM(SoPS) AS SoPS
                FROM ChungTu
                WHERE MaTKNo > 0 AND SoPS <> 0
                GROUP BY MaTKNo, ThangCT";

            DataTable dtNo = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlNo, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtNo);
                }
            }

            // Lấy tổng hợp số phát sinh Có
            string sqlCo = @"
                SELECT 
                    MaTKCo AS MaTaiKhoan,
                    ThangCT,
                    SUM(SoPS) AS SoPS
                FROM ChungTu
                WHERE MaTKCo > 0 AND SoPS <> 0
                GROUP BY MaTKCo, ThangCT";

            DataTable dtCo = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlCo, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtCo);
                }
            }

            // Cập nhật số phát sinh Nợ
            Console.WriteLine("\n📋 CẬP NHẬT SỐ PHÁT SINH NỢ:");
            foreach (DataRow row in dtNo.Rows)
            {
                int maTaiKhoan = Convert.ToInt32(row["MaTaiKhoan"]);
                int thang = Convert.ToInt32(row["ThangCT"]);
                double soPS = Convert.ToDouble(row["SoPS"]);
                string soHieu = GetSoHieuFromMaSo(conn, tran, maTaiKhoan);

                string sqlUpdate = $@"
                    UPDATE HethongTK 
                    SET No_{thang} = No_{thang} + @SoPS
                    WHERE MaSo = @MaTaiKhoan";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@SoPS", soPS);
                    cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        Console.WriteLine($"   ✅ No_{thang} + {soPS:N0} cho TK {soHieu} (MaSo={maTaiKhoan})");
                    }
                }
            }

            // Cập nhật số phát sinh Có
            Console.WriteLine("\n📋 CẬP NHẬT SỐ PHÁT SINH CÓ:");
            foreach (DataRow row in dtCo.Rows)
            {
                int maTaiKhoan = Convert.ToInt32(row["MaTaiKhoan"]);
                int thang = Convert.ToInt32(row["ThangCT"]);
                double soPS = Convert.ToDouble(row["SoPS"]);
                string soHieu = GetSoHieuFromMaSo(conn, tran, maTaiKhoan);

                string sqlUpdate = $@"
                    UPDATE HethongTK 
                    SET Co_{thang} = Co_{thang} + @SoPS
                    WHERE MaSo = @MaTaiKhoan";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@SoPS", soPS);
                    cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        Console.WriteLine($"   ✅ Co_{thang} + {soPS:N0} cho TK {soHieu} (MaSo={maTaiKhoan})");
                    }
                }
            }

            Console.WriteLine($"\n✅ Đã cập nhật số phát sinh Nợ: {dtNo.Rows.Count} dòng");
            Console.WriteLine($"✅ Đã cập nhật số phát sinh Có: {dtCo.Rows.Count} dòng");
        }

        // ============================================================
        // BƯỚC 5: TÁCH DƯ NỢ/DƯ CÓ BAN ĐẦU
        // ============================================================
        private void TachDuNoDuCoBanDau(SqlConnection conn, SqlTransaction tran)
        {
            string sql = @"
                UPDATE HethongTK SET 
                    DuNo_0 = IIF(DuNo_0 >= DuCo_0, DuNo_0 - DuCo_0, 0),
                    DuCo_0 = IIF(DuNo_0 < DuCo_0, DuCo_0 - DuNo_0, 0)
                WHERE TKCon = 0";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                int rows = cmd.ExecuteNonQuery();
                Console.WriteLine($"✅ Đã tách dư nợ/dư có ban đầu cho {rows} tài khoản");
            }
        }

        // ============================================================
        // BƯỚC 6: TÍNH DƯ NỢ/DƯ CÓ CHO TÀI KHOẢN CẤP CON
        // ============================================================
        private void TinhDuNoDuCoTaiKhoanCon(SqlConnection conn, SqlTransaction tran)
        {
            // Lấy danh sách MaSo có phát sinh, BỎ QUA TK GỐC
            string sqlGetMaSo = @"
        SELECT DISTINCT MaSo 
        FROM HethongTK 
        WHERE MaSo IN (
            SELECT MaTKNo FROM ChungTu WHERE SoPS <> 0
            UNION
            SELECT MaTKCo FROM ChungTu WHERE SoPS <> 0
        )
        AND MaSo NOT IN (1, 2, 6, 7, 20, 30, 40)  -- Bỏ qua TK gốc";

            DataTable dtMaSo = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlGetMaSo, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtMaSo);
                }
            }

            Console.WriteLine($"📌 Tính dư cho {dtMaSo.Rows.Count} tài khoản");

            foreach (DataRow row in dtMaSo.Rows)
            {
                int maSo = Convert.ToInt32(row["MaSo"]);

                string sql = $@"
            UPDATE HethongTK SET 
                DuNo_1 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1, 0),
                DuCo_1 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1), 0),
                DuNo_2 = IIF(DuNo_1 - DuCo_1 + No_2 - Co_2 > 0, DuNo_1 - DuCo_1 + No_2 - Co_2, 0),
                DuCo_2 = IIF(DuNo_1 - DuCo_1 + No_2 - Co_2 < 0, -(DuNo_1 - DuCo_1 + No_2 - Co_2), 0),
                DuNo_3 = IIF(DuNo_2 - DuCo_2 + No_3 - Co_3 > 0, DuNo_2 - DuCo_2 + No_3 - Co_3, 0),
                DuCo_3 = IIF(DuNo_2 - DuCo_2 + No_3 - Co_3 < 0, -(DuNo_2 - DuCo_2 + No_3 - Co_3), 0),
                DuNo_4 = IIF(DuNo_3 - DuCo_3 + No_4 - Co_4 > 0, DuNo_3 - DuCo_3 + No_4 - Co_4, 0),
                DuCo_4 = IIF(DuNo_3 - DuCo_3 + No_4 - Co_4 < 0, -(DuNo_3 - DuCo_3 + No_4 - Co_4), 0),
                DuNo_5 = IIF(DuNo_4 - DuCo_4 + No_5 - Co_5 > 0, DuNo_4 - DuCo_4 + No_5 - Co_5, 0),
                DuCo_5 = IIF(DuNo_4 - DuCo_4 + No_5 - Co_5 < 0, -(DuNo_4 - DuCo_4 + No_5 - Co_5), 0),
                DuNo_6 = IIF(DuNo_5 - DuCo_5 + No_6 - Co_6 > 0, DuNo_5 - DuCo_5 + No_6 - Co_6, 0),
                DuCo_6 = IIF(DuNo_5 - DuCo_5 + No_6 - Co_6 < 0, -(DuNo_5 - DuCo_5 + No_6 - Co_6), 0),
                DuNo_7 = IIF(DuNo_6 - DuCo_6 + No_7 - Co_7 > 0, DuNo_6 - DuCo_6 + No_7 - Co_7, 0),
                DuCo_7 = IIF(DuNo_6 - DuCo_6 + No_7 - Co_7 < 0, -(DuNo_6 - DuCo_6 + No_7 - Co_7), 0),
                DuNo_8 = IIF(DuNo_7 - DuCo_7 + No_8 - Co_8 > 0, DuNo_7 - DuCo_7 + No_8 - Co_8, 0),
                DuCo_8 = IIF(DuNo_7 - DuCo_7 + No_8 - Co_8 < 0, -(DuNo_7 - DuCo_7 + No_8 - Co_8), 0),
                DuNo_9 = IIF(DuNo_8 - DuCo_8 + No_9 - Co_9 > 0, DuNo_8 - DuCo_8 + No_9 - Co_9, 0),
                DuCo_9 = IIF(DuNo_8 - DuCo_8 + No_9 - Co_9 < 0, -(DuNo_8 - DuCo_8 + No_9 - Co_9), 0),
                DuNo_10 = IIF(DuNo_9 - DuCo_9 + No_10 - Co_10 > 0, DuNo_9 - DuCo_9 + No_10 - Co_10, 0),
                DuCo_10 = IIF(DuNo_9 - DuCo_9 + No_10 - Co_10 < 0, -(DuNo_9 - DuCo_9 + No_10 - Co_10), 0),
                DuNo_11 = IIF(DuNo_10 - DuCo_10 + No_11 - Co_11 > 0, DuNo_10 - DuCo_10 + No_11 - Co_11, 0),
                DuCo_11 = IIF(DuNo_10 - DuCo_10 + No_11 - Co_11 < 0, -(DuNo_10 - DuCo_10 + No_11 - Co_11), 0),
                DuNo_12 = IIF(DuNo_11 - DuCo_11 + No_12 - Co_12 > 0, DuNo_11 - DuCo_11 + No_12 - Co_12, 0),
                DuCo_12 = IIF(DuNo_11 - DuCo_11 + No_12 - Co_12 < 0, -(DuNo_11 - DuCo_11 + No_12 - Co_12), 0)
            WHERE MaSo = {maSo}";

                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
                {
                    int rows = cmd.ExecuteNonQuery();
                    if (maSo == 5107) // TK 133
                    {
                        Console.WriteLine($"✅ Đã tính dư cho TK 133 (MaSo=5107): {rows} dòng");
                    }
                }
            }

            Console.WriteLine("✅ Tính dư nợ/dư có cho tất cả tài khoản xong!");
        }

        // ============================================================
        // BƯỚC 7: TÍNH DƯ NỢ/DƯ CÓ CHO TÀI KHOẢN CẤP CHA
        // ============================================================
        private void TinhDuNoDuCoTaiKhoanCha(SqlConnection conn, SqlTransaction tran)
        {
            // Lấy danh sách tài khoản cấp cha (có TK con), BỎ QUA TK GỐC
            string sqlGetCha = @"
        SELECT DISTINCT TkCha0 AS MaSo
        FROM HethongTK 
        WHERE TkCha0 > 0
        AND TkCha0 NOT IN (1, 2, 6, 7, 20, 30, 40)";

            DataTable dtCha = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlGetCha, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtCha);
                }
            }

            Console.WriteLine($"📌 Có {dtCha.Rows.Count} tài khoản cấp cha cần tổng hợp (đã bỏ qua TK gốc)");

            // Cập nhật từng tài khoản cấp cha
            foreach (DataRow row in dtCha.Rows)
            {
                int maSoCha = Convert.ToInt32(row["MaSo"]);

                string sql = $@"
            UPDATE HethongTK 
            SET 
                DuNo_0 = (SELECT ISNULL(SUM(DuNo_0), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_0 = (SELECT ISNULL(SUM(DuCo_0), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_1 = (SELECT ISNULL(SUM(No_1), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_1 = (SELECT ISNULL(SUM(Co_1), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_1 = (SELECT ISNULL(SUM(DuNo_1), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_1 = (SELECT ISNULL(SUM(DuCo_1), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_2 = (SELECT ISNULL(SUM(No_2), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_2 = (SELECT ISNULL(SUM(Co_2), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_2 = (SELECT ISNULL(SUM(DuNo_2), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_2 = (SELECT ISNULL(SUM(DuCo_2), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_3 = (SELECT ISNULL(SUM(No_3), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_3 = (SELECT ISNULL(SUM(Co_3), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_3 = (SELECT ISNULL(SUM(DuNo_3), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_3 = (SELECT ISNULL(SUM(DuCo_3), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_4 = (SELECT ISNULL(SUM(No_4), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_4 = (SELECT ISNULL(SUM(Co_4), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_4 = (SELECT ISNULL(SUM(DuNo_4), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_4 = (SELECT ISNULL(SUM(DuCo_4), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_5 = (SELECT ISNULL(SUM(No_5), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_5 = (SELECT ISNULL(SUM(Co_5), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_5 = (SELECT ISNULL(SUM(DuNo_5), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_5 = (SELECT ISNULL(SUM(DuCo_5), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_6 = (SELECT ISNULL(SUM(No_6), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_6 = (SELECT ISNULL(SUM(Co_6), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_6 = (SELECT ISNULL(SUM(DuNo_6), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_6 = (SELECT ISNULL(SUM(DuCo_6), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_7 = (SELECT ISNULL(SUM(No_7), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_7 = (SELECT ISNULL(SUM(Co_7), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_7 = (SELECT ISNULL(SUM(DuNo_7), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_7 = (SELECT ISNULL(SUM(DuCo_7), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_8 = (SELECT ISNULL(SUM(No_8), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_8 = (SELECT ISNULL(SUM(Co_8), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_8 = (SELECT ISNULL(SUM(DuNo_8), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_8 = (SELECT ISNULL(SUM(DuCo_8), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_9 = (SELECT ISNULL(SUM(No_9), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_9 = (SELECT ISNULL(SUM(Co_9), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_9 = (SELECT ISNULL(SUM(DuNo_9), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_9 = (SELECT ISNULL(SUM(DuCo_9), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_10 = (SELECT ISNULL(SUM(No_10), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_10 = (SELECT ISNULL(SUM(Co_10), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_10 = (SELECT ISNULL(SUM(DuNo_10), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_10 = (SELECT ISNULL(SUM(DuCo_10), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_11 = (SELECT ISNULL(SUM(No_11), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_11 = (SELECT ISNULL(SUM(Co_11), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_11 = (SELECT ISNULL(SUM(DuNo_11), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_11 = (SELECT ISNULL(SUM(DuCo_11), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                No_12 = (SELECT ISNULL(SUM(No_12), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                Co_12 = (SELECT ISNULL(SUM(Co_12), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuNo_12 = (SELECT ISNULL(SUM(DuNo_12), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                DuCo_12 = (SELECT ISNULL(SUM(DuCo_12), 0) FROM HethongTK WHERE TkCha0 = {maSoCha})
            WHERE MaSo = {maSoCha}";

                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
                {
                    int rows = cmd.ExecuteNonQuery();
                    if (maSoCha == 5107) // TK 133
                    {
                        Console.WriteLine($"✅ Đã tổng hợp cho TK 133 (MaSo=5107): {rows} dòng");
                    }
                }
            }

            Console.WriteLine($"✅ Đã tính dư nợ/dư có cho {dtCha.Rows.Count} tài khoản cấp cha");
        }

        // ============================================================
        // BƯỚC 8: TÁCH DƯ NỢ/DƯ CÓ LẦN CUỐI
        // ============================================================
        private void TachDuNoDuCoLanCuoi(SqlConnection conn, SqlTransaction tran)
        {
            // Lấy danh sách MaSo có phát sinh, BỎ QUA TK GỐC
            string sqlGetMaSo = @"
        SELECT DISTINCT MaSo 
        FROM HethongTK 
        WHERE MaSo IN (
            SELECT MaTKNo FROM ChungTu WHERE SoPS <> 0
            UNION
            SELECT MaTKCo FROM ChungTu WHERE SoPS <> 0
        )
        AND MaSo NOT IN (1, 2, 6, 7, 20, 30, 40)";

            DataTable dtMaSo = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlGetMaSo, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtMaSo);
                }
            }

            foreach (DataRow row in dtMaSo.Rows)
            {
                int maSo = Convert.ToInt32(row["MaSo"]);

                string sql = $@"
            UPDATE HethongTK 
            SET 
                DuNo_0 = IIF(DuNo_0 >= DuCo_0, DuNo_0 - DuCo_0, 0),
                DuCo_0 = IIF(DuNo_0 < DuCo_0, DuCo_0 - DuNo_0, 0),
                DuNo_1 = IIF(DuNo_1 >= DuCo_1, DuNo_1 - DuCo_1, 0),
                DuCo_1 = IIF(DuNo_1 < DuCo_1, DuCo_1 - DuNo_1, 0),
                DuNo_2 = IIF(DuNo_2 >= DuCo_2, DuNo_2 - DuCo_2, 0),
                DuCo_2 = IIF(DuNo_2 < DuCo_2, DuCo_2 - DuNo_2, 0),
                DuNo_3 = IIF(DuNo_3 >= DuCo_3, DuNo_3 - DuCo_3, 0),
                DuCo_3 = IIF(DuNo_3 < DuCo_3, DuCo_3 - DuNo_3, 0),
                DuNo_4 = IIF(DuNo_4 >= DuCo_4, DuNo_4 - DuCo_4, 0),
                DuCo_4 = IIF(DuNo_4 < DuCo_4, DuCo_4 - DuNo_4, 0),
                DuNo_5 = IIF(DuNo_5 >= DuCo_5, DuNo_5 - DuCo_5, 0),
                DuCo_5 = IIF(DuNo_5 < DuCo_5, DuCo_5 - DuNo_5, 0),
                DuNo_6 = IIF(DuNo_6 >= DuCo_6, DuNo_6 - DuCo_6, 0),
                DuCo_6 = IIF(DuNo_6 < DuCo_6, DuCo_6 - DuNo_6, 0),
                DuNo_7 = IIF(DuNo_7 >= DuCo_7, DuNo_7 - DuCo_7, 0),
                DuCo_7 = IIF(DuNo_7 < DuCo_7, DuCo_7 - DuNo_7, 0),
                DuNo_8 = IIF(DuNo_8 >= DuCo_8, DuNo_8 - DuCo_8, 0),
                DuCo_8 = IIF(DuNo_8 < DuCo_8, DuCo_8 - DuNo_8, 0),
                DuNo_9 = IIF(DuNo_9 >= DuCo_9, DuNo_9 - DuCo_9, 0),
                DuCo_9 = IIF(DuNo_9 < DuCo_9, DuCo_9 - DuNo_9, 0),
                DuNo_10 = IIF(DuNo_10 >= DuCo_10, DuNo_10 - DuCo_10, 0),
                DuCo_10 = IIF(DuNo_10 < DuCo_10, DuCo_10 - DuNo_10, 0),
                DuNo_11 = IIF(DuNo_11 >= DuCo_11, DuNo_11 - DuCo_11, 0),
                DuCo_11 = IIF(DuNo_11 < DuCo_11, DuCo_11 - DuNo_11, 0),
                DuNo_12 = IIF(DuNo_12 >= DuCo_12, DuNo_12 - DuCo_12, 0),
                DuCo_12 = IIF(DuNo_12 < DuCo_12, DuCo_12 - DuNo_12, 0)
            WHERE MaSo = {maSo}";

                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"✅ Đã tách dư nợ/dư có lần cuối cho {dtMaSo.Rows.Count} tài khoản");
        }

        // ============================================================
        // BƯỚC 9: CẬP NHẬT SODUKHACHHANG
        // ============================================================
        private void CapNhatSoDuKhachHang(SqlConnection conn, SqlTransaction tran)
        {
            // Reset No, Co về 0 cho tất cả
            string sqlReset = @"
                UPDATE SoDuKhachHang SET 
                    No_1 = 0, Co_1 = 0, No_1_NT = 0, Co_1_NT = 0,
                    No_2 = 0, Co_2 = 0, No_2_NT = 0, Co_2_NT = 0,
                    No_3 = 0, Co_3 = 0, No_3_NT = 0, Co_3_NT = 0,
                    No_4 = 0, Co_4 = 0, No_4_NT = 0, Co_4_NT = 0,
                    No_5 = 0, Co_5 = 0, No_5_NT = 0, Co_5_NT = 0,
                    No_6 = 0, Co_6 = 0, No_6_NT = 0, Co_6_NT = 0,
                    No_7 = 0, Co_7 = 0, No_7_NT = 0, Co_7_NT = 0,
                    No_8 = 0, Co_8 = 0, No_8_NT = 0, Co_8_NT = 0,
                    No_9 = 0, Co_9 = 0, No_9_NT = 0, Co_9_NT = 0,
                    No_10 = 0, Co_10 = 0, No_10_NT = 0, Co_10_NT = 0,
                    No_11 = 0, Co_11 = 0, No_11_NT = 0, Co_11_NT = 0,
                    No_12 = 0, Co_12 = 0, No_12_NT = 0, Co_12_NT = 0";

            using (SqlCommand cmd = new SqlCommand(sqlReset, conn, tran))
            {
                cmd.ExecuteNonQuery();
                Console.WriteLine("✅ Đã reset SoDuKhachHang");
            }

            // Lấy MaSo của TK 331 từ SoHieu
            int maSo331 = GetMaSoFromSoHieu(conn, tran, "331");
            if (maSo331 > 0)
            {
                CapNhatSoDuKhachHangForTaiKhoan(conn, tran, maSo331, "MaKHC");
            }

            // Lấy MaSo của TK 131 từ SoHieu
            int maSo131 = GetMaSoFromSoHieu(conn, tran, "131");
            if (maSo131 > 0)
            {
                CapNhatSoDuKhachHangForTaiKhoan(conn, tran, maSo131, "MaKH");
            }

            // Tính dư nợ/dư có cho SoDuKhachHang
            TinhDuNoDuCoSoDuKhachHang(conn, tran);

            // Tính DuNT cho SoDuKhachHang
            TinhDuNTSoDuKhachHang(conn, tran);
        }

        // ============================================================
        // CẬP NHẬT SODUKHACHHANG CHO 1 TÀI KHOẢN
        // ============================================================
        private void CapNhatSoDuKhachHangForTaiKhoan(SqlConnection conn, SqlTransaction tran, int maTaiKhoan, string colDoiTuong)
        {
            string sql = $@"
                SELECT 
                    {colDoiTuong} AS MaDoiTuong,
                    ThangCT,
                    SUM(CASE WHEN MaTKNo = {maTaiKhoan} THEN SoPS ELSE 0 END) AS No_PS,
                    SUM(CASE WHEN MaTKCo = {maTaiKhoan} THEN SoPS ELSE 0 END) AS Co_PS
                FROM ChungTu
                WHERE {colDoiTuong} > 0 AND (MaTKNo = {maTaiKhoan} OR MaTKCo = {maTaiKhoan})
                GROUP BY {colDoiTuong}, ThangCT";

            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                int maDoiTuong = Convert.ToInt32(row["MaDoiTuong"]);
                int thang = Convert.ToInt32(row["ThangCT"]);
                double noPS = Convert.ToDouble(row["No_PS"]);
                double coPS = Convert.ToDouble(row["Co_PS"]);

                // Kiểm tra tồn tại
                string sqlCheck = @"
                    SELECT COUNT(*) FROM SoDuKhachHang 
                    WHERE MaKhachHang = @MaDoiTuong AND MaTaiKhoan = @MaTaiKhoan";

                using (SqlCommand cmd = new SqlCommand(sqlCheck, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@MaDoiTuong", maDoiTuong);
                    cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
                    object result = cmd.ExecuteScalar();
                    int count = 0;
                    if (result != null && result != DBNull.Value)
                    {
                        count = Convert.ToInt32(result);
                    }

                    if (count == 0)
                    {
                        string sqlInsert = @"
                            INSERT INTO SoDuKhachHang (MaKhachHang, MaTaiKhoan, DuNo_0, DuCo_0)
                            VALUES (@MaDoiTuong, @MaTaiKhoan, 0, 0)";

                        using (SqlCommand cmdInsert = new SqlCommand(sqlInsert, conn, tran))
                        {
                            cmdInsert.Parameters.AddWithValue("@MaDoiTuong", maDoiTuong);
                            cmdInsert.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
                            cmdInsert.ExecuteNonQuery();
                        }
                    }
                }

                // Cập nhật số phát sinh
                string sqlUpdate = $@"
                    UPDATE SoDuKhachHang 
                    SET No_{thang} = No_{thang} + @No_PS,
                        Co_{thang} = Co_{thang} + @Co_PS
                    WHERE MaKhachHang = @MaDoiTuong AND MaTaiKhoan = @MaTaiKhoan";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@No_PS", noPS);
                    cmd.Parameters.AddWithValue("@Co_PS", coPS);
                    cmd.Parameters.AddWithValue("@MaDoiTuong", maDoiTuong);
                    cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"✅ Đã cập nhật SoDuKhachHang cho TK {maTaiKhoan}: {dt.Rows.Count} dòng");
        }

        // ============================================================
        // TÍNH DƯ NỢ/DƯ CÓ CHO SODUKHACHHANG
        // ============================================================
        private void TinhDuNoDuCoSoDuKhachHang(SqlConnection conn, SqlTransaction tran)
        {
            string sql = @"
                UPDATE SoDuKhachHang SET 
                    DuNo_1 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1, 0),
                    DuCo_1 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1), 0),
                    DuNo_2 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2, 0),
                    DuCo_2 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2), 0),
                    DuNo_3 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3, 0),
                    DuCo_3 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3), 0),
                    DuNo_4 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4, 0),
                    DuCo_4 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4), 0),
                    DuNo_5 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5, 0),
                    DuCo_5 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5), 0),
                    DuNo_6 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6, 0),
                    DuCo_6 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6), 0),
                    DuNo_7 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7, 0),
                    DuCo_7 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7), 0),
                    DuNo_8 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8, 0),
                    DuCo_8 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8), 0),
                    DuNo_9 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9, 0),
                    DuCo_9 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9), 0),
                    DuNo_10 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10, 0),
                    DuCo_10 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10), 0),
                    DuNo_11 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 + No_11 - Co_11 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 + No_11 - Co_11, 0),
                    DuCo_11 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 + No_11 - Co_11 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 + No_11 - Co_11), 0),
                    DuNo_12 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 + No_11 - Co_11 + No_12 - Co_12 > 0, DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 + No_11 - Co_11 + No_12 - Co_12, 0),
                    DuCo_12 = IIF(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 + No_11 - Co_11 + No_12 - Co_12 < 0, -(DuNo_0 - DuCo_0 + No_1 - Co_1 + No_2 - Co_2 + No_3 - Co_3 + No_4 - Co_4 + No_5 - Co_5 + No_6 - Co_6 + No_7 - Co_7 + No_8 - Co_8 + No_9 - Co_9 + No_10 - Co_10 + No_11 - Co_11 + No_12 - Co_12), 0)";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.ExecuteNonQuery();
                Console.WriteLine("✅ Đã tính dư nợ/dư có cho SoDuKhachHang");
            }
        }

        // ============================================================
        // TÍNH DUNT CHO SODUKHACHHANG
        // ============================================================
        private void TinhDuNTSoDuKhachHang(SqlConnection conn, SqlTransaction tran)
        {
            for (int i = 1; i <= 12; i++)
            {
                string sql = $@"
                    UPDATE SoDuKhachHang 
                    SET DuNT_{i} = ABS(DuNT_{i - 1} + IIF(DuNo_{i - 1} - DuCo_{i - 1} >= 0, No_{i}_NT - Co_{i}_NT, Co_{i}_NT - No_{i}_NT))
                    WHERE EXISTS (
                        SELECT 1 FROM KhachHang 
                        WHERE KhachHang.MaSo = SoDuKhachHang.MaKhachHang 
                        AND KhachHang.MaNT <> 0
                    )";

                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine("✅ Đã tính DuNT cho SoDuKhachHang");
        }

        // ============================================================
        // BƯỚC 10: KIỂM TRA KẾT QUẢ
        // ============================================================
        private void CheckKetQua(SqlConnection conn, SqlTransaction tran)
        {
            Console.WriteLine("\n📊 KẾT QUẢ SAU KHI TÍNH:");
            Console.WriteLine("========================================");

            string[] listCheck = { "156", "1331", "331", "131" };

            foreach (string soHieu in listCheck)
            {
                int maSo = GetMaSoFromSoHieu(conn, tran, soHieu);
                if (maSo == 0)
                {
                    Console.WriteLine($"❌ KHÔNG tìm thấy TK {soHieu} trong HeThongTK!");
                    continue;
                }

                string sql = $@"
                    SELECT MaSo, SoHieu, Ten, 
                           No_7, Co_7, DuNo_7, DuCo_7
                    FROM HethongTK 
                    WHERE MaSo = {maSo}";

                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Console.WriteLine($"✅ TK {soHieu} (MaSo={maSo}):");
                            Console.WriteLine($"   Tháng 7: Nợ={reader["No_7"]}, Có={reader["Co_7"]}, Dư Nợ={reader["DuNo_7"]}, Dư Có={reader["DuCo_7"]}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ KHÔNG tìm thấy TK {soHieu}");
                        }
                        reader.Close();
                    }
                }
                Console.WriteLine("");
            }
        }
    }
}