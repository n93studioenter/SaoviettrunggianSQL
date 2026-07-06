
//using DevExpress.XtraEditors;
//using SaovietTax.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Windows.Forms;

//namespace SaovietTax
//{
//    public partial class AutoSumGiavon : DevExpress.XtraEditors.XtraForm
//    {
//        private readonly DatablankEntities _db;

//        public AutoSumGiavon()
//        {
//            InitializeComponent();
//            _db = new DatablankEntities();
//        }

//        private void AutoSumGiavon_Load(object sender, EventArgs e)
//        {
//            comboBoxEdit1.Properties.Items.Clear();
//            comboBoxEdit2.Properties.Items.Clear();

//            for (int i = 1; i <= 12; i++)
//            {
//                comboBoxEdit1.Properties.Items.Add(i);
//                comboBoxEdit2.Properties.Items.Add(i);
//            }

//            comboBoxEdit1.SelectedIndex = DateTime.Now.Month - 1;
//            comboBoxEdit2.SelectedIndex = DateTime.Now.Month - 1;
//        }

//        private void comboBoxEdit1_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            comboBoxEdit2.EditValue = comboBoxEdit1.EditValue;
//        }

//        private void simpleButton1_Click(object sender, EventArgs e)
//        {
//            if (comboBoxEdit1.EditValue == null || comboBoxEdit2.EditValue == null)
//            {
//                XtraMessageBox.Show("Vui lòng chọn tháng!", "Thông báo",
//                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return;
//            }

//            int tuThang = Convert.ToInt32(comboBoxEdit1.EditValue);
//            int denThang = Convert.ToInt32(comboBoxEdit2.EditValue);

//            if (tuThang > denThang)
//            {
//                XtraMessageBox.Show("Tháng bắt đầu phải nhỏ hơn hoặc bằng tháng kết thúc!",
//                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return;
//            }

//            this.Enabled = false;
//            try
//            {
//                TinhGiaVonBatch(tuThang, denThang);
//            }
//            finally
//            {
//                this.Enabled = true;
//            }
//        }

//        // ============================================================
//        // HÀM CHÍNH: TÍNH LẠI GIÁ VỐN (ENTITY FRAMEWORK)
//        // ============================================================
//        private void TinhGiaVonBatch(int tuThang, int denThang)
//        {
//            using (var transaction = _db.Database.BeginTransaction())
//            {
//                try
//                {
//                    // ============================================================
//                    // 1. KIỂM TRA DỮ LIỆU
//                    // ============================================================
//                    int total = _db.ChungTus.Count(c => c.ThangCT >= tuThang && c.ThangCT <= denThang);

//                    if (total == 0)
//                    {
//                        XtraMessageBox.Show($"Không có chứng từ từ tháng {tuThang} đến tháng {denThang}!",
//                            "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                        return;
//                    }

//                    // ============================================================
//                    // 2. TÍNH GIÁ VỐN CHO TẤT CẢ VẬT TƯ (1 LẦN DUY NHẤT) - EF
//                    // ============================================================
//                    var dictGiaVon = TinhGiaVonTatCaVatTu_EF(tuThang, denThang);

//                    // ============================================================
//                    // 3. LẤY DANH SÁCH CHỨNG TỪ CẦN XỬ LÝ - EF
//                    // ============================================================
//                    var listCT = _db.ChungTus
//                        .Where(c => c.ThangCT >= tuThang && c.ThangCT <= denThang
//                                    && (c.MaLoai == 2 || c.MaLoai == 8 || c.MaTKCo == 14038))
//                        .OrderBy(c => c.SoHieu)
//                        .ThenBy(c => c.ThangCT)
//                        .ThenBy(c => c.NgayCT)
//                        .ThenBy(c => c.MaLoai)
//                        .ToList();

//                    if (listCT.Count == 0)
//                    {
//                        XtraMessageBox.Show("Không có chứng từ bán hàng hoặc thuế!",
//                            "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                        return;
//                    }

//                    // ============================================================
//                    // 4. LẤY DANH SÁCH HÓA ĐƠN LIÊN QUAN - EF
//                    // ============================================================
//                    var listMaSo = listCT.Where(c => c.MaLoai == 8).Select(c => c.MaSo).Distinct().ToList();

//                    var listHD = new List<HoaDon>();
//                    if (listMaSo.Any())
//                    {
//                        listHD = _db.HoaDons.Where(h => listMaSo.Contains(h.MaSo)).ToList();
//                    }

