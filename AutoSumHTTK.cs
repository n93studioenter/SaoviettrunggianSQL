//using DevExpress.XtraEditors;
//using System;
//using System.Configuration;
//using System.Data;
//using System.Data.SqlClient;
//using System.Windows.Forms;

//namespace SaovietTax
//{
//    public partial class AutoSumHTTK : DevExpress.XtraEditors.XtraForm
//    {
//        public AutoSumHTTK()
//        {
//            InitializeComponent();
//        }

//        private string _connectionString;

//        public void TinhHTTK()
//        {
//            _connectionString = ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;
//            CapNhatHeThongTK();
//        }
//        private void AutoSumHTTK_Load(object sender, EventArgs e)
//        {
//            TinhHTTK();
//        }
//        private void ResetLoai3(SqlConnection conn, SqlTransaction tran)
//        {
//            string sql = @"
//        UPDATE HethongTK 
//        SET 
//            No_1 = 0, Co_1 = 0, DuNo_1 = 0, DuCo_1 = 0,
//            No_2 = 0, Co_2 = 0, DuNo_2 = 0, DuCo_2 = 0,
//            No_3 = 0, Co_3 = 0, DuNo_3 = 0, DuCo_3 = 0,
//            No_4 = 0, Co_4 = 0, DuNo_4 = 0, DuCo_4 = 0,
//            No_5 = 0, Co_5 = 0, DuNo_5 = 0, DuCo_5 = 0,
//            No_6 = 0, Co_6 = 0, DuNo_6 = 0, DuCo_6 = 0,
//            No_7 = 0, Co_7 = 0, DuNo_7 = 0, DuCo_7 = 0,
//            No_8 = 0, Co_8 = 0, DuNo_8 = 0, DuCo_8 = 0,
//            No_9 = 0, Co_9 = 0, DuNo_9 = 0, DuCo_9 = 0,
//            No_10 = 0, Co_10 = 0, DuNo_10 = 0, DuCo_10 = 0,
//            No_11 = 0, Co_11 = 0, DuNo_11 = 0, DuCo_11 = 0,
//            No_12 = 0, Co_12 = 0, DuNo_12 = 0, DuCo_12 = 0
//        WHERE SoHieu = 'Lo¹i 3'";

//            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
//            {
//              // var getcount= cmd.ExecuteNonQuery();
//                Console.WriteLine("✅ Đã reset Loại 3 (giữ nguyên Cap)");
//            }
//        }
//        public void CapNhatHeThongTK()
//        {
//            using (SqlConnection conn = new SqlConnection(_connectionString))
//            {
//                conn.Open();
//                using (SqlTransaction tran = conn.BeginTransaction())
//                {
//                    try
//                    {
//                        // ===== RESET LOẠI 3 TRƯỚC KHI TÍNH =====
//                        ResetLoai3(conn, tran);
//                        Console.WriteLine("🚀 BẮT ĐẦU CẬP NHẬT HETHONGTK...");

//                        // BƯỚC 1: CẬP NHẬT SỐ PHÁT SINH
//                        Console.WriteLine("📊 Cập nhật số phát sinh...");
//                        CapNhatSoPhatSinh(conn, tran);

//                        // BƯỚC 2: TÍNH DƯ CHO CÁC TK CẤP CON
//                        Console.WriteLine("📊 Tính dư...");
//                        TinhDu(conn, tran);

//                        // BƯỚC 3: TỔNG HỢP LÊN TK CẤP CHA (ĐỦ 12 THÁNG)
//                        Console.WriteLine("📊 Tổng hợp lên TK cấp cha...");
//                        TongHopLenCapCha(conn, tran);

//                        // BƯỚC 4: CẬP NHẬT SODUKHACHHANG
//                        Console.WriteLine("📊 Cập nhật SoDuKhachHang...");
//                        CapNhatSoDuKhachHang(conn, tran);

//                        // BƯỚC 5: KIỂM TRA
//                        CheckKetQua(conn, tran);

//                        tran.Commit();
//                        XtraMessageBox.Show("✅ Cập nhật HeThongTK thành công!", "Thông báo");
//                        this.Close();
//                    }
//                    catch (Exception ex)
//                    {
//                        tran.Rollback();
//                        XtraMessageBox.Show($"❌ Lỗi: {ex.Message}", "Lỗi");
//                        throw;
//                    }
//                }
//            }
//        }

//        // ============================================================
//        // BƯỚC 1: CẬP NHẬT SỐ PHÁT SINH (CHỈ TK CẤP CON)
//        // ============================================================
//        private void CapNhatSoPhatSinh(SqlConnection conn, SqlTransaction tran)
//        {
//            // Cập nhật Nợ
//            string sqlNo = @"
//                SELECT 
//                    MaTKNo AS MaSo,
//                    " + GetSumSql("No") + @"
//                FROM ChungTu
//                WHERE MaTKNo > 0
//                GROUP BY MaTKNo";

//            DataTable dtNo = new DataTable();
//            using (SqlCommand cmd = new SqlCommand(sqlNo, conn, tran))
//            {
//                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
//                {
//                    da.Fill(dtNo);
//                }
//            }

//            foreach (DataRow row in dtNo.Rows)
//            {
//                int maSo = Convert.ToInt32(row["MaSo"]);

//                // CHỈ CẬP NHẬT TK CẤP CON (TKCon = 0)
//                string sqlCheck = $"SELECT TKCon FROM HethongTK WHERE MaSo = {maSo}";
//                using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, conn, tran))
//                {
//                    object result = cmdCheck.ExecuteScalar();
//                    if (result == null || result == DBNull.Value) continue;
//                    int tkCon = Convert.ToInt32(result);
//                    if (tkCon != 0) continue;
//                }

//                string sqlUpdate = "UPDATE HethongTK SET ";
//                for (int i = 1; i <= 12; i++)
//                {
//                    string colName = $"No_{i}";
//                    double value = 0;
//                    if (row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value)
//                    {
//                        value = Convert.ToDouble(row[colName]);
//                    }
//                    sqlUpdate += $"No_{i} = {value}";
//                    if (i < 12) sqlUpdate += ", ";
//                }
//                sqlUpdate += $" WHERE MaSo = {maSo}";

//                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
//                {
//                    cmd.ExecuteNonQuery();
//                }
//            }

//            // Cập nhật Có
//            string sqlCo = @"
//                SELECT 
//                    MaTKCo AS MaSo,
//                    " + GetSumSql("Co") + @"
//                FROM ChungTu
//                WHERE MaTKCo > 0
//                GROUP BY MaTKCo";

