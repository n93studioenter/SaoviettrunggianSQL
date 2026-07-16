//using DevExpress.XtraEditors;
//using SaovietTax.Models;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Windows.Forms;
//using Tensorflow;

//namespace SaovietTax
//{
//    public partial class vb6Tinhgiavon : DevExpress.XtraEditors.XtraForm
//    {
//        private string connectionString = ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;

//        public vb6Tinhgiavon()
//        {
//            InitializeComponent();
//        }

//        #region Helper Methods for Safe Data Conversion

//        private int SafeGetInt(DataRow row, string columnName)
//        {
//            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
//            {
//                try { return Convert.ToInt32(row[columnName]); }
//                catch { return 0; }
//            }
//            return 0;
//        }

//        private double SafeGetDouble(DataRow row, string columnName)
//        {
//            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
//            {
//                try { return Convert.ToDouble(row[columnName]); }
//                catch { return 0; }
//            }
//            return 0;
//        }

//        private string SafeGetString(DataRow row, string columnName)
//        {
//            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
//            {
//                return row[columnName].ToString();
//            }
//            return string.Empty;
//        }

//        private DateTime? SafeGetDateTime(DataRow row, string columnName)
//        {
//            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
//            {
//                try { return Convert.ToDateTime(row[columnName]); }
//                catch { return null; }
//            }
//            return null;
//        }

//        private short SafeGetShort(DataRow row, string columnName)
//        {
//            if (!row.Table.Columns.Contains(columnName)) return 0;
//            object value = row[columnName];
//            if (value == null || value == DBNull.Value) return 0;
//            try { return Convert.ToInt16(value); }
//            catch { return 0; }
//        }

//        #endregion

//        private void vb6Tinhgiavon_Load(object sender, EventArgs e)
//        {
//            for (int i = 1; i <= 12; i++)
//            {
//                comboBoxEdit1.Properties.Items.Add(i);
//                comboBoxEdit2.Properties.Items.Add(i);
//            }
//            comboBoxEdit1.SelectedIndex = DateTime.Now.Month - 1;
//            comboBoxEdit2.SelectedIndex = DateTime.Now.Month - 1;
//        }

//        public void TinhGiaVon(int tuThang, int denThang,List<frmMain.FileImport> loai)
//        {
//            try
//            {
//                using (SqlConnection conn = new SqlConnection(connectionString))
//                {
//                    conn.Open();
//                    SqlTransaction transaction = conn.BeginTransaction();

//                    try
//                    {


//                        progressPanel1.Caption = "Đang lấy dữ liệu...";
//                        Application.DoEvents();

//                        // 1. Lấy dữ liệu cần xử lý
//                        DataTable dtDauVao = GetDataTable(conn, transaction,
//                            "SELECT * FROM ChungTu WHERE MaLoai = 1 AND MaVattu > 0");

//                        DataTable dtDauRa = GetDataTable(conn, transaction,
//                            "SELECT * FROM ChungTu WHERE MaLoai = 8");
//                        DataTable dttonkho= GetDataTable(conn, transaction,
//                            "SELECT * FROM TonKho");
//                        if (loai != null)
//                        {
//                            DataTable dtFiltered = dtDauRa.Clone();
//                            foreach (DataRow row in dtDauRa.Rows)
//                            {
//                                string soHieu = row["SoHieu"].ToString();
//                                DateTime dtHieu = DateTime.Parse(row["NgayCT"].ToString());
//                                // Kiểm tra trong list
//                                foreach (var item in loai.Where(m => m.Checked))
//                                {
//                                    if (item.SHDon == soHieu && item.NLap.Date == dtHieu.Date) // Điều kiện NLap > 0
//                                    {
//                                        dtFiltered.ImportRow(row);
//                                    }
//                                }
//                            }
//                            dtDauRa = dtFiltered;
//                        }


//                        DataTable dtHoaDon = GetDataTable(conn, transaction,
//                            "SELECT * FROM HoaDon");

//                        // 2. Lấy danh sách vật tư
//                        var groupVatTu = dtDauRa.AsEnumerable()
//                            .Where(m => {
//                                int thangCT = SafeGetInt(m, "ThangCT");
//                                int maVattu = SafeGetInt(m, "MaVattu");
//                                return thangCT >= tuThang && thangCT <= denThang && maVattu > 0;
//                            })
//                            .Select(m => SafeGetInt(m, "MaVattu"))
//                            .Distinct()
//                            .ToList();

//                        // 3. Backup hóa đơn
//                        var lstBakhd = dtHoaDon.AsEnumerable()
//                            .Where(m => dtDauRa.AsEnumerable()
//                                .Any(d => {
//                                    int maSoD = SafeGetInt(d, "MaSo");
//                                    int maSoM = SafeGetInt(m, "MaSo");
//                                    int thangCT = SafeGetInt(d, "ThangCT");
//                                    return maSoD == maSoM && thangCT >= tuThang && thangCT <= denThang;
//                                }))
//                            .ToList();

//                        progressPanel1.Caption = $"Đang tính tồn kho cho {groupVatTu.Count} vật tư...";
//                        Application.DoEvents();

//                        // 4. Tính toán tồn kho và giá vốn
//                        int count = 0;
//                        foreach (int maVatTu in groupVatTu)
//                        {
//                            count++;
//                            progressPanel1.Caption = $"Đang tính vật tư {count}/{groupVatTu.Count} - Mã: {maVatTu}";
//                            Application.DoEvents();

//                            DataRow tonKhoRow = GetTonKhoRow(conn, transaction, maVatTu);
//                            if (tonKhoRow == null) continue;

//                            for (int i = 1; i <= 12; i++)
//                            {
//                                try
//                                {

//                                    double luongThangTruoc = GetLuongThang(tonKhoRow, i - 1);
//                                    double tienThangTruoc = GetTienThang(tonKhoRow, i - 1);

//                                    double slNhap = dtDauVao.AsEnumerable()
//                                        .Where(m => SafeGetInt(m, "MaVattu") == maVatTu && SafeGetInt(m, "ThangCT") == i)
//                                        .Sum(m => SafeGetDouble(m, "SoPS2No"));

//                                    double tienNhap = dtDauVao.AsEnumerable()
//                                        .Where(m => SafeGetInt(m, "MaVattu") == maVatTu && SafeGetInt(m, "ThangCT") == i)
//                                        .Sum(m => SafeGetDouble(m, "SoPS"));

//                                    SetPropertyValue(tonKhoRow, $"Luong_Nhap_{i}", slNhap);
//                                    SetPropertyValue(tonKhoRow, $"Tien_Nhap_{i}", tienNhap);

//                                    double tongslNhap = slNhap + luongThangTruoc;
//                                    double tongtienNhap = tienNhap + tienThangTruoc;
//                                    double tinhgiavon = tongslNhap != 0 ? tongtienNhap / tongslNhap : 0;

//                                    double slXuat = dtDauRa.AsEnumerable()
//                                        .Where(m => SafeGetInt(m, "MaVattu") == maVatTu && SafeGetInt(m, "ThangCT") == i)
//                                        .Sum(m => SafeGetDouble(m, "SoPS2Co"));

//                                    double tienXuat = Math.Round(slXuat * tinhgiavon);

//                                    SetPropertyValue(tonKhoRow, $"Luong_Xuat_{i}", slXuat);
//                                    SetPropertyValue(tonKhoRow, $"Tien_Xuat_{i}", tienXuat);

//                                    double luongton = tongslNhap - slXuat;
//                                    double tienton = Math.Round(luongton * tinhgiavon);

//                                    SetPropertyValue(tonKhoRow, $"Luong_{i}", luongton);
//                                    SetPropertyValue(tonKhoRow, $"Tien_{i}", tienton);
//                                }
//                                catch (Exception ex)
//                                {
//                                    Console.WriteLine($"Lỗi tháng {i} - Mã vật tư {maVatTu}: {ex.Message}");
//                                }
//                            }

//                            UpdateTonKho(conn, transaction, tonKhoRow);
//                        }

//                        progressPanel1.Caption = "Đang xử lý hóa đơn...";
//                        Application.DoEvents();

//                        // 5. Group dữ liệu
//                        var groupChungtu = dtDauRa.AsEnumerable()
//                            .Where(m => SafeGetInt(m, "ThangCT") >= tuThang && SafeGetInt(m, "ThangCT") <= denThang)
//                            .GroupBy(m => new {
//                                SoHieu = SafeGetString(m, "SoHieu"),
//                                ThangCT = SafeGetInt(m, "ThangCT")
//                            })
//                            .ToList();

//                        // 6. Lấy Max MaCT TRƯỚC KHI XÓA
//                        int maxMaCT = GetMaxCT(conn, transaction);
//                        Console.WriteLine($"Max MaCT trước khi xóa: {maxMaCT}");

//                        // 7. Xóa dữ liệu cũ
//                        string deleteChungTu8 = "DELETE FROM ChungTu WHERE ThangCT >= @TuThang AND ThangCT <= @DenThang AND MaLoai = 8";
//                        using (SqlCommand cmd = new SqlCommand(deleteChungTu8, conn, transaction))
//                        {
//                            cmd.Parameters.AddWithValue("@TuThang", tuThang);
//                            cmd.Parameters.AddWithValue("@DenThang", denThang);
//                            cmd.ExecuteNonQuery();
//                        }

//                        string deleteChungTu2 = "DELETE FROM ChungTu WHERE ThangCT >= @TuThang AND ThangCT <= @DenThang AND MaLoai = 2";
//                        using (SqlCommand cmd = new SqlCommand(deleteChungTu2, conn, transaction))
//                        {
//                            cmd.Parameters.AddWithValue("@TuThang", tuThang);
//                            cmd.Parameters.AddWithValue("@DenThang", denThang);
//                            cmd.ExecuteNonQuery();
//                        }

//                        var maSoCanXoa = lstBakhd
//                            .Select(m => SafeGetInt(m, "MaSo"))
//                            .Where(m => m > 0)
//                            .Distinct()
//                            .ToList();

//                        if (maSoCanXoa.Count > 0)
//                        {
//                            string maSoString = string.Join(",", maSoCanXoa);
//                            string deleteHoaDon = $"DELETE FROM HoaDon WHERE MaSo IN ({maSoString})";
//                            using (SqlCommand cmd = new SqlCommand(deleteHoaDon, conn, transaction))
//                            {
//                                cmd.ExecuteNonQuery();
//                            }
//                        }

//                        // ====== 8. TẠO LẠI DỮ LIỆU ======
//                        // 8a. Tạo DataTable để gom giá vốn (Bulk Insert)
//                        DataTable dtGiaVon = dtDauRa.Clone();
//                        var insertedMaSo = new HashSet<int>();
//                        int stt = 1;
//                        int newMaSo = 0;
//                        int oldMaSo = 0;

//                        foreach (var group in groupChungtu)
//                        {
//                            var getlistHang = dtDauRa.AsEnumerable()
//                                .Where(m => SafeGetString(m, "SoHieu") == group.Key.SoHieu &&
//                                            SafeGetInt(m, "ThangCT") == group.Key.ThangCT &&
//                                            SafeGetInt(m, "MaTKCo") != 14038)
//                                .ToList();

//                            var getlistThue = dtDauRa.AsEnumerable()
//                                .Where(m => SafeGetString(m, "SoHieu") == group.Key.SoHieu &&
//                                            SafeGetInt(m, "ThangCT") == group.Key.ThangCT &&
//                                            SafeGetInt(m, "MaTKCo") == 14038)
//                                .ToList();

//                            // ===== BƯỚC 1: Insert từng cặp hàng hóa + giá vốn (1 dòng hàng → 1 dòng giá vốn) =====
//                            bool istang = false;
//                            foreach (DataRow hh in getlistHang)
//                            {
//                                int oldMaCT = SafeGetInt(hh, "MaCT");
//                                int maSoHang = SafeGetInt(hh, "MaSo");

//                                // 1a. Insert hàng hóa - lấy MaSo mới
//                                int newMaSoHang = InsertChungTuReturnMaSo(conn, transaction, hh, oldMaCT);
//                                Console.WriteLine($"Insert hàng: MaCT={oldMaCT}, MaSo mới={newMaSoHang}");