//                    // ============================================================
//                    // 5. XÓA DỮ LIỆU CŨ - EF
//                    // ============================================================
//                    var hoaDonXoa = _db.HoaDons.Where(h => listMaSo.Contains(h.MaSo)).ToList();
//                    if (hoaDonXoa.Any())
//                        _db.HoaDons.RemoveRange(hoaDonXoa);

//                    var chungTuXoa = _db.ChungTus
//                        .Where(c => c.ThangCT >= tuThang && c.ThangCT <= denThang
//                                    && (c.MaLoai == 2 || c.MaLoai == 8 || c.MaTKCo == 14038))
//                        .ToList();
//                    if (chungTuXoa.Any())
//                        _db.ChungTus.RemoveRange(chungTuXoa);

//                    _db.SaveChanges();

//                    // ============================================================
//                    // 6. LẤY MAX MaCT VÀ MaSo - EF
//                    // ============================================================
//                    int maxct = _db.ChungTus.Any() ? _db.ChungTus.Max(c => c.MaCT ?? 0) : 0;
//                    int maxMaSo = _db.ChungTus.Any() ? _db.ChungTus.Max(c => c.MaSo) : 0;

//                    // ============================================================
//                    // 7. NHÓM VÀ TẠO LẠI CHỨNG TỪ - EF
//                    // ============================================================
//                    var groups = listCT
//                        .Where(c => c.MaLoai == 8 || c.MaTKCo == 14038)
//                        .GroupBy(c => new { c.SoHieu, c.ThangCT, c.NgayCT })
//                        .ToList();

//                    var chungTuMoi = new List<ChungTu>();
//                    var hoaDonMoi = new List<HoaDon>();

//                    foreach (var group in groups)
//                    {
//                        var rows = group.ToList();

//                        // Tìm chứng từ bán hàng (MaLoai = 8)
//                        var rowBanHang = rows.FirstOrDefault(c => c.MaLoai == 8);
//                        if (rowBanHang == null) continue;

//                        // 👉 LẤY MaCT GỐC (chung cho cả nhóm)
//                        int maCTGoc = rowBanHang.MaCT ?? 0;
//                        if (maCTGoc == 0)
//                        {
//                            maxct++;
//                            maCTGoc = maxct;
//                        }

//                        // 👉 TẠO MaCT CHO GIÁ VỐN (chung cho cả nhóm)
//                        maxct++;
//                        int maCTGV = maxct;
//                        if (maCTGV == maCTGoc)
//                        {
//                            maxct++;
//                            maCTGV = maxct;
//                        }

//                        int maSoBanHangMoi = 0;

//                        // 👉 LẤY CHỨNG TỪ THUẾ
//                        var rowThue = rows.FirstOrDefault(c => c.MaTKCo == 14038);

//                        foreach (var row in rows)
//                        {
//                            int maLoai = row.MaLoai ?? 0;
//                            int maTKCo = row.MaTKCo ?? 0;

//                            if (maLoai == 2) continue;
//                            if (maTKCo == 14038) continue;

//                            // ============================================================
//                            // 👉 QUAN TRỌNG: Lấy maVattu TỪ DÒNG HIỆN TẠI
//                            // ============================================================
//                            int maVattuHienTai = row.MaVattu ?? 0;
//                            int thangCTHienTai = row.ThangCT ?? 0;

//                            // ============================================================
//                            // 👉 Lấy giá vốn CHO TỪNG MAVATTU
//                            // ============================================================
//                            double giaVon = 0;
//                            if (dictGiaVon.ContainsKey(maVattuHienTai))
//                            {
//                                var dictThang = dictGiaVon[maVattuHienTai];
//                                if (dictThang.ContainsKey(thangCTHienTai))
//                                {
//                                    giaVon = dictThang[thangCTHienTai];
//                                }
//                            }

//                            // ============================================================
//                            // 7a. TẠO CHỨNG TỪ BÁN HÀNG
//                            // ============================================================
//                            maxMaSo++;
//                            maSoBanHangMoi = maxMaSo;

//                            var newRow = new ChungTu();
//                            CopyChungTu(row, newRow);
//                            newRow.MaCT = maCTGoc;
//                            newRow.MaSo = maxMaSo;
//                            chungTuMoi.Add(newRow);