//            DataTable dtCo = new DataTable();
//            using (SqlCommand cmd = new SqlCommand(sqlCo, conn, tran))
//            {
//                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
//                {
//                    da.Fill(dtCo);
//                }
//            }

//            foreach (DataRow row in dtCo.Rows)
//            {
//                int maSo = Convert.ToInt32(row["MaSo"]);

//                // CHỈ CẬP NHẬT TK CẤP CON (TKCon = 0)
//                string sqlCheck = $"SELECT TKCon FROM HethongTK WHERE MaSo = {maSo}";
//                using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, conn, tran))
//                {
//                    object result = cmdCheck.ExecuteScalar();
//                    if (result == null || result == DBNull.Value) continue;
//                    int tkCon = Convert.ToInt32(result);
//                    if (tkCon != 0) continue;
//                }

//                string sqlUpdate = "UPDATE HethongTK SET ";
//                for (int i = 1; i <= 12; i++)
//                {
//                    string colName = $"Co_{i}";
//                    double value = 0;
//                    if (row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value)
//                    {
//                        value = Convert.ToDouble(row[colName]);
//                    }
//                    sqlUpdate += $"Co_{i} = {value}";
//                    if (i < 12) sqlUpdate += ", ";
//                }
//                sqlUpdate += $" WHERE MaSo = {maSo}";

//                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
//                {
//                    cmd.ExecuteNonQuery();
//                }
//            }

//            Console.WriteLine($"✅ Đã cập nhật số phát sinh");
//        }

//        // ============================================================
//        // BƯỚC 2: TÍNH DƯ CHO CÁC TK CẤP CON
//        // ============================================================
//        private void TinhDu(SqlConnection conn, SqlTransaction tran)
//        {
//            // Lấy danh sách TK cấp con (TKCon = 0) có phát sinh
//            string sqlGetMaSo = @"
//                SELECT MaSo 
//                FROM HethongTK 
//                WHERE TKCon = 0
//                AND MaSo IN (
//                    SELECT MaTKNo FROM ChungTu WHERE SoPS <> 0
//                    UNION
//                    SELECT MaTKCo FROM ChungTu WHERE SoPS <> 0
//                )";

//            DataTable dtMaSo = new DataTable();
//            using (SqlCommand cmd = new SqlCommand(sqlGetMaSo, conn, tran))
//            {
//                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
//                {
//                    da.Fill(dtMaSo);
//                }
//            }

//            Console.WriteLine($"📌 Tính dư cho {dtMaSo.Rows.Count} tài khoản cấp con");

//            foreach (DataRow row in dtMaSo.Rows)
//            {
//                int maSo = Convert.ToInt32(row["MaSo"]);