//                                // 1b. Tính giá vốn cho dòng hàng này
//                                DataRow tonKhoRow = GetTonKhoRow(conn, transaction, SafeGetInt(hh, "MaVattu"));
//                                if (tonKhoRow != null)
//                                {
//                                    int thang = SafeGetInt(hh, "ThangCT");

//                                    double tienTon = GetTienThang(tonKhoRow, thang - 1);
//                                    double luongTon = GetLuongThang(tonKhoRow, thang - 1);
//                                    double tienNhap = GetTienNhap(tonKhoRow, thang);
//                                    double luongNhap = GetLuongNhap(tonKhoRow, thang);

//                                    double tongTien = tienTon + tienNhap;
//                                    double tongLuong = luongTon + luongNhap;
//                                    double giaVon = tongLuong != 0 ? tongTien / tongLuong : 0;

//                                    double slXuat = SafeGetDouble(hh, "SoPS2Co");
//                                    double tienGiaVon = Math.Round(slXuat * giaVon);

//                                    // 1c. Insert giá vốn - MaCT MỚI = maxMaCT + 1
//                                    // CT_ID = 500000000 + MaSo của hàng hóa (newMaSoHang)
//                                    if (istang == false)
//                                    {
//                                        maxMaCT++;
//                                        istang = true;
//                                    }

//                                    InsertGiaVon(conn, transaction, hh, maxMaCT, tienGiaVon, newMaSoHang);
//                                    Console.WriteLine($"Insert giá vốn: MaCT={maxMaCT}, CT_ID=500000000+{newMaSoHang}={500000000 + newMaSoHang}");
//                                }
//                            }

//                            // ===== BƯỚC 2: Insert thuế =====
//                            foreach (DataRow item in getlistThue)
//                            {
//                                oldMaSo = SafeGetInt(item, "MaSo");

//                                int oldMaCT = SafeGetInt(item, "MaCT");
//                                newMaSo = InsertChungTuReturnMaSo(conn, transaction, item, oldMaCT);
//                                Console.WriteLine($"Insert thuế: MaCT={oldMaCT}, MaSo mới={newMaSo}");
//                                // ===== BƯỚC 3: Insert hóa đơn =====
//                                if (newMaSo > 0 && !insertedMaSo.Contains(newMaSo))
//                                {
//                                    var hd = lstBakhd.FirstOrDefault(m => SafeGetInt(m, "MaSo") == oldMaSo);

//                                    if (hd != null)
//                                    {
//                                        InsertHoaDon(conn, transaction, hd, newMaSo);
//                                        insertedMaSo.Add(newMaSo);
//                                        Console.WriteLine($"Insert hóa đơn: MaSo={newMaSo}");
//                                    }
//                                }
//                            }


//                            progressPanel1.Caption = $"Đang xử lý hóa đơn {group.Key.SoHieu} - {stt}/{groupChungtu.Count}";
//                            Application.DoEvents();
//                            stt++;
//                        }
//                        // 8e. Bulk Insert giá vốn (1 lần duy nhất)
//                        if (dtGiaVon.Rows.Count > 0)
//                        {
//                            InsertBulk(conn, transaction, dtGiaVon, "ChungTu");
//                            Console.WriteLine($"Bulk Insert {dtGiaVon.Rows.Count} dòng giá vốn");
//                        }

//                        transaction.Commit();
//                        XtraMessageBox.Show("Đã tính xong giá vốn!");
//                    }
//                    catch (Exception ex)
//                    {
//                        transaction.Rollback();
//                        XtraMessageBox.Show($"Lỗi: {ex.Message}\n{ex.StackTrace}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                XtraMessageBox.Show($"Lỗi kết nối: {ex.Message}");
//            }
//        }
//        private void simpleButton1_Click(object sender, EventArgs e)
//        {
//            int tuThang = Convert.ToInt32(comboBoxEdit1.EditValue);
//            int denThang = Convert.ToInt32(comboBoxEdit2.EditValue);
//            TinhGiaVon(tuThang,denThang,null);
//        }
//        private void InsertGiaVon(SqlConnection conn, SqlTransaction trans, DataRow hh, int maCT, double tienGiaVon, int maSoHang)
//        {
//            try
//            {
//                string query = @"
//            INSERT INTO ChungTu (
//                MaCT, MaLoai, ThangCT, SoHieu, NgayCT, NgayGS, NgayTL,
//                DienGiai, MaNguon, MaKho, MaTKNo, MaTKCo, SoPS, 
//                SoPS2No, SoPS2Co, MaTKTCNo, MaTKTCCo, MaVattu, GhiChu, 
//                CT_ID, SoXuat, MaDT, MaKH, CTGS, MaKHC, MaTP, 
//                DVT, User_ID, DienGiaiE, TyGia, MaNV, HanTT, 
//                SH1, T1, TLCK, CK, MaDT1, MaDT2, MaDT3, XuLy, 
//                MauSoHD, LoaiHoaDon, phantramchietkhau, sotienchietkhau, NgayImport
//            ) VALUES (
//                @MaCT, 2, @ThangCT, @SoHieu, @NgayCT, @NgayGS, @NgayTL,
//                @DienGiai, @MaNguon, @MaKho, 151, 39, @SoPS, 
//                @SoPS2No, @SoPS2Co, 151, 39, @MaVattu, @GhiChu, 
//                @CT_ID, @SoXuat, @MaDT, @MaKH, @CTGS, @MaKHC, @MaTP, 
//                @DVT, @User_ID, @DienGiaiE, @TyGia, @MaNV, @HanTT, 
//                @SH1, @T1, @TLCK, @CK, @MaDT1, @MaDT2, @MaDT3, @XuLy, 
//                @MauSoHD, @LoaiHoaDon, @phantramchietkhau, @sotienchietkhau, @NgayImport
//            )";

//                using (SqlCommand cmd = new SqlCommand(query, conn, trans))
//                {
//                    cmd.Parameters.AddWithValue("@MaCT", maCT);
//                    cmd.Parameters.AddWithValue("@ThangCT", SafeGetInt(hh, "ThangCT"));
//                    cmd.Parameters.AddWithValue("@SoHieu", $"{SafeGetString(hh, "SoHieu")}GV");
//                    cmd.Parameters.AddWithValue("@NgayCT", SafeGetDateTime(hh, "NgayCT") ?? DateTime.Now);
//                    cmd.Parameters.AddWithValue("@NgayGS", SafeGetDateTime(hh, "NgayGS") ?? DateTime.Now);
//                    cmd.Parameters.AddWithValue("@NgayTL", SafeGetDateTime(hh, "NgayTL") as object ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@DienGiai", SafeGetString(hh, "DienGiai"));
//                    cmd.Parameters.AddWithValue("@MaNguon", SafeGetInt(hh, "MaNguon"));
//                    cmd.Parameters.AddWithValue("@MaKho", SafeGetInt(hh, "MaKho"));
//                    cmd.Parameters.AddWithValue("@SoPS", tienGiaVon);
//                    cmd.Parameters.AddWithValue("@SoPS2No", 0);
//                    cmd.Parameters.AddWithValue("@SoPS2Co", SafeGetDouble(hh, "SoPS2Co"));
//                    cmd.Parameters.AddWithValue("@MaVattu", SafeGetInt(hh, "MaVattu"));
//                    cmd.Parameters.AddWithValue("@GhiChu", SafeGetString(hh, "GhiChu"));
//                    cmd.Parameters.AddWithValue("@CT_ID", 500000000 + maSoHang);
//                    cmd.Parameters.AddWithValue("@SoXuat", SafeGetDouble(hh, "SoXuat"));
//                    cmd.Parameters.AddWithValue("@MaDT", SafeGetInt(hh, "MaDT"));
//                    cmd.Parameters.AddWithValue("@MaKH", SafeGetInt(hh, "MaKH"));
//                    cmd.Parameters.AddWithValue("@CTGS", SafeGetInt(hh, "CTGS"));
//                    cmd.Parameters.AddWithValue("@MaKHC", SafeGetInt(hh, "MaKHC"));
//                    cmd.Parameters.AddWithValue("@MaTP", SafeGetInt(hh, "MaTP"));
//                    cmd.Parameters.AddWithValue("@DVT", SafeGetShort(hh, "DVT"));
//                    cmd.Parameters.AddWithValue("@User_ID", SafeGetInt(hh, "User_ID"));
//                    cmd.Parameters.AddWithValue("@DienGiaiE", SafeGetString(hh, "DienGiaiE"));
//                    cmd.Parameters.AddWithValue("@TyGia", SafeGetDouble(hh, "TyGia"));
//                    cmd.Parameters.AddWithValue("@MaNV", SafeGetInt(hh, "MaNV"));
//                    cmd.Parameters.AddWithValue("@HanTT", SafeGetInt(hh, "HanTT"));
//                    cmd.Parameters.AddWithValue("@SH1", SafeGetString(hh, "SH1"));
//                    cmd.Parameters.AddWithValue("@T1", SafeGetInt(hh, "T1"));
//                    cmd.Parameters.AddWithValue("@TLCK", SafeGetDouble(hh, "TLCK"));
//                    cmd.Parameters.AddWithValue("@CK", SafeGetDouble(hh, "CK"));
//                    cmd.Parameters.AddWithValue("@MaDT1", SafeGetInt(hh, "MaDT1"));
//                    cmd.Parameters.AddWithValue("@MaDT2", SafeGetInt(hh, "MaDT2"));
//                    cmd.Parameters.AddWithValue("@MaDT3", SafeGetInt(hh, "MaDT3"));
//                    cmd.Parameters.AddWithValue("@XuLy", SafeGetInt(hh, "XuLy"));
//                    cmd.Parameters.AddWithValue("@MauSoHD", SafeGetString(hh, "MauSoHD"));
//                    cmd.Parameters.AddWithValue("@LoaiHoaDon", SafeGetInt(hh, "LoaiHoaDon"));
//                    cmd.Parameters.AddWithValue("@phantramchietkhau", SafeGetString(hh, "phantramchietkhau"));
//                    cmd.Parameters.AddWithValue("@sotienchietkhau", SafeGetString(hh, "sotienchietkhau"));
//                    cmd.Parameters.AddWithValue("@NgayImport", SafeGetDateTime(hh, "NgayImport") as object ?? DBNull.Value);

//                    cmd.ExecuteNonQuery();
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error inserting GiaVon: {ex.Message}");
//                throw;
//            }
//        }
//        #region Database Helper Methods

//        private DataTable GetDataTable(SqlConnection conn, SqlTransaction trans, string query)
//        {
//            using (SqlCommand cmd = new SqlCommand(query, conn, trans))
//            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
//            {
//                DataTable dt = new DataTable();
//                da.Fill(dt);
//                return dt;
//            }
//        }

//        private DataRow GetTonKhoRow(SqlConnection conn, SqlTransaction trans, int maVatTu)
//        {
//            string query = $"SELECT * FROM TonKho WHERE MaVatTu = {maVatTu}";
//            DataTable dt = GetDataTable(conn, trans, query);
//            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
//        }

//        private void UpdateTonKho(SqlConnection conn, SqlTransaction trans, DataRow row)
//        {
//            if (row == null) return;

//            try
//            {
//                int maVatTu = Convert.ToInt32(row["MaVatTu"]);