//                            // ============================================================
//                            // 7b. TẠO GIÁ VỐN
//                            // ============================================================
//                            maxMaSo++;
//                            var newRowGV = new ChungTu();
//                            CopyChungTu(row, newRowGV);
//                            newRowGV.MaCT = maCTGV;
//                            newRowGV.MaSo = maxMaSo;
//                            newRowGV.SoHieu = row.SoHieu + "GV";

//                            double soPS2Co = row.SoPS2Co ?? 0;
//                            newRowGV.SoPS = soPS2Co * giaVon;
//                            newRowGV.SoPS2No = 0;
//                            newRowGV.SoPS2Co = soPS2Co;
//                            newRowGV.MaTKNo = 151;
//                            newRowGV.MaTKCo = 39;
//                            newRowGV.MaLoai = 2;
//                            newRowGV.MaTKTCNo = 151;
//                            newRowGV.MaTKTCCo = 39;
//                            newRowGV.CT_ID = 500000000 + row.MaSo;

//                            chungTuMoi.Add(newRowGV);
//                        }

//                        // ============================================================
//                        // 7c. TẠO CHỨNG TỪ THUẾ
//                        // ============================================================
//                        if (rowThue != null)
//                        {
//                            maxMaSo++;
//                            var newRowThue = new ChungTu();
//                            CopyChungTu(rowThue, newRowThue);
//                            newRowThue.MaCT = maCTGoc;
//                            newRowThue.MaSo = maxMaSo;
//                            chungTuMoi.Add(newRowThue);
//                        }

//                        // ============================================================
//                        // 7d. TẠO HÓA ĐƠN
//                        // ============================================================
//                        var rowHD = listHD.FirstOrDefault(h => h.MaSo == rowBanHang.MaSo);
//                        if (rowHD != null)
//                        {
//                            var newRowHD = new HoaDon();
//                            CopyHoaDon(rowHD, newRowHD);
//                            newRowHD.MaSo = maSoBanHangMoi;
//                            hoaDonMoi.Add(newRowHD);
//                        }
//                    }

//                    // ============================================================
//                    // 8. LƯU VÀO DATABASE (1 LẦN DUY NHẤT)
//                    // ============================================================
//                    if (chungTuMoi.Any())
//                        _db.ChungTus.AddRange(chungTuMoi);

//                    if (hoaDonMoi.Any())
//                        _db.HoaDons.AddRange(hoaDonMoi);

//                    _db.SaveChanges();

//                    // ============================================================
//                    // 9. COMMIT
//                    // ============================================================
//                    transaction.Commit();
//                    XtraMessageBox.Show($"Tính giá vốn thành công! Đã xử lý {chungTuMoi.Count} chứng từ.",
//                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                    this.Close();
//                }
//                catch (Exception ex)
//                {
//                    transaction.Rollback();
//                    XtraMessageBox.Show("Lỗi: " + ex.Message + "\n" + ex.StackTrace, "Lỗi",
//                        MessageBoxButtons.OK, MessageBoxIcon.Error);
//                }
//            }
//        }

//        // ============================================================
//        // HÀM TÍNH GIÁ VỐN CHO TẤT CẢ VẬT TƯ (ENTITY FRAMEWORK)
//        // ============================================================
//        // ============================================================
//        // HÀM TÍNH GIÁ VỐN CHO TẤT CẢ VẬT TƯ (ENTITY FRAMEWORK)
//        // ============================================================
//        private Dictionary<int, Dictionary<int, double>> TinhGiaVonTatCaVatTu_EF(int tuThang, int denThang)
//        {
//            var result = new Dictionary<int, Dictionary<int, double>>();

//            var nhapList = _db.ChungTus
//                .Where(c => c.MaLoai == 1 && c.ThangCT <= denThang && c.MaVattu > 0)
//                .OrderBy(c => c.MaVattu)
//                .ThenBy(c => c.ThangCT)
//                .ToList();

//            var xuatList = _db.ChungTus
//                .Where(c => c.MaLoai == 8 && c.ThangCT <= denThang && c.MaVattu > 0)
//                .GroupBy(c => new { c.MaVattu, c.ThangCT })
//                .Select(g => new
//                {
//                    MaVattu = g.Key.MaVattu ?? 0,
//                    ThangCT = g.Key.ThangCT ?? 0,
//                    SLXuat = g.Sum(c => c.SoPS2Co ?? 0)
//                })
//                .ToList();