//                string sql = $@"
//                    UPDATE HethongTK 
//                    SET 
//                        DuNo_1 = IIF(ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0) + ISNULL(No_1, 0) - ISNULL(Co_1, 0) > 0, 
//                                      ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0) + ISNULL(No_1, 0) - ISNULL(Co_1, 0), 0),
//                        DuCo_1 = IIF(ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0) + ISNULL(No_1, 0) - ISNULL(Co_1, 0) < 0, 
//                                      -(ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0) + ISNULL(No_1, 0) - ISNULL(Co_1, 0)), 0),
//                        DuNo_2 = IIF(ISNULL(DuNo_1, 0) - ISNULL(DuCo_1, 0) + ISNULL(No_2, 0) - ISNULL(Co_2, 0) > 0, 
//                                      ISNULL(DuNo_1, 0) - ISNULL(DuCo_1, 0) + ISNULL(No_2, 0) - ISNULL(Co_2, 0), 0),
//                        DuCo_2 = IIF(ISNULL(DuNo_1, 0) - ISNULL(DuCo_1, 0) + ISNULL(No_2, 0) - ISNULL(Co_2, 0) < 0, 
//                                      -(ISNULL(DuNo_1, 0) - ISNULL(DuCo_1, 0) + ISNULL(No_2, 0) - ISNULL(Co_2, 0)), 0),
//                        DuNo_3 = IIF(ISNULL(DuNo_2, 0) - ISNULL(DuCo_2, 0) + ISNULL(No_3, 0) - ISNULL(Co_3, 0) > 0, 
//                                      ISNULL(DuNo_2, 0) - ISNULL(DuCo_2, 0) + ISNULL(No_3, 0) - ISNULL(Co_3, 0), 0),
//                        DuCo_3 = IIF(ISNULL(DuNo_2, 0) - ISNULL(DuCo_2, 0) + ISNULL(No_3, 0) - ISNULL(Co_3, 0) < 0, 
//                                      -(ISNULL(DuNo_2, 0) - ISNULL(DuCo_2, 0) + ISNULL(No_3, 0) - ISNULL(Co_3, 0)), 0),
//                        DuNo_4 = IIF(ISNULL(DuNo_3, 0) - ISNULL(DuCo_3, 0) + ISNULL(No_4, 0) - ISNULL(Co_4, 0) > 0, 
//                                      ISNULL(DuNo_3, 0) - ISNULL(DuCo_3, 0) + ISNULL(No_4, 0) - ISNULL(Co_4, 0), 0),
//                        DuCo_4 = IIF(ISNULL(DuNo_3, 0) - ISNULL(DuCo_3, 0) + ISNULL(No_4, 0) - ISNULL(Co_4, 0) < 0, 
//                                      -(ISNULL(DuNo_3, 0) - ISNULL(DuCo_3, 0) + ISNULL(No_4, 0) - ISNULL(Co_4, 0)), 0),
//                        DuNo_5 = IIF(ISNULL(DuNo_4, 0) - ISNULL(DuCo_4, 0) + ISNULL(No_5, 0) - ISNULL(Co_5, 0) > 0, 
//                                      ISNULL(DuNo_4, 0) - ISNULL(DuCo_4, 0) + ISNULL(No_5, 0) - ISNULL(Co_5, 0), 0),
//                        DuCo_5 = IIF(ISNULL(DuNo_4, 0) - ISNULL(DuCo_4, 0) + ISNULL(No_5, 0) - ISNULL(Co_5, 0) < 0, 
//                                      -(ISNULL(DuNo_4, 0) - ISNULL(DuCo_4, 0) + ISNULL(No_5, 0) - ISNULL(Co_5, 0)), 0),
//                        DuNo_6 = IIF(ISNULL(DuNo_5, 0) - ISNULL(DuCo_5, 0) + ISNULL(No_6, 0) - ISNULL(Co_6, 0) > 0, 
//                                      ISNULL(DuNo_5, 0) - ISNULL(DuCo_5, 0) + ISNULL(No_6, 0) - ISNULL(Co_6, 0), 0),
//                        DuCo_6 = IIF(ISNULL(DuNo_5, 0) - ISNULL(DuCo_5, 0) + ISNULL(No_6, 0) - ISNULL(Co_6, 0) < 0, 
//                                      -(ISNULL(DuNo_5, 0) - ISNULL(DuCo_5, 0) + ISNULL(No_6, 0) - ISNULL(Co_6, 0)), 0),
//                        DuNo_7 = IIF(ISNULL(DuNo_6, 0) - ISNULL(DuCo_6, 0) + ISNULL(No_7, 0) - ISNULL(Co_7, 0) > 0, 
//                                      ISNULL(DuNo_6, 0) - ISNULL(DuCo_6, 0) + ISNULL(No_7, 0) - ISNULL(Co_7, 0), 0),
//                        DuCo_7 = IIF(ISNULL(DuNo_6, 0) - ISNULL(DuCo_6, 0) + ISNULL(No_7, 0) - ISNULL(Co_7, 0) < 0, 
//                                      -(ISNULL(DuNo_6, 0) - ISNULL(DuCo_6, 0) + ISNULL(No_7, 0) - ISNULL(Co_7, 0)), 0),
//                        DuNo_8 = IIF(ISNULL(DuNo_7, 0) - ISNULL(DuCo_7, 0) + ISNULL(No_8, 0) - ISNULL(Co_8, 0) > 0, 
//                                      ISNULL(DuNo_7, 0) - ISNULL(DuCo_7, 0) + ISNULL(No_8, 0) - ISNULL(Co_8, 0), 0),
//                        DuCo_8 = IIF(ISNULL(DuNo_7, 0) - ISNULL(DuCo_7, 0) + ISNULL(No_8, 0) - ISNULL(Co_8, 0) < 0, 
//                                      -(ISNULL(DuNo_7, 0) - ISNULL(DuCo_7, 0) + ISNULL(No_8, 0) - ISNULL(Co_8, 0)), 0),
//                        DuNo_9 = IIF(ISNULL(DuNo_8, 0) - ISNULL(DuCo_8, 0) + ISNULL(No_9, 0) - ISNULL(Co_9, 0) > 0, 
//                                      ISNULL(DuNo_8, 0) - ISNULL(DuCo_8, 0) + ISNULL(No_9, 0) - ISNULL(Co_9, 0), 0),
//                        DuCo_9 = IIF(ISNULL(DuNo_8, 0) - ISNULL(DuCo_8, 0) + ISNULL(No_9, 0) - ISNULL(Co_9, 0) < 0, 
//                                      -(ISNULL(DuNo_8, 0) - ISNULL(DuCo_8, 0) + ISNULL(No_9, 0) - ISNULL(Co_9, 0)), 0),
//                        DuNo_10 = IIF(ISNULL(DuNo_9, 0) - ISNULL(DuCo_9, 0) + ISNULL(No_10, 0) - ISNULL(Co_10, 0) > 0, 
//                                       ISNULL(DuNo_9, 0) - ISNULL(DuCo_9, 0) + ISNULL(No_10, 0) - ISNULL(Co_10, 0), 0),
//                        DuCo_10 = IIF(ISNULL(DuNo_9, 0) - ISNULL(DuCo_9, 0) + ISNULL(No_10, 0) - ISNULL(Co_10, 0) < 0, 
//                                       -(ISNULL(DuNo_9, 0) - ISNULL(DuCo_9, 0) + ISNULL(No_10, 0) - ISNULL(Co_10, 0)), 0),
//                        DuNo_11 = IIF(ISNULL(DuNo_10, 0) - ISNULL(DuCo_10, 0) + ISNULL(No_11, 0) - ISNULL(Co_11, 0) > 0, 
//                                       ISNULL(DuNo_10, 0) - ISNULL(DuCo_10, 0) + ISNULL(No_11, 0) - ISNULL(Co_11, 0), 0),
//                        DuCo_11 = IIF(ISNULL(DuNo_10, 0) - ISNULL(DuCo_10, 0) + ISNULL(No_11, 0) - ISNULL(Co_11, 0) < 0, 
//                                       -(ISNULL(DuNo_10, 0) - ISNULL(DuCo_10, 0) + ISNULL(No_11, 0) - ISNULL(Co_11, 0)), 0),
//                        DuNo_12 = IIF(ISNULL(DuNo_11, 0) - ISNULL(DuCo_11, 0) + ISNULL(No_12, 0) - ISNULL(Co_12, 0) > 0, 
//                                       ISNULL(DuNo_11, 0) - ISNULL(DuCo_11, 0) + ISNULL(No_12, 0) - ISNULL(Co_12, 0), 0),
//                        DuCo_12 = IIF(ISNULL(DuNo_11, 0) - ISNULL(DuCo_11, 0) + ISNULL(No_12, 0) - ISNULL(Co_12, 0) < 0, 
//                                       -(ISNULL(DuNo_11, 0) - ISNULL(DuCo_11, 0) + ISNULL(No_12, 0) - ISNULL(Co_12, 0)), 0)
//                    WHERE MaSo = {maSo}";

//                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
//                {
//                    cmd.ExecuteNonQuery();
//                }
//            }

//            Console.WriteLine($"✅ Đã tính dư cho {dtMaSo.Rows.Count} tài khoản cấp con");
//        }

//        // ============================================================
//        // BƯỚC 3: TỔNG HỢP LÊN TK CẤP CHA (ĐỦ 12 THÁNG)
//        // ============================================================
//        private void TongHopLenCapCha(SqlConnection conn, SqlTransaction tran)
//        {
//            // Lấy danh sách TK cấp cha CÓ TK CON
//            string sqlGetCha = @"
//        SELECT DISTINCT TkCha0 AS MaSo
//        FROM HethongTK 
//        WHERE TkCha0 > 0
//        AND TkCha0 IN (SELECT MaSo FROM HethongTK WHERE TKCon = 1)";

//            DataTable dtCha = new DataTable();
//            using (SqlCommand cmd = new SqlCommand(sqlGetCha, conn, tran))
//            {
//                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
//                {
//                    da.Fill(dtCha);
//                }
//            }