//                string query = @"
//                    UPDATE TonKho SET 
//                        Tien_Nhap_1 = @Tien_Nhap_1, Luong_Nhap_1 = @Luong_Nhap_1, 
//                        Tien_Xuat_1 = @Tien_Xuat_1, Luong_Xuat_1 = @Luong_Xuat_1, 
//                        Luong_1 = @Luong_1, Tien_1 = @Tien_1,
//                        Tien_Nhap_2 = @Tien_Nhap_2, Luong_Nhap_2 = @Luong_Nhap_2, 
//                        Tien_Xuat_2 = @Tien_Xuat_2, Luong_Xuat_2 = @Luong_Xuat_2, 
//                        Luong_2 = @Luong_2, Tien_2 = @Tien_2,
//                        Tien_Nhap_3 = @Tien_Nhap_3, Luong_Nhap_3 = @Luong_Nhap_3, 
//                        Tien_Xuat_3 = @Tien_Xuat_3, Luong_Xuat_3 = @Luong_Xuat_3, 
//                        Luong_3 = @Luong_3, Tien_3 = @Tien_3,
//                        Tien_Nhap_4 = @Tien_Nhap_4, Luong_Nhap_4 = @Luong_Nhap_4, 
//                        Tien_Xuat_4 = @Tien_Xuat_4, Luong_Xuat_4 = @Luong_Xuat_4, 
//                        Luong_4 = @Luong_4, Tien_4 = @Tien_4,
//                        Tien_Nhap_5 = @Tien_Nhap_5, Luong_Nhap_5 = @Luong_Nhap_5, 
//                        Tien_Xuat_5 = @Tien_Xuat_5, Luong_Xuat_5 = @Luong_Xuat_5, 
//                        Luong_5 = @Luong_5, Tien_5 = @Tien_5,
//                        Tien_Nhap_6 = @Tien_Nhap_6, Luong_Nhap_6 = @Luong_Nhap_6, 
//                        Tien_Xuat_6 = @Tien_Xuat_6, Luong_Xuat_6 = @Luong_Xuat_6, 
//                        Luong_6 = @Luong_6, Tien_6 = @Tien_6,
//                        Tien_Nhap_7 = @Tien_Nhap_7, Luong_Nhap_7 = @Luong_Nhap_7, 
//                        Tien_Xuat_7 = @Tien_Xuat_7, Luong_Xuat_7 = @Luong_Xuat_7, 
//                        Luong_7 = @Luong_7, Tien_7 = @Tien_7,
//                        Tien_Nhap_8 = @Tien_Nhap_8, Luong_Nhap_8 = @Luong_Nhap_8, 
//                        Tien_Xuat_8 = @Tien_Xuat_8, Luong_Xuat_8 = @Luong_Xuat_8, 
//                        Luong_8 = @Luong_8, Tien_8 = @Tien_8,
//                        Tien_Nhap_9 = @Tien_Nhap_9, Luong_Nhap_9 = @Luong_Nhap_9, 
//                        Tien_Xuat_9 = @Tien_Xuat_9, Luong_Xuat_9 = @Luong_Xuat_9, 
//                        Luong_9 = @Luong_9, Tien_9 = @Tien_9,
//                        Tien_Nhap_10 = @Tien_Nhap_10, Luong_Nhap_10 = @Luong_Nhap_10, 
//                        Tien_Xuat_10 = @Tien_Xuat_10, Luong_Xuat_10 = @Luong_Xuat_10, 
//                        Luong_10 = @Luong_10, Tien_10 = @Tien_10,
//                        Tien_Nhap_11 = @Tien_Nhap_11, Luong_Nhap_11 = @Luong_Nhap_11, 
//                        Tien_Xuat_11 = @Tien_Xuat_11, Luong_Xuat_11 = @Luong_Xuat_11, 
//                        Luong_11 = @Luong_11, Tien_11 = @Tien_11,
//                        Tien_Nhap_12 = @Tien_Nhap_12, Luong_Nhap_12 = @Luong_Nhap_12, 
//                        Tien_Xuat_12 = @Tien_Xuat_12, Luong_Xuat_12 = @Luong_Xuat_12, 
//                        Luong_12 = @Luong_12, Tien_12 = @Tien_12
//                    WHERE MaVatTu = @MaVatTu";

//                using (SqlCommand cmd = new SqlCommand(query, conn, trans))
//                {
//                    cmd.Parameters.AddWithValue("@MaVatTu", maVatTu);

//                    for (int i = 1; i <= 12; i++)
//                    {
//                        cmd.Parameters.AddWithValue($"@Tien_Nhap_{i}", GetPropertyValue(row, $"Tien_Nhap_{i}"));
//                        cmd.Parameters.AddWithValue($"@Luong_Nhap_{i}", GetPropertyValue(row, $"Luong_Nhap_{i}"));
//                        cmd.Parameters.AddWithValue($"@Tien_Xuat_{i}", GetPropertyValue(row, $"Tien_Xuat_{i}"));
//                        cmd.Parameters.AddWithValue($"@Luong_Xuat_{i}", GetPropertyValue(row, $"Luong_Xuat_{i}"));
//                        cmd.Parameters.AddWithValue($"@Luong_{i}", GetPropertyValue(row, $"Luong_{i}"));
//                        cmd.Parameters.AddWithValue($"@Tien_{i}", GetPropertyValue(row, $"Tien_{i}"));
//                    }

//                    cmd.ExecuteNonQuery();
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error in UpdateTonKho: {ex.Message}");
//                throw;
//            }
//        }

//        private int InsertChungTuReturnMaSo(SqlConnection conn, SqlTransaction trans, DataRow row, int maCT)
//        {
//            try
//            {
//                string query = @"
//                    INSERT INTO ChungTu (
//                        MaCT, MaLoai, ThangCT, SoHieu, NgayCT, NgayGS, NgayTL,
//                        DienGiai, MaNguon, MaKho, MaTKNo, MaTKCo, SoPS, 
//                        SoPS2No, SoPS2Co, MaTKTCNo, MaTKTCCo, MaVattu, GhiChu, 
//                        CT_ID, SoXuat, MaDT, MaKH, CTGS, MaKHC, MaTP, 
//                        DVT, User_ID, DienGiaiE, TyGia, MaNV, HanTT, 
//                        SH1, T1, TLCK, CK, MaDT1, MaDT2, MaDT3, XuLy, 
//                        MauSoHD, LoaiHoaDon, phantramchietkhau, sotienchietkhau, NgayImport
//                    ) 
//                    OUTPUT INSERTED.MaSo
//                    VALUES (
//                        @MaCT, @MaLoai, @ThangCT, @SoHieu, @NgayCT, @NgayGS, @NgayTL,
//                        @DienGiai, @MaNguon, @MaKho, @MaTKNo, @MaTKCo, @SoPS, 
//                        @SoPS2No, @SoPS2Co, @MaTKTCNo, @MaTKTCCo, @MaVattu, @GhiChu, 
//                        @CT_ID, @SoXuat, @MaDT, @MaKH, @CTGS, @MaKHC, @MaTP, 
//                        @DVT, @User_ID, @DienGiaiE, @TyGia, @MaNV, @HanTT, 
//                        @SH1, @T1, @TLCK, @CK, @MaDT1, @MaDT2, @MaDT3, @XuLy, 
//                        @MauSoHD, @LoaiHoaDon, @phantramchietkhau, @sotienchietkhau, @NgayImport
//                    )";

//                using (SqlCommand cmd = new SqlCommand(query, conn, trans))
//                {
//                    cmd.Parameters.AddWithValue("@MaCT", maCT);
//                    cmd.Parameters.AddWithValue("@MaLoai", SafeGetShort(row, "MaLoai"));
//                    cmd.Parameters.AddWithValue("@ThangCT", SafeGetInt(row, "ThangCT"));
//                    cmd.Parameters.AddWithValue("@SoHieu", SafeGetString(row, "SoHieu"));
//                    cmd.Parameters.AddWithValue("@NgayCT", SafeGetDateTime(row, "NgayCT") ?? DateTime.Now);
//                    cmd.Parameters.AddWithValue("@NgayGS", SafeGetDateTime(row, "NgayGS") ?? DateTime.Now);
//                    cmd.Parameters.AddWithValue("@NgayTL", SafeGetDateTime(row, "NgayTL") as object ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@DienGiai", SafeGetString(row, "DienGiai"));
//                    cmd.Parameters.AddWithValue("@MaNguon", SafeGetInt(row, "MaNguon"));
//                    cmd.Parameters.AddWithValue("@MaKho", SafeGetInt(row, "MaKho"));
//                    cmd.Parameters.AddWithValue("@MaTKNo", SafeGetInt(row, "MaTKNo"));
//                    cmd.Parameters.AddWithValue("@MaTKCo", SafeGetInt(row, "MaTKCo"));
//                    cmd.Parameters.AddWithValue("@SoPS", SafeGetDouble(row, "SoPS"));
//                    cmd.Parameters.AddWithValue("@SoPS2No", SafeGetDouble(row, "SoPS2No"));
//                    cmd.Parameters.AddWithValue("@SoPS2Co", SafeGetDouble(row, "SoPS2Co"));
//                    cmd.Parameters.AddWithValue("@MaTKTCNo", SafeGetInt(row, "MaTKTCNo"));
//                    cmd.Parameters.AddWithValue("@MaTKTCCo", SafeGetInt(row, "MaTKTCCo"));
//                    cmd.Parameters.AddWithValue("@MaVattu", SafeGetInt(row, "MaVattu"));
//                    cmd.Parameters.AddWithValue("@GhiChu", SafeGetString(row, "GhiChu"));
//                    cmd.Parameters.AddWithValue("@CT_ID", SafeGetDouble(row, "CT_ID"));
//                    cmd.Parameters.AddWithValue("@SoXuat", SafeGetDouble(row, "SoXuat"));
//                    cmd.Parameters.AddWithValue("@MaDT", SafeGetInt(row, "MaDT"));
//                    cmd.Parameters.AddWithValue("@MaKH", SafeGetInt(row, "MaKH"));
//                    cmd.Parameters.AddWithValue("@CTGS", SafeGetInt(row, "CTGS"));
//                    cmd.Parameters.AddWithValue("@MaKHC", SafeGetInt(row, "MaKHC"));
//                    cmd.Parameters.AddWithValue("@MaTP", SafeGetInt(row, "MaTP"));
//                    cmd.Parameters.AddWithValue("@DVT", SafeGetShort(row, "DVT"));
//                    cmd.Parameters.AddWithValue("@User_ID", SafeGetInt(row, "User_ID"));
//                    cmd.Parameters.AddWithValue("@DienGiaiE", SafeGetString(row, "DienGiaiE"));
//                    cmd.Parameters.AddWithValue("@TyGia", SafeGetDouble(row, "TyGia"));
//                    cmd.Parameters.AddWithValue("@MaNV", SafeGetInt(row, "MaNV"));
//                    cmd.Parameters.AddWithValue("@HanTT", SafeGetInt(row, "HanTT"));
//                    cmd.Parameters.AddWithValue("@SH1", SafeGetString(row, "SH1"));
//                    cmd.Parameters.AddWithValue("@T1", SafeGetInt(row, "T1"));
//                    cmd.Parameters.AddWithValue("@TLCK", SafeGetDouble(row, "TLCK"));
//                    cmd.Parameters.AddWithValue("@CK", SafeGetDouble(row, "CK"));
//                    cmd.Parameters.AddWithValue("@MaDT1", SafeGetInt(row, "MaDT1"));
//                    cmd.Parameters.AddWithValue("@MaDT2", SafeGetInt(row, "MaDT2"));
//                    cmd.Parameters.AddWithValue("@MaDT3", SafeGetInt(row, "MaDT3"));
//                    cmd.Parameters.AddWithValue("@XuLy", SafeGetInt(row, "XuLy"));
//                    cmd.Parameters.AddWithValue("@MauSoHD", SafeGetString(row, "MauSoHD"));
//                    cmd.Parameters.AddWithValue("@LoaiHoaDon", SafeGetInt(row, "LoaiHoaDon"));
//                    cmd.Parameters.AddWithValue("@phantramchietkhau", SafeGetString(row, "phantramchietkhau"));
//                    cmd.Parameters.AddWithValue("@sotienchietkhau", SafeGetString(row, "sotienchietkhau"));
//                    cmd.Parameters.AddWithValue("@NgayImport", SafeGetDateTime(row, "NgayImport") as object ?? DBNull.Value);

//                    return Convert.ToInt32(cmd.ExecuteScalar());
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error inserting ChungTu Return MaSo: {ex.Message}");
//                throw;
//            }
//        }

//        private void InsertBulk(SqlConnection conn, SqlTransaction trans, DataTable dt, string tableName)
//        {
//            if (dt == null || dt.Rows.Count == 0) return;

//            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, trans))
//            {
//                bulkCopy.DestinationTableName = tableName;
//                bulkCopy.BatchSize = 1000;
//                bulkCopy.BulkCopyTimeout = 600;

//                foreach (DataColumn col in dt.Columns)
//                {
//                    bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
//                }

