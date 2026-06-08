using ClosedXML.Excel;
using DevExpress.Utils.Extensions;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraMap.Native;
using DevExpress.XtraWaitForm;
using Newtonsoft.Json;
using SaovietTax.Database;
using SaovietTax.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tensorflow;
using Windows.Media.Protection.PlayReady;
using static SaovietTax.frmMain;
using static SaovietTax.KTHT;

namespace SaovietTax
{
    public partial class KTHT : DevExpress.XtraEditors.XtraForm
    {
        public class Hoadonsai
        {
            public string SoHD { get; set; }
            public string KHHD { get; set; }    
            public DateTime NgayLap { get; set; }
        }
        public KTHT()
        {
            InitializeComponent();
        }
        #region Khai báo
        public frmMain frmMain;
        private List<Hoadonsai> danhsachHdSaingay=new List<Hoadonsai>();  
        public class clsKTHT
        {
            public int STT { get; set; }
            public int Type { get; set; } //1 ,2 ,3, 4, 5
            public string STTType { get; set; }
            public string KHMS { get; set; }
            public string SoHD { get; set; }
            public string KHHD { get; set; }
            public DateTime NgayLap { get; set; }
            public DateTime NgayTai { get; set; }
            public int StatusImport { get; set; }   
            public DateTime NgayNhap { get; set; }

            public string MST { get; set; }
            public string MSTHD { get; set; }

            public string TenKH { get; set; }
            public string TenKHHD { get; set; }

            public double TienTrcThue { get; set; }
            public double TienTrcThueHD { get; set; }
            public double TongTienPhi { get; set; } 
            public double TienThue { get; set; }
            public double TienThueHD { get; set; }

            public double TongTienTT { get; set; }
            public double TongTienTTHD { get; set; }
            public string GhiChu { get; set; }  
            public bool Checked { get; set; }
            public string Path { get; set; }
            public int Khautruthue { get; set; }    
        }
        List<clsKTHT> clsKTHTs = new List<clsKTHT>();
        int DV1, DV2, DV3;
        string connectionString;
        string dbPath = "";
        DataTable tbimport, ChungTu, HoaDon, KhachHang, ChungTuLQ;
        #endregion