//            var nhapGroups = nhapList
//                .GroupBy(c => new { c.MaVattu, c.ThangCT })
//                .Select(g => new
//                {
//                    MaVattu = g.Key.MaVattu ?? 0,
//                    ThangCT = g.Key.ThangCT ?? 0,
//                    TienNhap = g.Sum(c => c.SoPS ?? 0),
//                    SLNhap = g.Sum(c => c.SoPS2No ?? 0)
//                })
//                .ToList();

//            var dictTon = new Dictionary<int, double>();
//            var dictTienTon = new Dictionary<int, double>();

//            foreach (var item in nhapGroups)
//            {
//                if (!dictTon.ContainsKey(item.MaVattu))
//                {
//                    dictTon[item.MaVattu] = 0;
//                    dictTienTon[item.MaVattu] = 0;
//                }

//                dictTon[item.MaVattu] += item.SLNhap;
//                dictTienTon[item.MaVattu] += item.TienNhap;

//                var xuat = xuatList.FirstOrDefault(x => x.MaVattu == item.MaVattu && x.ThangCT == item.ThangCT);
//                if (xuat != null)
//                {
//                    double giaBQ = dictTon[item.MaVattu] > 0
//                        ? dictTienTon[item.MaVattu] / dictTon[item.MaVattu]
//                        : 0;
//                    double tienXuat = xuat.SLXuat * giaBQ;

//                    dictTon[item.MaVattu] -= xuat.SLXuat;
//                    dictTienTon[item.MaVattu] -= tienXuat;
//                }

//                // ✅ Làm tròn về số nguyên (giống VB6)
//                double giaVon = dictTon[item.MaVattu] > 0
//                    ? Math.Round(dictTienTon[item.MaVattu] / dictTon[item.MaVattu], 0, MidpointRounding.AwayFromZero)
//                    : 0;

//                if (!result.ContainsKey(item.MaVattu))
//                    result[item.MaVattu] = new Dictionary<int, double>();

//                result[item.MaVattu][item.ThangCT] = giaVon;
//            }

//            return result;
//        }

//        // ============================================================
//        // HÀM COPY CHỨNG TỪ
//        // ============================================================
//        private void CopyChungTu(ChungTu source, ChungTu dest)
//        {
//            dest.ThangCT = source.ThangCT;
//            dest.NgayCT = source.NgayCT;
//            dest.NgayGS = source.NgayGS;
//            dest.SoHieu = source.SoHieu;
//            dest.SoPS = source.SoPS;
//            dest.SoPS2No = source.SoPS2No;
//            dest.SoPS2Co = source.SoPS2Co;
//            dest.MaTKNo = source.MaTKNo;
//            dest.MaTKCo = source.MaTKCo;
//            dest.MaLoai = source.MaLoai;
//            dest.MaNguon = source.MaNguon;
//            dest.MaVattu = source.MaVattu;
//            dest.GhiChu = source.GhiChu;
//            dest.LoaiHoaDon = source.LoaiHoaDon;
//            dest.MaKho = source.MaKho;
//            dest.MaDT = source.MaDT;
//            dest.CTGS = source.CTGS;
//            dest.MaKH = source.MaKH;
//            dest.MaKHC = source.MaKHC;
//            dest.MaTP = source.MaTP;
//            dest.DVT = source.DVT;
//            dest.User_ID = source.User_ID;
//            dest.DienGiai = source.DienGiai;
//            dest.DienGiaiE = source.DienGiaiE;
//            dest.HanTT = source.HanTT;
//            dest.NgayImport = source.NgayImport;
//            dest.MaDT1 = source.MaDT1;
//            dest.MaDT2 = source.MaDT2;
//            dest.MaDT3 = source.MaDT3;
//            dest.MaNV = source.MaNV;
//            dest.SH1 = source.SH1;
//            dest.T1 = source.T1;
//            dest.TLCK = source.TLCK;
//            dest.CK = source.CK;
//            dest.MauSoHD = source.MauSoHD;
//            dest.MaTKTCNo = source.MaTKTCNo;
//            dest.MaTKTCCo = source.MaTKTCCo;
//            dest.CT_ID = source.CT_ID;
//            dest.solo = source.solo;
//        }