//                bulkCopy.WriteToServer(dt);
//                Console.WriteLine($"Bulk Insert {dt.Rows.Count} rows into {tableName}");
//            }
//        }

//        private void InsertHoaDon(SqlConnection conn, SqlTransaction trans, DataRow hoaDonRow, int maSo)
//        {
//            try
//            {
//                string query = @"
//                    INSERT INTO HoaDon (
//                        MaSo, Loai, MaKhachHang, KyHieu, SoHD, NgayPH, MatHang, 
//                        SoLuong, ThanhTien, TyLe, HD, KCT, GiaTT, HTTT, 
//                        MauSo, HDBL, NK, TS, DC, TyGia, pathInvoice, 
//                        Ghichuhd, IdNhap, StatusPH, IdTemplate, TendoHDid, 
//                        TendoHDState, has_e_invoice
//                    ) VALUES (
//                        @MaSo, @Loai, @MaKhachHang, @KyHieu, @SoHD, @NgayPH, @MatHang, 
//                        @SoLuong, @ThanhTien, @TyLe, @HD, @KCT, @GiaTT, @HTTT, 
//                        @MauSo, @HDBL, @NK, @TS, @DC, @TyGia, @pathInvoice, 
//                        @Ghichuhd, @IdNhap, @StatusPH, @IdTemplate, @TendoHDid, 
//                        @TendoHDState, @has_e_invoice
//                    )";

//                using (SqlCommand cmd = new SqlCommand(query, conn, trans))
//                {
//                    cmd.Parameters.AddWithValue("@MaSo", maSo);
//                    cmd.Parameters.AddWithValue("@Loai", SafeGetShort(hoaDonRow, "Loai"));
//                    cmd.Parameters.AddWithValue("@MaKhachHang", SafeGetInt(hoaDonRow, "MaKhachHang"));
//                    cmd.Parameters.AddWithValue("@KyHieu", SafeGetString(hoaDonRow, "KyHieu"));
//                    cmd.Parameters.AddWithValue("@SoHD", SafeGetString(hoaDonRow, "SoHD"));
//                    cmd.Parameters.AddWithValue("@NgayPH", SafeGetDateTime(hoaDonRow, "NgayPH") ?? DateTime.Now);
//                    cmd.Parameters.AddWithValue("@MatHang", SafeGetString(hoaDonRow, "MatHang"));
//                    cmd.Parameters.AddWithValue("@SoLuong", SafeGetDouble(hoaDonRow, "SoLuong"));
//                    cmd.Parameters.AddWithValue("@ThanhTien", SafeGetDouble(hoaDonRow, "ThanhTien"));
//                    cmd.Parameters.AddWithValue("@TyLe", SafeGetShort(hoaDonRow, "TyLe"));
//                    cmd.Parameters.AddWithValue("@HD", SafeGetShort(hoaDonRow, "HD"));
//                    cmd.Parameters.AddWithValue("@KCT", SafeGetShort(hoaDonRow, "KCT"));
//                    cmd.Parameters.AddWithValue("@GiaTT", SafeGetDouble(hoaDonRow, "GiaTT"));
//                    cmd.Parameters.AddWithValue("@HTTT", SafeGetString(hoaDonRow, "HTTT"));
//                    cmd.Parameters.AddWithValue("@MauSo", SafeGetString(hoaDonRow, "MauSo"));
//                    cmd.Parameters.AddWithValue("@HDBL", SafeGetShort(hoaDonRow, "HDBL"));
//                    cmd.Parameters.AddWithValue("@NK", SafeGetShort(hoaDonRow, "NK"));
//                    cmd.Parameters.AddWithValue("@TS", SafeGetShort(hoaDonRow, "TS"));
//                    cmd.Parameters.AddWithValue("@DC", SafeGetShort(hoaDonRow, "DC"));
//                    cmd.Parameters.AddWithValue("@TyGia", SafeGetDouble(hoaDonRow, "TyGia"));
//                    cmd.Parameters.AddWithValue("@pathInvoice", SafeGetString(hoaDonRow, "pathInvoice"));
//                    cmd.Parameters.AddWithValue("@Ghichuhd", SafeGetString(hoaDonRow, "Ghichuhd"));
//                    cmd.Parameters.AddWithValue("@IdNhap", SafeGetString(hoaDonRow, "IdNhap"));
//                    cmd.Parameters.AddWithValue("@StatusPH", SafeGetString(hoaDonRow, "StatusPH"));
//                    cmd.Parameters.AddWithValue("@IdTemplate", SafeGetString(hoaDonRow, "IdTemplate"));
//                    cmd.Parameters.AddWithValue("@TendoHDid", SafeGetString(hoaDonRow, "TendoHDid"));
//                    cmd.Parameters.AddWithValue("@TendoHDState", SafeGetString(hoaDonRow, "TendoHDState"));
//                    cmd.Parameters.AddWithValue("@has_e_invoice", SafeGetDouble(hoaDonRow, "has_e_invoice"));

//                    cmd.ExecuteNonQuery();
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error inserting HoaDon: {ex.Message}");
//                throw;
//            }
//        }

//        private int GetMaxCT(SqlConnection conn, SqlTransaction trans)
//        {
//            string query = "SELECT ISNULL(MAX(MaCT), 0) FROM ChungTu ";
//            using (SqlCommand cmd = new SqlCommand(query, conn, trans))
//            {
//                object result = cmd.ExecuteScalar();
//                if (result == DBNull.Value || result == null)
//                    return 0;
//                return Convert.ToInt32(result);
//            }
//        }

//        private void ResetTonKho(SqlConnection conn, SqlTransaction trans)
//        {
//            string query = @"
//                UPDATE TonKho SET 
//                    Tien_Nhap_1 = 0, Luong_Nhap_1 = 0, Tien_Xuat_1 = 0, Luong_Xuat_1 = 0, Luong_1 = 0, Tien_1 = 0,
//                    Tien_Nhap_2 = 0, Luong_Nhap_2 = 0, Tien_Xuat_2 = 0, Luong_Xuat_2 = 0, Luong_2 = 0, Tien_2 = 0,
//                    Tien_Nhap_3 = 0, Luong_Nhap_3 = 0, Tien_Xuat_3 = 0, Luong_Xuat_3 = 0, Luong_3 = 0, Tien_3 = 0,
//                    Tien_Nhap_4 = 0, Luong_Nhap_4 = 0, Tien_Xuat_4 = 0, Luong_Xuat_4 = 0, Luong_4 = 0, Tien_4 = 0,
//                    Tien_Nhap_5 = 0, Luong_Nhap_5 = 0, Tien_Xuat_5 = 0, Luong_Xuat_5 = 0, Luong_5 = 0, Tien_5 = 0,
//                    Tien_Nhap_6 = 0, Luong_Nhap_6 = 0, Tien_Xuat_6 = 0, Luong_Xuat_6 = 0, Luong_6 = 0, Tien_6 = 0,
//                    Tien_Nhap_7 = 0, Luong_Nhap_7 = 0, Tien_Xuat_7 = 0, Luong_Xuat_7 = 0, Luong_7 = 0, Tien_7 = 0,
//                    Tien_Nhap_8 = 0, Luong_Nhap_8 = 0, Tien_Xuat_8 = 0, Luong_Xuat_8 = 0, Luong_8 = 0, Tien_8 = 0,
//                    Tien_Nhap_9 = 0, Luong_Nhap_9 = 0, Tien_Xuat_9 = 0, Luong_Xuat_9 = 0, Luong_9 = 0, Tien_9 = 0,
//                    Tien_Nhap_10 = 0, Luong_Nhap_10 = 0, Tien_Xuat_10 = 0, Luong_Xuat_10 = 0, Luong_10 = 0, Tien_10 = 0,
//                    Tien_Nhap_11 = 0, Luong_Nhap_11 = 0, Tien_Xuat_11 = 0, Luong_Xuat_11 = 0, Luong_11 = 0, Tien_11 = 0,
//                    Tien_Nhap_12 = 0, Luong_Nhap_12 = 0, Tien_Xuat_12 = 0, Luong_Xuat_12 = 0, Luong_12 = 0, Tien_12 = 0";

//            using (SqlCommand cmd = new SqlCommand(query, conn, trans))
//            {
//                cmd.ExecuteNonQuery();
//            }
//        }

//        #endregion

//        #region Property Helper Methods

//        private double GetPropertyValue(DataRow row, string propertyName)
//        {
//            if (row.Table.Columns.Contains(propertyName) && row[propertyName] != DBNull.Value)
//            {
//                try { return Convert.ToDouble(row[propertyName]); }
//                catch { return 0; }
//            }
//            return 0;
//        }

//        private void SetPropertyValue(DataRow row, string propertyName, double value)
//        {
//            if (row.Table.Columns.Contains(propertyName))
//            {
//                row[propertyName] = value;
//            }
//        }

//        private double GetLuongThang(DataRow row, int thang) => GetPropertyValue(row, $"Luong_{thang}");
//        private double GetTienThang(DataRow row, int thang) => GetPropertyValue(row, $"Tien_{thang}");
//        private double GetLuongNhap(DataRow row, int thang) => GetPropertyValue(row, $"Luong_Nhap_{thang}");
//        private double GetTienNhap(DataRow row, int thang) => GetPropertyValue(row, $"Tien_Nhap_{thang}");

//        #endregion

//        private void comboBoxEdit2_SelectedIndexChanged(object sender, EventArgs e) { }