//            Console.WriteLine($"📌 Có {dtCha.Rows.Count} TK cấp cha có TK con");

//            foreach (DataRow row in dtCha.Rows)
//            {
//                int maSoCha = Convert.ToInt32(row["MaSo"]);

//                // KIỂM TRA: TK này có TK con không?
//                string sqlCheck = $"SELECT COUNT(*) FROM HethongTK WHERE TkCha0 = {maSoCha}";
//                using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, conn, tran))
//                {
//                    int count = (int)cmdCheck.ExecuteScalar();
//                    if (count == 0) continue;
//                }

//                // Cập nhật dữ liệu tổng hợp cho 12 tháng (CẢ No, Co, DuNo, DuCo)
//                string sql = $@"
//            UPDATE HethongTK 
//            SET 
//                No_1 = (SELECT ISNULL(SUM(No_1), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_1 = (SELECT ISNULL(SUM(Co_1), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_1 = (SELECT ISNULL(SUM(DuNo_1), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_1 = (SELECT ISNULL(SUM(DuCo_1), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_2 = (SELECT ISNULL(SUM(No_2), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_2 = (SELECT ISNULL(SUM(Co_2), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_2 = (SELECT ISNULL(SUM(DuNo_2), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_2 = (SELECT ISNULL(SUM(DuCo_2), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_3 = (SELECT ISNULL(SUM(No_3), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_3 = (SELECT ISNULL(SUM(Co_3), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_3 = (SELECT ISNULL(SUM(DuNo_3), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_3 = (SELECT ISNULL(SUM(DuCo_3), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_4 = (SELECT ISNULL(SUM(No_4), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_4 = (SELECT ISNULL(SUM(Co_4), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_4 = (SELECT ISNULL(SUM(DuNo_4), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_4 = (SELECT ISNULL(SUM(DuCo_4), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_5 = (SELECT ISNULL(SUM(No_5), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_5 = (SELECT ISNULL(SUM(Co_5), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_5 = (SELECT ISNULL(SUM(DuNo_5), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_5 = (SELECT ISNULL(SUM(DuCo_5), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_6 = (SELECT ISNULL(SUM(No_6), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_6 = (SELECT ISNULL(SUM(Co_6), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_6 = (SELECT ISNULL(SUM(DuNo_6), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_6 = (SELECT ISNULL(SUM(DuCo_6), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_7 = (SELECT ISNULL(SUM(No_7), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_7 = (SELECT ISNULL(SUM(Co_7), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_7 = (SELECT ISNULL(SUM(DuNo_7), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_7 = (SELECT ISNULL(SUM(DuCo_7), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_8 = (SELECT ISNULL(SUM(No_8), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_8 = (SELECT ISNULL(SUM(Co_8), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_8 = (SELECT ISNULL(SUM(DuNo_8), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_8 = (SELECT ISNULL(SUM(DuCo_8), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_9 = (SELECT ISNULL(SUM(No_9), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_9 = (SELECT ISNULL(SUM(Co_9), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_9 = (SELECT ISNULL(SUM(DuNo_9), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_9 = (SELECT ISNULL(SUM(DuCo_9), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_10 = (SELECT ISNULL(SUM(No_10), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_10 = (SELECT ISNULL(SUM(Co_10), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_10 = (SELECT ISNULL(SUM(DuNo_10), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_10 = (SELECT ISNULL(SUM(DuCo_10), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_11 = (SELECT ISNULL(SUM(No_11), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_11 = (SELECT ISNULL(SUM(Co_11), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_11 = (SELECT ISNULL(SUM(DuNo_11), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_11 = (SELECT ISNULL(SUM(DuCo_11), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                No_12 = (SELECT ISNULL(SUM(No_12), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                Co_12 = (SELECT ISNULL(SUM(Co_12), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuNo_12 = (SELECT ISNULL(SUM(DuNo_12), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
//                DuCo_12 = (SELECT ISNULL(SUM(DuCo_12), 0) FROM HethongTK WHERE TkCha0 = {maSoCha})
//            WHERE MaSo = {maSoCha}";

//                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
//                {
//                    cmd.ExecuteNonQuery();

//                    if (maSoCha == 79) // Loại 3
//                    {
//                        Console.WriteLine($"   ✅ Đã tổng hợp Loại 3 (MaSo=79)");
//                    }
//                }
//            }

//            Console.WriteLine("✅ Đã tổng hợp lên TK cấp cha");
//        }
//        // ============================================================
//        // BƯỚC 4: CẬP NHẬT SODUKHACHHANG
//        // ============================================================
//        private void CapNhatSoDuKhachHang(SqlConnection conn, SqlTransaction tran)
//        {
//            // Reset No_i, Co_i về 0
//            string sqlReset = "UPDATE SoDuKhachHang SET ";
//            for (int i = 1; i <= 12; i++)
//            {
//                sqlReset += $"No_{i}=0, Co_{i}=0";
//                if (i < 12) sqlReset += ", ";
//            }

//            using (SqlCommand cmd = new SqlCommand(sqlReset, conn, tran))
//            {
//                cmd.ExecuteNonQuery();
//                Console.WriteLine("✅ Đã reset SoDuKhachHang");
//            }

//            // Cập nhật từ chứng từ
//            string sqlNo = @"
//                SELECT MaTKNo, MaKH, ThangCT, SUM(SoPS) AS TPS
//                FROM ChungTu
//                WHERE MaTKNo > 0 AND MaKH > 0 
//                GROUP BY MaTKNo, MaKH, ThangCT";

//            DataTable dtNo = new DataTable();
//            using (SqlCommand cmd = new SqlCommand(sqlNo, conn, tran))
//            {
//                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
//                {
//                    da.Fill(dtNo);
//                }
//            }

//            foreach (DataRow row in dtNo.Rows)
//            {
//                int maTK = Convert.ToInt32(row["MaTKNo"]);
//                int maKH = Convert.ToInt32(row["MaKH"]);
//                int thang = Convert.ToInt32(row["ThangCT"]);
//                double tps = Convert.ToDouble(row["TPS"]);

//                string sqlUpdate = $@"
//                    UPDATE SoDuKhachHang 
//                    SET No_{thang} = ISNULL(No_{thang}, 0) + {tps}
//                    WHERE MaTaiKhoan = {maTK} AND MaKhachHang = {maKH}";