//        // ============================================================
//        // HÀM COPY HÓA ĐƠN
//        // ============================================================
//        private void CopyHoaDon(HoaDon source, HoaDon dest)
//        {
//            dest.Loai = source.Loai;
//            dest.MaKhachHang = source.MaKhachHang;
//            dest.KyHieu = source.KyHieu;
//            dest.SoHD = source.SoHD;
//            dest.NgayPH = source.NgayPH;
//            dest.MatHang = source.MatHang;
//            dest.SoLuong = source.SoLuong;
//            dest.ThanhTien = source.ThanhTien;
//            dest.TyLe = source.TyLe;
//            dest.HD = source.HD;
//            dest.KCT = source.KCT;
//            dest.HTTT = source.HTTT;
//            dest.MauSo = source.MauSo;
//            dest.NK = source.NK;
//            dest.TS = source.TS;
//            dest.DC = source.DC;
//            dest.TyGia = source.TyGia;
//            dest.HDBL = source.HDBL;
//        }
//    }
//}

using DevExpress.XtraEditors;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace SaovietTax
{
    public partial class AutoSumGiavon : DevExpress.XtraEditors.XtraForm
    {
        private readonly string _connectionString;

        public AutoSumGiavon()
        {
            InitializeComponent();
            _connectionString = ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;
        }

        private void AutoSumGiavon_Load(object sender, EventArgs e)
        {
            comboBoxEdit1.Properties.Items.Clear();
            comboBoxEdit2.Properties.Items.Clear();

            for (int i = 1; i <= 12; i++)
            {
                comboBoxEdit1.Properties.Items.Add(i);
                comboBoxEdit2.Properties.Items.Add(i);
            }

            comboBoxEdit1.SelectedIndex = DateTime.Now.Month - 1;
            comboBoxEdit2.SelectedIndex = DateTime.Now.Month - 1;
        }

        private void comboBoxEdit1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxEdit2.EditValue = comboBoxEdit1.EditValue;
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            if (comboBoxEdit1.EditValue == null || comboBoxEdit2.EditValue == null)
            {
                XtraMessageBox.Show("Vui lòng chọn tháng!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int tuThang = Convert.ToInt32(comboBoxEdit1.EditValue);
            int denThang = Convert.ToInt32(comboBoxEdit2.EditValue);

            if (tuThang > denThang)
            {
                XtraMessageBox.Show("Tháng bắt đầu phải nhỏ hơn hoặc bằng tháng kết thúc!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.Enabled = false;
            try
            {
                TinhGiaVonBatch_SQL(tuThang, denThang);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        // ============================================================
        // HÀM CHÍNH: TÍNH LẠI GIÁ VỐN (SQL + BULK INSERT)
        // ============================================================
        private void TinhGiaVonBatch_SQL(int tuThang, int denThang)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // ============================================================
                        // 1. KIỂM TRA DỮ LIỆU
                        // ============================================================
                        string sqlCheck = @"
                            SELECT COUNT(*) AS Total 
                            FROM ChungTu 
                            WHERE ThangCT BETWEEN @tuThang AND @denThang";

                        int total = 0;
                        using (SqlCommand cmd = new SqlCommand(sqlCheck, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@tuThang", tuThang);
                            cmd.Parameters.AddWithValue("@denThang", denThang);
                            total = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (total == 0)
                        {
                            XtraMessageBox.Show($"Không có chứng từ từ tháng {tuThang} đến tháng {denThang}!");
                            return;
                        }

                        // ============================================================
                        // 2. TÍNH GIÁ VỐN (1 SQL DUY NHẤT)
                        // ============================================================
                        DataTable dtGiaVon = TinhGiaVonTatCaVatTu_SQL(conn, tran, tuThang, denThang);

                        // ============================================================
                        // 3. LẤY DANH SÁCH CHỨNG TỪ
                        // ============================================================
                        string sqlGetCT = @"
                            SELECT * FROM ChungTu
                            WHERE ThangCT BETWEEN @tuThang AND @denThang
                              AND (MaLoai IN (2, 8) OR MaTKCo = 14038)
                            ORDER BY SoHieu, ThangCT, NgayCT, MaLoai";

                        DataTable dtCT = new DataTable();
                        using (SqlCommand cmd = new SqlCommand(sqlGetCT, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@tuThang", tuThang);
                            cmd.Parameters.AddWithValue("@denThang", denThang);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dtCT);
                            }
                        }

                        if (dtCT.Rows.Count == 0)
                        {
                            XtraMessageBox.Show("Không có chứng từ bán hàng hoặc thuế!");
                            return;
                        }

                        // ============================================================
                        // 4. XÓA DỮ LIỆU CŨ (1 CÂU LỆNH)
                        // ============================================================
                        string sqlDelete = @"
                            DELETE FROM HoaDon WHERE MaSo IN (
                                SELECT MaSo FROM ChungTu
                                WHERE ThangCT BETWEEN @tuThang AND @denThang AND MaLoai = 8
                            );
                            DELETE FROM ChungTu WHERE ThangCT BETWEEN @tuThang AND @denThang
                                AND (MaLoai IN (2,8) OR MaTKCo = 14038);";

                        using (SqlCommand cmd = new SqlCommand(sqlDelete, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@tuThang", tuThang);
                            cmd.Parameters.AddWithValue("@denThang", denThang);
                            cmd.CommandTimeout = 300;
                            cmd.ExecuteNonQuery();
                        }

                        // ============================================================
                        // 5. TẠO DATATABLE ĐỂ BULK INSERT
                        // ============================================================
                        DataTable dtChungTuMoi = dtCT.Clone();
                        DataTable dtHoaDonMoi = new DataTable();
                        dtHoaDonMoi.Columns.Add("MaSo", typeof(int));
                        dtHoaDonMoi.Columns.Add("Loai", typeof(int));
                        dtHoaDonMoi.Columns.Add("MaKhachHang", typeof(int));
                        dtHoaDonMoi.Columns.Add("KyHieu", typeof(string));
                        dtHoaDonMoi.Columns.Add("SoHD", typeof(string));
                        dtHoaDonMoi.Columns.Add("NgayPH", typeof(DateTime));
                        dtHoaDonMoi.Columns.Add("MatHang", typeof(string));
                        dtHoaDonMoi.Columns.Add("SoLuong", typeof(double));
                        dtHoaDonMoi.Columns.Add("ThanhTien", typeof(double));
                        dtHoaDonMoi.Columns.Add("TyLe", typeof(short));
                        dtHoaDonMoi.Columns.Add("HD", typeof(int));
                        dtHoaDonMoi.Columns.Add("KCT", typeof(int));
                        dtHoaDonMoi.Columns.Add("HTTT", typeof(string));
                        dtHoaDonMoi.Columns.Add("MauSo", typeof(string));
                        dtHoaDonMoi.Columns.Add("NK", typeof(int));
                        dtHoaDonMoi.Columns.Add("TS", typeof(int));
                        dtHoaDonMoi.Columns.Add("DC", typeof(int));
                        dtHoaDonMoi.Columns.Add("TyGia", typeof(double));
                        dtHoaDonMoi.Columns.Add("HDBL", typeof(int));

                        int maxct = GetMaxMaCT(conn, tran);
                        int maxMaSo = GetMaxMaSo(conn, tran);

                        // ============================================================
                        // 6. NHÓM VÀ TẠO LẠI CHỨNG TỪ
                        // ============================================================
                        var groups = dtCT.AsEnumerable()
                            .Where(r => GetInt(r, "MaLoai") == 8 || GetInt(r, "MaTKCo") == 14038)
                            .GroupBy(r => new
                            {
                                SoHieu = GetString(r, "SoHieu"),
                                ThangCT = GetInt(r, "ThangCT"),
                                NgayCT = GetDateTime(r, "NgayCT")
                            });

                        foreach (var group in groups)
                        {
                            var rows = group.ToList();

                            var rowBanHang = rows.FirstOrDefault(r => GetInt(r, "MaLoai") == 8);
                            if (rowBanHang == null) continue;

                            int maCTGoc = GetInt(rowBanHang, "MaCT");
                            if (maCTGoc == 0)
                            {
                                maxct++;
                                maCTGoc = maxct;
                            }

                            maxct++;
                            int maCTGV = maxct;
                            if (maCTGV == maCTGoc)
                            {
                                maxct++;
                                maCTGV = maxct;
                            }

                            int maSoBanHangMoi = 0;
                            var rowThue = rows.FirstOrDefault(r => GetInt(r, "MaTKCo") == 14038);

                            // Lấy hóa đơn
                            DataRow rowHD = null;
                            int maSoBanHang = GetInt(rowBanHang, "MaSo");
                            var hdRows = dtCT.Select($"MaSo = {maSoBanHang}");
                            if (hdRows.Length > 0)
                            {
                                // Lấy hóa đơn từ bảng HoaDon
                                string sqlGetHD = $"SELECT * FROM HoaDon WHERE MaSo = {maSoBanHang}";
                                DataTable dtHDTemp = new DataTable();
                                using (SqlCommand cmd = new SqlCommand(sqlGetHD, conn, tran))
                                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                                {
                                    da.Fill(dtHDTemp);
                                }
                                if (dtHDTemp.Rows.Count > 0)
                                    rowHD = dtHDTemp.Rows[0];
                            }

                            foreach (DataRow row in rows)
                            {
                                int maLoai = GetInt(row, "MaLoai");
                                int maTKCo = GetInt(row, "MaTKCo");

                                if (maLoai == 2) continue;
                                if (maTKCo == 14038) continue;

                                int maVattuHienTai = GetInt(row, "MaVattu");
                                int thangCTHienTai = GetInt(row, "ThangCT");

                                // Lấy giá vốn từ DataTable
                                double giaVon = 0;
                                DataRow[] gvRows = dtGiaVon.Select($"MaVattu = {maVattuHienTai} AND ThangCT = {thangCTHienTai}");
                                if (gvRows.Length > 0)
                                {
                                    giaVon = Convert.ToDouble(gvRows[0]["GiaVon"]);
                                }

                                // Tạo chứng từ bán hàng
                                maxMaSo++;
                                maSoBanHangMoi = maxMaSo;

                                DataRow newRow = dtChungTuMoi.NewRow();
                                CopyRow(row, newRow);
                                newRow["MaCT"] = maCTGoc;
                                newRow["MaSo"] = maxMaSo;
                                dtChungTuMoi.Rows.Add(newRow);

                                // Tạo giá vốn
                                maxMaSo++;
                                DataRow newRowGV = dtChungTuMoi.NewRow();
                                CopyRow(row, newRowGV);
                                newRowGV["MaCT"] = maCTGV;
                                newRowGV["MaSo"] = maxMaSo;
                                newRowGV["SoHieu"] = GetString(row, "SoHieu") + "GV";

                                double soPS2Co = GetDouble(row, "SoPS2Co");
                                newRowGV["SoPS"] = soPS2Co * giaVon;
                                newRowGV["SoPS2No"] = 0;
                                newRowGV["SoPS2Co"] = soPS2Co;
                                newRowGV["MaTKNo"] = 151;
                                newRowGV["MaTKCo"] = 39;
                                newRowGV["MaLoai"] = 2;
                                newRowGV["MaTKTCNo"] = 151;
                                newRowGV["MaTKTCCo"] = 39;
                                newRowGV["CT_ID"] = 500000000 + GetInt(row, "MaSo");
                                dtChungTuMoi.Rows.Add(newRowGV);
                            }

                            // Tạo chứng từ thuế
                            if (rowThue != null)
                            {
                                maxMaSo++;
                                DataRow newRowThue = dtChungTuMoi.NewRow();
                                CopyRow(rowThue, newRowThue);
                                newRowThue["MaCT"] = maCTGoc;
                                newRowThue["MaSo"] = maxMaSo;
                                dtChungTuMoi.Rows.Add(newRowThue);
                            }

                            // Tạo hóa đơn
                            if (rowHD != null)
                            {
                                DataRow newRowHD = dtHoaDonMoi.NewRow();
                                CopyRow(rowHD, newRowHD);
                                newRowHD["MaSo"] = maSoBanHangMoi;
                                dtHoaDonMoi.Rows.Add(newRowHD);
                            }
                        }

                        // ============================================================
                        // 7. BULK INSERT
                        // ============================================================
                        using (SqlBulkCopy bulkCT = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
                        {
                            bulkCT.DestinationTableName = "ChungTu";
                            bulkCT.BulkCopyTimeout = 600;
                            bulkCT.BatchSize = 5000;
                            foreach (DataColumn col in dtChungTuMoi.Columns)
                                bulkCT.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                            bulkCT.WriteToServer(dtChungTuMoi);
                        }

                        if (dtHoaDonMoi.Rows.Count > 0)
                        {
                            using (SqlBulkCopy bulkHD = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
                            {
                                bulkHD.DestinationTableName = "HoaDon";
                                bulkHD.BulkCopyTimeout = 600;
                                bulkHD.BatchSize = 5000;
                                foreach (DataColumn col in dtHoaDonMoi.Columns)
                                    bulkHD.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                                bulkHD.WriteToServer(dtHoaDonMoi);
                            }
                        }

                        // ============================================================
                        // 8. COMMIT
                        // ============================================================
                        tran.Commit();
                        XtraMessageBox.Show($"Tính giá vốn thành công! Đã xử lý {dtChungTuMoi.Rows.Count} chứng từ.");
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        XtraMessageBox.Show("Lỗi: " + ex.Message + "\n" + ex.StackTrace);
                    }
                }
            }
        }

        // ============================================================
        // TÍNH GIÁ VỐN BẰNG SQL (1 CÂU LỆNH DUY NHẤT)
        // ============================================================
        private DataTable TinhGiaVonTatCaVatTu_SQL(SqlConnection conn, SqlTransaction tran, int tuThang, int denThang)
        {
            string sql = @"
        WITH NhapXuat AS (
            SELECT
                MaVattu,
                ThangCT,
                SUM(CASE WHEN MaLoai = 1 THEN SoPS2No ELSE 0 END) AS SLNhap,
                SUM(CASE WHEN MaLoai = 1 THEN SoPS ELSE 0 END) AS TienNhap,
                SUM(CASE WHEN MaLoai = 8 THEN SoPS2Co ELSE 0 END) AS SLXuat
            FROM ChungTu
            WHERE MaVattu > 0 AND ThangCT <= @denThang
            GROUP BY MaVattu, ThangCT
        ),
        TonLuyKe AS (
            SELECT
                MaVattu,
                ThangCT,
                SLNhap,
                TienNhap,
                SLXuat,
                -- Tồn đầu tháng = Tổng nhập - Tổng xuất của các tháng trước
                ISNULL(SUM(SLNhap) OVER (PARTITION BY MaVattu ORDER BY ThangCT ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0) 
                    - ISNULL(SUM(SLXuat) OVER (PARTITION BY MaVattu ORDER BY ThangCT ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0) AS TonDauSL,
                ISNULL(SUM(TienNhap) OVER (PARTITION BY MaVattu ORDER BY ThangCT ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0) 
                    - ISNULL(SUM(SLXuat) OVER (PARTITION BY MaVattu ORDER BY ThangCT ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING) * 
                      (ISNULL(SUM(TienNhap) OVER (PARTITION BY MaVattu ORDER BY ThangCT ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0) / 
                       NULLIF(ISNULL(SUM(SLNhap) OVER (PARTITION BY MaVattu ORDER BY ThangCT ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0), 0))
                    , 0) AS TonDauTien
            FROM NhapXuat
        )
        SELECT
            MaVattu,
            ThangCT,
            ROUND(
                CASE WHEN (TonDauSL + SLNhap) > 0
                     THEN (TonDauTien + TienNhap) / (TonDauSL + SLNhap)
                     ELSE 0 
                END, 0
            ) AS GiaVon,
            TonDauSL,
            TonDauTien,
            SLNhap,
            TienNhap,
            SLXuat
        FROM TonLuyKe
        WHERE ThangCT BETWEEN @tuThang AND @denThang
        ORDER BY MaVattu, ThangCT";

            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sql, conn, tran))
            {
                cmd.Parameters.AddWithValue("@tuThang", tuThang);
                cmd.Parameters.AddWithValue("@denThang", denThang);
                cmd.CommandTimeout = 300;
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }
        // ============================================================
        // HÀM HELPER
        // ============================================================
        private int GetMaxMaCT(SqlConnection conn, SqlTransaction tran)
        {
            using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(MaCT), 0) FROM ChungTu", conn, tran))
                return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private int GetMaxMaSo(SqlConnection conn, SqlTransaction tran)
        {
            using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(MaSo), 0) FROM ChungTu", conn, tran))
                return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private int GetInt(DataRow row, string column)
        {
            if (row == null || !row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return 0;
            return Convert.ToInt32(row[column]);
        }

        private double GetDouble(DataRow row, string column)
        {
            if (row == null || !row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return 0;
            return Convert.ToDouble(row[column]);
        }

        private string GetString(DataRow row, string column)
        {
            if (row == null || !row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return "";
            return row[column].ToString();
        }

        private DateTime GetDateTime(DataRow row, string column)
        {
            if (row == null || !row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return DateTime.Now;
            return Convert.ToDateTime(row[column]);
        }

        private void CopyRow(DataRow source, DataRow dest)
        {
            foreach (DataColumn col in source.Table.Columns)
            {
                if (dest.Table.Columns.Contains(col.ColumnName))
                {
                    dest[col.ColumnName] = source[col];
                }
            }
        }
    }
}