//        private void comboBoxEdit1_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            comboBoxEdit2.EditValue = comboBoxEdit1.EditValue;
//        }
//    }
//} 
using DevExpress.XtraEditors;
using SaovietTax.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace SaovietTax
{
    public partial class vb6Tinhgiavon : DevExpress.XtraEditors.XtraForm
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;

        public vb6Tinhgiavon()
        {
            InitializeComponent();
        }

        #region Helper Methods for Safe Data Conversion

        private int SafeGetInt(DataRow row, string columnName)
        {
            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
            {
                try { return Convert.ToInt32(row[columnName]); }
                catch { return 0; }
            }
            return 0;
        }

        private double SafeGetDouble(DataRow row, string columnName)
        {
            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
            {
                try { return Convert.ToDouble(row[columnName]); }
                catch { return 0; }
            }
            return 0;
        }

        private string SafeGetString(DataRow row, string columnName)
        {
            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
            {
                return row[columnName].ToString();
            }
            return string.Empty;
        }

        private DateTime? SafeGetDateTime(DataRow row, string columnName)
        {
            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
            {
                try { return Convert.ToDateTime(row[columnName]); }
                catch { return null; }
            }
            return null;
        }

        private short SafeGetShort(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName)) return 0;
            object value = row[columnName];
            if (value == null || value == DBNull.Value) return 0;
            try { return Convert.ToInt16(value); }
            catch { return 0; }
        }

        #endregion

        private void vb6Tinhgiavon_Load(object sender, EventArgs e)
        {
            for (int i = 1; i <= 12; i++)
            {
                comboBoxEdit1.Properties.Items.Add(i);
                comboBoxEdit2.Properties.Items.Add(i);
            }
            comboBoxEdit1.SelectedIndex = DateTime.Now.Month - 1;
            comboBoxEdit2.SelectedIndex = DateTime.Now.Month - 1;
        }

        public void TinhGiaVon(int tuThang, int denThang, List<frmMain.FileImport> loai)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        progressPanel1.Caption = "Đang lấy dữ liệu...";
                        Application.DoEvents();

                        // 1. Lấy dữ liệu cần xử lý
                        DataTable dtDauVao = GetDataTable(conn, transaction,
                            "SELECT * FROM ChungTu WHERE MaLoai = 1 AND MaVattu > 0");

                        DataTable dtDauRa = GetDataTable(conn, transaction,
                            "SELECT * FROM ChungTu WHERE MaLoai = 8");

                        if (loai != null)
                        {
                            DataTable dtFiltered = dtDauRa.Clone();
                            foreach (DataRow row in dtDauRa.Rows)
                            {
                                string soHieu = row["SoHieu"].ToString();
                                DateTime dtHieu = DateTime.Parse(row["NgayCT"].ToString());
                                foreach (var item in loai.Where(m => m.Checked))
                                {
                                    if (item.SHDon == soHieu && item.NLap.Date == dtHieu.Date)
                                    {
                                        dtFiltered.ImportRow(row);
                                    }
                                }
                            }
                            dtDauRa = dtFiltered;
                        }

                        DataTable dtHoaDon = GetDataTable(conn, transaction,
                            "SELECT * FROM HoaDon");

                        // 2. Lấy danh sách vật tư
                        var groupVatTu = dtDauRa.AsEnumerable()
                            .Where(m => {
                                int thangCT = SafeGetInt(m, "ThangCT");
                                int maVattu = SafeGetInt(m, "MaVattu");
                                return thangCT >= tuThang && thangCT <= denThang && maVattu > 0;
                            })
                            .Select(m => SafeGetInt(m, "MaVattu"))
                            .Distinct()
                            .ToList();

                        // 3. Backup hóa đơn
                        var lstBakhd = dtHoaDon.AsEnumerable()
                            .Where(m => dtDauRa.AsEnumerable()
                                .Any(d => {
                                    int maSoD = SafeGetInt(d, "MaSo");
                                    int maSoM = SafeGetInt(m, "MaSo");
                                    int thangCT = SafeGetInt(d, "ThangCT");
                                    return maSoD == maSoM && thangCT >= tuThang && thangCT <= denThang;
                                }))
                            .ToList();

                        Console.WriteLine($"=== BACKUP HÓA ĐƠN ===");
                        Console.WriteLine($"Số hóa đơn backup: {lstBakhd.Count}");
                        foreach (DataRow hd in lstBakhd)
                        {
                            Console.WriteLine($"  MaSo={SafeGetInt(hd, "MaSo")}, SoHD={SafeGetString(hd, "SoHD")}");
                        }

                        progressPanel1.Caption = $"Đang tính tồn kho cho {groupVatTu.Count} vật tư...";
                        Application.DoEvents();

                        // 4. Tính toán tồn kho và giá vốn
                        int countVatTu = 0;
                        foreach (int maVatTu in groupVatTu)
                        {
                            countVatTu++;
                            progressPanel1.Caption = $"Đang tính vật tư {countVatTu}/{groupVatTu.Count} - Mã: {maVatTu}";
                            Application.DoEvents();

                            DataRow tonKhoRow = GetTonKhoRow(conn, transaction, maVatTu);
                            if (tonKhoRow == null) continue;

                            for (int i = 1; i <= 12; i++)
                            {
                                try
                                {
                                    double luongThangTruoc = GetLuongThang(tonKhoRow, i - 1);
                                    double tienThangTruoc = GetTienThang(tonKhoRow, i - 1);

                                    double slNhap = dtDauVao.AsEnumerable()
                                        .Where(m => SafeGetInt(m, "MaVattu") == maVatTu && SafeGetInt(m, "ThangCT") == i)
                                        .Sum(m => SafeGetDouble(m, "SoPS2No"));

                                    double tienNhap = dtDauVao.AsEnumerable()
                                        .Where(m => SafeGetInt(m, "MaVattu") == maVatTu && SafeGetInt(m, "ThangCT") == i)
                                        .Sum(m => SafeGetDouble(m, "SoPS"));

                                    SetPropertyValue(tonKhoRow, $"Luong_Nhap_{i}", slNhap);
                                    SetPropertyValue(tonKhoRow, $"Tien_Nhap_{i}", tienNhap);

                                    double tongslNhap = slNhap + luongThangTruoc;
                                    double tongtienNhap = tienNhap + tienThangTruoc;
                                    double tinhgiavon = tongslNhap != 0 ? tongtienNhap / tongslNhap : 0;

                                    double slXuat = dtDauRa.AsEnumerable()
                                        .Where(m => SafeGetInt(m, "MaVattu") == maVatTu && SafeGetInt(m, "ThangCT") == i)
                                        .Sum(m => SafeGetDouble(m, "SoPS2Co"));

                                    double tienXuat = Math.Round(slXuat * tinhgiavon);

                                    SetPropertyValue(tonKhoRow, $"Luong_Xuat_{i}", slXuat);
                                    SetPropertyValue(tonKhoRow, $"Tien_Xuat_{i}", tienXuat);

                                    double luongton = tongslNhap - slXuat;
                                    double tienton = Math.Round(luongton * tinhgiavon);

                                    SetPropertyValue(tonKhoRow, $"Luong_{i}", luongton);
                                    SetPropertyValue(tonKhoRow, $"Tien_{i}", tienton);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Lỗi tháng {i} - Mã vật tư {maVatTu}: {ex.Message}");
                                }
                            }

                            UpdateTonKho(conn, transaction, tonKhoRow);
                        }

                        progressPanel1.Caption = "Đang xử lý hóa đơn...";
                        Application.DoEvents();

                        // 5. Group dữ liệu
                        var groupChungtu = dtDauRa.AsEnumerable()
                            .Where(m => SafeGetInt(m, "ThangCT") >= tuThang && SafeGetInt(m, "ThangCT") <= denThang)
                            .GroupBy(m => new {
                                SoHieu = SafeGetString(m, "SoHieu"),
                                ThangCT = SafeGetInt(m, "ThangCT")
                            })
                            .ToList();

                        // 6. Lấy Max MaCT TRƯỚC KHI XÓA
                        int maxMaCT = GetMaxCT(conn, transaction);
                        Console.WriteLine($"Max MaCT trước khi xóa: {maxMaCT}");

                        // 7. Xóa dữ liệu cũ
                        string deleteChungTu8 = "DELETE FROM ChungTu WHERE ThangCT >= @TuThang AND ThangCT <= @DenThang AND MaLoai = 8";
                        using (SqlCommand cmd = new SqlCommand(deleteChungTu8, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TuThang", tuThang);
                            cmd.Parameters.AddWithValue("@DenThang", denThang);
                            int deleted = cmd.ExecuteNonQuery();
                            Console.WriteLine($"Đã xóa {deleted} dòng ChungTu MaLoai=8");
                        }

                        string deleteChungTu2 = "DELETE FROM ChungTu WHERE ThangCT >= @TuThang AND ThangCT <= @DenThang AND MaLoai = 2";
                        using (SqlCommand cmd = new SqlCommand(deleteChungTu2, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TuThang", tuThang);
                            cmd.Parameters.AddWithValue("@DenThang", denThang);
                            int deleted = cmd.ExecuteNonQuery();
                            Console.WriteLine($"Đã xóa {deleted} dòng ChungTu MaLoai=2");
                        }

                        // ====== 8. XÓA HÓA ĐƠN CŨ ======
                        var maSoCanXoa = lstBakhd
                            .Select(m => SafeGetInt(m, "MaSo"))
                            .Where(m => m > 0)
                            .Distinct()
                            .ToList();

                        if (maSoCanXoa.Count > 0)
                        {
                            string maSoString = string.Join(",", maSoCanXoa);
                            string deleteHoaDon = $"DELETE FROM HoaDon WHERE MaSo IN ({maSoString})";
                            using (SqlCommand cmd = new SqlCommand(deleteHoaDon, conn, transaction))
                            {
                                int deleted = cmd.ExecuteNonQuery();
                                Console.WriteLine($"Đã xóa {deleted} hóa đơn cũ");
                            }
                        }

                        // ====== 9. TẠO LẠI DỮ LIỆU CHUNGTU ======
                        DataTable dtChungTuMoi = GetChungTuSchema();
                        dtChungTuMoi.Columns.Add("OldMaSo", typeof(int));

                        int newMaCT = maxMaCT;
                        int sttHoaDon = 1;

                        // Dictionary để lưu mapping
                        var mappingKeyToNewMaSo = new Dictionary<string, int>(); // Key = "MaCT_MaLoai_OldMaSo"
                        var mappingHangOldToNew = new Dictionary<int, int>();   // OldMaSo → NewMaSo (hàng)
                        var mappingThueOldToNew = new Dictionary<int, int>();   // OldMaSo → NewMaSo (thuế)
                        var mappingGiaVonOldToNew = new Dictionary<int, int>(); // OldMaSo → NewMaSo (giá vốn)

                        foreach (var group in groupChungtu)
                        {
                            // Lấy hàng hóa (MaLoai=8, MaTKCo!=14038)
                            var getlistHang = dtDauRa.AsEnumerable()
                                .Where(m => SafeGetString(m, "SoHieu") == group.Key.SoHieu &&
                                            SafeGetInt(m, "ThangCT") == group.Key.ThangCT &&
                                            SafeGetInt(m, "MaTKCo") != 14038)
                                .ToList();

                            // Lấy thuế (MaTKCo=14038)
                            var getlistThue = dtDauRa.AsEnumerable()
                                .Where(m => SafeGetString(m, "SoHieu") == group.Key.SoHieu &&
                                            SafeGetInt(m, "ThangCT") == group.Key.ThangCT &&
                                            SafeGetInt(m, "MaTKCo") == 14038)
                                .ToList();

                            Console.WriteLine($"=== HÓA ĐƠN {group.Key.SoHieu} - Tháng {group.Key.ThangCT} ===");
                            Console.WriteLine($"Số dòng hàng: {getlistHang.Count}");
                            Console.WriteLine($"Số dòng thuế: {getlistThue.Count}");

                            // ===== BƯỚC 1: Xử lý hàng hóa =====
                            bool istang = false;
                            foreach (DataRow hh in getlistHang)
                            {
                                int oldMaCT = SafeGetInt(hh, "MaCT");
                                int oldMaSoHang = SafeGetInt(hh, "MaSo");

                                // Tạo dòng hàng hóa
                                DataRow newHangRow = dtChungTuMoi.NewRow();
                                CopyRowData(hh, newHangRow);
                                newHangRow["OldMaSo"] = oldMaSoHang;
                                dtChungTuMoi.Rows.Add(newHangRow);
                                Console.WriteLine($"✅ Hàng: MaCT={oldMaCT}, MaSo cũ={oldMaSoHang}");

                                // Tính giá vốn
                                DataRow tonKhoRow = GetTonKhoRow(conn, transaction, SafeGetInt(hh, "MaVattu"));
                                if (tonKhoRow != null)
                                {
                                    int thang = SafeGetInt(hh, "ThangCT");
                                    double tienTon = GetTienThang(tonKhoRow, thang - 1);
                                    double luongTon = GetLuongThang(tonKhoRow, thang - 1);
                                    double tienNhap = GetTienNhap(tonKhoRow, thang);
                                    double luongNhap = GetLuongNhap(tonKhoRow, thang);

                                    double tongTien = tienTon + tienNhap;
                                    double tongLuong = luongTon + luongNhap;
                                    double giaVon = tongLuong != 0 ? tongTien / tongLuong : 0;

                                    double slXuat = SafeGetDouble(hh, "SoPS2Co");
                                    double tienGiaVon = Math.Round(slXuat * giaVon);

                                    // CHỈ TĂNG MaCT 1 LẦN CHO CẢ HÓA ĐƠN
                                    if (!istang)
                                    {
                                        newMaCT++;
                                        istang = true;
                                    }

                                    // ⭐ TÌM GIÁ VỐN CŨ ĐỂ LẤY MaCT (NẾU CÓ)
                                    string soHieuGV = $"{SafeGetString(hh, "SoHieu")}GV";
                                    var giaVonGoc = dtDauRa.AsEnumerable()
                                        .FirstOrDefault(m => SafeGetString(m, "SoHieu") == soHieuGV &&
                                                             SafeGetInt(m, "ThangCT") == SafeGetInt(hh, "ThangCT") &&
                                                             SafeGetInt(m, "MaVattu") == SafeGetInt(hh, "MaVattu"));

                                    int maCTGiaVon;
                                    if (giaVonGoc != null)
                                    {
                                        // ✅ CÓ GIÁ VỐN CŨ → Lấy MaCT từ giá vốn cũ
                                        maCTGiaVon = SafeGetInt(giaVonGoc, "MaCT");
                                        Console.WriteLine($"  Tìm thấy giá vốn gốc: MaCT={maCTGiaVon}");
                                    }
                                    else
                                    {
                                        // ❌ KHÔNG CÓ GIÁ VỐN CŨ → Dùng MaCT mới (đã tăng)
                                        maCTGiaVon = newMaCT;
                                        Console.WriteLine($"  Không có giá vốn gốc, dùng MaCT={maCTGiaVon}");
                                    }

                                    // Tạo dòng giá vốn
                                    DataRow giaVonRow = dtChungTuMoi.NewRow();
                                    CopyRowData(hh, giaVonRow);
                                    giaVonRow["MaCT"] = maCTGiaVon;
                                    giaVonRow["MaLoai"] = 2;
                                    giaVonRow["SoHieu"] = soHieuGV;
                                    giaVonRow["MaTKNo"] = 151;
                                    giaVonRow["MaTKCo"] = 39;
                                    giaVonRow["SoPS"] = tienGiaVon;
                                    giaVonRow["SoPS2No"] = 0;
                                    giaVonRow["SoPS2Co"] = SafeGetDouble(hh, "SoPS2Co");
                                    giaVonRow["CT_ID"] = 500000000 + oldMaSoHang; // Tạm thời
                                    giaVonRow["OldMaSo"] = oldMaSoHang;
                                    dtChungTuMoi.Rows.Add(giaVonRow);
                                    Console.WriteLine($"✅ Giá vốn: MaCT={maCTGiaVon}, CT_ID={500000000 + oldMaSoHang}");
                                }
                            }

                            // ===== BƯỚC 2: Xử lý thuế =====
                            foreach (DataRow item in getlistThue)
                            {
                                int oldMaSoThue = SafeGetInt(item, "MaSo");
                                int oldMaCT = SafeGetInt(item, "MaCT");

                                DataRow newRow = dtChungTuMoi.NewRow();
                                CopyRowData(item, newRow);
                                newRow["OldMaSo"] = oldMaSoThue;
                                dtChungTuMoi.Rows.Add(newRow);
                                Console.WriteLine($"✅ Thuế: MaCT={oldMaCT}, MaSo cũ={oldMaSoThue}, SoPS={SafeGetDouble(item, "SoPS")}");
                            }

                            progressPanel1.Caption = $"Đang xử lý hóa đơn {group.Key.SoHieu} - {sttHoaDon}/{groupChungtu.Count}";
                            Application.DoEvents();
                            sttHoaDon++;
                        }

                        Console.WriteLine($"=== TỔNG KẾT ===");
                        Console.WriteLine($"Tổng số dòng ChungTu mới: {dtChungTuMoi.Rows.Count}");

                        progressPanel1.Caption = "Đang INSERT dữ liệu (BULK)...";
                        Application.DoEvents();

                        // ====== 10. BULK INSERT CHUNGTU ======

                        string tempTable = "#TempChungTu";
                        CreateTempTable(conn, transaction, tempTable);

                        DataTable dtInsert = dtChungTuMoi.Clone();
                        if (dtInsert.Columns.Contains("MaSo"))
                            dtInsert.Columns.Remove("MaSo");

                        foreach (DataRow row in dtChungTuMoi.Rows)
                        {
                            DataRow newRow = dtInsert.NewRow();
                            foreach (DataColumn col in dtInsert.Columns)
                            {
                                if (row.Table.Columns.Contains(col.ColumnName))
                                {
                                    object value = row[col.ColumnName];
                                    if (col.DataType == typeof(DateTime) && value != DBNull.Value && value != null)
                                    {
                                        try
                                        {
                                            if (value is string)
                                            {
                                                string dateStr = value.ToString();
                                                DateTime parsedDate;
                                                if (DateTime.TryParse(dateStr, out parsedDate))
                                                    newRow[col.ColumnName] = parsedDate;
                                                else
                                                    newRow[col.ColumnName] = DBNull.Value;
                                            }
                                            else if (value is DateTime)
                                                newRow[col.ColumnName] = value;
                                            else
                                                newRow[col.ColumnName] = DBNull.Value;
                                        }
                                        catch
                                        {
                                            newRow[col.ColumnName] = DBNull.Value;
                                        }
                                    }
                                    else
                                    {
                                        newRow[col.ColumnName] = value;
                                    }
                                }
                            }
                            dtInsert.Rows.Add(newRow);
                        }

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = tempTable;
                            bulkCopy.BatchSize = 1000;
                            bulkCopy.BulkCopyTimeout = 600;
                            bulkCopy.WriteToServer(dtInsert);
                        }

                        // ====== 11. MERGE CHUNGTU ======
                        string mergeSql = $@"
                    MERGE ChungTu AS target
                    USING {tempTable} AS source
                    ON (target.MaCT = source.MaCT AND target.MaLoai = source.MaLoai)
                    WHEN MATCHED THEN
                        UPDATE SET 
                            target.ThangCT = source.ThangCT,
                            target.SoHieu = source.SoHieu,
                            target.NgayCT = source.NgayCT,
                            target.NgayGS = source.NgayGS,
                            target.NgayTL = source.NgayTL,
                            target.DienGiai = source.DienGiai,
                            target.MaNguon = source.MaNguon,
                            target.MaKho = source.MaKho,
                            target.MaTKNo = source.MaTKNo,
                            target.MaTKCo = source.MaTKCo,
                            target.SoPS = source.SoPS,
                            target.SoPS2No = source.SoPS2No,
                            target.SoPS2Co = source.SoPS2Co,
                            target.MaTKTCNo = source.MaTKTCNo,
                            target.MaTKTCCo = source.MaTKTCCo,
                            target.MaVattu = source.MaVattu,
                            target.GhiChu = source.GhiChu,
                            target.CT_ID = source.CT_ID,
                            target.SoXuat = source.SoXuat,
                            target.MaDT = source.MaDT,
                            target.MaKH = source.MaKH,
                            target.CTGS = source.CTGS,
                            target.MaKHC = source.MaKHC,
                            target.MaTP = source.MaTP,
                            target.DVT = source.DVT,
                            target.User_ID = source.User_ID,
                            target.DienGiaiE = source.DienGiaiE,
                            target.TyGia = source.TyGia,
                            target.MaNV = source.MaNV,
                            target.HanTT = source.HanTT,
                            target.SH1 = source.SH1,
                            target.T1 = source.T1,
                            target.TLCK = source.TLCK,
                            target.CK = source.CK,
                            target.MaDT1 = source.MaDT1,
                            target.MaDT2 = source.MaDT2,
                            target.MaDT3 = source.MaDT3,
                            target.XuLy = source.XuLy,
                            target.MauSoHD = source.MauSoHD,
                            target.LoaiHoaDon = source.LoaiHoaDon,
                            target.Nguoimuahang = source.Nguoimuahang,
                            target.hinhthucthanhtoan = source.hinhthucthanhtoan,
                            target.sophieudathang = source.sophieudathang,
                            target.chondiengiai = source.chondiengiai,
                            target.phantramchietkhau = source.phantramchietkhau,
                            target.sotienchietkhau = source.sotienchietkhau,
                            target.solo = source.solo,
                            target.handung = source.handung,
                            target.SoPSGoc = source.SoPSGoc,
                            target.nhanban = source.nhanban,
                            target.NgayImport = source.NgayImport
                    WHEN NOT MATCHED THEN
                        INSERT (
                            MaCT, MaLoai, ThangCT, SoHieu, NgayCT, NgayGS, NgayTL,
                            DienGiai, MaNguon, MaKho, MaTKNo, MaTKCo, SoPS, 
                            SoPS2No, SoPS2Co, MaTKTCNo, MaTKTCCo, MaVattu, GhiChu, 
                            CT_ID, SoXuat, MaDT, MaKH, CTGS, MaKHC, MaTP, 
                            DVT, User_ID, DienGiaiE, TyGia, MaNV, HanTT, 
                            SH1, T1, TLCK, CK, MaDT1, MaDT2, MaDT3, XuLy, 
                            MauSoHD, LoaiHoaDon, Nguoimuahang, hinhthucthanhtoan,
                            sophieudathang, chondiengiai, phantramchietkhau, 
                            sotienchietkhau, solo, handung, SoPSGoc, nhanban, NgayImport
                        )
                        VALUES (
                            source.MaCT, source.MaLoai, source.ThangCT, source.SoHieu, 
                            source.NgayCT, source.NgayGS, source.NgayTL, source.DienGiai, 
                            source.MaNguon, source.MaKho, source.MaTKNo, source.MaTKCo, 
                            source.SoPS, source.SoPS2No, source.SoPS2Co, 
                            source.MaTKTCNo, source.MaTKTCCo, source.MaVattu, source.GhiChu, 
                            source.CT_ID, source.SoXuat, source.MaDT, source.MaKH, 
                            source.CTGS, source.MaKHC, source.MaTP, source.DVT, 
                            source.User_ID, source.DienGiaiE, source.TyGia, source.MaNV, 
                            source.HanTT, source.SH1, source.T1, source.TLCK, source.CK, 
                            source.MaDT1, source.MaDT2, source.MaDT3, source.XuLy, 
                            source.MauSoHD, source.LoaiHoaDon, source.Nguoimuahang,
                            source.hinhthucthanhtoan, source.sophieudathang, 
                            source.chondiengiai, source.phantramchietkhau, 
                            source.sotienchietkhau, source.solo, source.handung, 
                            source.SoPSGoc, source.nhanban, source.NgayImport
                        )
                    OUTPUT INSERTED.MaSo, source.MaCT, source.OldMaSo;";

                        // ====== 12. LẤY MAPPING ======
                        using (SqlCommand cmd = new SqlCommand(mergeSql, conn, transaction))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int newMaSo = reader.GetInt32(0);
                                    int maCT = reader.GetInt32(1);
                                    int oldMaSo = reader.GetInt32(2);

                                    foreach (DataRow row in dtChungTuMoi.Rows)
                                    {
                                        int rowMaCT = SafeGetInt(row, "MaCT");
                                        int rowOldMaSo = SafeGetInt(row, "OldMaSo");
                                        if (rowMaCT == maCT && rowOldMaSo == oldMaSo)
                                        {
                                            int rowMaLoai = SafeGetInt(row, "MaLoai");
                                            int rowMaTKCo = SafeGetInt(row, "MaTKCo");

                                            string key = $"{maCT}_{rowMaLoai}_{oldMaSo}";
                                            if (!mappingKeyToNewMaSo.ContainsKey(key))
                                            {
                                                mappingKeyToNewMaSo[key] = newMaSo;
                                                Console.WriteLine($"Key={key} → NewMaSo={newMaSo}");
                                            }

                                            if (rowMaLoai == 8 && rowMaTKCo != 14038) // Hàng hóa
                                            {
                                                if (!mappingHangOldToNew.ContainsKey(oldMaSo))
                                                {
                                                    mappingHangOldToNew[oldMaSo] = newMaSo;
                                                    Console.WriteLine($"✅ Hàng: OldMaSo={oldMaSo} → NewMaSo={newMaSo}");
                                                }
                                            }
                                            else if (rowMaLoai == 2) // Giá vốn
                                            {
                                                if (!mappingGiaVonOldToNew.ContainsKey(oldMaSo))
                                                {
                                                    mappingGiaVonOldToNew[oldMaSo] = newMaSo;
                                                    Console.WriteLine($"✅ Giá vốn: OldMaSo={oldMaSo} → NewMaSo={newMaSo}");
                                                }
                                            }
                                            else if (rowMaTKCo == 14038) // Thuế
                                            {
                                                if (!mappingThueOldToNew.ContainsKey(oldMaSo))
                                                {
                                                    mappingThueOldToNew[oldMaSo] = newMaSo;
                                                    Console.WriteLine($"✅ Thuế: OldMaSo={oldMaSo} → NewMaSo={newMaSo}");
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        // ====== 13. CẬP NHẬT CT_ID CHO GIÁ VỐN ======
                        progressPanel1.Caption = "Đang cập nhật CT_ID cho giá vốn...";
                        Application.DoEvents();

                        foreach (DataRow row in dtChungTuMoi.Rows)
                        {
                            int maLoai = SafeGetInt(row, "MaLoai");
                            int oldMaSo = SafeGetInt(row, "OldMaSo");

                            if (maLoai == 2 && oldMaSo > 0) // Giá vốn
                            {
                                if (mappingGiaVonOldToNew.TryGetValue(oldMaSo, out int newMaSoGiaVon))
                                {
                                    if (mappingHangOldToNew.TryGetValue(oldMaSo, out int newMaSoHang))
                                    {
                                        double newCT_ID = 500000000 + newMaSoHang;
                                        row["CT_ID"] = newCT_ID;

                                        string updateCTID = "UPDATE ChungTu SET CT_ID = @CT_ID WHERE MaSo = @MaSo AND MaLoai = 2";
                                        using (SqlCommand cmd = new SqlCommand(updateCTID, conn, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@CT_ID", newCT_ID);
                                            cmd.Parameters.AddWithValue("@MaSo", newMaSoGiaVon);
                                            cmd.ExecuteNonQuery();
                                            Console.WriteLine($"✅ CT_ID: MaSo={newMaSoGiaVon}, CT_ID={newCT_ID} (MaSo hàng mới={newMaSoHang})");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"⚠️ Không tìm thấy MaSo hàng mới cho OldMaSo={oldMaSo}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"⚠️ Không tìm thấy mapping giá vốn cho OldMaSo={oldMaSo}");
                                }
                            }
                        }

                        // Xóa bảng tạm
                        DropTempTable(conn, transaction, tempTable);

                        // ====== 14. TẠO MAPPING CHO HÓA ĐƠN ======
                        var mappingHoaDonOldToNew = new Dictionary<int, int>();

                        // Lấy danh sách MaSo đã tồn tại trong HoaDon
                        var existingHoaDonMaSo = new HashSet<int>();
                        string getHoaDonSql = "SELECT MaSo FROM HoaDon";
                        using (SqlCommand cmd = new SqlCommand(getHoaDonSql, conn, transaction))
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                existingHoaDonMaSo.Add(reader.GetInt32(0));
                            }
                        }

                        int maxMaSo = existingHoaDonMaSo.Count > 0 ? existingHoaDonMaSo.Max() : 0;
                        var usedNewMaSo = new HashSet<int>(existingHoaDonMaSo);

                        foreach (DataRow thueRow in dtChungTuMoi.Rows)
                        {
                            int maLoai = SafeGetInt(thueRow, "MaLoai");
                            int maTKCo = SafeGetInt(thueRow, "MaTKCo");

                            if (maLoai == 8 && maTKCo == 14038) // Thuế
                            {
                                int oldMaSoThue = SafeGetInt(thueRow, "OldMaSo");

                                if (mappingThueOldToNew.TryGetValue(oldMaSoThue, out int newMaSoValue))
                                {
                                    if (usedNewMaSo.Contains(newMaSoValue))
                                    {
                                        Console.WriteLine($"⚠️ MaSo {newMaSoValue} đã tồn tại! Tạo mới...");
                                        maxMaSo++;
                                        newMaSoValue = maxMaSo;
                                        while (usedNewMaSo.Contains(newMaSoValue))
                                        {
                                            maxMaSo++;
                                            newMaSoValue = maxMaSo;
                                        }
                                    }

                                    mappingHoaDonOldToNew[oldMaSoThue] = newMaSoValue;
                                    usedNewMaSo.Add(newMaSoValue);
                                    Console.WriteLine($"✅ Hóa đơn: {oldMaSoThue} → {newMaSoValue}");
                                }
                                else
                                {
                                    int maCT = SafeGetInt(thueRow, "MaCT");
                                    string key = $"{maCT}_8_{oldMaSoThue}";

                                    if (mappingKeyToNewMaSo.TryGetValue(key, out int newMaSoValue2))
                                    {
                                        if (usedNewMaSo.Contains(newMaSoValue2))
                                        {
                                            maxMaSo++;
                                            newMaSoValue2 = maxMaSo;
                                            while (usedNewMaSo.Contains(newMaSoValue2))
                                            {
                                                maxMaSo++;
                                                newMaSoValue2 = maxMaSo;
                                            }
                                        }
                                        mappingHoaDonOldToNew[oldMaSoThue] = newMaSoValue2;
                                        usedNewMaSo.Add(newMaSoValue2);
                                        Console.WriteLine($"✅ Hóa đơn (từ key): {oldMaSoThue} → {newMaSoValue2}");
                                    }
                                    else
                                    {
                                        maxMaSo++;
                                        int newMaSoValue3 = maxMaSo;
                                        while (usedNewMaSo.Contains(newMaSoValue3))
                                        {
                                            maxMaSo++;
                                            newMaSoValue3 = maxMaSo;
                                        }
                                        mappingHoaDonOldToNew[oldMaSoThue] = newMaSoValue3;
                                        usedNewMaSo.Add(newMaSoValue3);
                                        Console.WriteLine($"⚠️ Hóa đơn (tạo mới): {oldMaSoThue} → {newMaSoValue3}");
                                    }
                                }
                            }
                        }

                        // ====== 15. TẠO LẠI HÓA ĐƠN ======
                        progressPanel1.Caption = "Đang tạo lại hóa đơn...";
                        Application.DoEvents();

                        int insertedCount = 0;
                        foreach (var kv in mappingHoaDonOldToNew)
                        {
                            int oldMaSoHoaDon = kv.Key;
                            int newMaSoHoaDon = kv.Value;

                            var hd = lstBakhd.FirstOrDefault(m => SafeGetInt(m, "MaSo") == oldMaSoHoaDon);
                            if (hd != null)
                            {
                                InsertHoaDon(conn, transaction, hd, newMaSoHoaDon);
                                insertedCount++;
                                Console.WriteLine($"✅ Tạo hóa đơn: {oldMaSoHoaDon} → {newMaSoHoaDon}");
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Không tìm thấy hóa đơn cũ: MaSo={oldMaSoHoaDon}");
                            }
                        }

                        Console.WriteLine($"Đã tạo {insertedCount} hóa đơn mới");

                        progressPanel1.Caption = "Hoàn thành!";
                        Application.DoEvents();

                        transaction.Commit();

                        int afterCount8 = GetCount(conn, $"SELECT COUNT(*) FROM ChungTu WHERE MaLoai = 8 AND ThangCT >= {tuThang} AND ThangCT <= {denThang}");
                        int afterCount2 = GetCount(conn, $"SELECT COUNT(*) FROM ChungTu WHERE MaLoai = 2 AND ThangCT >= {tuThang} AND ThangCT <= {denThang}");
                        int afterCountHoaDon = GetCount(conn, $"SELECT COUNT(*) FROM HoaDon");

                        XtraMessageBox.Show($"✅ Đã tính xong giá vốn!\n" +
                            $"- Số dòng hàng (MaLoai=8): {afterCount8}\n" +
                            $"- Số dòng giá vốn (MaLoai=2): {afterCount2}\n" +
                            $"- Số hóa đơn: {afterCountHoaDon}\n" +
                            $"- Đã tạo {insertedCount} hóa đơn mới");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        XtraMessageBox.Show($"❌ Lỗi: {ex.Message}\n{ex.StackTrace}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"❌ Lỗi kết nối: {ex.Message}");
            }
        }

        private int GetCount(SqlConnection conn, string query)
        {
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            int tuThang = Convert.ToInt32(comboBoxEdit1.EditValue);
            int denThang = Convert.ToInt32(comboBoxEdit2.EditValue);
            TinhGiaVon(tuThang, denThang, null);
        }

        private void InsertHoaDon(SqlConnection conn, SqlTransaction trans, DataRow hoaDonRow, int maSo)
        {
            try
            {
                string query = @"
                    INSERT INTO HoaDon (
                        MaSo, Loai, MaKhachHang, KyHieu, SoHD, NgayPH, MatHang, 
                        SoLuong, ThanhTien, TyLe, HD, KCT, GiaTT, HTTT, 
                        MauSo, HDBL, NK, TS, DC, TyGia, pathInvoice, 
                        Ghichuhd, IdNhap, StatusPH, IdTemplate, TendoHDid, 
                        TendoHDState, has_e_invoice
                    ) VALUES (
                        @MaSo, @Loai, @MaKhachHang, @KyHieu, @SoHD, @NgayPH, @MatHang, 
                        @SoLuong, @ThanhTien, @TyLe, @HD, @KCT, @GiaTT, @HTTT, 
                        @MauSo, @HDBL, @NK, @TS, @DC, @TyGia, @pathInvoice, 
                        @Ghichuhd, @IdNhap, @StatusPH, @IdTemplate, @TendoHDid, 
                        @TendoHDState, @has_e_invoice
                    )";

                using (SqlCommand cmd = new SqlCommand(query, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@MaSo", maSo);
                    cmd.Parameters.AddWithValue("@Loai", SafeGetShort(hoaDonRow, "Loai"));
                    cmd.Parameters.AddWithValue("@MaKhachHang", SafeGetInt(hoaDonRow, "MaKhachHang"));
                    cmd.Parameters.AddWithValue("@KyHieu", SafeGetString(hoaDonRow, "KyHieu"));
                    cmd.Parameters.AddWithValue("@SoHD", SafeGetString(hoaDonRow, "SoHD"));
                    cmd.Parameters.AddWithValue("@NgayPH", SafeGetDateTime(hoaDonRow, "NgayPH") ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@MatHang", SafeGetString(hoaDonRow, "MatHang"));
                    cmd.Parameters.AddWithValue("@SoLuong", SafeGetDouble(hoaDonRow, "SoLuong"));
                    cmd.Parameters.AddWithValue("@ThanhTien", SafeGetDouble(hoaDonRow, "ThanhTien"));
                    cmd.Parameters.AddWithValue("@TyLe", SafeGetShort(hoaDonRow, "TyLe"));
                    cmd.Parameters.AddWithValue("@HD", SafeGetShort(hoaDonRow, "HD"));
                    cmd.Parameters.AddWithValue("@KCT", SafeGetShort(hoaDonRow, "KCT"));
                    cmd.Parameters.AddWithValue("@GiaTT", SafeGetDouble(hoaDonRow, "GiaTT"));
                    cmd.Parameters.AddWithValue("@HTTT", SafeGetString(hoaDonRow, "HTTT"));
                    cmd.Parameters.AddWithValue("@MauSo", SafeGetString(hoaDonRow, "MauSo"));
                    cmd.Parameters.AddWithValue("@HDBL", SafeGetShort(hoaDonRow, "HDBL"));
                    cmd.Parameters.AddWithValue("@NK", SafeGetShort(hoaDonRow, "NK"));
                    cmd.Parameters.AddWithValue("@TS", SafeGetShort(hoaDonRow, "TS"));
                    cmd.Parameters.AddWithValue("@DC", SafeGetShort(hoaDonRow, "DC"));
                    cmd.Parameters.AddWithValue("@TyGia", SafeGetDouble(hoaDonRow, "TyGia"));
                    cmd.Parameters.AddWithValue("@pathInvoice", SafeGetString(hoaDonRow, "pathInvoice"));
                    cmd.Parameters.AddWithValue("@Ghichuhd", SafeGetString(hoaDonRow, "Ghichuhd"));
                    cmd.Parameters.AddWithValue("@IdNhap", SafeGetString(hoaDonRow, "IdNhap"));
                    cmd.Parameters.AddWithValue("@StatusPH", SafeGetString(hoaDonRow, "StatusPH"));
                    cmd.Parameters.AddWithValue("@IdTemplate", SafeGetString(hoaDonRow, "IdTemplate"));
                    cmd.Parameters.AddWithValue("@TendoHDid", SafeGetString(hoaDonRow, "TendoHDid"));
                    cmd.Parameters.AddWithValue("@TendoHDState", SafeGetString(hoaDonRow, "TendoHDState"));
                    cmd.Parameters.AddWithValue("@has_e_invoice", SafeGetDouble(hoaDonRow, "has_e_invoice"));

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting HoaDon: {ex.Message}");
                throw;
            }
        }

        #region Database Helper Methods

        private DataTable GetDataTable(SqlConnection conn, SqlTransaction trans, string query)
        {
            using (SqlCommand cmd = new SqlCommand(query, conn, trans))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private DataRow GetTonKhoRow(SqlConnection conn, SqlTransaction trans, int maVatTu)
        {
            string query = $"SELECT * FROM TonKho WHERE MaVatTu = {maVatTu}";
            DataTable dt = GetDataTable(conn, trans, query);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private void UpdateTonKho(SqlConnection conn, SqlTransaction trans, DataRow row)
        {
            if (row == null) return;

            try
            {
                int maVatTu = Convert.ToInt32(row["MaVatTu"]);

                string query = @"
                    UPDATE TonKho SET 
                        Tien_Nhap_1 = @Tien_Nhap_1, Luong_Nhap_1 = @Luong_Nhap_1, 
                        Tien_Xuat_1 = @Tien_Xuat_1, Luong_Xuat_1 = @Luong_Xuat_1, 
                        Luong_1 = @Luong_1, Tien_1 = @Tien_1,
                        Tien_Nhap_2 = @Tien_Nhap_2, Luong_Nhap_2 = @Luong_Nhap_2, 
                        Tien_Xuat_2 = @Tien_Xuat_2, Luong_Xuat_2 = @Luong_Xuat_2, 
                        Luong_2 = @Luong_2, Tien_2 = @Tien_2,
                        Tien_Nhap_3 = @Tien_Nhap_3, Luong_Nhap_3 = @Luong_Nhap_3, 
                        Tien_Xuat_3 = @Tien_Xuat_3, Luong_Xuat_3 = @Luong_Xuat_3, 
                        Luong_3 = @Luong_3, Tien_3 = @Tien_3,
                        Tien_Nhap_4 = @Tien_Nhap_4, Luong_Nhap_4 = @Luong_Nhap_4, 
                        Tien_Xuat_4 = @Tien_Xuat_4, Luong_Xuat_4 = @Luong_Xuat_4, 
                        Luong_4 = @Luong_4, Tien_4 = @Tien_4,
                        Tien_Nhap_5 = @Tien_Nhap_5, Luong_Nhap_5 = @Luong_Nhap_5, 
                        Tien_Xuat_5 = @Tien_Xuat_5, Luong_Xuat_5 = @Luong_Xuat_5, 
                        Luong_5 = @Luong_5, Tien_5 = @Tien_5,
                        Tien_Nhap_6 = @Tien_Nhap_6, Luong_Nhap_6 = @Luong_Nhap_6, 
                        Tien_Xuat_6 = @Tien_Xuat_6, Luong_Xuat_6 = @Luong_Xuat_6, 
                        Luong_6 = @Luong_6, Tien_6 = @Tien_6,
                        Tien_Nhap_7 = @Tien_Nhap_7, Luong_Nhap_7 = @Luong_Nhap_7, 
                        Tien_Xuat_7 = @Tien_Xuat_7, Luong_Xuat_7 = @Luong_Xuat_7, 
                        Luong_7 = @Luong_7, Tien_7 = @Tien_7,
                        Tien_Nhap_8 = @Tien_Nhap_8, Luong_Nhap_8 = @Luong_Nhap_8, 
                        Tien_Xuat_8 = @Tien_Xuat_8, Luong_Xuat_8 = @Luong_Xuat_8, 
                        Luong_8 = @Luong_8, Tien_8 = @Tien_8,
                        Tien_Nhap_9 = @Tien_Nhap_9, Luong_Nhap_9 = @Luong_Nhap_9, 
                        Tien_Xuat_9 = @Tien_Xuat_9, Luong_Xuat_9 = @Luong_Xuat_9, 
                        Luong_9 = @Luong_9, Tien_9 = @Tien_9,
                        Tien_Nhap_10 = @Tien_Nhap_10, Luong_Nhap_10 = @Luong_Nhap_10, 
                        Tien_Xuat_10 = @Tien_Xuat_10, Luong_Xuat_10 = @Luong_Xuat_10, 
                        Luong_10 = @Luong_10, Tien_10 = @Tien_10,
                        Tien_Nhap_11 = @Tien_Nhap_11, Luong_Nhap_11 = @Luong_Nhap_11, 
                        Tien_Xuat_11 = @Tien_Xuat_11, Luong_Xuat_11 = @Luong_Xuat_11, 
                        Luong_11 = @Luong_11, Tien_11 = @Tien_11,
                        Tien_Nhap_12 = @Tien_Nhap_12, Luong_Nhap_12 = @Luong_Nhap_12, 
                        Tien_Xuat_12 = @Tien_Xuat_12, Luong_Xuat_12 = @Luong_Xuat_12, 
                        Luong_12 = @Luong_12, Tien_12 = @Tien_12
                    WHERE MaVatTu = @MaVatTu";

                using (SqlCommand cmd = new SqlCommand(query, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@MaVatTu", maVatTu);

                    for (int i = 1; i <= 12; i++)
                    {
                        cmd.Parameters.AddWithValue($"@Tien_Nhap_{i}", GetPropertyValue(row, $"Tien_Nhap_{i}"));
                        cmd.Parameters.AddWithValue($"@Luong_Nhap_{i}", GetPropertyValue(row, $"Luong_Nhap_{i}"));
                        cmd.Parameters.AddWithValue($"@Tien_Xuat_{i}", GetPropertyValue(row, $"Tien_Xuat_{i}"));
                        cmd.Parameters.AddWithValue($"@Luong_Xuat_{i}", GetPropertyValue(row, $"Luong_Xuat_{i}"));
                        cmd.Parameters.AddWithValue($"@Luong_{i}", GetPropertyValue(row, $"Luong_{i}"));
                        cmd.Parameters.AddWithValue($"@Tien_{i}", GetPropertyValue(row, $"Tien_{i}"));
                    }

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateTonKho: {ex.Message}");
                throw;
            }
        }

        private int GetMaxCT(SqlConnection conn, SqlTransaction trans)
        {
            string query = "SELECT ISNULL(MAX(MaCT), 0) FROM ChungTu";
            using (SqlCommand cmd = new SqlCommand(query, conn, trans))
            {
                object result = cmd.ExecuteScalar();
                return result == DBNull.Value || result == null ? 0 : Convert.ToInt32(result);
            }
        }

        #endregion

        #region Schema Helper Methods

        private DataTable GetChungTuSchema()
        {
            DataTable dt = new DataTable();

            string query = @"
                SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'ChungTu' 
                ORDER BY ORDINAL_POSITION";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string colName = reader["COLUMN_NAME"].ToString();
                        string dataType = reader["DATA_TYPE"].ToString();

                        Type colType = GetTypeFromSqlType(dataType);
                        dt.Columns.Add(colName, colType);
                    }
                }
            }

            return dt;
        }

        private Type GetTypeFromSqlType(string sqlType)
        {
            switch (sqlType.ToLower())
            {
                case "int": return typeof(int);
                case "bigint": return typeof(long);
                case "smallint": return typeof(short);
                case "tinyint": return typeof(byte);
                case "decimal": return typeof(decimal);
                case "float": return typeof(double);
                case "real": return typeof(float);
                case "money": return typeof(decimal);
                case "datetime": return typeof(DateTime);
                case "datetime2": return typeof(DateTime);
                case "date": return typeof(DateTime);
                case "char": return typeof(string);
                case "varchar": return typeof(string);
                case "nvarchar": return typeof(string);
                case "text": return typeof(string);
                case "ntext": return typeof(string);
                case "bit": return typeof(bool);
                default: return typeof(object);
            }
        }

        private void CopyRowData(DataRow source, DataRow dest)
        {
            foreach (DataColumn col in dest.Table.Columns)
            {
                if (col.ColumnName == "MaSo" || col.ColumnName == "OldMaSo") continue;

                if (source.Table.Columns.Contains(col.ColumnName))
                {
                    object value = source[col.ColumnName];
                    if (value != DBNull.Value && value != null)
                    {
                        try
                        {
                            if (col.DataType == typeof(DateTime))
                            {
                                if (value is string)
                                {
                                    string dateStr = value.ToString();
                                    DateTime parsedDate;
                                    if (DateTime.TryParse(dateStr, out parsedDate))
                                        dest[col.ColumnName] = parsedDate;
                                    else
                                        dest[col.ColumnName] = DBNull.Value;
                                }
                                else if (value is DateTime)
                                    dest[col.ColumnName] = value;
                                else
                                    dest[col.ColumnName] = DBNull.Value;
                            }
                            else
                            {
                                dest[col.ColumnName] = value;
                            }
                        }
                        catch
                        {
                            dest[col.ColumnName] = DBNull.Value;
                        }
                    }
                    else
                    {
                        dest[col.ColumnName] = DBNull.Value;
                    }
                }
            }
        }

        #endregion

        #region Temp Table Helper Methods

        private void CreateTempTable(SqlConnection conn, SqlTransaction trans, string tempTableName)
        {
            string dropSql = $"IF OBJECT_ID('tempdb..{tempTableName}') IS NOT NULL DROP TABLE {tempTableName}";
            using (SqlCommand cmd = new SqlCommand(dropSql, conn, trans))
            {
                cmd.ExecuteNonQuery();
            }

            string createSql = $@"
                CREATE TABLE {tempTableName} (
                    MaCT INT,
                    MaLoai SMALLINT,
                    ThangCT SMALLINT,
                    SoHieu NVARCHAR(20),
                    NgayCT DATETIME2(0),
                    NgayGS DATETIME2(0),
                    NgayTL DATETIME2(0),
                    DienGiai NVARCHAR(500),
                    MaKho INT,
                    MaNguon INT,
                    MaTKNo INT,
                    MaTKCo INT,
                    SoPS FLOAT,
                    SoPS2No FLOAT,
                    SoPS2Co FLOAT,
                    MaTKTCNo INT,
                    MaTKTCCo INT,
                    MaVattu INT,
                    GhiChu NVARCHAR(20),
                    CT_ID FLOAT,
                    SoXuat FLOAT,
                    MaDT INT,
                    MaKH INT,
                    CTGS INT,
                    MaKHC INT,
                    MaTP INT,
                    DVT SMALLINT,
                    User_ID INT,
                    DienGiaiE NVARCHAR(150),
                    TyGia FLOAT,
                    MaNV INT,
                    HanTT SMALLINT,
                    SH1 NVARCHAR(20),
                    T1 SMALLINT,
                    TLCK FLOAT,
                    CK FLOAT,
                    MaDT1 INT,
                    MaDT2 INT,
                    MaDT3 INT,
                    XuLy SMALLINT,
                    MauSoHD NVARCHAR(255),
                    LoaiHoaDon NVARCHAR(255),
                    Nguoimuahang NVARCHAR(255),
                    hinhthucthanhtoan NVARCHAR(255),
                    sophieudathang NVARCHAR(255),
                    chondiengiai NVARCHAR(255),
                    phantramchietkhau NVARCHAR(255),
                    sotienchietkhau NVARCHAR(255),
                    solo NVARCHAR(255),
                    handung DATETIME2(0),
                    SoPSGoc FLOAT,
                    nhanban FLOAT,
                    NgayImport DATETIME2(0),
                    OldMaSo INT
                )";

            using (SqlCommand cmd = new SqlCommand(createSql, conn, trans))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void DropTempTable(SqlConnection conn, SqlTransaction trans, string tempTableName)
        {
            string sql = $"IF OBJECT_ID('tempdb..{tempTableName}') IS NOT NULL DROP TABLE {tempTableName}";
            using (SqlCommand cmd = new SqlCommand(sql, conn, trans))
            {
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Property Helper Methods

        private double GetPropertyValue(DataRow row, string propertyName)
        {
            if (row.Table.Columns.Contains(propertyName) && row[propertyName] != DBNull.Value)
            {
                try { return Convert.ToDouble(row[propertyName]); }
                catch { return 0; }
            }
            return 0;
        }

        private void SetPropertyValue(DataRow row, string propertyName, double value)
        {
            if (row.Table.Columns.Contains(propertyName))
            {
                row[propertyName] = value;
            }
        }

        private double GetLuongThang(DataRow row, int thang) => GetPropertyValue(row, $"Luong_{thang}");
        private double GetTienThang(DataRow row, int thang) => GetPropertyValue(row, $"Tien_{thang}");
        private double GetLuongNhap(DataRow row, int thang) => GetPropertyValue(row, $"Luong_Nhap_{thang}");
        private double GetTienNhap(DataRow row, int thang) => GetPropertyValue(row, $"Tien_Nhap_{thang}");

        #endregion

        private void comboBoxEdit2_SelectedIndexChanged(object sender, EventArgs e) { }

        private void comboBoxEdit1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxEdit2.EditValue = comboBoxEdit1.EditValue;
        }
    }
}