//                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
//                {
//                    int rows = cmd.ExecuteNonQuery();
//                    if (rows == 0)
//                    {
//                        long newId = LngMaxValue("SoDuKhachHang", conn, tran) + 1;
//                                            string sqlInsert = $@"
//                        INSERT INTO SoDuKhachHang (MaTaiKhoan, MaKhachHang)
//                        VALUES ({maTK}, {maKH})";  // ✅ MaSo tự động tăng, không chỉ định
//                        using (SqlCommand cmdInsert = new SqlCommand(sqlInsert, conn, tran))
//                        {
//                            cmdInsert.ExecuteNonQuery();
//                        }
//                        using (SqlCommand cmdRetry = new SqlCommand(sqlUpdate, conn, tran))
//                        {
//                            cmdRetry.ExecuteNonQuery();
//                        }
//                    }
//                }
//            }

//            // Cập nhật từ chứng từ (TK Có)
//            string sqlCo = @"
//                SELECT MaTKCo, MaKHC, ThangCT, SUM(SoPS) AS TPS
//                FROM ChungTu
//                WHERE MaTKCo > 0 AND MaKHC > 0 
//                GROUP BY MaTKCo, MaKHC, ThangCT";

//            DataTable dtCo = new DataTable();
//            using (SqlCommand cmd = new SqlCommand(sqlCo, conn, tran))
//            {
//                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
//                {
//                    da.Fill(dtCo);
//                }
//            }

//            foreach (DataRow row in dtCo.Rows)
//            {
//                int maTK = Convert.ToInt32(row["MaTKCo"]);
//                int maKH = Convert.ToInt32(row["MaKHC"]);
//                int thang = Convert.ToInt32(row["ThangCT"]);
//                double tps = Convert.ToDouble(row["TPS"]);

//                string sqlUpdate = $@"
//                    UPDATE SoDuKhachHang 
//                    SET Co_{thang} = ISNULL(Co_{thang}, 0) + {tps}
//                    WHERE MaTaiKhoan = {maTK} AND MaKhachHang = {maKH}";

//                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
//                {
//                    int rows = cmd.ExecuteNonQuery();
//                    if (rows == 0)
//                    {
//                        long newId = LngMaxValue("SoDuKhachHang", conn, tran) + 1;
//                        string sqlInsert = $@"
//                             INSERT INTO SoDuKhachHang (MaTaiKhoan, MaKhachHang)
//                        VALUES ({maTK}, {maKH})";  // ✅ MaSo tự động tăng, không chỉ định
//                        using (SqlCommand cmdInsert = new SqlCommand(sqlInsert, conn, tran))
//                        {
//                            cmdInsert.ExecuteNonQuery();
//                        }
//                        using (SqlCommand cmdRetry = new SqlCommand(sqlUpdate, conn, tran))
//                        {
//                            cmdRetry.ExecuteNonQuery();
//                        }
//                    }
//                }
//            }

//            // Tính dư cho SoDuKhachHang
//            string sqlDu = "UPDATE SoDuKhachHang SET ";
//            for (int i = 1; i <= 12; i++)
//            {
//                string st = "ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0)";
//                for (int j = 1; j <= i; j++)
//                {
//                    st += $" + ISNULL(No_{j}, 0) - ISNULL(Co_{j}, 0)";
//                }
//                sqlDu += $"DuNo_{i} = IIF({st} > 0, {st}, 0), ";
//                sqlDu += $"DuCo_{i} = IIF({st} < 0, -({st}), 0)";
//                if (i < 12) sqlDu += ", ";
//            }

//            using (SqlCommand cmd = new SqlCommand(sqlDu, conn, tran))
//            {
//                cmd.ExecuteNonQuery();
//                Console.WriteLine("✅ Đã tính dư cho SoDuKhachHang");
//            }
//        }

//        // ============================================================
//        // HÀM LẤY MAX VALUE
//        // ============================================================
//        private long LngMaxValue(string tableName, SqlConnection conn, SqlTransaction tran)
//        {
//            string sql = $"SELECT ISNULL(MAX(MaSo), 0) FROM {tableName}";
//            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
//            {
//                object result = cmd.ExecuteScalar();
//                if (result != null && result != DBNull.Value)
//                {
//                    return Convert.ToInt64(result);
//                }
//                return 0;
//            }
//        }

//        // ============================================================
//        // HÀM TẠO SQL SUM CHO 12 THÁNG
//        // ============================================================
//        private string GetSumSql(string prefix)
//        {
//            string result = "";
//            for (int i = 1; i <= 12; i++)
//            {
//                result += $", SUM(IIF(ThangCT = {i}, SoPS, 0)) AS {prefix}_{i}";
//            }
//            return result.TrimStart(',');
//        }

//        // ============================================================
//        // KIỂM TRA KẾT QUẢ
//        // ============================================================
//        private void CheckKetQua(SqlConnection conn, SqlTransaction tran)
//        {
//            Console.WriteLine("\n📊 KẾT QUẢ SAU KHI TÍNH:");
//            Console.WriteLine("========================================");

//            string sql = @"
//                SELECT SoHieu, No_7, Co_7, DuNo_7, DuCo_7
//                FROM HethongTK 
//                WHERE SoHieu IN ('1', '79', '131', '133', '1331', '156', '331', '333', '3331', '511', '632')";

//            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
//            {
//                using (SqlDataReader reader = cmd.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        string soHieu = reader["SoHieu"].ToString();
//                        double no = Convert.ToDouble(reader["No_7"]);
//                        double co = Convert.ToDouble(reader["Co_7"]);
//                        double duNo = Convert.ToDouble(reader["DuNo_7"]);
//                        double duCo = Convert.ToDouble(reader["DuCo_7"]);

//                        Console.WriteLine($"   TK {soHieu}: No_7={no:N0}, Co_7={co:N0}, DuNo_7={duNo:N0}, DuCo_7={duCo:N0}");
//                    }
//                    reader.Close();
//                }
//            }

//            Console.WriteLine("\n📌 TỔNG KẾT:");
//            string sqlTong = @"
//                SELECT 
//                    ISNULL(SUM(DuNo_7), 0) AS TongDuNo,
//                    ISNULL(SUM(DuCo_7), 0) AS TongDuCo
//                FROM HethongTK 
//                WHERE Cap = 0";