        public void Xulyexel(string token, int type,int thang)
        {
            DateTime dtFrom = new DateTime((int)cbbNam.EditValue, thang, 1);
            DateTime dtTo = dtFrom.AddMonths(1).AddDays(-1); // Lấy ngày cuối cùng của tháng

            string formattedDate1 = dtFrom.ToString("dd/MM/yyyyTHH:mm:ss");
            string formattedDate2 = dtTo.ToString("dd/MM/yyyyTHH:mm:ss");
            //https://hoadondientu.gdt.gov.vn:30000/query/invoices/export-excel-sold?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=01/04/2025T00:00:00;tdlap=le=30/04/2025T23:59:59;ttxly==5%20%20%20%20&type=purchase

            string url = "";
            if (type == 1)
            {
                url = @"https://hoadondientu.gdt.gov.vn/api/query/invoices/export-excel-sold?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=" + formattedDate1 + ";tdlap=le=" + formattedDate2 + ";ttxly==5%20%20%20%20&type=purchase";
            }
            if (type == 2)
            {
                url = @"https://hoadondientu.gdt.gov.vn/api/query/invoices/export-excel-sold?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=" + formattedDate1 + ";tdlap=le=" + formattedDate2 + ";ttxly==6%20%20%20%20&type=purchase";
            }
            if (type == 3)
            {
                url = @"https://hoadondientu.gdt.gov.vn/api/query/invoices/export-excel-sold?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=" + formattedDate1 + ";tdlap=le=" + formattedDate2 + ";ttxly==8%20%20%20%20&type=purchase";
            }
            string filename = "";
            if (type == 1)
                filename = $"{mstcongty}_HDDienTuDaCapMa.xlsx";
            if (type == 2)
                filename = $"{mstcongty}_HDDienTuKhongMa.xlsx";
            if (type == 3)
                filename = $"{mstcongty}_HDDienTuMayTinhTien.xlsx";

         
            //Xóa tat ca file excel truc khi tải
            string currentYear = $"HD{cbbNam.EditValue}";
            string path = savedPath + @"\" + currentYear + @"\" + "HDVao" + @"\" + dtFrom.Month + @"\" + filename;
            string directoryPath = Path.Combine(savedPath, currentYear, "HDVao", dtFrom.Month.ToString());
            string deletpath = Path.Combine(directoryPath, filename);
            // Xóa tất cả các tệp Excel trong thư mục
            if (File.Exists(deletpath))
            {
                FileInfo fileInfo = new FileInfo(deletpath);
                DateTime creationDate = fileInfo.CreationTime;

                // Kiểm tra xem ngày tạo của file có nhỏ hơn ngày hiện tại hay không
                if (creationDate.Date < DateTime.Now.Date)
                {
                    // Xóa file
                  //  File.Delete(deletpath);
                    Console.WriteLine($"File '{filename}' đã được xóa.");
                }
                else
                {
                    Console.WriteLine($"File '{filename}' không thể xóa vì ngày tạo không nhỏ hơn ngày hiện tại.");
                    return;
                }
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream")); // Định dạng nhị phân 
                try
                {
                    Thread.Sleep(300); // Đợi một chút trước khi gửi yêu cầu    
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    response.EnsureSuccessStatusCode(); // Ném ngoại lệ nếu không thành công
                    var fileBytes = response.Content.ReadAsByteArrayAsync().Result;

                    // Lưu file ZIP
                    File.WriteAllBytes(path, fileBytes); // Sử dụng WriteAllBytes
                    progressPanel1.Caption = $"Đang tải file Excel tháng {thang}...";
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show($"Đã xảy ra lỗi khi tải file Excel tháng {thang}, vui lòng bấm tải lại");
                }
            }
        }
        public void Xulyexel2(string token,  int type,int thang)
        {
            DateTime dtFrom = new DateTime((int)cbbNam.EditValue, thang, 1);
            string formattedDate1 = dtFrom.ToString("dd/MM/yyyyTHH:mm:ss");
            string formattedDate2 = dtFrom.AddMonths(1).AddDays(-1).ToString("dd/MM/yyyyTHH:mm:ss");

            string url = "";
            if (type == 1)
                url = @"https://hoadondientu.gdt.gov.vn/api/query/invoices/export-excel?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=" + formattedDate1 + ";tdlap=le=" + formattedDate2;
            if (type == 2)
                url = @"https://hoadondientu.gdt.gov.vn/api/sco-query/invoices/export-excel?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=" + formattedDate1 + ";tdlap=le=" + formattedDate2;

            string filename = "";
            if (type == 1)
                filename = $"{mstcongty}_Hoadondientu.xlsx";
            if (type == 2 || type == 3)
                filename = $"{mstcongty}_HDDienTuMayTinhTien.xlsx";

            
            // Xóa tất cả file excel trước khi tải
            string currentYear = $"HD{cbbNam.EditValue}";
            string path = savedPath + @"\" + currentYear + @"\" + "HDRa" + @"\" + dtFrom.Month + @"\" + filename;
            string directoryPath = Path.Combine(savedPath, currentYear, "HDRa", dtFrom.Month.ToString());
            string deletpath = Path.Combine(directoryPath, filename);
            if (File.Exists(deletpath))
            {
                FileInfo fileInfo = new FileInfo(deletpath);
                DateTime creationDate = fileInfo.CreationTime;

                // Kiểm tra xem ngày tạo của file có nhỏ hơn ngày hiện tại hay không
                if (creationDate.Date < DateTime.Now.Date)
                {
                    // Xóa file
                  //  File.Delete(deletpath);
                    Console.WriteLine($"File '{filename}' đã được xóa.");
                }
                else
                {
                    Console.WriteLine($"File '{filename}' không thể xóa vì ngày tạo không nhỏ hơn ngày hiện tại.");
                    return;
                }
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                try
                {
                    Thread.Sleep(300); // Đợi một chút trước khi gửi yêu cầu    
                    HttpResponseMessage response = client.GetAsync(url).Result; // Sử dụng Result
                    response.EnsureSuccessStatusCode(); // Ném ngoại lệ nếu không thành công
                    var fileBytes = response.Content.ReadAsByteArrayAsync().Result; // Sử dụng Result

                    // Lưu file ZIP
                    File.WriteAllBytes(path, fileBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");
                }
            }
        }
        private void Taiexcelvao()
        {
            progressPanel1.Visible = true;
            progressPanel1.Caption = "Đang tải file Excel";
            //Lấy tháng của từ tháng
            int tuthang = int.Parse(cbbChonthang.Text.Replace("Tháng ", ""));
            int denthang = int.Parse(cbbDenthang.Text.Replace("Tháng ", ""));
            if (radDauvao.Checked)
            {
                for(int i=tuthang;i<=denthang; i++)
                {
                    Xulyexel(myTokken, 1,i);
                    Xulyexel(myTokken, 2,i);
                    Xulyexel(myTokken, 3,i);
                }
            }
            else
            {
                for (int i = tuthang; i <= denthang; i++)
                {
                    Xulyexel2(myTokken, 1,i);
                    Xulyexel2(myTokken, 2,i);
                }
                   
            }
            LoadDanhsachExcel();
            progressPanel1.Visible=false;
        }
        double TongTienExcel = 0;
        double TongTienTrcthueExcel = 0;
        private void LoadDanhsachExcel2()
        {
            progressPanel1.Visible = true;
            progressPanel1.Caption = "Đang xử lý dữ liệu";
            Application.DoEvents();
            string qr = "SELECT * FROM tbGhichuHT";
            dtGhichuht = ExecuteQuery(qr, null);
            TongTienExcel = 0;
            TongTienTrcthueExcel = 0;
            //Tải file excel về trước


            DV1 = DV2= DV3 = 0;
            clsKTHTs = new List<clsKTHT>();
            string typeHD = "";
            typeHD = radDauvao.Checked ? "HDVao" : "HDRa";
            int tuthang = int.Parse(cbbChonthang.Text.Replace("Tháng ", ""));
            int denthang= int.Parse(cbbDenthang.Text.Replace("Tháng ", ""));

            string sqlv = "SELECT DISTINCTROW KyHieu,SoHD,ChungTu.NgayCT as NgayPH,MatHang,SoLuong,ThanhTien,KhachHang.Ten,KhachHang.MST,ChungTu.SoHieu,SoPS,KhachHang.DiaChi,TyLe,HTTT,MauSo,MaCT,HoaDon.MaSo,KCT FROM  (HoaDon INNER JOIN ChungTu ON HoaDon.MaSo=ChungTu.MaSo) LEFT JOIN KhachHang ON HoaDon.MaKhachHang=KhachHang.MaSo  WHERE Loai=-1 AND HD=1 AND  (ThangCT>=? AND ThangCT<=?)  AND (HDBL=0 OR KCT=0) AND (HoaDon.DC=0 OR HD=1) ORDER BY NgayPH,MaCT";
            var parameters = new SqlParameter[]
                     {
            new SqlParameter("?",tuthang),
            new SqlParameter("?",denthang),
                     };
            var kqvao = ExecuteQuery(sqlv, parameters);

            //Ra
            string sqlr = "SELECT DISTINCTROW HoaDon.KyHieu,SoHD,ChungTu.NgayCT as NgayPH,MatHang,SoLuong,ThanhTien,KhachHang.Ten,KhachHang.MST,ChungTu.SoHieu,IIF(TK_ID=3007,SoPS,-SoPS) AS Thue,ChungTu.MauSoHD as DiaChi,TyLe,HTTT,MauSo,MaCT,KCT FROM  ((HoaDon INNER JOIN ChungTu ON HoaDon.MaSo=ChungTu.MaSo) LEFT JOIN HethongTK ON ChungTu.MaTKCo=HethongTK.MaSo) LEFT JOIN KhachHang ON HoaDon.MaKhachHang=KhachHang.MaSo  WHERE HoaDon.Loai=1 AND  (ThangCT>=? AND ThangCT<=?)  AND (HoaDon.DC=0 OR HD=1) ORDER BY NgayPH";
            parameters = new SqlParameter[]
                    {
            new SqlParameter("?",tuthang),
            new SqlParameter("?",denthang),
                    };
            var kqra = ExecuteQuery(sqlr, parameters);

            for (int i=tuthang;i<=denthang;i++)
            {
                string directoryPath = Path.Combine(savedPath, typeHD,i.ToString()).Trim();


                if (Directory.Exists(directoryPath))
                {
                    var excelFiles = Directory.EnumerateFiles(directoryPath, "*.xlsx", SearchOption.AllDirectories).ToList();

                    int STT = 1;
                    int totalrow = 0;
                    int dong = 1;

                    foreach (var excelFile in excelFiles)
                    {
                        if (!excelFile.Contains(mstcongty))
                        {
                            continue;
                        }
                        using (var workbook = new XLWorkbook(excelFile))
                        {
                            var worksheet = workbook.Worksheet(1); // Lấy sheet đầu tiên
                            foreach (var row in worksheet.RowsUsed().Skip(3)) {
                                totalrow += 1;
                            }

                        }

                    }
                        foreach (var excelFile in excelFiles)
                    {
                        if (!excelFile.Contains(mstcongty))
                        {
                            continue;
                        }
                        using (var workbook = new XLWorkbook(excelFile))

                        {
                            var worksheet = workbook.Worksheet(1); // Lấy sheet đầu tiên
                           
                            foreach (var row in worksheet.RowsUsed().Skip(3)) // Bỏ qua 6 hàng đầu tiên
                            {
                                clsKTHT clsKTHT = new clsKTHT();
                                progressPanel1.Caption = $"Đang đọc dòng thứ {dong}/{totalrow}";
                                Application.DoEvents();
                                if (typeHD == "HDVao")
                                {
                                    if (excelFile.Contains("HDDienTuDaCapMa"))
                                    {
                                        DV1 += 1;
                                        clsKTHT.Type = 1;
                                        clsKTHT.STTType = "";
                                    }
                                    else
                                    {
                                        if (excelFile.Contains("HDDienTuKhongMa"))
                                        {
                                            DV2 += 1;
                                            clsKTHT.Type = 2;
                                            clsKTHT.STTType = "";
                                        }
                                        else
                                        {
                                            DV3 += 1;
                                            clsKTHT.Type = 3;
                                            clsKTHT.STTType = "";
                                        }
                                    }
                                }
                                else
                                {
                                    if (excelFile.Contains("Hoadondientu"))
                                    {
                                        DV1 += 1;
                                        clsKTHT.Type = 4;
                                        clsKTHT.STTType = "";
                                    }
                                    else
                                    {
                                        DV2 += 1;
                                        clsKTHT.Type = 5;
                                        clsKTHT.STTType = "";
                                    }
                                }

                                clsKTHT.STT = STT;
                                clsKTHT.GhiChu = "";
                                clsKTHT.KHMS = row.Cell("B").Value.ToString();
                                clsKTHT.KHHD = row.Cell("C").Value.ToString();
                                clsKTHT.SoHD = row.Cell("D").Value.ToString();
                                if (clsKTHT.SoHD == "16198")
                                {
                                    int ao = 10;
                                }
                                if (clsKTHT.SoHD == "1")
                                {
                                    int a = 10;
                                }
                                if (radDauvao.Checked)
                                {
                                    clsKTHT.MST = row.Cell("F").Value.ToString();
                                    clsKTHT.TongTienPhi = row.Cell("N").Value.ToString() != "" ? Math.Round(double.Parse(row.Cell("N").Value.ToString())) : 0;
                                }
                                else
                                    clsKTHT.MST = row.Cell("H").Value.ToString();
                                clsKTHT.TenKH = row.Cell("G").Value.ToString();
                                clsKTHT.NgayLap = DateTime.Parse(row.Cell("E").Value.ToString());

                                //Kiểm tra xem đã có tải hoá đơn chưa
                                DataRow getrow = tbimport.AsEnumerable().ToList()
        .Where(m => Helpers.RemoveLeadingZeros(m.Field<string>("SHDon")) == Helpers.RemoveLeadingZeros(clsKTHT.SoHD)
                     && m.Field<DateTime>("NLap").ToString("dd/MM/yy") == clsKTHT.NgayLap.ToString("dd/MM/yy"))
        .FirstOrDefault();

                                if (getrow != null)
                                {
                                    clsKTHT.Path = getrow.Field<string>("Path") != null ? getrow.Field<string>("Path").ToString() : "";
                                    clsKTHT.StatusImport = int.Parse(getrow["Status"].ToString());
                                    clsKTHT.NgayTai = DateTime.Parse(getrow["NgayTao"].ToString());
                                    clsKTHT.Khautruthue= getrow["Khautruthue"] != null ? int.Parse(getrow["Khautruthue"].ToString()) : 0;
                                }
                                //Lấy ra danh sách hoadontruoc

                                //Kiểm tra hoá đơn đã nhập chưa
                                var getHD = (from c in ChungTu.AsEnumerable()
                                             where c.Field<DateTime>("NgayCT").Date == clsKTHT.NgayLap.Date
                                                 && Helpers.RemoveLeadingZeros(c["SoHieu"].ToString()).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.') 
                                             select new { ChungTu = c }).ToList();
                                getHD = getHD.Distinct().ToList();

                                var tt2 = (from h in HoaDon.AsEnumerable()
                                          join c in ChungTu.AsEnumerable()
                                          on h.Field<int>("MaSo") equals c.Field<int>("MaSo")
                                          where c.Field<DateTime>("NgayCT").Date == clsKTHT.NgayLap.Date
                                              //&& h["KyHieu"].ToString() == clsKTHT.KHHD
                                              && Helpers.RemoveLeadingZeros(h["SoHD"].ToString()).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.')
                                          select new {ChungTu=c, Hoadon = h }).ToList();

                                string getmstF = row.Cell("F").Value.ToString();
                                var getcusf = KhachHang.AsEnumerable().Where(m => m.Field<string>("MST") == getmstF).FirstOrDefault();
                                //var checkhd = tt2.ToList().Where(m => m.Hoadon.Field<int>("MaKhachHang") == getcusf.Field<int>("MaSo")).ToList(); 
                                if (getHD != null && getHD.Count > 0)
                                {
                                    clsKTHT.NgayNhap = getHD.FirstOrDefault().ChungTu.Field<DateTime>("NgayCT");
                                    clsKTHT.Checked = true;
                                    //Lấy tiền truoc thuê
                                    if (radDauvao.Checked)
                                    {
                                        if (clsKTHT.SoHD == "7294")
                                        {
                                            int a = 10;
                                        }
                                        //Kiểm tra xem có phải 711 không
                                        //if (getHD.AsEnumerable().Any(m => m.ChungTu.Field<int>("MaTKCo") == 169))
                                        //{
                                        //    double tien = 0;
                                        //    tien = getHD.AsEnumerable().Where(m => m.ChungTu.Field<int>("MaTKNo") != 5108 && m.ChungTu.Field<int>("MaTKCo") == 0).Distinct().ToList().Sum(m => m.ChungTu.Field<double>("SoPS"));
                                        //    double tien711 = getHD.AsEnumerable().Where(m => m.ChungTu.Field<int>("MaTKNo") != 5108 && m.ChungTu.Field<int>("MaTKCo") == 169).Distinct().ToList().Sum(m => m.ChungTu.Field<double>("SoPS"));
                                        //    clsKTHT.TienTrcThueHD = tien - tien711;
                                        //}
                                        //else
                                        //{
                                        //    clsKTHT.TienTrcThueHD = getHD.AsEnumerable().Where(m => m.ChungTu.Field<int>("MaTKNo") != 5108 && m.ChungTu.Field<int>("MaTKCo")!=0).Distinct().ToList().Sum(m => m.ChungTu.Field<double>("SoPS"));
                                        //}

                                        //clsKTHT.TienThueHD = getHD.AsEnumerable().Where(m => m.ChungTu.Field<int>("MaTKNo") == 5108).Distinct().ToList().Sum(m => m.ChungTu.Field<double>("SoPS"));
                                        //clsKTHT.TongTienTTHD = clsKTHT.TienTrcThueHD + clsKTHT.TienThueHD;

                                        var getdata = kqvao.AsEnumerable().Where(m => Helpers.RemoveLeadingZeros(m.Field<string>("SoHD")).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.') && m.Field<DateTime>("NgayPH") == clsKTHT.NgayLap && m.Field<string>("MST")== getmstF).ToList();
                                        if (getdata != null)
                                        {
                                            clsKTHT.TienTrcThueHD = getdata.Sum(m=>m.Field<double>("ThanhTien"));
                                            clsKTHT.TienThueHD = getdata.Sum(m => m.Field<double>("SoPS"));
                                            clsKTHT.TongTienTTHD = clsKTHT.TienTrcThueHD + clsKTHT.TienThueHD;
                                        }
                                    }
                                    else
                                    {
                                        var getdata = kqra.AsEnumerable().Where(m => Helpers.RemoveLeadingZeros(m.Field<string>("SoHD")).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.') && m.Field<DateTime>("NgayPH") == clsKTHT.NgayLap).ToList();
                                        if (getdata != null)
                                        {
                                            clsKTHT.TienTrcThueHD = getdata.Sum(m => m.Field<double>("ThanhTien"));
                                            clsKTHT.TienThueHD = getdata.Sum(m => m.Field<double>("Thue"));
                                            clsKTHT.TongTienTTHD = clsKTHT.TienTrcThueHD + clsKTHT.TienThueHD;
                                        }

                                        //clsKTHT.TienTrcThueHD = getHD.AsEnumerable().Where(m => m.ChungTu.Field<int>("MaTKCo") != 14038 && m.ChungTu.Field<int>("MaTKCo")!=0).Distinct().ToList().Sum(m => m.ChungTu.Field<double>("SoPS"));
                                        //clsKTHT.TienThueHD = getHD.AsEnumerable().Where(m => m.ChungTu.Field<int>("MaTKCo") == 14038).Distinct().ToList().Sum(m => m.ChungTu.Field<double>("SoPS"));
                                        //clsKTHT.TongTienTTHD = clsKTHT.TienTrcThueHD + clsKTHT.TienThueHD;
                                    }
                                    //Nếu tiền âm thì bỏ check

                                    //Tìm khách hàng
                                    string loaict;
                                    if (radDauvao.Checked)
                                        loaict = "1,0,4";
                                    else
                                        loaict = "8";
                                        var tt = (from h in HoaDon.AsEnumerable()
                                                  join c in ChungTu.AsEnumerable()
                                                  on h.Field<int>("MaSo") equals c.Field<int>("MaSo")
                                                  where c.Field<DateTime>("NgayCT").Date == clsKTHT.NgayLap.Date
                                                      //&& h["KyHieu"].ToString() == clsKTHT.KHHD
                                                      && Helpers.RemoveLeadingZeros(h["SoHD"].ToString()).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.')
                                                      && loaict.ToString().Contains(c["MaLoai"].ToString())
                                                  select new { Hoadon = h }).ToList();

                                    if (tt != null && tt.Count > 0)
                                    {
                                        if (radDauvao.Checked)
                                        {
                                           
                                            if (getcusf != null)
                                            {
                                                var checkany = tt.ToList().Where(m => m.Hoadon.Field<int>("MaKhachHang") == getcusf.Field<int>("MaSo")).FirstOrDefault();
                                                if(checkany != null)
                                                {
                                                    clsKTHT.MSTHD = getcusf.Field<string>("MST");
                                                    clsKTHT.TenKHHD = Helpers.ConvertVniToUnicode(getcusf.Field<string>("Ten"));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var fkh = KhachHang.AsEnumerable().Where(m => m.Field<int>("MaSo") == tt.FirstOrDefault().Hoadon.Field<int>("MaKhachHang")).FirstOrDefault();
                                            if (fkh != null)
                                            {
                                                clsKTHT.MSTHD = fkh.Field<string>("MST");
                                                clsKTHT.TenKHHD = Helpers.ConvertVniToUnicode(fkh.Field<string>("Ten"));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        int a = 10;
                                    }
                                }
                                else
                                    clsKTHT.Checked = false;

                                var cellK6 = worksheet.Cell("K6").Value.ToString();

                                if (cellK6 == "Căn cước công dân")
                                {
                                    if (!string.IsNullOrEmpty(row.Cell("L").Value.ToString()))
                                        clsKTHT.TienTrcThue = Math.Round(double.Parse(row.Cell("L").Value.ToString()));
                                    if (!string.IsNullOrEmpty(row.Cell("M").Value.ToString()))
                                        clsKTHT.TienThue = Math.Round(double.Parse(row.Cell("M").Value.ToString()));
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(row.Cell("K").Value.ToString()))
                                        clsKTHT.TienTrcThue = Math.Round(double.Parse(row.Cell("K").Value.ToString()));
                                    if (!string.IsNullOrEmpty(row.Cell("L").Value.ToString()))
                                        clsKTHT.TienThue = Math.Round(double.Parse(row.Cell("L").Value.ToString()));
                                }
                                clsKTHT.TongTienTT = double.Parse(row.Cell("O").Value.ToString());
                                if (clsKTHT.Checked)
                                    TongTienExcel += clsKTHT.TongTienTT;
                                //if (clsKTHT.TienTrcThue < 0)
                                //    clsKTHT.Checked = false;
                                //Điền ghi chú
                                if (clsKTHT.Checked)
                                {
                                    if (clsKTHT.TienTrcThue != clsKTHT.TienTrcThueHD)
                                    {
                                        if (clsKTHT.TienTrcThue != 0)
                                        {
                                            if (clsKTHT.TienTrcThue > clsKTHT.TienTrcThueHD)
                                                clsKTHT.GhiChu += $"Tiền trước thuế bị lệch {clsKTHT.TienTrcThue - clsKTHT.TienTrcThueHD} đ  Tiền gốc là  {clsKTHT.TienTrcThue} đ";
                                            else
                                                // Giả sử TienThueHD và TienThue là kiểu decimal hoặc double
                                                clsKTHT.GhiChu += $"Tiền trước thuế bị lệch: {(clsKTHT.TienTrcThueHD - clsKTHT.TienTrcThue):N0} Tiền gốc là: {clsKTHT.TienTrcThue:N0} đ";
                                        }
                                        //Trường hợp k thuế và lệch tổng tiền

                                        else
                                        {
                                            if(clsKTHT.TienTrcThueHD!= clsKTHT.TongTienTT)
                                            {
                                                clsKTHT.GhiChu += $"Tiền trước thuế bị lệch: {(clsKTHT.TongTienTT - clsKTHT.TienTrcThueHD):N0} Tiền gốc là: {clsKTHT.TongTienTT:N0} đ";
                                            }
                                        }
                                        
                                    }
                                    if ((clsKTHT.TienThue != 0 && clsKTHT.TienThueHD != 0) && clsKTHT.TienThue != clsKTHT.TienThueHD)
                                    {
                                        if (clsKTHT.TienThue > clsKTHT.TienThueHD)
                                            clsKTHT.GhiChu += $"Tiền thuế bị lệch {clsKTHT.TienThue - clsKTHT.TienThueHD} đ  Tiền gốc là  {clsKTHT.TienThue} đ";
                                        else
                                            // Giả sử TienThueHD và TienThue là kiểu decimal hoặc double
                                            clsKTHT.GhiChu += $"Tiền thuế bị lệch: {(clsKTHT.TienThueHD - clsKTHT.TienThue):N0} Tiền gốc là: {clsKTHT.TienThue:N0} đ";
                                    }
                                    if (!string.IsNullOrEmpty(clsKTHT.MST) && !string.IsNullOrEmpty(clsKTHT.MSTHD) && clsKTHT.MST != clsKTHT.MSTHD)
                                    {
                                        clsKTHT.GhiChu += $"MST thuế không đúng, MST gốc là {clsKTHT.MST}"; 
                                    }
                                }
                                else
                                {
                                    var checkds = dtGhichuht.AsEnumerable().Where(m => Helpers.RemoveLeadingZeros(m.Field<string>("SoHD")).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.') && m.Field<DateTime>("NgayLap").Date == clsKTHT.NgayLap.Date).FirstOrDefault();
                                    if(checkds != null)
                                    {
                                        clsKTHT.GhiChu += checkds.Field<string>("Noidung");
                                    }
                                    else
                                    {
                                        //Kiểm tra lấy từ ghi chú tbimport
                                        var checkimport=tbimport.AsEnumerable().Where(m => Helpers.RemoveLeadingZeros(m.Field<string>("SHDon")).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.') && m.Field<DateTime>("NLap").Date == clsKTHT.NgayLap.Date).FirstOrDefault();
                                        if (checkimport != null)
                                        {
                                            if (checkimport["Status"].ToString() !="3")
                                                clsKTHT.GhiChu = "Hoá đơn chưa được nhập";
                                            else
                                            {
                                                //if (getHD.Count > 0)
                                                //    clsKTHT.GhiChu = Helpers.ConvertVniToUnicode(checkimport.Field<string>("Noidung"));
                                                //else
                                                //    clsKTHT.GhiChu = "Hoá đơn chưa được nhập";
                                                clsKTHT.GhiChu = Helpers.ConvertVniToUnicode(checkimport.Field<string>("Noidung"));
                                            }
                                            //clsKTHT.GhiChu = checkimport.Field<string>("Noidung");
                                        }
                                        else
                                        {
                                            clsKTHT.GhiChu = "Hoá đơn chưa được nhập";
                                        }
                                       
                                    }
                                }
                                if (getHD.Count == 1)
                                {
                                    clsKTHT.GhiChu = "Hoá đơn bị thiếu thông tin hàng";
                                }

                                 STT += 1;
                                clsKTHTs.Add(clsKTHT);
                                dong += 1;
                                if (dong >= totalrow)
                                    dong = totalrow;
                            }
                           
                        }
                    }
                }

            }


            clsKTHTs = clsKTHTs.OrderBy(m => m.NgayLap).ToList();
            //Lập lại stt
            int stt = 1;
            foreach(var it in clsKTHTs)
            {
                it.STT = stt;
                stt += 1;
            }
            double sumtt = clsKTHTs.Where(m=>m.Checked).Sum(m => m.TongTienTTHD);
            clsKTHT clsKTHT2 = new clsKTHT();
            clsKTHT2.TenKHHD = "Tổng tiền";
            clsKTHT2.TienTrcThueHD = clsKTHTs.Where(m => m.Checked).Sum(m => m.TienTrcThueHD);
            clsKTHT2.TienThueHD = clsKTHTs.Where(m => m.Checked).Sum(m => m.TienThueHD);
            clsKTHT2.TongTienTTHD = sumtt;
            //clsKTHT2.TongTienTT = TongTienExcel;
           //clsKTHTs.Add(clsKTHT2);

            clsKTHT clsKTHT2ex = new clsKTHT();
            clsKTHT2ex.TenKHHD = "Tổng tiền excel";
            // clsKTHT2ex.TongTienTTHD = sumtt;
            clsKTHT2ex.TienTrcThueHD = clsKTHTs.Where(m => m.Checked).Sum(m => m.TienTrcThue);
            clsKTHT2ex.TienThueHD = clsKTHTs.Where(m => m.Checked).Sum(m => m.TienThue);
            clsKTHT2ex.TongTienTTHD = TongTienExcel;

            //Thêm hoá đơn thừa
            int typecheck = 0;
            if (radDauvao.Checked)
                typecheck = 1;
            else
                typecheck = 8;
                var gethoadontheothang = ChungTu.AsEnumerable().Where(m => m["MaLoai"].ToString() == typecheck.ToString()).OrderByDescending(m => m.Field<DateTime>("NgayCT")).Where(m => m.Field<DateTime>("NgayCT").Month >= tuthang && m.Field<DateTime>("NgayCT").Month <= denthang && !m.Field<string>("SoHieu").Contains("GV") ).GroupBy(m => m.Field<string>("SoHieu")).Select(g => g.First())  // Lấy bản ghi đầu tiên của mỗi nhóm
        .ToList(); ;
            var getdifferent = gethoadontheothang
     .Where(m => !clsKTHTs.Any(n => Helpers.RemoveLeadingZeros(n.SoHD).TrimEnd(',') == Helpers.RemoveLeadingZeros(m.Field<string>("SoHieu")).TrimEnd('.'))).ToList();

            foreach (DataRow g in getdifferent)
            {
                clsKTHT gplus = new clsKTHT();
                gplus.SoHD = g.Field<string>("SoHieu");
                gplus.NgayLap= g.Field<DateTime>("NgayCT");
                gplus.GhiChu = "Hoá đơn nhập dư";
                clsKTHTs.Add(gplus);
            }


            // clsKTHTs.Add(clsKTHT2ex);
            gridControl1.DataSource = clsKTHTs;
            gridControl1.RefreshDataSource();
            lblResult3.Text = $"{clsKTHT2.TienTrcThueHD.ToString("N0")}";
            lblResult2.Text = $"{clsKTHT2ex.TienTrcThueHD.ToString("N0")}";
            labelControl4.Text = $"{clsKTHT2.TienThueHD.ToString("N0")}";
            labelControl5.Text = $"{clsKTHT2ex.TienThueHD.ToString("N0")}";
            lbltshd1.Text = clsKTHTs.Where(m=>m.Checked).Count().ToString();
            lbltshd2.Text = clsKTHTs.Where(m=>m.STT!=0).Count().ToString();

            //lấy từ vb
            //Nếu là đầu vào
            //if (radDauvao.Checked)
            //{
            //    string sqlvao = "SELECT DISTINCTROW KyHieu,SoHD,ChungTu.NgayCT as NgayPH,MatHang,SoLuong,ThanhTien,KhachHang.Ten,KhachHang.MST,ChungTu.SoHieu,SoPS,KhachHang.DiaChi,TyLe,HTTT,MauSo,MaCT,HoaDon.MaSo,KCT FROM  (HoaDon INNER JOIN ChungTu ON HoaDon.MaSo=ChungTu.MaSo) LEFT JOIN KhachHang ON HoaDon.MaKhachHang=KhachHang.MaSo  WHERE Loai=-1 AND HD=1 AND  (ThangCT>=? AND ThangCT<=?)  AND (HDBL=0 OR KCT=0) AND (HoaDon.DC=0 OR HD=1) ORDER BY NgayPH,MaCT";
            //     parameters = new OleDbParameter[]
            //             {
            //new OleDbParameter("?",tuthang),
            //new OleDbParameter("?",denthang),
            //             };
            //    var kq = ExecuteQuery(sqlvao, parameters);
            //    double sumTientrcthuevb = kq.AsEnumerable().Sum(m => m.Field<double>("ThanhTien"));
            //    double sumTienThuevb = kq.AsEnumerable().Sum(m => m.Field<double>("SoPS"));
            //    lblResult1.Text = $"Tổng tiền trước thuế {sumTientrcthuevb.ToString("N0")} | Tổng tiền thuế {sumTienThuevb.ToString("N0")}";
            //    lblResult3.Text = $"Tổng tiền trước thuế {clsKTHT2.TienTrcThueHD.ToString("N0")} | Tổng tiền thuế {clsKTHT2.TienThueHD.ToString("N0")}";
            //    lblResult2.Text = $"Tổng tiền trước thuế {clsKTHT2ex.TienTrcThueHD.ToString("N0")} | Tổng tiền thuế {clsKTHT2ex.TienThueHD.ToString("N0")}";
            //}
            //else
            //{
            //    string sqlra = "SELECT DISTINCTROW HoaDon.KyHieu,SoHD,ChungTu.NgayCT as NgayPH,MatHang,SoLuong,ThanhTien,KhachHang.Ten,KhachHang.MST,ChungTu.SoHieu,IIF(TK_ID=3007,SoPS,-SoPS) AS Thue,ChungTu.MauSoHD as DiaChi,TyLe,HTTT,MauSo,MaCT,KCT FROM  ((HoaDon INNER JOIN ChungTu ON HoaDon.MaSo=ChungTu.MaSo) LEFT JOIN HethongTK ON ChungTu.MaTKCo=HethongTK.MaSo) LEFT JOIN KhachHang ON HoaDon.MaKhachHang=KhachHang.MaSo  WHERE HoaDon.Loai=1 AND  (ThangCT>=? AND ThangCT<=?)  AND (HoaDon.DC=0 OR HD=1) ORDER BY NgayPH";
            //     parameters = new OleDbParameter[]
            //             {
            //new OleDbParameter("?",tuthang),
            //new OleDbParameter("?",denthang),
            //             };
            //    var kq = ExecuteQuery(sqlra, parameters);
            //    double sumTientrcthuevb = kq.AsEnumerable().Sum(m => m.Field<double>("ThanhTien"));
            //    double sumTienThuevb = kq.AsEnumerable().Sum(m => m.Field<double>("Thue"));
            //    lblResult1.Text = $"Tổng tiền trước thuế {sumTientrcthuevb.ToString("N0")} | Tổng tiền thuế {sumTienThuevb.ToString("N0")}";
            //    lblResult3.Text = $"Tổng tiền trước thuế {clsKTHT2.TienTrcThueHD.ToString("N0")} | Tổng tiền thuế {clsKTHT2.TienThueHD.ToString("N0")}";
            //    lblResult2.Text = $"Tổng tiền trước thuế {clsKTHT2ex.TienTrcThueHD.ToString("N0")} | Tổng tiền thuế {clsKTHT2ex.TienThueHD.ToString("N0")}";
            //}
            progressPanel1.Visible = false;

        }
        private void LoadDanhsachExcel()
        {
            if (mstcongty == "8046549703")
                mstcongty = "048172000197";
            progressPanel1.Visible = true;
            progressPanel1.Caption = "Đang xử lý dữ liệu";
            Application.DoEvents();

            try
            {
                // Load ghi chú hệ thống
                dtGhichuht = ExecuteQuery("SELECT * FROM tbGhichuHT", null);

                TongTienExcel = 0;
                DV1 = DV2 = DV3 = 0;
                clsKTHTs = new List<clsKTHT>();

                string typeHD = radDauvao.Checked ? "HDVao" : "HDRa";
                int tuthang = int.Parse(cbbChonthang.Text.Replace("Tháng ", ""));
                int denthang = int.Parse(cbbDenthang.Text.Replace("Tháng ", ""));
                string TTHD = "";
                // Query dữ liệu từ DB để đối chiếu
                var parameters = new SqlParameter[] { new SqlParameter("@TuThang", tuthang), new SqlParameter("@DenThang", denthang) };

                DataTable kqvao = radDauvao.Checked
                    ? ExecuteQuery(
                        "SELECT DISTINCT KyHieu,SoHD,ChungTu.NgayCT as NgayPH,MatHang,SoLuong,ThanhTien,KhachHang.Ten,KhachHang.MST,ChungTu.SoHieu,SoPS,KhachHang.DiaChi,TyLe,HTTT,MauSo,MaCT,HoaDon.MaSo,KCT " +
                        "FROM (HoaDon INNER JOIN ChungTu ON HoaDon.MaSo=ChungTu.MaSo) LEFT JOIN KhachHang ON HoaDon.MaKhachHang=KhachHang.MaSo " +
                        "WHERE Loai=-1 AND HD=1 AND (ThangCT>= @TuThang  AND ThangCT<= @DenThang) AND (HDBL=0 OR KCT=0) AND (HoaDon.DC=0 OR HD=1) ORDER BY NgayPH,MaCT", parameters)
                    : null;

                DataTable kqra = !radDauvao.Checked
                    ? ExecuteQuery(
                        "SELECT DISTINCT HoaDon.KyHieu,SoHD,ChungTu.NgayCT as NgayPH,MatHang,SoLuong,ThanhTien,KhachHang.Ten,KhachHang.MST,ChungTu.SoHieu,IIF(TK_ID=3007,SoPS,-SoPS) AS Thue,ChungTu.MauSoHD as DiaChi,TyLe,HTTT,MauSo,MaCT,KCT " +
                        "FROM ((HoaDon INNER JOIN ChungTu ON HoaDon.MaSo=ChungTu.MaSo) LEFT JOIN HethongTK ON ChungTu.MaTKCo=HethongTK.MaSo) LEFT JOIN KhachHang ON HoaDon.MaKhachHang=KhachHang.MaSo " +
                        "WHERE HoaDon.Loai=1 AND (ThangCT>= @TuThang AND ThangCT<= @DenThang) AND (HoaDon.DC=0 OR HD=1) ORDER BY NgayPH", parameters)
                    : null;

                int STT = 1;
                long totalRows = 0; // Tổng dòng cần xử lý (cho progress)
                long currentRow = 0;

                // Bước 1: Đếm tổng số dòng dữ liệu trong tất cả file Excel hợp lệ
                for (int i = tuthang; i <= denthang; i++)
                {
                    string CurrentYear = $"HD{cbbNam.EditValue}";
                    string directoryPath = Path.Combine(savedPath, CurrentYear, typeHD, i.ToString()).Trim();
                    if (!Directory.Exists(directoryPath)) continue;

                    var excelFiles = Directory.EnumerateFiles(directoryPath, "*.xlsx", SearchOption.AllDirectories)
                                             .Where(f => f.Contains(mstcongty) || f.Contains(CCCD))
                                             .ToList();
                    if(excelFiles.Count==0)
                    {
                        XtraMessageBox.Show($"Không tìm thấy file Excel trong thư mục {directoryPath}, vui lòng bấm cập nhật file Excel", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        
                    }
                    foreach (var file in excelFiles)
                    {
                        using (var workbook = new XLWorkbook(file))
                        {
                            var worksheet = workbook.Worksheet(1);
                            totalRows += worksheet.RowsUsed().Skip(3).Count();
                        }
                    }
                }

                // Bước 2: Xử lý từng file và từng dòng
                for (int i = tuthang; i <= denthang; i++)
                {
                    string CurrentYear = $"HD{cbbNam.EditValue}";
                    string directoryPath = Path.Combine(savedPath, CurrentYear, typeHD, i.ToString()).Trim();
                    if (!Directory.Exists(directoryPath)) continue;

                    var excelFiles = Directory.EnumerateFiles(directoryPath, "*.xlsx", SearchOption.AllDirectories)
                                             .Where(f => f.Contains(mstcongty) || f.Contains(CCCD))
                                             .ToList();

                    foreach (var excelFile in excelFiles)
                    {
                        using (var workbook = new XLWorkbook(excelFile))
                        {
                            var worksheet = workbook.Worksheet(1);

                            foreach (var row in worksheet.RowsUsed().Skip(3))
                            {
                                try
                                {
                                    currentRow++;
                                    if (currentRow % 50 == 0 || currentRow == totalRows) // Giảm tần suất DoEvents
                                    {
                                        progressPanel1.Caption = $"Đang đọc dòng thứ {currentRow:N0}/{totalRows:N0}";
                                        Application.DoEvents();
                                    }

                                    var clsKTHT = new clsKTHT { STT = STT++, GhiChu = "" };

                                    // Xác định loại hóa đơn và đếm DV
                                    if (typeHD == "HDVao")
                                    {
                                        if (excelFile.Contains("HDDienTuDaCapMa")) {
                                            DV1++; clsKTHT.Type = 1; 
                                            TTHD= GetCellValue(row.Cell("R")).ToString();
                                            clsKTHT.STTType = TTHD;
                                        }
                                        else if (excelFile.Contains("HDDienTuKhongMa")) {
                                            DV2++; clsKTHT.Type = 2; 
                                            TTHD = GetCellValue(row.Cell("R")).ToString();
                                            clsKTHT.STTType = TTHD;
                                        }
                                        else { 
                                            DV3++; clsKTHT.Type = 3;
                                            TTHD = GetCellValue(row.Cell("P")).ToString();
                                            clsKTHT.STTType = TTHD;
                                        }
                                    }
                                    else
                                    {
                                        if (excelFile.Contains("Hoadondientu")) { DV1++; clsKTHT.Type = 4;
                                            TTHD = GetCellValue(row.Cell("R")).ToString();
                                            clsKTHT.STTType = TTHD; }
                                        else { DV2++; clsKTHT.Type = 5;
                                            TTHD = GetCellValue(row.Cell("P")).ToString();
                                            clsKTHT.STTType = TTHD; }
                                    }

                                    // Đọc dữ liệu từ Excel
                                    clsKTHT.KHMS = GetCellValue(row.Cell("B"));
                                    clsKTHT.KHHD = GetCellValue(row.Cell("C"));
                                    clsKTHT.SoHD = GetCellValue(row.Cell("D"));
                                    if (clsKTHT.SoHD == "104" && clsKTHT.KHHD == "C26MAA")
                                    {
                                        int a = 10;
                                    }
                                    clsKTHT.TenKH = GetCellValue(row.Cell("G"));
                                    clsKTHT.NgayLap = DateTime.Parse(GetCellValue(row.Cell("E")));

                                    clsKTHT.MST = radDauvao.Checked
                                        ? GetCellValue(row.Cell("F"))
                                        : GetCellValue(row.Cell("H"));




                                    if (radDauvao.Checked && double.TryParse(GetCellValue(row.Cell("N")), out double phi))
                                        clsKTHT.TongTienPhi = Math.Round(phi);

                                    // Đọc tiền từ Excel
                                    string k6 = worksheet.Cell("K6").Value.ToString();
                                    string colTrcThue = k6 == "Căn cước công dân" ? "L" : "K";
                                    string colThue = k6 == "Căn cước công dân" ? "M" : "L";

                                    clsKTHT.TienTrcThue = ParseDouble(row.Cell(colTrcThue));
                                    clsKTHT.TienThue = ParseDouble(row.Cell(colThue));
                                    clsKTHT.TongTienTT = ParseDouble(row.Cell("O"));
                                    if (clsKTHT.Checked) TongTienExcel += clsKTHT.TongTienTT;

                                    // Kiểm tra đã import chưa
                                    var imported = tbimport.AsEnumerable()
                                        .FirstOrDefault(m => Helpers.RemoveLeadingZeros(m.Field<string>("SHDon")) == Helpers.RemoveLeadingZeros(clsKTHT.SoHD)
                                                          && m.Field<DateTime>("NLap").Date == clsKTHT.NgayLap.Date);

                                    if (imported != null)
                                    {
                                        clsKTHT.Path = imported.Field<string>("Path") ?? "";
                                        clsKTHT.StatusImport = Convert.ToInt32(imported["Status"]);
                                        clsKTHT.NgayTai = DateTime.Parse(imported["NgayTao"].ToString());
                                        clsKTHT.Khautruthue = !string.IsNullOrEmpty(imported["Khautruthue"].ToString()) ? int.Parse(imported["Khautruthue"].ToString()) : 0;
                                    }

                                    // Tìm hóa đơn trong hệ thống
                                    List<DataRow> matchedHD = new List<DataRow>();
                                    if (radDauvao.Checked)
                                    {
                                        //matchedHD= kqvao.AsEnumerable()
                                        //.Where(m => Helpers.RemoveLeadingZeros(m.Field<string>("SoHD")).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.')
                                        //         && m.Field<DateTime>("NgayPH").Date == clsKTHT.NgayLap.Date && m.Field<string>("KyHieu") == clsKTHT.KHHD)
                                        //.ToList();
                                        matchedHD = kqvao.AsEnumerable()
                                      .Where(m => Helpers.RemoveLeadingZeros(m.Field<string>("SoHD")).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.')
                                              && m.Field<string>("KyHieu") == clsKTHT.KHHD)
                                      .ToList();
                                    }
                                    else
                                    {
                                        matchedHD = kqra.AsEnumerable()
                                        .Where(m => Helpers.RemoveLeadingZeros(m.Field<string>("SoHD")).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.')
                                                && m.Field<string>("KyHieu") == clsKTHT.KHHD)
                                        .ToList();
                                    }
                                      
                                    if (radDauvao.Checked)
                                    {
                                        matchedHD= matchedHD.Where(m=> m.Field<string>("MST") == clsKTHT.MST).ToList(); 
                                    } 
                                        //Đếm số chứng từ
                                        int countct = 0;
                                    if (matchedHD.Count > 0)
                                    {
                                        countct = tbChungtu.AsEnumerable().Where(m => m.Field<string>("SoHieu") == RemoveLeadingZeros(matchedHD.FirstOrDefault().Field<string>("SoHD"))).Count();
                                    }

                                    if (matchedHD != null && matchedHD.Any())
                                    {
                                        clsKTHT.Checked = true;
                                        clsKTHT.NgayNhap = matchedHD.First().Field<DateTime>("NgayPH");

                                        if(clsKTHT.Khautruthue!=1)
                                        {
                                            clsKTHT.TienTrcThueHD = matchedHD.Sum(m => m.Field<double>("ThanhTien"));
                                            clsKTHT.TienThueHD = matchedHD.Sum(m => radDauvao.Checked ? m.Field<double>("SoPS") : m.Field<double>("Thue"));
                                            clsKTHT.TongTienTTHD = clsKTHT.TienTrcThueHD + clsKTHT.TienThueHD;
                                        }
                                        else
                                        {
                                            var timHDKhautru = tbHoadon.AsEnumerable().Where(m => m["SoHD"].ToString() == clsKTHT.SoHD && m.Field<DateTime>("NgayPH").Date == clsKTHT.NgayLap.Date).ToList().Sum(m => double.Parse(m["ThanhTien"].ToString()));
                                            clsKTHT.TienTrcThueHD = timHDKhautru;
                                            clsKTHT.GhiChu = "Hoá đơn không khấu trừ thuế";
                                        }
                                            // Tìm MST và tên KH từ DB
                                            var khFromDB = KhachHang.AsEnumerable()
                                                .FirstOrDefault(k => k.Field<string>("MST") == clsKTHT.MST);

                                        if (khFromDB != null)
                                        {
                                            clsKTHT.MSTHD = khFromDB.Field<string>("MST");
                                            clsKTHT.TenKHHD = Helpers.ConvertVniToUnicode(khFromDB.Field<string>("Ten"));
                                        }
                                        //Trường hợp không có MST thì tìm Số hiệu khách hàng từ hoá đơn
                                        else
                                        {
                                            int getMaso = 0;
                                            if (radDauvao.Checked)
                                            {
                                                getMaso = tbChungtu.AsEnumerable().Where(m => m.Field<int>("MaTKNo") == 5108 && m.Field<int>("MaCT") == matchedHD.FirstOrDefault().Field<int>("MaCT")).FirstOrDefault().Field<int>("MaSo");
                                            }
                                            else
                                            {
                                                getMaso = tbChungtu.AsEnumerable().Where(m => m.Field<int>("MaTKCo") == 14038 && m.Field<int>("MaCT") == matchedHD.FirstOrDefault().Field<int>("MaCT")).FirstOrDefault().Field<int>("MaSo");
                                            }
                                               
                                            var findmkh = tbHoadon.AsEnumerable().Where(m => m.Field<int>("MaSo") == getMaso).FirstOrDefault().Field<int>("MaKhachHang");
                                            var kh = KhachHang.AsEnumerable().Where(m => m.Field<int>("MaSo") == findmkh).FirstOrDefault();
                                            if (kh != null)
                                            {
                                                clsKTHT.MSTHD = kh.Field<string>("SoHieu");
                                                clsKTHT.TenKHHD = Helpers.ConvertVniToUnicode(kh.Field<string>("Ten"));
                                            }
                                        }
                                        //Nhập sai ngày
                                        if (clsKTHT.NgayLap.Date != clsKTHT.NgayNhap.Date)
                                        {
                                            clsKTHT.GhiChu += $"Ngày nhập bị sai";
                                            Hoadonsai hoadonsai = new Hoadonsai
                                            {
                                                SoHD = clsKTHT.SoHD,
                                                KHHD = clsKTHT.KHHD,
                                                NgayLap = clsKTHT.NgayLap
                                            };  
                                            danhsachHdSaingay.Add(hoadonsai);
                                        }
                                    }
                                    else
                                    {
                                        clsKTHT.Checked = false;
                                    }

                                    // Ghi chú chênh lệch hoặc chưa nhập
                                    if (clsKTHT.Khautruthue != 1 || 1<2)
                                    {
                                        if (clsKTHT.Checked)
                                        {
                                            if (clsKTHT.TienTrcThue != clsKTHT.TienTrcThueHD && clsKTHT.TienTrcThue != 0)
                                            {
                                                double diff = clsKTHT.TienTrcThueHD - clsKTHT.TienTrcThue;
                                                clsKTHT.GhiChu += $"Tiền trước thuế bị lệch: {diff:N0} đ (gốc: {clsKTHT.TienTrcThue:N0} đ); ";
                                            }
                                            else if (clsKTHT.TienTrcThue == 0 && clsKTHT.TienTrcThueHD != clsKTHT.TongTienTT)
                                            {
                                                clsKTHT.GhiChu += $"Tiền trước thuế bị lệch: {(clsKTHT.TongTienTT - clsKTHT.TienTrcThueHD):N0} đ (gốc: {clsKTHT.TongTienTT:N0} đ); ";
                                            }

                                            if (clsKTHT.TienThue != 0 && clsKTHT.TienThueHD != 0 && clsKTHT.TienThue != clsKTHT.TienThueHD)
                                            {
                                                double diff = clsKTHT.TienThueHD - clsKTHT.TienThue;
                                                clsKTHT.GhiChu += $"Tiền thuế bị lệch: {diff:N0} đ (gốc: {clsKTHT.TienThue:N0} đ); ";
                                            }

                                            if (!string.IsNullOrEmpty(clsKTHT.MST) && !string.IsNullOrEmpty(clsKTHT.MSTHD) && clsKTHT.MST != clsKTHT.MSTHD)
                                            {
                                                clsKTHT.GhiChu += $"MST không đúng (gốc: {clsKTHT.MST}); ";
                                            }
                                            if (countct == 1)
                                            {
                                                clsKTHT.GhiChu += $"Hoá đơn thiếu thông tin hàng hoá ";
                                            }
                                            if (imported != null)
                                            {
                                                if (imported.Field<string>("Noidung").Contains("Ñieàu chænh"))
                                                {
                                                    clsKTHT.GhiChu = Helpers.ConvertVniToUnicode(imported.Field<string>("Noidung"));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var note = dtGhichuht.AsEnumerable()
                                                .FirstOrDefault(m => Helpers.RemoveLeadingZeros(m.Field<string>("SoHD")).TrimEnd('.') == Helpers.RemoveLeadingZeros(clsKTHT.SoHD).TrimEnd('.')
                                                                  && m.Field<DateTime>("NgayLap").Date == clsKTHT.NgayLap.Date);

                                            if (note != null)
                                                clsKTHT.GhiChu = note.Field<string>("Noidung");
                                            else if (imported != null)
                                                clsKTHT.GhiChu = imported["Status"].ToString() == "3"
                                                    ? Helpers.ConvertVniToUnicode(imported.Field<string>("Noidung"))
                                                    : "Hoá đơn chưa được nhập";
                                            else
                                                clsKTHT.GhiChu = "Hoá đơn chưa được nhập";
                                        }
                                    }
                                    else
                                    {

                                    }

                                        //                               var checktrung = tbChungtu.AsEnumerable().Where(m => m["SoHieu"].ToString() == clsKTHT.SoHD && DateTime.Parse(m["NgayCT"].ToString()).Date== clsKTHT.NgayLap.Date)
                                        //.GroupBy(m => new {
                                        //    SoHieu = m["SoHieu"],
                                        //    NgayCT = m["NgayCT"]
                                        //})
                                        //.Where(g => g.Select(r => r["MaCT"]).Distinct().Count() > 1)
                                        //.ToList();

                                        //                               if (checktrung.Any())
                                        //                               {
                                        //                                   clsKTHT.GhiChu = "Hoá đơn nhập trùng, "+ clsKTHT.GhiChu;
                                        //                               }
                                        clsKTHTs.Add(clsKTHT);
                                }
                                catch (Exception ex)
                                {
                                    XtraMessageBox.Show(ex.Message +"   ");
                                }
                                 

                            }
                        }
                    }
                }

                // Sắp xếp và đánh lại STT
                clsKTHTs = clsKTHTs.OrderBy(m => m.NgayLap).ThenBy(m => m.STT).ToList();
                for (int i = 0; i < clsKTHTs.Count; i++) clsKTHTs[i].STT = i + 1;

                // Thêm hóa đơn thừa (nhập dư trong hệ thống)
                int maLoai = radDauvao.Checked ? 1 : 8;
                var hdThua = ChungTu.AsEnumerable()
                    .Where(m => m["MaLoai"].ToString() == maLoai.ToString()
                             && m.Field<DateTime>("NgayCT").Month >= tuthang
                             && m.Field<DateTime>("NgayCT").Month <= denthang
                             && !m.Field<string>("SoHieu").Contains("GV"))
                    .GroupBy(m => m.Field<string>("SoHieu"))
                    .Select(g => g.OrderByDescending(r => r.Field<DateTime>("NgayCT")).First())
                    .Where(m => !clsKTHTs.Any(n => Helpers.RemoveLeadingZeros(n.SoHD).TrimEnd('.') == Helpers.RemoveLeadingZeros(m.Field<string>("SoHieu")).TrimEnd('.')))
                    .ToList();

                try
                {
                    foreach (var row in hdThua)
                    {
                        clsKTHTs.Add(new clsKTHT
                        {
                            SoHD = row.Field<string>("SoHieu"),
                            NgayLap = row.Field<DateTime>("NgayCT"),
                            GhiChu = "Hoá đơn nhập dư",
                            Checked = true,
                            //TienThueHD = double.Parse(tbChungtu.AsEnumerable().Where(m => m["SoHieu"].ToString() == row.Field<string>("SoHieu") && m["MaTKTCNo"].ToString() == "5108").FirstOrDefault()["SoPS"].ToString()),
                           // TienTrcThueHD = row.Field<double>("SoPS"),
                        });
                    }
                }
               catch(Exception ex)
                {
                    XtraMessageBox.Show(ex.Message);
                }

                // Cập nhật giao diện
                gridControl1.DataSource = clsKTHTs;
                gridControl1.RefreshDataSource();

                var checkedItems = clsKTHTs.Where(m => m.Checked);
                double sumTrcThueHD = checkedItems.Sum(m => m.TienTrcThueHD);
                double sumThueHD = checkedItems.Sum(m => m.TienThueHD);
                double sumTongHD = checkedItems.Sum(m => m.TongTienTTHD);

                lblResult3.Text = sumTrcThueHD.ToString("N0");
                lblResult2.Text = checkedItems.Sum(m => m.TienTrcThue).ToString("N0");
                labelControl4.Text = sumThueHD.ToString("N0");
                labelControl5.Text = checkedItems.Sum(m => m.TienThue).ToString("N0");
                lbltshd1.Text = checkedItems.Count().ToString();
                lbltshd2.Text = clsKTHTs.Count(m => m.STT != 0).ToString();
            }
            catch(Exception ex)
            {
               
            }
            finally
            {
                progressPanel1.Visible = false;
            }
        }

        // Hàm hỗ trợ nhỏ (không tính là tách hàm lớn)
        private string GetCellValue(IXLCell cell)
        {
            if (cell == null )
                return "";

            string value = cell.Value.ToString();
            if (value == null)
                return "";

            return value.Trim();
        }
        private double ParseDouble(IXLCell cell)
        {
            return double.TryParse(GetCellValue(cell).Replace(",", ""), out double val) ? Math.Round(val) : 0;
        }
        private void LoadControl()
        {
            string[] months = new string[]
     {
            "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5",
            "Tháng 6", "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10",
            "Tháng 11", "Tháng 12"
     };

            cbbChonthang.Properties.Items.AddRange(months);
            int currentMonth = DateTime.Now.Month;
            cbbChonthang.SelectedIndex = currentMonth - 1; // Tháng hiện tại (0-indexed)

            cbbDenthang.Properties.Items.AddRange(months);
            cbbDenthang.SelectedIndex = currentMonth - 1; // Tháng hiện tại (0-indexed)
        }
        DataTable dtGhichuht;
        public string pathThumuc = "";
        private void LoadData()
        {
             
            connectionString = "Server=pc43\\SQLEXPRESS;Database=thanhhuongbendinh;User Id=sa;Password=123456;";

            string qr = "SELECT * FROM tbimport";
            tbimport = ExecuteQuery(qr, null);

              qr = "SELECT * FROM ChungTu";
            ChungTu = ExecuteQuery(qr, null);

              qr = "SELECT * FROM HoaDon";
            HoaDon = ExecuteQuery(qr, null);

            qr = "SELECT * FROM KhachHang";
            KhachHang = ExecuteQuery(qr, null);

            qr = "SELECT * FROM ChungTuLQ";
            ChungTuLQ = ExecuteQuery(qr, null);

        }
        public System.Data.DataTable ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Thêm các tham số vào command
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }

                        using (SqlDataAdapter dataAdapter = new SqlDataAdapter(command))
                        {
                            dataAdapter.Fill(dataTable);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            return dataTable;
        }
        public int ExecuteQueryResult(string query, params SqlParameter[] parameters)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Kết nối đến cơ sở dữ liệu thành công!");

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Thêm các tham số vào command
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    int rowsAffected = command.ExecuteNonQuery(); // Thực thi câu lệnh
                    return rowsAffected;
                }
            }

            return -1;
        }
        private string myTokken = "";
        bool needLogin = true;
        public string tokken { get; set; } = "";
        private async void Getttoken()
        {
            progressPanel1.Visible = true;
            progressPanel1.Caption = "Đang lấy thông tin tokken";
            Application.DoEvents();
            string querykh = @" SELECT *  FROM tbRegister"; // Sử dụng ? thay cho @mst trong OleDb

            var tbRegister = ExecuteQuery(querykh, new SqlParameter("?", ""));
            string gettimeTokken = tbRegister.AsEnumerable().FirstOrDefault()["TimeTokken"].ToString();
            //if (!string.IsNullOrEmpty(gettimeTokken))
            //{
            //    var timpsan = DateTime.Now - DateTime.Parse(gettimeTokken);
            //    if (timpsan.TotalMinutes <= 10)
            //    {
            //        needLogin = false;
            //        myTokken = tbRegister.AsEnumerable().FirstOrDefault().Field<string>("tokken");
            //    }
            //}
            if (needLogin)
            {
                try
                {
                    // ===== HttpClient + CookieContainer (BẮT BUỘC) =====
                    var handler = new HttpClientHandler()
                    {
                        UseCookies = true,
                        CookieContainer = new CookieContainer(),
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    };

                    using (var client = new HttpClient(handler))
                    {
                        // ===== Header giống trình duyệt =====
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("User-Agent",
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0");
                        client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
                        client.DefaultRequestHeaders.Add("Accept-Language", "vi-VN,vi;q=0.9");
                        client.DefaultRequestHeaders.Add("Origin", "https://hoadondientu.gdt.gov.vn");
                        client.DefaultRequestHeaders.Add("Referer", "https://hoadondientu.gdt.gov.vn/");
                        client.DefaultRequestHeaders.ExpectContinue = false;
                         
                        Application.DoEvents();

                        string capUrl = "https://hoadondientu.gdt.gov.vn/api/captcha";
                        var resCap = await client.GetAsync(capUrl);

                        if (!resCap.IsSuccessStatusCode)
                        {
                            XtraMessageBox.Show("Không lấy được captcha");
                            return;
                        }

                        string capBody = await resCap.Content.ReadAsStringAsync();
                        MyJson capJson = JsonConvert.DeserializeObject<MyJson>(capBody);

                        string svgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captcha.svg");
                        File.WriteAllText(svgPath, capJson.Content);

                        // ===== LẤY XSRF-TOKEN (RẤT QUAN TRỌNG) =====
                        var cookies = handler.CookieContainer
                            .GetCookies(new Uri("https://hoadondientu.gdt.gov.vn"));

                        var xsrfToken = cookies["XSRF-TOKEN"]?.Value;
                        if (!string.IsNullOrEmpty(xsrfToken))
                        {
                            client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
                            client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", xsrfToken);
                        }

                        // ================= STEP 2: SOLVE CAPTCHA ================= 
                        Application.DoEvents();

                        SvgCaptchaSolver solver = new SvgCaptchaSolver();
                        string cvalue = solver.SolveCaptcha(svgPath);

                        // ================= STEP 3: LOGIN ================= 
                        Application.DoEvents();

                        string loginUrl = "https://hoadondientu.gdt.gov.vn/api/security-taxpayer/authenticate";

                        var payload = new
                        {
                            username = tbRegister.Rows[0]["Username"].ToString(),
                            password = tbRegister.Rows[0]["Password"].ToString(),
                            cvalue = cvalue,
                            ckey = capJson.Key
                        };

                        var content = new StringContent(
                            JsonConvert.SerializeObject(payload),
                            Encoding.UTF8,
                            "application/json"
                        );

                        var loginRes = await client.PostAsync(loginUrl, content);

                        if (loginRes.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            string err = await loginRes.Content.ReadAsStringAsync();
                            XtraMessageBox.Show("Đăng nhập thất bại (401): " + err);
                            return;
                        }

                        //  loginRes.EnsureSuccessStatusCode();

                        string loginBody = await loginRes.Content.ReadAsStringAsync();
                        var tokenData = JsonConvert.DeserializeObject<TokenResponse>(loginBody);
                        this.tokken = tokenData.token;
                        myTokken = this.tokken;
                        Taiexcelvao();
                        // ================= STEP 4: PROFILE (KHÔNG TẠO CLIENT MỚI) =================
                        try
                        {
                            var req = new HttpRequestMessage(
                                HttpMethod.Get,
                                "https://hoadondientu.gdt.gov.vn/security-taxpayer/profile"
                            );

                            req.Headers.Authorization =
                                new AuthenticationHeaderValue("Bearer", this.tokken);

                            var profRes = await client.SendAsync(req);

                            if (profRes.IsSuccessStatusCode)
                            {
                                string profBody = await profRes.Content.ReadAsStringAsync();
                                var prof = JsonConvert.DeserializeObject<ProfileResponse>(profBody);

                                if (!string.IsNullOrEmpty(prof.password_expire))
                                {
                                    DateTime expireDate = DateTime.Parse(prof.password_expire);
                                    TimeSpan remain = expireDate - DateTime.Now;

                                    if (remain.TotalDays <= 0)
                                    {
                                        XtraMessageBox.Show(
                                            $"Mật khẩu đã hết hạn ngày {expireDate:dd/MM/yyyy}.",
                                            "Hết hạn!",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Warning
                                        );
                                        return;
                                    }
                                    else if (remain.TotalDays <= 3)
                                    {
                                        XtraMessageBox.Show(
                                            $"⚠ Mật khẩu sẽ hết hạn sau {remain.Days} ngày!\nNgày: {expireDate:dd/MM/yyyy}",
                                            "Cảnh báo",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Warning
                                        );
                                    }
                                    else if (remain.TotalDays <= 7)
                                    {
                                        XtraMessageBox.Show(
                                            $"Mật khẩu sắp hết hạn {expireDate:dd/MM/yyyy} (còn {remain.Days} ngày)",
                                            "Cảnh báo",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Warning
                                        );
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            XtraMessageBox.Show("Không kiểm tra được ngày hết hạn: " + ex.Message);
                        }

                        // ================= SAVE TOKEN TIME =================
                        ExecuteQueryResult(
                            "UPDATE tbRegister SET TimeTokken=?",
                            new SqlParameter[]
                            {
                new SqlParameter("?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                            }
                        );
                         
                        Application.DoEvents();
                    }
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show("Lỗi đăng nhập hệ thống thuế: " + ex.Message);
                }

            }
            progressPanel1.Visible = false;
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            Getttoken();
           

        }

        private void btnExportExcelVao_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Excel Files|*.xlsx";
            saveDialog.Title = "Export to Excel";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                gridView1.ExportToXlsx(saveDialog.FileName);
                XtraMessageBox.Show("Export thành công!");
            }
        }

        private async void gridControl1_DoubleClick(object sender, EventArgs e)
        {
           
            DevExpress.XtraGrid.Views.Grid.GridView gridView = gridControl1.MainView as DevExpress.XtraGrid.Views.Grid.GridView;
            var hitInfo = gridView.CalcHitInfo(gridView.GridControl.PointToClient(MousePosition));
            var selectedRow = gridView.GetRow(gridView.FocusedRowHandle) as clsKTHT;

            // Kiểm tra nếu nhấp vào một ô
            if (hitInfo.InRowCell)
            {
                int columnIndex = hitInfo.Column.VisibleIndex; // Chỉ số cột
                //if (columnIndex != 2)
                 //   return;
                System.Windows.Forms.WebBrowser webBrowser1 = new System.Windows.Forms.WebBrowser
                {
                    Dock = DockStyle.Fill // Đổ đầy không gian của form
                };
                // Lấy giá trị trong ô đã nhấp
                string mst = "";
                if (radDauvao.Checked)
                {
                    mst = selectedRow.MST;
                }
                else
                {
                    string qr = "SELECT * FROM tbRegister";
                    var kq2 = ExecuteQuery(qr, null);
                    string mstcty = kq2.Rows[0]["Username"].ToString();
                    mst = mstcty;
                }
                if (mst == "8046549703")
                    mst = "048172000197";
                string pathravao = radDauvao.Checked ? "HDVao" : "HDRa";
                string fn = $"{mst}_{selectedRow.SoHD}_{selectedRow.KHHD}.html";
                int tuthang = int.Parse(cbbChonthang.Text.Replace("Tháng ", ""));
                string query = "SELECT * FROM License";

                // Tạo mảng tham số với giá trị cho câu lệnh SQL

                var kq = ExecuteQuery(query, null);
                string yearPath = $"HD{kq.Rows[0]["NamTC"].ToString()}";
                string ph = Path.Combine(savedPath, yearPath, pathravao, tuthang.ToString(), fn);
                var hiddenValue = ph;
                //Kiểm tra file có tồn tại ko, nếu không có thì tải file mới về

                if (!File.Exists(hiddenValue.ToString()))
                {
                    Match match = Regex.Match(hiddenValue.ToString(), @"\\Hoadon\\.*$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string result = match.Value; // "\Hoadon\HDVao\12\0306340860_2880_C25TDT.xml"
                        hiddenValue = pathThumuc + result;
                    }
                }


                if (!string.IsNullOrEmpty(hiddenValue) && File.Exists(hiddenValue))
                {
                    frmWebbrowser frmCongTrinh = new frmWebbrowser();
                    frmCongTrinh.Text = hiddenValue.ToString().Replace(".xml", "");
                    string filePath = hiddenValue.ToString().Replace(".xml", ".html");
                    frmCongTrinh.filep = filePath;
                    frmCongTrinh.Show();
                    frmCongTrinh.BringToFront();
                    frmCongTrinh.Activate();
                    // Thêm điều khiển WebBrowser vào Form
                    frmCongTrinh.Controls.Add(webBrowser1); 
                    webBrowser1.Navigate("file:///" + filePath.Replace("\\", "/")); 
                }
                else
                {
                    Getttoken();
                    int type = 0;
                    if (selectedRow.Type == 1)
                        type = 4;
                    if (selectedRow.Type == 2)
                        type = 6;
                    if (selectedRow.Type == 3)
                        type =5;
                    if (selectedRow.Type == 4)
                        type = 4;
                    if (selectedRow.Type == 5)
                        type =5;
               
                        string url = GetInvoiceUrl(type, mst, selectedRow.KHHD, selectedRow.SoHD, selectedRow.KHMS);

               
                    string filename = $"{mst}_{selectedRow.SoHD}_{selectedRow.KHHD}.zip";
                    string path = Path.Combine(savedPath, yearPath, pathravao, tuthang.ToString(), filename);

                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.myTokken);
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                        try
                        {
                            this.Cursor = Cursors.WaitCursor;
                            Application.UseWaitCursor = true;

                            HttpResponseMessage response = await client.GetAsync(url);

                            response.EnsureSuccessStatusCode(); // Ném ngoại lệ nếu không thành công

                            // Đọc nội dung phản hồi dưới dạng byte
                            var fileBytes = await response.Content.ReadAsByteArrayAsync();

                            // Lưu file ZIP bằng FileStream
                            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                            {
                                 fileStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                            }

                            Console.WriteLine($"File ZIP đã được lưu tại: {path}");


                            try
                            {
                                string rootPath = Path.GetDirectoryName(path);
                                string getnamefile = Path.GetFileNameWithoutExtension(path);
                                string directoryPath = rootPath + @"\Giainen" + "_" + getnamefile;

                                ZipFile.ExtractToDirectory(path, directoryPath);
                                var files = Directory.GetFiles(directoryPath, "invoice.html", SearchOption.AllDirectories);
                                string targetFilePath = Path.Combine(rootPath, getnamefile + ".html");

                                File.Move(files.FirstOrDefault(), targetFilePath);
                                File.Delete(path);
                                Directory.Delete(directoryPath, true);
                                frmWebbrowser frmCongTrinh = new frmWebbrowser(); 
                                frmCongTrinh.Show();
                                frmCongTrinh.BringToFront();
                                frmCongTrinh.Activate();
                                // Thêm điều khiển WebBrowser vào Form
                                frmCongTrinh.Controls.Add(webBrowser1);
                                string filePath = targetFilePath;
                                selectedRow.Path= filePath; 
                                webBrowser1.Navigate("file:///" + filePath.Replace("\\", "/"));
                                this.Cursor = Cursors.Default;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Lỗi khi giải nén hoặc xử lý file: {ex.Message}");
                                XtraMessageBox.Show("Không thể giải nén file, vui lòng thử lại.");

                                this.Cursor = Cursors.Default;
                                Application.UseWaitCursor = false;
                                Cursor.Current = Cursors.Default;

                            }
                            finally
                            {
                              
                            }
                            //(path, fileImport); // Giải nén file ZIP
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");
                            XtraMessageBox.Show("Không thể tải file xuống, vui lòng thử lại.");

                            this.Cursor = Cursors.Default;
                            Application.UseWaitCursor = false;
                            Cursor.Current = Cursors.Default;

                        }
                        finally
                        {
                        }

                        this.Cursor = Cursors.Default;
                        Application.UseWaitCursor = false;
                        Cursor.Current = Cursors.Default;

                    }
                }
               
            }
        }
        public string GetInvoiceUrl(int invoiceType, string nbmst, string khhdon, string shdon, string Khmshdon)
        {
            string url;

            if (invoiceType == 4 || invoiceType == 6 || invoiceType == 8)
            {
                url = $"https://hoadondientu.gdt.gov.vn/api/query/invoices/export-xml?nbmst={nbmst}&khhdon={khhdon}&shdon={shdon}&khmshdon={Khmshdon}";
            }
            else if (invoiceType == 5 || invoiceType == 10)
            {
                url = $"https://hoadondientu.gdt.gov.vn/api/sco-query/invoices/export-xml?nbmst={nbmst}&khhdon={khhdon}&shdon={shdon}&khmshdon={Khmshdon}";
            }
            else
            {
                throw new ArgumentException("Loại hóa đơn không hợp lệ.");
            }

            return url;
        }
        private void simpleButton6_Click(object sender, EventArgs e)
        {
            // simpleButton1.PerformClick();
            gridControl1.DataSource = null;
            gridControl1.RefreshDataSource();
            LoadDanhsachExcel();
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            cbbChonthang.EditValue = "Tháng 1";
            cbbDenthang.EditValue = "Tháng 3";
        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            cbbChonthang.EditValue = "Tháng 4";
            cbbDenthang.EditValue = "Tháng 6";
        }

        private void simpleButton4_Click(object sender, EventArgs e)
        {
            cbbChonthang.EditValue = "Tháng 7";
            cbbDenthang.EditValue = "Tháng 9";
        }

        private void simpleButton5_Click(object sender, EventArgs e)
        {
            cbbChonthang.EditValue = "Tháng 10";
            cbbDenthang.EditValue = "Tháng 12";
        }

        private void gridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {

           string qr = "SELECT * FROM tbGhichuHT";
            dtGhichuht = ExecuteQuery(qr, null);

            var selectedRow = gridView1.GetRow(e.RowHandle) as clsKTHT;
            //Tìm trong danh sách đã có chưa
            var checkds = dtGhichuht.AsEnumerable().Where(m => m.Field<string>("SoHD") == selectedRow.SoHD && m.Field<DateTime>("NgayLap").Date == selectedRow.NgayLap.Date).FirstOrDefault();
                if (checkds != null)
            {
                // SQL Server - dùng @tên tham số
                var query = @"UPDATE tbGhichuHT SET Noidung = @Noidung WHERE ID = @ID";
                var parameters = new SqlParameter[]
                {
    new SqlParameter("@Noidung", selectedRow.GhiChu),
    new SqlParameter("@ID", checkds.Field<int>("ID"))
                };
                var rowsAffected = ExecuteQueryResult(query, parameters);
            }
            else
            {
                // SQL Server - dùng @tên tham số
                var query = @"INSERT INTO tbGhichuHT (SoHD, NgayLap, Noidung) VALUES (@SoHD, @NgayLap, @Noidung)";
                var parameters = new SqlParameter[]
                {
    new SqlParameter("@SoHD", selectedRow.SoHD),
    new SqlParameter("@NgayLap", selectedRow.NgayLap),
    new SqlParameter("@Noidung", selectedRow.GhiChu),
                };
                var  rowsAffected = ExecuteQueryResult(query, parameters);
            }
        }

        private void KTHT_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (frmMain != null)
                if (frmMain.typetxt == "2")
                    this.frmMain.Close();
        }
        public class ThongKe
        {
            public string Name { get; set; }
            public double TienTrcThue { get; set; }
            public double TienThue { get; set; }    
            public double TongTien { get; set; }
        }
        string mstcongty = "";
        string CCCD=""; 
        string savedPath = "";
        string user = "";
        string password = "";
        DataTable tbChungtu;
        DataTable tbHoadon;

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string currentYear = $"HD{cbbNam.EditValue}";
                string currentpath = Path.Combine(savedPath, currentYear,
                    radDauvao.Checked ? "HDVao" : "HDRa",
                    cbbChonthang.EditValue.ToString().Replace("Tháng","").Trim());

                // Mở thư mục
                System.Diagnostics.Process.Start("explorer.exe", currentpath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở thư mục: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Trong form load hoặc phương thức khởi tạo
        private void LoadComboBoxYear()
        {
            try
            {
                // Xóa items cũ nếu có
                cbbNam.Properties.Items.Clear();

                // Lấy năm hiện tại
                int currentYear = DateTime.Now.Year;

                // Thêm các năm từ 2000 đến năm hiện tại
                for (int year = 2000; year <= currentYear; year++)
                {
                    cbbNam.Properties.Items.Add(year);
                }

                // Chọn năm hiện tại mặc định
                cbbNam.SelectedItem = currentYear;

                // Hoặc có thể format hiển thị nếu cần
                // cbbNam.Properties.Items.Add($"Năm {year}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách năm: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Gọi phương thức này trong Form_Load
    
        private void ThietLapControl()
        {
            //Disable gõ text
            cbbChonthang.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            cbbDenthang.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            cbbNam.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            cbbDenthang.Properties.AccessibleDefaultActionDescription = "Chọn tháng đến";
            //Thiết lập kích thước  
            //(int)(this.ClientSize.Width * 0.28);
            cbbChonthang.Width = (int)(this.ClientSize.Width * 0.06);
            cbbDenthang.Width = (int)(this.ClientSize.Width * 0.06);
            labelControl7.Location=new Point(cbbChonthang.Location.X + cbbChonthang.Width + 5, labelControl7.Location.Y);
            cbbDenthang.Location = new Point(labelControl7.Location.X + labelControl7.Width + 5, cbbDenthang.Location.Y);
            labelControl8.Location = new Point(cbbDenthang.Location.X + cbbDenthang.Width + 5, labelControl8.Location.Y);   
            cbbNam.Location = new Point(labelControl8.Location.X + labelControl8.Width + 5, cbbNam.Location.Y);
            simpleButton6.Location = new Point(cbbNam.Location.X + cbbNam.Width + 5, simpleButton6.Location.Y);
        }

        private void cbbDenthang_SelectedIndexChanged(object sender, EventArgs e)
        {
            int tuthang = int.Parse(cbbChonthang.Text.Replace("Tháng ", ""));
            int denthang = int.Parse(cbbDenthang.Text.Replace("Tháng ", ""));
            if (denthang < tuthang)
            {
                cbbChonthang.EditValue = cbbDenthang.EditValue;
            }
            // cbbDenthang.EditValue = cbbChonthang.EditValue;
        }

        private void KTHT_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.G)
            {
                var dd = danhsachHdSaingay;
                //Tìm MaSo từ  Hoadon trước
                foreach(var it in danhsachHdSaingay)
                {
                    var getmst = tbHoadon.AsEnumerable().Where(m => m["SoHD"].ToString() == it.SoHD && m["KyHieu"].ToString()==it.KHHD).FirstOrDefault();
                    if (getmst != null)
                    {
                         //Tìm tiếp danh sách chungtu từ masao
                         var getMaCT=tbChungtu.AsEnumerable().Where(m => m["MaSo"].ToString() == getmst["MaSo"].ToString()).FirstOrDefault();
                         var getMaCTGV= tbChungtu.AsEnumerable().Where(m => m["SoHieu"].ToString() == $"{getMaCT["SoHieu"].ToString()}GV" && m["NgayCT"].ToString()== getMaCT["NgayCT"].ToString()).FirstOrDefault();
                        if (getMaCT != null)
                        {
                            //Lấy danh sách chungtu liên quan từ MaCT
                            var getListCT = tbChungtu.AsEnumerable().Where(m => m["MaCT"].ToString() == getMaCT["MaCT"].ToString()).ToList();
                            foreach(var item in getListCT)
                            {
                                string query = @"UPDATE ChungTu SET NgayCT=@NgayCT, NgayGS=@NgayGS where MaSo=@MaSo";

                                var parameters = new SqlParameter[]
                         {
               new SqlParameter("@NgayCT", it.NgayLap),
                 new SqlParameter("@NgayGS", it.NgayLap),
                   new SqlParameter("@MaSo", item["MaSo"].ToString()),
                         };
                                int rowsAffected = ExecuteQueryResult(query, parameters);
                            }
                        }

                        if (getMaCTGV != null)
                        {
                            //Lấy danh sách chungtu liên quan từ MaCT
                            var getListCT = tbChungtu.AsEnumerable().Where(m => m["MaCT"].ToString() == getMaCTGV["MaCT"].ToString()).ToList();
                            foreach (var item in getListCT)
                            {
                                string query = @"UPDATE ChungTu SET NgayCT = @NgayCT, NgayGS = @NgayGS WHERE MaSo = @MaSo";

                                var parameters = new SqlParameter[]
                                {
    new SqlParameter("@NgayCT", it.NgayLap),
    new SqlParameter("@NgayGS", it.NgayLap),
    new SqlParameter("@MaSo", item["MaSo"].ToString()),
                                };
                                int rowsAffected = ExecuteQueryResult(query, parameters);
                            }
                        }
                    }
                } 
            }
        }

        private void KTHT_Load(object sender, EventArgs e)
        {
            this.KeyPreview = true;

            ThietLapControl();
            LoadComboBoxYear();
            progressPanel1.Description = "";
            LoadData();
        
            //Lấy tooken 1 lần đầu tiên
          
            LoadControl();
            string query = "SELECT * FROM License";

            // Tạo mảng tham số với giá trị cho câu lệnh SQL

            var kq = ExecuteQuery(query, null);
            mstcongty = kq.Rows[0]["MaSoThue"].ToString();
            CCCD = kq.Rows[0]["CCCD"].ToString();   
            int namtc= int.Parse(kq.Rows[0]["NamTC"].ToString());
            cbbNam.SelectedItem = namtc;

            // simpleButton1.PerformClick();

            query = "SELECT * FROM tbRegister";
            // Tạo mảng tham số với giá trị cho câu lệnh SQL

             kq = ExecuteQuery(query, null);
            savedPath = kq.Rows[0]["Hoadonpath"].ToString();
            user = kq.Rows[0]["Username"].ToString();
            password = kq.Rows[0]["Password"].ToString();
            progressPanel1.Visible=false;

            query = "SELECT * FROM ChungTu";
            tbChungtu = ExecuteQuery(query, null);
            query = "SELECT * FROM HoaDon";
            tbHoadon = ExecuteQuery(query, null);
        } 
        private void radDauvao_CheckedChanged(object sender, EventArgs e)
        {
           
        }

        private void gridView1_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            var selectedRow = gridView1.GetRow(e.RowHandle) as clsKTHT;
            if (selectedRow.Khautruthue == 1)
                return;
            if (e.Column.FieldName == "STTType")
            {
                if (e.CellValue == null)
                    return;
                //Tìm type
                int rowHandle = e.RowHandle;
                var gridView = sender as GridView;
                var getype = gridView.GetRowCellValue(rowHandle,"Type");
                var getValue = e.CellValue.ToString();
          
                if (e.CellValue.ToString() == "Hóa đơn mới")
                { 
                    e.Appearance.ForeColor = Color.Black; // Tô màu chữ đỏ
                }
                else
                {
                    e.Appearance.BackColor = Color.Red;
                    e.Appearance.ForeColor = Color.White; // Tô màu chữ đỏ
                }
            }
            //Kiểm tra tiền trước thuế
            if (e.Column.FieldName == "TienTrcThueHD")
            {
                var gridView = sender as GridView;
                int rowHandle = e.RowHandle;
                double getdt1 = (double)gridView1.GetRowCellValue(rowHandle, "TienTrcThue");
                double gettt = (double)gridView1.GetRowCellValue(rowHandle, "TongTienTT");
                //TongTienPhi 
                double getdt2 = double.Parse(e.CellValue.ToString());
                if ( getdt1!= getdt2)
                {
                    if (getdt1 != 0)
                    {
                        e.Appearance.ForeColor = Color.Red; // Tô màu chữ đỏ
                    }
                    else
                    {
                        if (getdt2 != gettt)
                        {
                            e.Appearance.ForeColor = Color.Red; // Tô màu chữ đỏ
                        }
                    }
                   
                }
            }
            //Kiểm tra tiền  thuế
            if (e.Column.FieldName == "TienThueHD")
            {
                var gridView = sender as GridView;
                int rowHandle = e.RowHandle;
                double getdt1 = (double)gridView1.GetRowCellValue(rowHandle, "TienThue");
                double getdt2 =double.Parse(e.CellValue.ToString());
                if (getdt1 != 0 && getdt2 != 0 &&  getdt1 != getdt2)
                {
                    e.Appearance.ForeColor = Color.Red; // Tô màu chữ đỏ
                }
            }
            if (e.Column.FieldName == "TongTienTTHD")
            {
                var gridView = sender as GridView;
                int rowHandle = e.RowHandle;
                double getdt1 = (double)gridView1.GetRowCellValue(rowHandle, "TongTienTT");
                double getdt2 = double.Parse(e.CellValue.ToString());
                if (getdt1 != 0 && getdt2 != 0 && getdt1 != getdt2)
                {
                    e.Appearance.ForeColor = Color.Red; // Tô màu chữ đỏ
                }
            }
            if (e.Column.FieldName == "MSTHD")
            {
                var gridView = sender as GridView;
                int rowHandle = e.RowHandle;
                if(gridView.GetRowCellValue(rowHandle, "MST")!=null)
                {
                    string getdt1 = gridView.GetRowCellValue(rowHandle, "MST").ToString();
                    string getdt2 = e.CellValue?.ToString();
                    if (!string.IsNullOrEmpty(getdt1) && !string.IsNullOrEmpty(getdt2) && getdt1 != getdt2)
                    {
                        e.Appearance.ForeColor = Color.Red; // Tô màu chữ đỏ
                    }
                }
               
            }
            if (e.Column.FieldName == "NgayTai")
            {
                var gridView = sender as GridView;
                int rowHandle = e.RowHandle;
                if (Convert.ToDateTime(e.CellValue) != DateTime.MinValue) // 01/01/01
                {
                    if (selectedRow.StatusImport != -1)
                    {
                        e.Appearance.ForeColor = Color.White;
                        e.Appearance.BackColor = Color.Red; // Tô màu chữ đỏ
                    }
                    if (selectedRow.StatusImport == -1)
                    {
                        e.Appearance.ForeColor = Color.Black;
                        e.Appearance.BackColor = Color.Yellow; // Tô màu chữ đỏ
                    }
                 
                }
            }
            if (e.Column.FieldName == "NgayNhap")
            {
                var gridView = sender as GridView;
                int rowHandle = e.RowHandle;
                if (Convert.ToDateTime(e.CellValue) != DateTime.MinValue) // 01/01/01
                {
                    e.Appearance.ForeColor = Color.White;
                    e.Appearance.BackColor = Color.Green; // Tô màu chữ đỏ
                }
            }
        }

        private void gridView1_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column.FieldName == "NgayNhap" || e.Column.FieldName == "NgayTai") // Thay thế "NgayNhap" bằng tên cột của bạn
            {
                // Kiểm tra giá trị ngày nhập
                if (Convert.ToDateTime(e.Value) == DateTime.MinValue) // 01/01/01
                {
                    e.DisplayText = "";
                }
               
            }
            //TienTrcThueHD
            if (e.Column.FieldName == "TienTrcThueHD" || e.Column.FieldName == "TienThueHD" || e.Column.FieldName == "TongTienTTHD") // Thay thế "NgayNhap" bằng tên cột của bạn
            {
                // Kiểm tra giá trị ngày nhập
                if (Convert.ToDouble(e.Value) ==0) // 01/01/01
                {
                    e.DisplayText = "";
                }

            }
        }

        private void radDaura_CheckedChanged(object sender, EventArgs e)
        {
          
        }

        private void cbbChonthang_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbbDenthang.EditValue= cbbChonthang.EditValue;
        }
    }
}