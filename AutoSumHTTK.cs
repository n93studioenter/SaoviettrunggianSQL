using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaovietTax
{
    public partial class AutoSumHTTK : DevExpress.XtraEditors.XtraForm
    {
        public AutoSumHTTK()
        {
            InitializeComponent();
        }
        private  string _connectionString;

        private void AutoSumHTTK_Load(object sender, EventArgs e)
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;
            CapNhatHeThongTK();
        }
        public void CapNhatHeThongTK()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        ResetSoDuKhachHangFull(conn, tran);  // 👈 THÊM BƯỚC NÀY

                        TaoSoDuKhachHang(conn, tran);
                        // ===== BƯỚC 1: RESET Co_1..12 =====
                        ResetCoThang(conn, tran);

                        // ===== BƯỚC 2: Cập nhật No_i, Co_i từ chứng từ =====
                        CapNhatSoDuKhachHangTuChungTu(conn, tran);

                        // ===== BƯỚC 3: Tính DuNo, DuCo cho SoDuKhachHang =====
                        TinhDuNoDuCo_SoDuKhachHang(conn, tran);

                        // ===== BƯỚC 4: Tính DuNT =====
                        TinhDuNT_SoDuKhachHang(conn, tran);

                        // ===== BƯỚC 5: Tổng hợp lên HeThongTK =====
                        TongHopLenHeThongTK(conn, tran);

                        // ===== BƯỚC 6: Tổng hợp tài khoản cấp cha =====
                        TongHopTaiKhoanCapCha(conn, tran);

                        // ===== BƯỚC 7: Tách Dư Nợ/Dư Có =====
                        TachDuNoDuCo_HeThongTK(conn, tran);

                        tran.Commit();
                        Console.WriteLine("✅ Cập nhật HeThongTK thành công!");
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        Console.WriteLine($"❌ Lỗi: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        private void ResetSoDuKhachHangFull(SqlConnection conn, SqlTransaction tran)
        {
            string sql = @"
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
            No_12 = 0, Co_12 = 0, No_12_NT = 0, Co_12_NT = 0,
            DuNo_0 = 0, DuCo_0 = 0,
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
        WHERE MaKhachHang > 0";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.ExecuteNonQuery();
            }
        }
        private void TaoSoDuKhachHang(SqlConnection conn, SqlTransaction tran)
        {
            string sql = @"
        INSERT INTO SoDuKhachHang (MaKhachHang, MaTaiKhoan, DuNo_0, DuCo_0,
                                    No_1, Co_1, No_2, Co_2, No_3, Co_3,
                                    No_4, Co_4, No_5, Co_5, No_6, Co_6,
                                    No_7, Co_7, No_8, Co_8, No_9, Co_9,
                                    No_10, Co_10, No_11, Co_11, No_12, Co_12)
        SELECT 
            MaKH,
            18 AS MaTaiKhoan,
            0 AS DuNo_0,
            0 AS DuCo_0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0
        FROM ChungTu
        WHERE MaKH > 0
          AND NOT EXISTS (SELECT 1 FROM SoDuKhachHang WHERE MaKhachHang = ChungTu.MaKH)
        
        UNION
        
        SELECT 
            MaKHC,
            82 AS MaTaiKhoan,
            0 AS DuNo_0,
            0 AS DuCo_0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0
        FROM ChungTu
        WHERE MaKHC > 0
          AND NOT EXISTS (SELECT 1 FROM SoDuKhachHang WHERE MaKhachHang = ChungTu.MaKHC)";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                int soLuong = cmd.ExecuteNonQuery();
                if (soLuong > 0)
                    Console.WriteLine($"✅ Đã tạo {soLuong} dòng SoDuKhachHang mới");
            }
        }
        // ============================================================
        // BƯỚC 1: RESET Co_1..12 VỀ 0
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
                cmd.ExecuteNonQuery();
            }
        }

        // ============================================================
        // BƯỚC 2: Cập nhật No_i, Co_i từ chứng từ
        // ============================================================
        /// <summary>
        /// Cập nhật No_i, Co_i cho SoDuKhachHang từ chứng từ
        /// </summary>
        private void CapNhatSoDuKhachHangTuChungTu(SqlConnection conn, SqlTransaction tran)
        {
            // ============ BƯỚC 1: Lấy danh sách tài khoản công nợ (TK_ID = 3500 hoặc 3310) ============
            string sqlTK = "SELECT MaSo FROM HethongTK WHERE TK_ID IN (3500, 3310)";
            DataTable dtTK = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlTK, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtTK);
                }
            }

            // Nếu không có tài khoản công nợ thì thoát
            if (dtTK.Rows.Count == 0)
            {
                Console.WriteLine("⚠️ Không có tài khoản công nợ (TK_ID=3500,3310)");
                return;
            }

            // Tạo chuỗi IN (ví dụ: 18,82,101)
            string inClause = "";
            for (int i = 0; i < dtTK.Rows.Count; i++)
            {
                if (i > 0) inClause += ",";
                inClause += dtTK.Rows[i]["MaSo"].ToString();
            }

            Console.WriteLine($"📌 Danh sách TK công nợ: {inClause}");

            // ============ BƯỚC 2: Lấy tổng hợp chứng từ công nợ ============
            string sqlSelect = $@"
        SELECT 
            MaKH,
            MaKHC,
            ThangCT,
            SUM(CASE WHEN MaTKNo IN ({inClause}) THEN SoPS ELSE 0 END) AS No_PS,
            SUM(CASE WHEN MaTKCo IN ({inClause}) THEN SoPS ELSE 0 END) AS Co_PS
        FROM ChungTu
        WHERE (MaKH > 0 Or MaKHC >0)
        GROUP BY MaKH,MaKHC, ThangCT
        ORDER BY MaKH,MaKHC,ThangCT";

            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlSelect, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            Console.WriteLine($"📌 Có {dt.Rows.Count} dòng chứng từ công nợ cần cập nhật");

            // ============ BƯỚC 3: Cập nhật từng dòng vào SoDuKhachHang ============
            foreach (DataRow row in dt.Rows)
            {
                int maKH = Convert.ToInt32(row["MaKH"]);
                if (maKH == 0)
                {
                    maKH = Convert.ToInt32(row["MaKHC"]);
                }
                int thang = Convert.ToInt32(row["ThangCT"]);
                double no = Convert.ToDouble(row["No_PS"]);
                double co = Convert.ToDouble(row["Co_PS"]);

                string colNo = $"No_{thang}";
                string colCo = $"Co_{thang}";

                string sqlUpdate = $@"
            UPDATE SoDuKhachHang 
            SET 
                {colNo} = ISNULL({colNo}, 0) + @No,
                No_{thang}_NT = ISNULL(No_{thang}_NT, 0) + @No,  -- Nếu có cột ngoại tệ
                {colCo} = ISNULL({colCo}, 0) + @Co,
                Co_{thang}_NT = ISNULL(Co_{thang}_NT, 0) + @Co   -- Nếu có cột ngoại tệ
            WHERE MaKhachHang = @MaKH";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@No", no);
                    cmd.Parameters.AddWithValue("@Co", co);
                    cmd.Parameters.AddWithValue("@MaKH", maKH);
                    cmd.ExecuteNonQuery();
                }
            }

            XtraMessageBox.Show("Đã tính xong!");
            this.Close();
        }

        // ============================================================
        // BƯỚC 3: TÍNH DuNo, DuCo CHO SoDuKhachHang
        // ============================================================
        private void TinhDuNoDuCo_SoDuKhachHang(SqlConnection conn, SqlTransaction tran)
        {
            string sql = @"
        -- ===== RESET DuNo, DuCo về 0 =====
        UPDATE SoDuKhachHang SET 
            DuNo_1 = 0, DuCo_1 = 0,
            DuNo_2 = 0, DuCo_2 = 0,
            DuNo_3 = 0, DuCo_3 = 0,
            DuNo_4 = 0, DuCo_4 = 0,
            DuNo_5 = 0, DuCo_5 = 0,
            DuNo_6 = 0, DuCo_6 = 0,
            DuNo_7 = 0, DuCo_7 = 0,
            DuNo_8 = 0, DuCo_8 = 0,
            DuNo_9 = 0, DuCo_9 = 0,
            DuNo_10 = 0, DuCo_10 = 0,
            DuNo_11 = 0, DuCo_11 = 0,
            DuNo_12 = 0, DuCo_12 = 0
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 1 =====
        UPDATE SoDuKhachHang SET 
            DuNo_1 = IIF(ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0) + ISNULL(No_1, 0) - ISNULL(Co_1, 0) > 0, 
                         ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0) + ISNULL(No_1, 0) - ISNULL(Co_1, 0), 0),
            DuCo_1 = IIF(ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0) + ISNULL(No_1, 0) - ISNULL(Co_1, 0) < 0, 
                         -(ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0) + ISNULL(No_1, 0) - ISNULL(Co_1, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 2 =====
        UPDATE SoDuKhachHang SET 
            DuNo_2 = IIF(ISNULL(DuNo_1, 0) - ISNULL(DuCo_1, 0) + ISNULL(No_2, 0) - ISNULL(Co_2, 0) > 0, 
                         ISNULL(DuNo_1, 0) - ISNULL(DuCo_1, 0) + ISNULL(No_2, 0) - ISNULL(Co_2, 0), 0),
            DuCo_2 = IIF(ISNULL(DuNo_1, 0) - ISNULL(DuCo_1, 0) + ISNULL(No_2, 0) - ISNULL(Co_2, 0) < 0, 
                         -(ISNULL(DuNo_1, 0) - ISNULL(DuCo_1, 0) + ISNULL(No_2, 0) - ISNULL(Co_2, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 3 =====
        UPDATE SoDuKhachHang SET 
            DuNo_3 = IIF(ISNULL(DuNo_2, 0) - ISNULL(DuCo_2, 0) + ISNULL(No_3, 0) - ISNULL(Co_3, 0) > 0, 
                         ISNULL(DuNo_2, 0) - ISNULL(DuCo_2, 0) + ISNULL(No_3, 0) - ISNULL(Co_3, 0), 0),
            DuCo_3 = IIF(ISNULL(DuNo_2, 0) - ISNULL(DuCo_2, 0) + ISNULL(No_3, 0) - ISNULL(Co_3, 0) < 0, 
                         -(ISNULL(DuNo_2, 0) - ISNULL(DuCo_2, 0) + ISNULL(No_3, 0) - ISNULL(Co_3, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 4 =====
        UPDATE SoDuKhachHang SET 
            DuNo_4 = IIF(ISNULL(DuNo_3, 0) - ISNULL(DuCo_3, 0) + ISNULL(No_4, 0) - ISNULL(Co_4, 0) > 0, 
                         ISNULL(DuNo_3, 0) - ISNULL(DuCo_3, 0) + ISNULL(No_4, 0) - ISNULL(Co_4, 0), 0),
            DuCo_4 = IIF(ISNULL(DuNo_3, 0) - ISNULL(DuCo_3, 0) + ISNULL(No_4, 0) - ISNULL(Co_4, 0) < 0, 
                         -(ISNULL(DuNo_3, 0) - ISNULL(DuCo_3, 0) + ISNULL(No_4, 0) - ISNULL(Co_4, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 5 =====
        UPDATE SoDuKhachHang SET 
            DuNo_5 = IIF(ISNULL(DuNo_4, 0) - ISNULL(DuCo_4, 0) + ISNULL(No_5, 0) - ISNULL(Co_5, 0) > 0, 
                         ISNULL(DuNo_4, 0) - ISNULL(DuCo_4, 0) + ISNULL(No_5, 0) - ISNULL(Co_5, 0), 0),
            DuCo_5 = IIF(ISNULL(DuNo_4, 0) - ISNULL(DuCo_4, 0) + ISNULL(No_5, 0) - ISNULL(Co_5, 0) < 0, 
                         -(ISNULL(DuNo_4, 0) - ISNULL(DuCo_4, 0) + ISNULL(No_5, 0) - ISNULL(Co_5, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 6 =====
        UPDATE SoDuKhachHang SET 
            DuNo_6 = IIF(ISNULL(DuNo_5, 0) - ISNULL(DuCo_5, 0) + ISNULL(No_6, 0) - ISNULL(Co_6, 0) > 0, 
                         ISNULL(DuNo_5, 0) - ISNULL(DuCo_5, 0) + ISNULL(No_6, 0) - ISNULL(Co_6, 0), 0),
            DuCo_6 = IIF(ISNULL(DuNo_5, 0) - ISNULL(DuCo_5, 0) + ISNULL(No_6, 0) - ISNULL(Co_6, 0) < 0, 
                         -(ISNULL(DuNo_5, 0) - ISNULL(DuCo_5, 0) + ISNULL(No_6, 0) - ISNULL(Co_6, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 7 =====
        UPDATE SoDuKhachHang SET 
            DuNo_7 = IIF(ISNULL(DuNo_6, 0) - ISNULL(DuCo_6, 0) + ISNULL(No_7, 0) - ISNULL(Co_7, 0) > 0, 
                         ISNULL(DuNo_6, 0) - ISNULL(DuCo_6, 0) + ISNULL(No_7, 0) - ISNULL(Co_7, 0), 0),
            DuCo_7 = IIF(ISNULL(DuNo_6, 0) - ISNULL(DuCo_6, 0) + ISNULL(No_7, 0) - ISNULL(Co_7, 0) < 0, 
                         -(ISNULL(DuNo_6, 0) - ISNULL(DuCo_6, 0) + ISNULL(No_7, 0) - ISNULL(Co_7, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 8 =====
        UPDATE SoDuKhachHang SET 
            DuNo_8 = IIF(ISNULL(DuNo_7, 0) - ISNULL(DuCo_7, 0) + ISNULL(No_8, 0) - ISNULL(Co_8, 0) > 0, 
                         ISNULL(DuNo_7, 0) - ISNULL(DuCo_7, 0) + ISNULL(No_8, 0) - ISNULL(Co_8, 0), 0),
            DuCo_8 = IIF(ISNULL(DuNo_7, 0) - ISNULL(DuCo_7, 0) + ISNULL(No_8, 0) - ISNULL(Co_8, 0) < 0, 
                         -(ISNULL(DuNo_7, 0) - ISNULL(DuCo_7, 0) + ISNULL(No_8, 0) - ISNULL(Co_8, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 9 =====
        UPDATE SoDuKhachHang SET 
            DuNo_9 = IIF(ISNULL(DuNo_8, 0) - ISNULL(DuCo_8, 0) + ISNULL(No_9, 0) - ISNULL(Co_9, 0) > 0, 
                         ISNULL(DuNo_8, 0) - ISNULL(DuCo_8, 0) + ISNULL(No_9, 0) - ISNULL(Co_9, 0), 0),
            DuCo_9 = IIF(ISNULL(DuNo_8, 0) - ISNULL(DuCo_8, 0) + ISNULL(No_9, 0) - ISNULL(Co_9, 0) < 0, 
                         -(ISNULL(DuNo_8, 0) - ISNULL(DuCo_8, 0) + ISNULL(No_9, 0) - ISNULL(Co_9, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 10 =====
        UPDATE SoDuKhachHang SET 
            DuNo_10 = IIF(ISNULL(DuNo_9, 0) - ISNULL(DuCo_9, 0) + ISNULL(No_10, 0) - ISNULL(Co_10, 0) > 0, 
                          ISNULL(DuNo_9, 0) - ISNULL(DuCo_9, 0) + ISNULL(No_10, 0) - ISNULL(Co_10, 0), 0),
            DuCo_10 = IIF(ISNULL(DuNo_9, 0) - ISNULL(DuCo_9, 0) + ISNULL(No_10, 0) - ISNULL(Co_10, 0) < 0, 
                          -(ISNULL(DuNo_9, 0) - ISNULL(DuCo_9, 0) + ISNULL(No_10, 0) - ISNULL(Co_10, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 11 =====
        UPDATE SoDuKhachHang SET 
            DuNo_11 = IIF(ISNULL(DuNo_10, 0) - ISNULL(DuCo_10, 0) + ISNULL(No_11, 0) - ISNULL(Co_11, 0) > 0, 
                          ISNULL(DuNo_10, 0) - ISNULL(DuCo_10, 0) + ISNULL(No_11, 0) - ISNULL(Co_11, 0), 0),
            DuCo_11 = IIF(ISNULL(DuNo_10, 0) - ISNULL(DuCo_10, 0) + ISNULL(No_11, 0) - ISNULL(Co_11, 0) < 0, 
                          -(ISNULL(DuNo_10, 0) - ISNULL(DuCo_10, 0) + ISNULL(No_11, 0) - ISNULL(Co_11, 0)), 0)
        WHERE MaKhachHang > 0;

        -- ===== THÁNG 12 =====
        UPDATE SoDuKhachHang SET 
            DuNo_12 = IIF(ISNULL(DuNo_11, 0) - ISNULL(DuCo_11, 0) + ISNULL(No_12, 0) - ISNULL(Co_12, 0) > 0, 
                          ISNULL(DuNo_11, 0) - ISNULL(DuCo_11, 0) + ISNULL(No_12, 0) - ISNULL(Co_12, 0), 0),
            DuCo_12 = IIF(ISNULL(DuNo_11, 0) - ISNULL(DuCo_11, 0) + ISNULL(No_12, 0) - ISNULL(Co_12, 0) < 0, 
                          -(ISNULL(DuNo_11, 0) - ISNULL(DuCo_11, 0) + ISNULL(No_12, 0) - ISNULL(Co_12, 0)), 0)
        WHERE MaKhachHang > 0;";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("✅ Đã tính DuNo, DuCo cho 12 tháng!");
        }

        // ============================================================
        // BƯỚC 4: TÍNH DuNT_1..12 (Ngoại tệ)
        // ============================================================
        private void TinhDuNT_SoDuKhachHang(SqlConnection conn, SqlTransaction tran)
        {
            string fromClause = "FROM SoDuKhachHang INNER JOIN KhachHang ON SoDuKhachHang.MaKhachHang = KhachHang.MaSo";

            for (int i = 1; i <= 12; i++)
            {
                string sql = $@"
                    UPDATE SoDuKhachHang 
                    SET DuNT_{i} = ABS(DuNT_{i - 1} + IIF(DuNo_{i - 1} - DuCo_{i - 1} >= 0, No_{i}_NT - Co_{i}_NT, Co_{i}_NT - No_{i}_NT))
                    {fromClause}
                    WHERE KhachHang.MaNT <> 0";

                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        // BƯỚC 5: TỔNG HỢP TỪ SoDuKhachHang LÊN HeThongTK
        // ============================================================
        /// <summary>
        /// Bước 5: Tổng hợp từ SoDuKhachHang lên HeThongTK
        /// </summary>
        private void TongHopLenHeThongTK(SqlConnection conn, SqlTransaction tran)
        {
            // ============ BƯỚC 1: Lấy danh sách MaTaiKhoan từ SoDuKhachHang ============
            string sqlGetTK = "SELECT DISTINCT MaTaiKhoan FROM SoDuKhachHang WHERE MaTaiKhoan > 0";
            DataTable dtTK = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlGetTK, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtTK);
                }
            }

            if (dtTK.Rows.Count == 0)
            {
                Console.WriteLine("⚠️ Không có dữ liệu trong SoDuKhachHang");
                return;
            }

            Console.WriteLine($"📌 Có {dtTK.Rows.Count} tài khoản cần tổng hợp");

            // ============ BƯỚC 2: Tổng hợp từng tài khoản ============
            foreach (DataRow rowTK in dtTK.Rows)
            {
                int maTaiKhoan = Convert.ToInt32(rowTK["MaTaiKhoan"]);

                // Tổng hợp số liệu từ SoDuKhachHang
                string sqlTongHop = $@"
            SELECT 
                ISNULL(SUM(DuNo_0), 0) AS DuNo_0,
                ISNULL(SUM(DuCo_0), 0) AS DuCo_0,
                ISNULL(SUM(No_1), 0) AS No_1,
                ISNULL(SUM(Co_1), 0) AS Co_1,
                ISNULL(SUM(No_2), 0) AS No_2,
                ISNULL(SUM(Co_2), 0) AS Co_2,
                ISNULL(SUM(No_3), 0) AS No_3,
                ISNULL(SUM(Co_3), 0) AS Co_3,
                ISNULL(SUM(No_4), 0) AS No_4,
                ISNULL(SUM(Co_4), 0) AS Co_4,
                ISNULL(SUM(No_5), 0) AS No_5,
                ISNULL(SUM(Co_5), 0) AS Co_5,
                ISNULL(SUM(No_6), 0) AS No_6,
                ISNULL(SUM(Co_6), 0) AS Co_6,
                ISNULL(SUM(No_7), 0) AS No_7,
                ISNULL(SUM(Co_7), 0) AS Co_7,
                ISNULL(SUM(No_8), 0) AS No_8,
                ISNULL(SUM(Co_8), 0) AS Co_8,
                ISNULL(SUM(No_9), 0) AS No_9,
                ISNULL(SUM(Co_9), 0) AS Co_9,
                ISNULL(SUM(No_10), 0) AS No_10,
                ISNULL(SUM(Co_10), 0) AS Co_10,
                ISNULL(SUM(No_11), 0) AS No_11,
                ISNULL(SUM(Co_11), 0) AS Co_11,
                ISNULL(SUM(No_12), 0) AS No_12,
                ISNULL(SUM(Co_12), 0) AS Co_12
            FROM SoDuKhachHang
            WHERE MaTaiKhoan = @MaTaiKhoan";

                DataTable dtTong = new DataTable();
                using (SqlCommand cmd = new SqlCommand(sqlTongHop, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dtTong);
                    }
                }

                if (dtTong.Rows.Count == 0) continue;

                DataRow row = dtTong.Rows[0];

                // Cập nhật HeThongTK
                string sqlUpdate = $@"
            UPDATE HethongTK SET 
                DuNo_0 = ISNULL(DuNo_0, 0) + @DuNo_0,
                DuCo_0 = ISNULL(DuCo_0, 0) + @DuCo_0,
                No_1 = ISNULL(No_1, 0) + @No_1,
                Co_1 = ISNULL(Co_1, 0) + @Co_1,
                No_2 = ISNULL(No_2, 0) + @No_2,
                Co_2 = ISNULL(Co_2, 0) + @Co_2,
                No_3 = ISNULL(No_3, 0) + @No_3,
                Co_3 = ISNULL(Co_3, 0) + @Co_3,
                No_4 = ISNULL(No_4, 0) + @No_4,
                Co_4 = ISNULL(Co_4, 0) + @Co_4,
                No_5 = ISNULL(No_5, 0) + @No_5,
                Co_5 = ISNULL(Co_5, 0) + @Co_5,
                No_6 = ISNULL(No_6, 0) + @No_6,
                Co_6 = ISNULL(Co_6, 0) + @Co_6,
                No_7 = ISNULL(No_7, 0) + @No_7,
                Co_7 = ISNULL(Co_7, 0) + @Co_7,
                No_8 = ISNULL(No_8, 0) + @No_8,
                Co_8 = ISNULL(Co_8, 0) + @Co_8,
                No_9 = ISNULL(No_9, 0) + @No_9,
                Co_9 = ISNULL(Co_9, 0) + @Co_9,
                No_10 = ISNULL(No_10, 0) + @No_10,
                Co_10 = ISNULL(Co_10, 0) + @Co_10,
                No_11 = ISNULL(No_11, 0) + @No_11,
                Co_11 = ISNULL(Co_11, 0) + @Co_11,
                No_12 = ISNULL(No_12, 0) + @No_12,
                Co_12 = ISNULL(Co_12, 0) + @Co_12
            WHERE MaSo = @MaTaiKhoan";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
                    cmd.Parameters.AddWithValue("@DuNo_0", Convert.ToDouble(row["DuNo_0"]));
                    cmd.Parameters.AddWithValue("@DuCo_0", Convert.ToDouble(row["DuCo_0"]));
                    cmd.Parameters.AddWithValue("@No_1", Convert.ToDouble(row["No_1"]));
                    cmd.Parameters.AddWithValue("@Co_1", Convert.ToDouble(row["Co_1"]));
                    cmd.Parameters.AddWithValue("@No_2", Convert.ToDouble(row["No_2"]));
                    cmd.Parameters.AddWithValue("@Co_2", Convert.ToDouble(row["Co_2"]));
                    cmd.Parameters.AddWithValue("@No_3", Convert.ToDouble(row["No_3"]));
                    cmd.Parameters.AddWithValue("@Co_3", Convert.ToDouble(row["Co_3"]));
                    cmd.Parameters.AddWithValue("@No_4", Convert.ToDouble(row["No_4"]));
                    cmd.Parameters.AddWithValue("@Co_4", Convert.ToDouble(row["Co_4"]));
                    cmd.Parameters.AddWithValue("@No_5", Convert.ToDouble(row["No_5"]));
                    cmd.Parameters.AddWithValue("@Co_5", Convert.ToDouble(row["Co_5"]));
                    cmd.Parameters.AddWithValue("@No_6", Convert.ToDouble(row["No_6"]));
                    cmd.Parameters.AddWithValue("@Co_6", Convert.ToDouble(row["Co_6"]));
                    cmd.Parameters.AddWithValue("@No_7", Convert.ToDouble(row["No_7"]));
                    cmd.Parameters.AddWithValue("@Co_7", Convert.ToDouble(row["Co_7"]));
                    cmd.Parameters.AddWithValue("@No_8", Convert.ToDouble(row["No_8"]));
                    cmd.Parameters.AddWithValue("@Co_8", Convert.ToDouble(row["Co_8"]));
                    cmd.Parameters.AddWithValue("@No_9", Convert.ToDouble(row["No_9"]));
                    cmd.Parameters.AddWithValue("@Co_9", Convert.ToDouble(row["Co_9"]));
                    cmd.Parameters.AddWithValue("@No_10", Convert.ToDouble(row["No_10"]));
                    cmd.Parameters.AddWithValue("@Co_10", Convert.ToDouble(row["Co_10"]));
                    cmd.Parameters.AddWithValue("@No_11", Convert.ToDouble(row["No_11"]));
                    cmd.Parameters.AddWithValue("@Co_11", Convert.ToDouble(row["Co_11"]));
                    cmd.Parameters.AddWithValue("@No_12", Convert.ToDouble(row["No_12"]));
                    cmd.Parameters.AddWithValue("@Co_12", Convert.ToDouble(row["Co_12"]));

                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine("✅ Đã tổng hợp lên HeThongTK!");
        }

        /// <summary>
        /// Bước 6: Tổng hợp tài khoản cấp cha
        /// </summary>
        private void TongHopTaiKhoanCapCha(SqlConnection conn, SqlTransaction tran)
        {
            // ============ BƯỚC 1: Lấy danh sách tài khoản cấp cha ============
            string sqlGet = "SELECT MaSo FROM HethongTK WHERE TkCha0 > 0 AND TKCon = 0";
            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlGet, conn, tran))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0)
            {
                Console.WriteLine("⚠️ Không có tài khoản cấp cha cần tổng hợp");
                return;
            }

            Console.WriteLine($"📌 Có {dt.Rows.Count} tài khoản cấp cha cần tổng hợp");

            // ============ BƯỚC 2: Cập nhật từng tài khoản cấp cha ============
            foreach (DataRow row in dt.Rows)
            {
                int maSo = Convert.ToInt32(row["MaSo"]);

                string sqlUpdate = $@"
            UPDATE HethongTK SET 
                DuNo_0 = (SELECT ISNULL(SUM(DuNo_0), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                DuCo_0 = (SELECT ISNULL(SUM(DuCo_0), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_1 = (SELECT ISNULL(SUM(No_1), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_1 = (SELECT ISNULL(SUM(Co_1), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_2 = (SELECT ISNULL(SUM(No_2), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_2 = (SELECT ISNULL(SUM(Co_2), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_3 = (SELECT ISNULL(SUM(No_3), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_3 = (SELECT ISNULL(SUM(Co_3), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_4 = (SELECT ISNULL(SUM(No_4), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_4 = (SELECT ISNULL(SUM(Co_4), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_5 = (SELECT ISNULL(SUM(No_5), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_5 = (SELECT ISNULL(SUM(Co_5), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_6 = (SELECT ISNULL(SUM(No_6), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_6 = (SELECT ISNULL(SUM(Co_6), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_7 = (SELECT ISNULL(SUM(No_7), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_7 = (SELECT ISNULL(SUM(Co_7), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_8 = (SELECT ISNULL(SUM(No_8), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_8 = (SELECT ISNULL(SUM(Co_8), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_9 = (SELECT ISNULL(SUM(No_9), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_9 = (SELECT ISNULL(SUM(Co_9), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_10 = (SELECT ISNULL(SUM(No_10), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_10 = (SELECT ISNULL(SUM(Co_10), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_11 = (SELECT ISNULL(SUM(No_11), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_11 = (SELECT ISNULL(SUM(Co_11), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                No_12 = (SELECT ISNULL(SUM(No_12), 0) FROM HethongTK WHERE TkCha0 = @MaSo),
                Co_12 = (SELECT ISNULL(SUM(Co_12), 0) FROM HethongTK WHERE TkCha0 = @MaSo)
            WHERE MaSo = @MaSo";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@MaSo", maSo);
                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine("✅ Đã tổng hợp tài khoản cấp cha!");
        }

        private void TachDuNoDuCo_HeThongTK(SqlConnection conn, SqlTransaction tran)
        {
            string sql = @"
                UPDATE HethongTK SET 
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
                WHERE TK_ID2 <> 1310";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}