//            using (SqlCommand cmd = new SqlCommand(sqlTong, conn, tran))
//            {
//                using (SqlDataReader reader = cmd.ExecuteReader())
//                {
//                    if (reader.Read())
//                    {
//                        double tongNo = Convert.ToDouble(reader["TongDuNo"]);
//                        double tongCo = Convert.ToDouble(reader["TongDuCo"]);
//                        Console.WriteLine($"Tổng Dư Nợ: {tongNo:N0}");
//                        Console.WriteLine($"Tổng Dư Có: {tongCo:N0}");
//                        Console.WriteLine($"{(tongNo == tongCo ? "✅ CÂN ĐỐI" : "❌ MẤT CÂN ĐỐI")}");
//                    }
//                    reader.Close();
//                }
//            }
//        }
//    }
//}


using DevExpress.XtraEditors;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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

        public void TinhHTTK(int tuThang = 0)
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;
            CapNhatHeThongTK(tuThang);
        }

        private void AutoSumHTTK_Load(object sender, EventArgs e)
        {
            TinhHTTK(0); // Mặc định tính cả năm
        }

        // ============================================================
        // RESET LOẠI 3
        // ============================================================
        private void ResetLoai3(SqlConnection conn, SqlTransaction tran, int tuThang)
        {
            string sql = @"
                UPDATE HethongTK 
                SET ";

            int startMonth = tuThang > 0 ? tuThang : 1;
            for (int i = startMonth; i <= 12; i++)
            {
                sql += $"No_{i} = 0, Co_{i} = 0, DuNo_{i} = 0, DuCo_{i} = 0";
                if (i < 12) sql += ", ";
            }

            sql += " WHERE SoHieu = 'Loại 3'";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.ExecuteNonQuery();
                Console.WriteLine($"✅ Đã reset Loại 3 từ tháng {startMonth} đến 12");
            }
        }

        // ============================================================
        // CẬP NHẬT HETHONGTK
        // ============================================================
        public void CapNhatHeThongTK(int tuThang = 0)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Reset Loại 3
                        ResetLoai3(conn, tran, tuThang);
                        Console.WriteLine($"🚀 BẮT ĐẦU CẬP NHẬT HETHONGTK từ tháng {(tuThang > 0 ? tuThang : 1)}...");

                        // BƯỚC 1: CẬP NHẬT SỐ PHÁT SINH
                        Console.WriteLine("📊 Cập nhật số phát sinh...");
                        CapNhatSoPhatSinh(conn, tran, tuThang);

                        // BƯỚC 2: TÍNH DƯ CHO CÁC TK CẤP CON
                        Console.WriteLine("📊 Tính dư...");
                        TinhDu(conn, tran, tuThang);

                        // BƯỚC 3: TỔNG HỢP LÊN TK CẤP CHA
                        Console.WriteLine("📊 Tổng hợp lên TK cấp cha...");
                        TongHopLenCapCha(conn, tran, tuThang);

                        // BƯỚC 4: CẬP NHẬT SODUKHACHHANG
                        Console.WriteLine("📊 Cập nhật SoDuKhachHang...");
                        CapNhatSoDuKhachHang(conn, tran, tuThang);

                        // BƯỚC 5: KIỂM TRA
                        CheckKetQua(conn, tran);

                        tran.Commit();
                        XtraMessageBox.Show("✅ Cập nhật HeThongTK thành công!", "Thông báo");
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        XtraMessageBox.Show($"❌ Lỗi: {ex.Message}", "Lỗi");
                        throw;
                    }
                }
            }
        }

        // ============================================================
        // BƯỚC 1: CẬP NHẬT SỐ PHÁT SINH
        // ============================================================
        private void CapNhatSoPhatSinh(SqlConnection conn, SqlTransaction tran, int tuThang)
        {
            int startMonth = tuThang > 0 ? tuThang : 1;

            // Lấy danh sách TK cấp con
            DataTable dtTkCon = new DataTable();
            string sqlGetTkCon = "SELECT MaSo FROM HethongTK WHERE TKCon = 0";
            using (SqlCommand cmd = new SqlCommand(sqlGetTkCon, conn, tran))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                da.Fill(dtTkCon);
            }

            var listTkCon = new System.Collections.Generic.List<int>();
            foreach (DataRow row in dtTkCon.Rows)
            {
                listTkCon.Add(Convert.ToInt32(row["MaSo"]));
            }

            if (listTkCon.Count == 0) return;

            // ===== CẬP NHẬT NỢ =====
            string sqlNo = @"
                SELECT 
                    MaTKNo AS MaSo,
                    " + GetSumSqlFromMonth("No", startMonth) + @"
                FROM ChungTu
                WHERE MaTKNo > 0 AND ThangCT >= @TuThang
                GROUP BY MaTKNo";

            DataTable dtNo = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlNo, conn, tran))
            {
                cmd.Parameters.AddWithValue("@TuThang", startMonth);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtNo);
                }
            }

            foreach (DataRow row in dtNo.Rows)
            {
                int maSo = Convert.ToInt32(row["MaSo"]);
                if (!listTkCon.Contains(maSo)) continue;

                string sqlUpdate = "UPDATE HethongTK SET ";
                for (int i = startMonth; i <= 12; i++)
                {
                    string colName = $"No_{i}";
                    double value = 0;
                    if (row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value)
                    {
                        value = Convert.ToDouble(row[colName]);
                    }
                    sqlUpdate += $"No_{i} = {value}";
                    if (i < 12) sqlUpdate += ", ";
                }
                sqlUpdate += $" WHERE MaSo = {maSo}";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            // ===== CẬP NHẬT CÓ =====
            string sqlCo = @"
                SELECT 
                    MaTKCo AS MaSo,
                    " + GetSumSqlFromMonth("Co", startMonth) + @"
                FROM ChungTu
                WHERE MaTKCo > 0 AND ThangCT >= @TuThang
                GROUP BY MaTKCo";

            DataTable dtCo = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlCo, conn, tran))
            {
                cmd.Parameters.AddWithValue("@TuThang", startMonth);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtCo);
                }
            }

            foreach (DataRow row in dtCo.Rows)
            {
                int maSo = Convert.ToInt32(row["MaSo"]);
                if (!listTkCon.Contains(maSo)) continue;

                string sqlUpdate = "UPDATE HethongTK SET ";
                for (int i = startMonth; i <= 12; i++)
                {
                    string colName = $"Co_{i}";
                    double value = 0;
                    if (row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value)
                    {
                        value = Convert.ToDouble(row[colName]);
                    }
                    sqlUpdate += $"Co_{i} = {value}";
                    if (i < 12) sqlUpdate += ", ";
                }
                sqlUpdate += $" WHERE MaSo = {maSo}";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"✅ Đã cập nhật số phát sinh từ tháng {startMonth} đến 12");
        }

        // ============================================================
        // BƯỚC 2: TÍNH DƯ
        // ============================================================
        private void TinhDu(SqlConnection conn, SqlTransaction tran, int tuThang)
        {
            int startMonth = tuThang > 0 ? tuThang : 1;

            // Lấy danh sách TK cấp con có phát sinh
            string sqlGetMaSo = @"
                SELECT DISTINCT MaSo 
                FROM HethongTK 
                WHERE TKCon = 0
                AND MaSo IN (
                    SELECT MaTKNo FROM ChungTu WHERE SoPS <> 0
                    UNION
                    SELECT MaTKCo FROM ChungTu WHERE SoPS <> 0
                )";

            DataTable dtMaSo = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlGetMaSo, conn, tran))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                da.Fill(dtMaSo);
            }

            Console.WriteLine($"📌 Tính dư cho {dtMaSo.Rows.Count} tài khoản cấp con");

            foreach (DataRow row in dtMaSo.Rows)
            {
                int maSo = Convert.ToInt32(row["MaSo"]);

                // Lấy dư đầu kỳ của tháng startMonth
                int prevMonth = startMonth - 1;
                string sql = $@"
                    DECLARE @DuNoPrev FLOAT = ISNULL((SELECT DuNo_{prevMonth} FROM HethongTK WHERE MaSo = {maSo}), 0);
                    DECLARE @DuCoPrev FLOAT = ISNULL((SELECT DuCo_{prevMonth} FROM HethongTK WHERE MaSo = {maSo}), 0);
                    DECLARE @ChenhLech FLOAT;";

                for (int i = startMonth; i <= 12; i++)
                {
                    sql += $@"
                        SET @ChenhLech = ISNULL((SELECT SUM(SoPS) FROM ChungTu WHERE MaTKNo = {maSo} AND ThangCT = {i}), 0) 
                                      - ISNULL((SELECT SUM(SoPS) FROM ChungTu WHERE MaTKCo = {maSo} AND ThangCT = {i}), 0);
                        SET @DuNoPrev = @DuNoPrev + @ChenhLech;
                        
                        UPDATE HethongTK SET 
                            DuNo_{i} = IIF(@DuNoPrev > 0, @DuNoPrev, 0),
                            DuCo_{i} = IIF(@DuNoPrev < 0, -@DuNoPrev, 0)
                        WHERE MaSo = {maSo};";
                }

                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"✅ Đã tính dư từ tháng {startMonth} đến 12");
        }

        // ============================================================
        // BƯỚC 3: TỔNG HỢP LÊN TK CẤP CHA
        // ============================================================
        private void TongHopLenCapCha(SqlConnection conn, SqlTransaction tran, int tuThang)
        {
            int startMonth = tuThang > 0 ? tuThang : 1;

            // Lấy danh sách TK cấp cha
            string sqlGetCha = @"
                SELECT DISTINCT TkCha0 AS MaSo
                FROM HethongTK 
                WHERE TkCha0 > 0
                AND TkCha0 IN (SELECT MaSo FROM HethongTK WHERE TKCon = 1)";

            DataTable dtCha = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlGetCha, conn, tran))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                da.Fill(dtCha);
            }

            Console.WriteLine($"📌 Có {dtCha.Rows.Count} TK cấp cha có TK con");

            foreach (DataRow row in dtCha.Rows)
            {
                int maSoCha = Convert.ToInt32(row["MaSo"]);

                // Kiểm tra TK này có TK con không
                string sqlCheck = $"SELECT COUNT(*) FROM HethongTK WHERE TkCha0 = {maSoCha}";
                using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, conn, tran))
                {
                    int count = (int)cmdCheck.ExecuteScalar();
                    if (count == 0) continue;
                }

                string sql = $"UPDATE HethongTK SET ";
                for (int i = startMonth; i <= 12; i++)
                {
                    sql += $@"
                        No_{i} = (SELECT ISNULL(SUM(No_{i}), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                        Co_{i} = (SELECT ISNULL(SUM(Co_{i}), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                        DuNo_{i} = (SELECT ISNULL(SUM(DuNo_{i}), 0) FROM HethongTK WHERE TkCha0 = {maSoCha}),
                        DuCo_{i} = (SELECT ISNULL(SUM(DuCo_{i}), 0) FROM HethongTK WHERE TkCha0 = {maSoCha})";
                    if (i < 12) sql += ", ";
                }
                sql += $" WHERE MaSo = {maSoCha}";

                using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"✅ Đã tổng hợp lên TK cấp cha từ tháng {startMonth} đến 12");
        }

        // ============================================================
        // BƯỚC 4: CẬP NHẬT SODUKHACHHANG
        // ============================================================
        private void CapNhatSoDuKhachHang(SqlConnection conn, SqlTransaction tran, int tuThang)
        {
            int startMonth = tuThang > 0 ? tuThang : 1;

            // Reset No_i, Co_i từ tháng startMonth
            string sqlReset = "UPDATE SoDuKhachHang SET ";
            for (int i = startMonth; i <= 12; i++)
            {
                sqlReset += $"No_{i}=0, Co_{i}=0";
                if (i < 12) sqlReset += ", ";
            }

            using (SqlCommand cmd = new SqlCommand(sqlReset, conn, tran))
            {
                cmd.ExecuteNonQuery();
                Console.WriteLine($"✅ Đã reset SoDuKhachHang từ tháng {startMonth}");
            }

            // Cập nhật Nợ từ tháng startMonth
            string sqlNo = @"
                SELECT MaTKNo, MaKH, ThangCT, SUM(SoPS) AS TPS
                FROM ChungTu
                WHERE MaTKNo > 0 AND MaKH > 0 AND ThangCT >= @TuThang
                GROUP BY MaTKNo, MaKH, ThangCT";

            DataTable dtNo = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlNo, conn, tran))
            {
                cmd.Parameters.AddWithValue("@TuThang", startMonth);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtNo);
                }
            }

            foreach (DataRow row in dtNo.Rows)
            {
                int maTK = Convert.ToInt32(row["MaTKNo"]);
                int maKH = Convert.ToInt32(row["MaKH"]);
                int thang = Convert.ToInt32(row["ThangCT"]);
                double tps = Convert.ToDouble(row["TPS"]);

                string sqlUpdate = $@"
                    UPDATE SoDuKhachHang 
                    SET No_{thang} = ISNULL(No_{thang}, 0) + {tps}
                    WHERE MaTaiKhoan = {maTK} AND MaKhachHang = {maKH}";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        long newId = LngMaxValue("SoDuKhachHang", conn, tran) + 1;
                        string sqlInsert = $@"
                            INSERT INTO SoDuKhachHang (MaTaiKhoan, MaKhachHang)
                            VALUES ({maTK}, {maKH})";
                        using (SqlCommand cmdInsert = new SqlCommand(sqlInsert, conn, tran))
                        {
                            cmdInsert.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdRetry = new SqlCommand(sqlUpdate, conn, tran))
                        {
                            cmdRetry.ExecuteNonQuery();
                        }
                    }
                }
            }

            // Cập nhật Có từ tháng startMonth
            string sqlCo = @"
                SELECT MaTKCo, MaKHC, ThangCT, SUM(SoPS) AS TPS
                FROM ChungTu
                WHERE MaTKCo > 0 AND MaKHC > 0 AND ThangCT >= @TuThang
                GROUP BY MaTKCo, MaKHC, ThangCT";

            DataTable dtCo = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sqlCo, conn, tran))
            {
                cmd.Parameters.AddWithValue("@TuThang", startMonth);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtCo);
                }
            }

            foreach (DataRow row in dtCo.Rows)
            {
                int maTK = Convert.ToInt32(row["MaTKCo"]);
                int maKH = Convert.ToInt32(row["MaKHC"]);
                int thang = Convert.ToInt32(row["ThangCT"]);
                double tps = Convert.ToDouble(row["TPS"]);

                string sqlUpdate = $@"
                    UPDATE SoDuKhachHang 
                    SET Co_{thang} = ISNULL(Co_{thang}, 0) + {tps}
                    WHERE MaTaiKhoan = {maTK} AND MaKhachHang = {maKH}";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, conn, tran))
                {
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        long newId = LngMaxValue("SoDuKhachHang", conn, tran) + 1;
                        string sqlInsert = $@"
                            INSERT INTO SoDuKhachHang (MaTaiKhoan, MaKhachHang)
                            VALUES ({maTK}, {maKH})";
                        using (SqlCommand cmdInsert = new SqlCommand(sqlInsert, conn, tran))
                        {
                            cmdInsert.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdRetry = new SqlCommand(sqlUpdate, conn, tran))
                        {
                            cmdRetry.ExecuteNonQuery();
                        }
                    }
                }
            }

            // Tính dư cho SoDuKhachHang từ tháng startMonth
            string sqlDu = "UPDATE SoDuKhachHang SET ";
            for (int i = startMonth; i <= 12; i++)
            {
                string st = "ISNULL(DuNo_0, 0) - ISNULL(DuCo_0, 0)";
                for (int j = 1; j <= i; j++)
                {
                    st += $" + ISNULL(No_{j}, 0) - ISNULL(Co_{j}, 0)";
                }
                sqlDu += $"DuNo_{i} = IIF({st} > 0, {st}, 0), ";
                sqlDu += $"DuCo_{i} = IIF({st} < 0, -({st}), 0)";
                if (i < 12) sqlDu += ", ";
            }

            using (SqlCommand cmd = new SqlCommand(sqlDu, conn, tran))
            {
                cmd.ExecuteNonQuery();
                Console.WriteLine("✅ Đã tính dư cho SoDuKhachHang");
            }
        }

        // ============================================================
        // HÀM TẠO SQL SUM TỪ THÁNG CHỈ ĐỊNH
        // ============================================================
        private string GetSumSqlFromMonth(string prefix, int tuThang)
        {
            string result = "";
            for (int i = tuThang; i <= 12; i++)
            {
                result += $", SUM(IIF(ThangCT = {i}, SoPS, 0)) AS {prefix}_{i}";
            }
            return result.TrimStart(',');
        }

        // ============================================================
        // HÀM LẤY MAX VALUE
        // ============================================================
        private long LngMaxValue(string tableName, SqlConnection conn, SqlTransaction tran)
        {
            string sql = $"SELECT ISNULL(MAX(MaSo), 0) FROM {tableName}";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt64(result);
                }
                return 0;
            }
        }

        // ============================================================
        // KIỂM TRA KẾT QUẢ
        // ============================================================
        private void CheckKetQua(SqlConnection conn, SqlTransaction tran)
        {
            Console.WriteLine("\n📊 KẾT QUẢ SAU KHI TÍNH:");
            Console.WriteLine("========================================");

            string sql = @"
                SELECT SoHieu, No_7, Co_7, DuNo_7, DuCo_7
                FROM HethongTK 
                WHERE SoHieu IN ('1', '79', '131', '133', '1331', '156', '331', '333', '3331', '511', '632')";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string soHieu = reader["SoHieu"].ToString();
                    double no = Convert.ToDouble(reader["No_7"]);
                    double co = Convert.ToDouble(reader["Co_7"]);
                    double duNo = Convert.ToDouble(reader["DuNo_7"]);
                    double duCo = Convert.ToDouble(reader["DuCo_7"]);

                    Console.WriteLine($"   TK {soHieu}: No_7={no:N0}, Co_7={co:N0}, DuNo_7={duNo:N0}, DuCo_7={duCo:N0}");
                }
            }

            Console.WriteLine("\n📌 TỔNG KẾT:");
            string sqlTong = @"
                SELECT 
                    ISNULL(SUM(DuNo_7), 0) AS TongDuNo,
                    ISNULL(SUM(DuCo_7), 0) AS TongDuCo
                FROM HethongTK 
                WHERE Cap = 0";

            using (SqlCommand cmd = new SqlCommand(sqlTong, conn, tran))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    double tongNo = Convert.ToDouble(reader["TongDuNo"]);
                    double tongCo = Convert.ToDouble(reader["TongDuCo"]);
                    Console.WriteLine($"Tổng Dư Nợ: {tongNo:N0}");
                    Console.WriteLine($"Tổng Dư Có: {tongCo:N0}");
                    Console.WriteLine($"{(tongNo == tongCo ? "✅ CÂN ĐỐI" : "❌ MẤT CÂN ĐỐI")}");
                }
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            // Tính cả năm (tuThang = 0)
            TinhHTTK(0);
            this.Close();
        }
    }
}