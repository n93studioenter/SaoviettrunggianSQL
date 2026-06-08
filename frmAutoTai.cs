using ClosedXML.Excel;
using DevExpress.CodeParser;
using DevExpress.Utils.About;
using DevExpress.XtraEditors;
using DevExpress.XtraMap.Native;
using DevExpress.XtraWaitForm;
using Microsoft.Win32;
using Newtonsoft.Json;
using SaovietTax.Database;
using SaovietTax.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
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
using System.Xml;
using Windows.Media.Protection.PlayReady;
using static SaovietTax.frmKhachhang;
using static SaovietTax.frmMain;

namespace SaovietTax
{
    public partial class frmAutoTai : DevExpress.XtraEditors.XtraForm
    {
        public frmAutoTai()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            //this.ShowInTaskbar = false;

            //// 2. Thu nhỏ ngay
            //this.WindowState = FormWindowState.Minimized;

            //// 3. Chặn việc show form
            //this.Shown += (s, e) => this.Hide();
        }
        //protected override void SetVisibleCore(bool value)
        //{
        //    base.SetVisibleCore(false); // ❗ chặn hiển thị
        //}
        string password, connectionString;
        public System.Data.DataTable ExecuteQuery(string query, params OleDbParameter[] parameters)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        // Thêm các tham số vào command
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }

                        using (OleDbDataAdapter dataAdapter = new OleDbDataAdapter(command))
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

            return dataTable; // Trả về DataTable chứa dữ liệu
        }
        public int ExecuteQueryResult(string query, params OleDbParameter[] parameters)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Kết nối đến cơ sở dữ liệu thành công!");

                using (OleDbCommand command = new OleDbCommand(query, connection))
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
        public void getconnectstring()
        {
            string appPath = Assembly.GetExecutingAssembly().Location;

            // Lấy thư mục chứa ứng dụng
            string directoryPath = Path.GetDirectoryName(appPath);

            // Xóa phần \bin\Debug để lấy đường dẫn gốc
            string rootDirectory = Path.GetFullPath(Path.Combine(directoryPath, @"..\.."));

            // Tạo đường dẫn đến file dpPath.txt trong thư mục hoadon
            string filePaths = Path.Combine(rootDirectory, "hoadon", "dpPath.txt");
            string pathThumuc = Path.Combine(rootDirectory);
            string dbPath = "";
            //MessageBox.Show(pathThumuc);
            try
            {
                string content = File.ReadAllText(filePaths);
                dbPath = content;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi đọc file: " + ex.Message);
            }

            // Đọc toàn bộ nội dung tệp
            string password = "1@35^7*9)1";
            connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Jet OLEDB:Database Password={password};";
        }
        int trylogin = 0;
        string tokken = "";
        string mstcongty = "";
        string savedPath = "";
        public void Gettokken()
        { 
            using (var client = new HttpClient())
            {
                try
                {

                    HttpResponseMessage response = new HttpResponseMessage();
                    string url = "https://hoadondientu.gdt.gov.vn:30000/captcha";
                    int retry = 0;
                    int maxRetry = 10; // thử tối đa 10 lần

                    while (retry < maxRetry)
                    {
                        try
                        {
                            response = client.GetAsync(url).Result;

                            if (response.IsSuccessStatusCode)
                            {
                                byte[] captchaBytes = response.Content.ReadAsByteArrayAsync().Result;

                                if (captchaBytes.Length > 0)
                                {
                                    // ✅ ĐÃ CÓ CAPTCHA → THOÁT
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // bỏ qua, thử lại
                        }

                        retry++;
                        Thread.Sleep(1000); // ⏳ chờ 1 giây rồi thử lại
                    }

                    if (response == null || !response.IsSuccessStatusCode)
                    { 
                        return;
                    }

                    //Đọc nội dung phản hồi
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    MyJson myJson = JsonConvert.DeserializeObject<MyJson>(responseBody);
                    //string filePath = "output.svg";
                    string filePath = AppDomain.CurrentDomain.BaseDirectory + "output.svg"; // Đảm bảo tệp ở cùng thư mục với chương trình
                                                                                            //Lưu chuỗi SVG vào tệp
                    File.WriteAllText(filePath, myJson.Content);
                    Thread.Sleep(50);
                 
                    SvgCaptchaSolver solver = new SvgCaptchaSolver();
                    string result = solver.SolveCaptcha(filePath);

                    url = "https://hoadondientu.gdt.gov.vn:30000/security-taxpayer/authenticate";
                    string querykh = @" SELECT *  FROM tbRegister"; // Sử dụng ? thay cho @mst trong OleDb

                    var tbRegister = ExecuteQuery(querykh, new OleDbParameter("?", ""));
                    savedPath = tbRegister.Rows[0]["Hoadonpath"].ToString();
                    mstcongty= tbRegister.Rows[0]["Username"].ToString();
                    var payload = new
                    {
                        username = tbRegister.Rows[0].Field<string>("Username"),
                        password = tbRegister.Rows[0].Field<string>("Password"),
                        cvalue = result,
                        ckey = myJson.Key
                    };
                    string json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = client.PostAsync(url, content).Result;
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Thread.Sleep(1000);
                        trylogin += 1;
                        if (trylogin > 1)
                        {
                           // XtraMessageBox.Show("Không thể đăng nhập vui lòng thử lại");
                            return;
                        }
                        Thread.Sleep(1000);
                        Gettokken();
                    }

                    response.EnsureSuccessStatusCode();
                    Thread.Sleep(50);
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
                    this.tokken = tokenResponse.token;
                  var  query = @"UPDATE tbRegister SET TimeTokken=? ";

                    var parameters = new OleDbParameter[]
             {
                                   new OleDbParameter("?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))

             };
                    int rowsAffected = ExecuteQueryResult(query, parameters);
 
                }
                catch (Exception ex)
                {
                    //XtraMessageBox.Show(ex.Message);
                    return;
                }

            }
        }
        int type = 0;
        List<string> lstfile = new List<string>();  
        private void frmAutoTai_Load(object sender, EventArgs e)
        {
            getconnectstring();
            Gettokken();
            Xulyexelvao(tokken, 1);
            Xulyexelvao(tokken, 2);
            Xulyexelvao(tokken, 3);
            type = 1;
            string querykh = @" SELECT *  FROM tbRegister"; // Sử dụng ? thay cho @mst trong OleDb

            var tbRegister = ExecuteQuery(querykh, new OleDbParameter("?", ""));
            string originpath = tbRegister.Rows[0]["Hoadonpath"].ToString();
            string currentYear = $"HD{DateTime.Now.Year}";
            string directoryPath = Path.Combine(tbRegister.Rows[0]["Hoadonpath"].ToString(), currentYear, "HDVao");
            DocfileExcelVao(tbRegister.Rows[0]["Username"].ToString(), directoryPath, originpath);

             
            //Xử lý ra 
            Xulyexelra(tokken, 1);
            Xulyexelra(tokken, 2);
            directoryPath = Path.Combine(tbRegister.Rows[0]["Hoadonpath"].ToString(), currentYear, "HDRa");
            DocfileExcelRa(tbRegister.Rows[0]["Username"].ToString(), directoryPath, originpath);
            //XtraMessageBox.Show("Đã hoàn thành tự động tải hoá đơn"+ mstcongty);
            this.Close();
            return;
        }
        public void DocfileExcelRa(string mstcongty, string savedPath, string originpath)
        {
            string directoryPath = Path.Combine(savedPath, DateTime.Now.Month.ToString());
            var excelFiles = Directory.EnumerateFiles(directoryPath, "*.xlsx", SearchOption.AllDirectories).Where(m => m.Contains(mstcongty)).ToList();

            int tongsohodadon = excelFiles.Count;
            int i = 1;
            foreach (var excelFile in excelFiles)
            {
                using (var workbook = new XLWorkbook(excelFile))

                {
                    var worksheet = workbook.Worksheet(1); // Lấy sheet đầu tiên
                    foreach (var row in worksheet.RowsUsed().Skip(3)) // Bỏ qua 6 hàng đầu tiên
                    {
                        string khhd = row.Cell("B").Value.ToString(); // Lấy giá trị của cột A trong hàng hiện tại
                        string getSHHD = row.Cell("C").Value.ToString(); // Lấy giá trị của cột A trong hàng hiện tại
                        string getSohd = RemoveLeadingZeros(row.Cell("D").Value.ToString()); // Lấy giá trị của cột C trong hàng hiện tại 
                        string GetNLap = row.Cell("E").Value.ToString();
                        string mstnb = row.Cell("F").Value.ToString();

                        if (getSohd == "3423")
                        {
                            int a = 10;
                        }
                        //Kiểm tra từ ngày đến ngày
                        DateTime getdate = DateTime.Parse(GetNLap);

                        //Kiểm tra file đã tải rồi
                        var checkfile = savedPath + "\\" + mstcongty + "_" + getSohd + "_" + getSHHD + ".xml";
                        if (File.Exists(checkfile))
                        {
                            Console.WriteLine("File đã import");
                            continue;
                        }
                        // var checkFile=tbimport.AsEnumerable().Where()
                        var checkExist = tbimport.AsEnumerable().Where(m => m.Field<string>("SHDon") == getSohd && m.Field<DateTime>("Nlap").Date == getdate.Date).FirstOrDefault();
                        if (checkExist != null)
                        {
                            Console.WriteLine("File đã import");
                            continue;
                        }
                        var checkExistct = tbchungtu.AsEnumerable().Where(m => m.Field<string>("SoHieu") == getSohd && m.Field<DateTime>("NgayCT").Date == getdate.Date).FirstOrDefault();
                        if (checkExistct != null)
                        {
                            Console.WriteLine("File đã import");
                            continue;
                        }


                        //Tải file xml
                        string url = "";
                        if (i == 1)
                        {
                            url = $"https://hoadondientu.gdt.gov.vn:30000/sco-query/invoices/export-xml?nbmst={mstnb}&khhdon={getSHHD}&shdon={getSohd}&khmshdon={khhd}";
                        }
                        if (i == 2)
                        {

                            url = $"https://hoadondientu.gdt.gov.vn:30000/query/invoices/export-xml?nbmst={mstnb}&khhdon={getSHHD}&shdon={getSohd}&khmshdon={khhd}";
                        }
                        //https://hoadondientu.gdt.gov.vn:30000/sco-query/invoices/export-xml?nbmst=036084000738&khhdon=C25MVN&shdon=211&khmshdon=2
                        //https://hoadondientu.gdt.gov.vn:30000/sco-query/invoices/export-xml?nbmst=3502386218&khhdon=C25MAA&shdon=3294&khmshdon=2

                        lookupTbImport.Add((mstcongty, getSohd, getdate.Date));
                        string pathravao = "HDRa";
                        string filename = $"{mstcongty}_{getSohd}_{getSHHD}.zip";
                        string path = Path.Combine(directoryPath, filename);
                        string filenamexml = $"{mstcongty}_{getSohd}_{getSHHD}.xml";
                        string pathxml = Path.Combine(directoryPath, filenamexml);
                        //Kiểm tra nếu hoá đơn chưa dc tải thì tải về
                        if (!File.Exists(path) && !File.Exists(pathxml))
                        {
                            using (var client = new HttpClient())
                            {
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokken);
                                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                                try
                                {

                                    HttpResponseMessage response = client.GetAsync(url).Result; // Sử dụng .Result
                                    response.EnsureSuccessStatusCode(); // Ném ngoại lệ nếu không thành công

                                    // Đọc nội dung phản hồi dưới dạng byte
                                    var fileBytes = response.Content.ReadAsByteArrayAsync().Result;

                                    // Lưu file ZIP bằng FileStream
                                    using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096))
                                    {
                                        fileStream.Write(fileBytes, 0, fileBytes.Length);
                                    }

                                    Console.WriteLine($"File ZIP đã được lưu tại: {path}");
                                    ExtractZipXML(path); // Giải nén file ZIP 
                                    Application.DoEvents();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                i++;
            }
        }
        DataTable tbimport;
        DataTable tbchungtu;
        List<KhachHang> lstKhachhangs = new List<KhachHang>();
        public DataTable tbKhachhang = new DataTable();
        private void LoadHoadonCT()
        {
            var query = "SELECT * FROM KhachHang"; // Giả sử bạn muốn lấy tất cả dữ liệu từ bảng KhachHang
            tbKhachhang = ExecuteQuery(query);
            lstKhachhangs = tbKhachhang.ToList<KhachHang>();
             query = @"
        SELECT 
            hd.SoHD,
            hd.NgayPH,
            hd.MaKhachHang,
            ct.NgayCT
        FROM 
            Hoadon hd
        INNER JOIN 
            Chungtu ct ON hd.MaSo = ct.MaSo
        WHERE 
            hd.KyHieu <> '...'";

            var data = ExecuteQuery(query);

            // 🔥 lookup KHÁCH HÀNG (O(1))
            var khachHangMstLookup = lstKhachhangs.ToDictionary(
                k => k.MaSo,
                k => (k.MST ?? "").Trim()
            );

            lookupHoaDonCT.Clear();

            foreach (DataRow item in data.Rows)
            {
                string soHD = Helpers.RemoveLeadingZeros(
                    item["SoHD"]?.ToString() ?? ""
                ).Trim();

                DateTime ngayPH = ((DateTime)item["NgayCT"]).Date;

                int maKhachHang = (int)item["MaKhachHang"];

                string mst = khachHangMstLookup.TryGetValue(maKhachHang, out var v)
                    ? v
                    : "";

                lookupHoaDonCT.Add((mst, soHD, ngayPH));
            }
        }
        DataTable tbimports { get; set; }
        List<TbImport> lsttbimport = new List<TbImport>();
        public HashSet<(string Mst, string SoHD, DateTime NLap)> lookupHoaDonCT { get; }
        = new HashSet<(string Mst, string SoHD, DateTime NLap)>();
        // KHAI BÁO NGOÀI HÀM (ở cấp độ class)
        private HashSet<(string MST, string SHDon, DateTime NLap)> lookupTbImport
            = new HashSet<(string MST, string SHDon, DateTime NLap)>();
        private void Loadtbimport()
        {
            var query = "SELECT *   FROM tbimport";
            tbimports = ExecuteQuery(query);
            // GỌI EXTENSION METHOD ĐÚNG
            try
            {

                lsttbimport = tbimports.ToList<TbImport>();
                lookupTbImport = new HashSet<(string MST, string SHDon, DateTime NLap)>(
                    lsttbimport.Select(x => (x.Mst ?? "", x.SHDon ?? "", x.NLap))
                );
            }
            catch (Exception ex)
            {
              //  XtraMessageBox.Show(ex.Message);
            }
        }
        public void DocfileExcelVao(string mstcongty, string savedPath, string originpath)
        {
            LoadHoadonCT();
            Loadtbimport();
            string querykh = @" SELECT *  FROM tbimport"; // Sử dụng ? thay cho @mst trong OleDb
            tbimport = ExecuteQuery(querykh, new OleDbParameter("?", ""));
             querykh = @" SELECT *  FROM Chungtu"; // Sử dụng ? thay cho @mst trong OleDb
            tbchungtu = ExecuteQuery(querykh, new OleDbParameter("?", ""));

            //  string directoryPath = Path.Combine(savedPath, "HDVao", DateTime.Now.Month.ToString());
            string directoryPath = Path.Combine(savedPath, DateTime.Now.Month.ToString());
            var excelFiles = Directory.EnumerateFiles(directoryPath, "*.xlsx", SearchOption.AllDirectories).Where(m => m.Contains(mstcongty)).ToList();
            int totalInvoices = 0;
            
            int i = 1;
            foreach (var excelFile in excelFiles)
            {
                using (var workbook = new XLWorkbook(excelFile))

                {
                    var worksheet = workbook.Worksheet(1); // Lấy sheet đầu tiên
                    foreach (var row in worksheet.RowsUsed().Skip(3)) // Bỏ qua 6 hàng đầu tiên
                    {
                        string khhd = row.Cell("B").Value.ToString(); // Lấy giá trị của cột A trong hàng hiện tại
                        string getSHHD = row.Cell("C").Value.ToString(); // Lấy giá trị của cột A trong hàng hiện tại
                        string getSohd = RemoveLeadingZeros(row.Cell("D").Value.ToString()); // Lấy giá trị của cột C trong hàng hiện tại 
                        string GetNLap = row.Cell("E").Value.ToString();
                        string mstnb = row.Cell("F").Value.ToString();

                        //Kiểm tra từ ngày đến ngày
                        DateTime getdate = DateTime.Parse(GetNLap);
                        bool daTonTai = lookupHoaDonCT.Contains((mstnb, getSohd, getdate.Date));
                        bool daTonTaiimport = lookupTbImport.Contains((mstnb, getSohd, getdate.Date));
                        if (daTonTai || daTonTaiimport)
                        {
                            continue;
                        }

                        //Kiểm tra xem hoá đơn đã có trong bảng tbimport chưa


                        // var checkFile=tbimport.AsEnumerable().Where()
                        var checkExist = tbimport.AsEnumerable().Where(m => m.Field<string>("SHDon") == getSohd && m.Field<string>("Mst") == mstnb && m.Field<DateTime>("Nlap").Date == getdate.Date).FirstOrDefault();
                        if (checkExist != null)
                        {
                            Console.WriteLine("File đã import");
                            continue;
                        }
                        if (getSohd == "71881")
                        {
                            int a = 10;
                        }
                        var checkExistct = tbchungtu.AsEnumerable().Where(m => m.Field<string>("SoHieu") == getSohd && m.Field<DateTime>("NgayCT").Date == getdate.Date).FirstOrDefault();
                        if (checkExistct != null)
                        {
                            Console.WriteLine("File đã import");
                            continue;
                        }
                        //Tải file xml
                        string url = "";
                        if (i == 1 || i == 2)
                        {
                            url = $"https://hoadondientu.gdt.gov.vn:30000/query/invoices/export-xml?nbmst={mstnb}&khhdon={getSHHD}&shdon={getSohd}&khmshdon={khhd}";
                        }
                        if (i == 3)
                        {
                            url = $"https://hoadondientu.gdt.gov.vn:30000/sco-query/invoices/export-xml?nbmst={mstnb}&khhdon={getSHHD}&shdon={getSohd}&khmshdon={khhd}";
                        }


                        string pathravao = "HDVao";
                        string filename = $"{mstnb}_{getSohd}_{getSHHD}.zip";
                        string path = Path.Combine(directoryPath, filename);
                        string filenamexml = $"{mstnb}_{getSohd}_{getSHHD}.xml";
                        string pathxml = Path.Combine(directoryPath, filenamexml);
                        string folderpath = Path.Combine(directoryPath);

                        lookupTbImport.Add((mstnb, getSohd, getdate.Date));
                        //Kiểm tra nếu hoá đơn chưa dc tải thì tải về
                        if (!File.Exists(path) && !File.Exists(pathxml))
                        {
                            using (var client = new HttpClient())
                            {
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokken);
                                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                                try
                                {
                                    HttpResponseMessage response = client.GetAsync(url).Result; // Sử dụng .Result
                                    response.EnsureSuccessStatusCode(); // Ném ngoại lệ nếu không thành công

                                    // Đọc nội dung phản hồi dưới dạng byte
                                    var fileBytes = response.Content.ReadAsByteArrayAsync().Result;

                                    // Lưu file ZIP bằng FileStream
                                    using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096))
                                    {
                                        fileStream.Write(fileBytes, 0, fileBytes.Length);
                                    }

                                    Console.WriteLine($"File ZIP đã được lưu tại: {path}");
                                    ExtractZipXML(path); // Giải nén file ZIP 
                                    Application.DoEvents();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");
                                    if (i == 2)
                                        GetKNMXML(mstnb, getSHHD, getSohd, tokken, getdate, folderpath, filename);
                                }
                            }
                        }
                    }
                }
                i++;
            }
        }
        public void GetKNMXML(string nbmst, string khhdon, string shdon, string tokken, DateTime GetNLap, string path, string filename)
        {
            GDTClient.UpdateToken(tokken);
            string url = $"https://hoadondientu.gdt.gov.vn:30000/query/invoices/detail?nbmst={nbmst}&khhdon={khhdon}&shdon={shdon}&khmshdon=1";

            using (var client = new HttpClient())
            {
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokken);
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    // Gửi yêu cầu GET đồng bộ
                    string responseBody = Task.Run(async () => await GDTClient.GetJsonAsync(url)).Result;
                    var rootObject = JsonConvert.DeserializeObject<Invoice>(responseBody);
                    // Tạo phần tử gốc <HDon>
                    TaoFileXmlChiCoDLHDon(path, filename.Replace(".zip", ""), rootObject, GetNLap); 

                    string ph = Path.Combine(path, filename.Replace(".zip", "_KNM.xml")); 
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                }
            }
        }
        public static class GDTClient2
        {
            private static readonly HttpClient _client;

            static GDTClient2()
            {
                var handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    UseProxy = false
                };

                _client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(40) };

                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.ConnectionClose = false; // Keep-Alive
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            }

            public static void UpdateToken(string token)
                => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            public static async Task<string> GetJsonAsync(string url, int maxRetries = 3)
            {
                for (int i = 0; i <= maxRetries; i++)
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        var response = await _client.GetAsync(url);
                        string json = await response.Content.ReadAsStringAsync();
                        sw.Stop();

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"GDT OK → {sw.ElapsedMilliseconds}ms");
                            return json;
                        }

                        // 401 → token sai → không retry
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                            throw new UnauthorizedAccessException("Token hết hạn hoặc sai!");

                        // Các lỗi khác (500, 503…) → retry
                        Console.WriteLine($"GDT lỗi {response.StatusCode} → retry {i + 1}/{maxRetries}");
                    }
                    catch (TaskCanceledException) when (i < maxRetries)
                    {
                        Console.WriteLine($"Timeout → retry {i + 1}/{maxRetries}");
                    }
                    catch (Exception ex) when (i < maxRetries)
                    {
                        Console.WriteLine($"Lỗi mạng → retry {i + 1}/{maxRetries}: {ex.Message}");
                    }

                    if (i < maxRetries)
                        await Task.Delay(500 * (i + 1)); // backoff: 500ms, 1000ms, 1500ms
                }

                throw new Exception("Gọi API GDT thất bại sau nhiều lần thử");
            }
            // Thay đổi phương thức thành async 
            public static async Task DownloadFileAsync(
     string url,
     string savePath,
     string token = null,
     DateTime dt = default,
     Action<bool, string, long> completionCallback = null)
            {
                if (!string.IsNullOrEmpty(token))
                    UpdateToken(token);

                const int maxRetries = 2;
                int retryCount = 0;

                var sw = Stopwatch.StartNew();

                while (retryCount < maxRetries)
                {
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.Accept.Clear();
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                        // Thêm các header khác nếu cần

                        HttpResponseMessage response = new HttpResponseMessage();

                        try
                        {
                            response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false); // QUAN TRỌNG: Không capture UI context 
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception ex)
                        {
                            // XtraMessageBox.Show(ex.Message);
                            await Task.Delay(1000); // 2s, 4s, 6s
                        }


                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                        {
                            await stream.CopyToAsync(fs);
                        }

                        sw.Stop();
                        Console.WriteLine($"Tải thành công: {Path.GetFileName(savePath)} - Thời gian: {sw.ElapsedMilliseconds} ms");

                        ExtractZipXMLAsynce(savePath); // Giải nén file ZIP
                        currentProgress2 += 1;
                        completionCallback?.Invoke(true, $"Tải thành công: {Path.GetFileName(savePath)}", currentProgress2);

                        return; // Thành công → thoát hẳn
                    }
                    catch (Exception ex) when (retryCount < maxRetries - 1) // Chỉ retry nếu còn lượt
                    {
                        retryCount++;
                        Console.WriteLine($"Lỗi tải file lần {retryCount}: {ex.Message}. Thử lại sau 2 giây...");

                        // Optional: delay tăng dần (exponential backoff)
                        await Task.Delay(1000); // 2s, 4s, 6s

                        // Nếu là lỗi mạng/timeout thì tiếp tục retry, các lỗi khác có thể không muốn retry
                        // Bạn có thể lọc cụ thể hơn:
                        // if (ex is HttpRequestException || ex is TaskCanceledException) { ... }
                    }
                }

                // Nếu ra khỏi vòng lặp nghĩa là đã thử 3 lần vẫn thất bại
                sw.Stop();
                string errorMsg = $"Tải file thất bại sau {maxRetries} lần thử: {Path.GetFileName(savePath)}";
                Console.WriteLine(errorMsg);
                completionCallback?.Invoke(false, errorMsg, currentProgress2);

                // Có thể throw hoặc không tùy nhu cầu
                throw new Exception(errorMsg);
            }
        }
        public async Task DocfileExcelVaoAsync(string mstcongty, string savedPath, string originpath)
        {
            GDTClient.UpdateToken(tokken);
            LoadHoadonCT();
            Loadtbimport();
            string querykh = @" SELECT *  FROM tbimport"; // Sử dụng ? thay cho @mst trong OleDb
            tbimport = ExecuteQuery(querykh, new OleDbParameter("?", ""));
            querykh = @" SELECT *  FROM Chungtu"; // Sử dụng ? thay cho @mst trong OleDb
            tbchungtu = ExecuteQuery(querykh, new OleDbParameter("?", ""));

            //  string directoryPath = Path.Combine(savedPath, "HDVao", DateTime.Now.Month.ToString());
            string directoryPath = Path.Combine(savedPath, DateTime.Now.Month.ToString());
            var excelFiles = Directory.EnumerateFiles(directoryPath, "*.xlsx", SearchOption.AllDirectories).Where(m => m.Contains(mstcongty)).ToList();
            int totalInvoices = 0;

            int i = 1;
            // Đếm tổng số hóa đơn cần xử lý (để hiển thị tiến độ chính xác)
            foreach (var excelFile in excelFiles)
            {
                using (var workbook = new XLWorkbook(excelFile))
                {
                    var worksheet = workbook.Worksheet(1);
                    foreach (var row in worksheet.RowsUsed().Skip(3))
                    {
                        string GetNLap = row.Cell("E").Value.ToString();
                        string getSohd = Helpers.RemoveLeadingZeros(row.Cell("D").Value.ToString()); // Lấy giá trị của cột C trong hàng hiện tại 
                        string mstnb = row.Cell("F").Value.ToString();
                        if (DateTime.TryParse(GetNLap, out DateTime getdate))
                        {
                            DateTime gd = DateTime.Parse(GetNLap);
                            bool daTonTai = lookupHoaDonCT.Contains((mstnb, getSohd, gd.Date));
                            bool daTonTaiimport = lookupTbImport.Contains((mstnb, getSohd, gd.Date));
                            if (daTonTai || daTonTaiimport)
                            {
                                continue;
                            }
                            totalInvoices++;
                        }
                    }
                }
            }

            foreach (var excelFile in excelFiles)
            {
                using (var workbook = new XLWorkbook(excelFile))

                {
                    var worksheet = workbook.Worksheet(1); // Lấy sheet đầu tiên
                    foreach (var row in worksheet.RowsUsed().Skip(3)) // Bỏ qua 6 hàng đầu tiên
                    {
                        string khhd = row.Cell("B").Value.ToString(); // Lấy giá trị của cột A trong hàng hiện tại
                        string getSHHD = row.Cell("C").Value.ToString(); // Lấy giá trị của cột A trong hàng hiện tại
                        string getSohd = RemoveLeadingZeros(row.Cell("D").Value.ToString()); // Lấy giá trị của cột C trong hàng hiện tại 
                        string GetNLap = row.Cell("E").Value.ToString();
                        string mstnb = row.Cell("F").Value.ToString();

                        //Kiểm tra từ ngày đến ngày
                        DateTime getdate = DateTime.Parse(GetNLap);
                        bool daTonTai = lookupHoaDonCT.Contains((mstnb, getSohd, getdate.Date));
                        bool daTonTaiimport = lookupTbImport.Contains((mstnb, getSohd, getdate.Date));
                        if (daTonTai || daTonTaiimport)
                        {
                            continue;
                        }

                        //Kiểm tra xem hoá đơn đã có trong bảng tbimport chưa


                        // var checkFile=tbimport.AsEnumerable().Where()
                        var checkExist = tbimport.AsEnumerable().Where(m => m.Field<string>("SHDon") == getSohd && m.Field<string>("Mst") == mstnb && m.Field<DateTime>("Nlap").Date == getdate.Date).FirstOrDefault();
                        if (checkExist != null)
                        {
                            Console.WriteLine("File đã import");
                            continue;
                        }
                        if (getSohd == "71881")
                        {
                            int a = 10;
                        }
                        var checkExistct = tbchungtu.AsEnumerable().Where(m => m.Field<string>("SoHieu") == getSohd && m.Field<DateTime>("NgayCT").Date == getdate.Date).FirstOrDefault();
                        if (checkExistct != null)
                        {
                            Console.WriteLine("File đã import");
                            continue;
                        }
                        //Tải file xml
                        string url = "";
                        if (i == 1 || i == 2)
                        {
                            url = $"https://hoadondientu.gdt.gov.vn:30000/query/invoices/export-xml?nbmst={mstnb}&khhdon={getSHHD}&shdon={getSohd}&khmshdon={khhd}";
                        }
                        if (i == 3)
                        {
                            url = $"https://hoadondientu.gdt.gov.vn:30000/sco-query/invoices/export-xml?nbmst={mstnb}&khhdon={getSHHD}&shdon={getSohd}&khmshdon={khhd}";
                        }


                        string pathravao = "HDVao";
                        string filename = $"{mstnb}_{getSohd}_{getSHHD}.zip";
                        string path = Path.Combine(directoryPath, filename);
                        string filenamexml = $"{mstnb}_{getSohd}_{getSHHD}.xml";
                        string pathxml = Path.Combine(directoryPath, filenamexml);
                        //Kiểm tra nếu hoá đơn chưa dc tải thì tải về
                        if (!File.Exists(path) && !File.Exists(pathxml))
                        {
                            try
                            {
                                // Tối ưu: Bỏ Thread.Sleep không cần thiết 
                                await GDTClient2.DownloadFileAsync(
                                url: url,
                                savePath: path,
                                token: tokken,
                                dt: getdate,
                                completionCallback: (success, message, progressCount) =>  // THÊM CALLBACK
                                {
                                    // CHỈ CHẠY KHI FILE TẢI XONG THẬT SỰ
                                    if (success)
                                    {
                                        // Cập nhật UI
                                        progressPanel1.Invoke(new Action(async () =>
                                        {


                                            if (currentProgress2 == totalInvoices)
                                            {

                                            }

                                        }));

                                        Console.WriteLine($"✅ Đã tải xong: {message}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"❌ Lỗi: {message}");
                                    }
                                }
                            );
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");

                                //if (i == 2)
                                //    GetKNMXML(mstnb, getSHHD, getSohd, tokken, getdate, folderpath, filename);

                            }
                        }
                    }
                }
                i++;
            }
        }
        static int currentProgress2 = 0;
        private static void ExtractZipXMLAsynce(string path)
        {

            try
            {
                while (File.Exists(path))
                {
                    Application.DoEvents();
                    string rootPath = Path.GetDirectoryName(path);
                    string getnamefile = Path.GetFileNameWithoutExtension(path);
                    string directoryPath = rootPath + @"\Giainen" + "_" + getnamefile;

                    ZipFile.ExtractToDirectory(path, directoryPath);

                    var files = Directory.GetFiles(directoryPath, "invoice.html", SearchOption.AllDirectories);
                    string targetFilePath = Path.Combine(rootPath, getnamefile + ".html");
                    File.Move(files.FirstOrDefault(), targetFilePath);

                    //xml
                    var filesxml = Directory.GetFiles(directoryPath, "invoice.xml", SearchOption.AllDirectories);
                    string targetFilePathxml = Path.Combine(rootPath, getnamefile + ".xml");
                    File.Move(filesxml.FirstOrDefault(), targetFilePathxml);

                    File.Delete(path);
                    Directory.Delete(directoryPath, true);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi giải nén hoặc xử lý file: {ex.Message}");
            }

        }
        private static void ExtractZipXML(string path)
        {

            try
            {

                Application.DoEvents();
                string rootPath = Path.GetDirectoryName(path);
                string getnamefile = Path.GetFileNameWithoutExtension(path);
                string directoryPath = rootPath + @"\Giainen" + "_" + getnamefile;

                ZipFile.ExtractToDirectory(path, directoryPath);

                var files = Directory.GetFiles(directoryPath, "invoice.html", SearchOption.AllDirectories);
                string targetFilePath = Path.Combine(rootPath, getnamefile + ".html");
                File.Move(files.FirstOrDefault(), targetFilePath);

                //xml
                var filesxml = Directory.GetFiles(directoryPath, "invoice.xml", SearchOption.AllDirectories);
                string targetFilePathxml = Path.Combine(rootPath, getnamefile + ".xml");
                File.Move(filesxml.FirstOrDefault(), targetFilePathxml);

                File.Delete(path);
                Directory.Delete(directoryPath, true);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi giải nén hoặc xử lý file: {ex.Message}");
            }

        }
        public void Xulyexelvao(string token, int _type)
        {
            // Tối ưu: Tính toán datetime một lần
            DateTime dtFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            //DateTime dtTo = dtFrom.AddMonths(1).AddDays(-1);
            DateTime dtTo = DateTime.Now;

            // Tối ưu: Format string một lần
            string formattedDate1 = dtFrom.ToString("dd/MM/yyyyTHH:mm:ss");
            string formattedDate2 = dtTo.ToString("dd/MM/yyyyTHH:mm:ss");

            // Tối ưu: Dùng switch case thay vì nhiều if
            string url, filename;
            switch (_type)
            {
                case 1:
                    url = $@"https://hoadondientu.gdt.gov.vn:30000/query/invoices/export-excel-sold?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge={formattedDate1};tdlap=le={formattedDate2};ttxly==5%20%20%20%20&type=purchase";
                    filename = $"{mstcongty}_HDDienTuDaCapMa.xlsx";
                    break;
                case 2:
                    url = $@"https://hoadondientu.gdt.gov.vn:30000/query/invoices/export-excel-sold?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge={formattedDate1};tdlap=le={formattedDate2};ttxly==6%20%20%20%20&type=purchase";
                    filename = $"{mstcongty}_HDDienTuKhongMa.xlsx";
                    break;
                case 3:
                    url = $@"https://hoadondientu.gdt.gov.vn:30000/sco-query/invoices/export-excel-sold?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge={formattedDate1};tdlap=le={formattedDate2};ttxly==8%20%20%20%20&type=purchase";
                    filename = $"{mstcongty}_HDDienTuMayTinhTien.xlsx";
                    break;
                default:
                    return;
            }
            string currentYear = $"HD{DateTime.Now.Year}";
            string directoryPath = Path.Combine(savedPath, currentYear, "HDVao", DateTime.Now.Month.ToString());
            string filePath = Path.Combine(directoryPath, filename);

            // Tối ưu: Đảm bảo thư mục tồn tại trước
            Directory.CreateDirectory(directoryPath);

            // Xóa file cũ nếu tồn tại
            if (File.Exists(filePath))
            {
                DateTime lastWriteTime = File.GetLastWriteTime(filePath);
                TimeSpan timeDifference = DateTime.Now - lastWriteTime;

                if (timeDifference.TotalMinutes > 30)
                {
                    File.Delete(filePath);
                    Console.WriteLine($"Đã xóa file: {filePath}");
                }
                else
                {
                    Console.WriteLine($"File chưa đủ 30 phút để xóa. Thời gian còn lại: {30 - timeDifference.TotalMinutes:F1} phút");
                    return;
                }
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                try
                {
                    // Tối ưu: Bỏ Thread.Sleep không cần thiết
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    progressPanel1.Caption = $"Đang tải {filePath} ";
                    Application.DoEvents();
                    response.EnsureSuccessStatusCode();

                    var fileBytes = response.Content.ReadAsByteArrayAsync().Result;
                    File.WriteAllBytes(filePath, fileBytes);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");
                }
            }
        }
        public void Xulyexelra(string token, int _type)
        {
            // Tối ưu: Tính toán datetime một lần
            DateTime dtFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime dtTo = dtFrom.AddMonths(1).AddDays(-1);

            // Tối ưu: Format string một lần
            string formattedDate1 = dtFrom.ToString("dd/MM/yyyyTHH:mm:ss");
            string formattedDate2 = dtTo.ToString("dd/MM/yyyyTHH:mm:ss");

            // Tối ưu: Dùng switch case thay vì nhiều if
            string url, filename;
            switch (_type)
            {
                case 1:
                    url = @"https://hoadondientu.gdt.gov.vn:30000/query/invoices/export-excel?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=" + formattedDate1 + ";tdlap=le=" + formattedDate2;
                    filename = $"{mstcongty}_Hoadondientu.xlsx";
                    break;
                case 2:
                    url = @"https://hoadondientu.gdt.gov.vn:30000/sco-query/invoices/export-excel?sort=tdlap:desc,khmshdon:asc,shdon:desc&search=tdlap=ge=" + formattedDate1 + ";tdlap=le=" + formattedDate2;
                    filename = $"{mstcongty}_HDDienTuMayTinhTien.xlsx";
                    break;
                default:
                    return;
            }
            string currentYear = $"HD{DateTime.Now.Year}";
            string directoryPath = Path.Combine(savedPath, currentYear, "HDRa", DateTime.Now.Month.ToString());
            string filePath = Path.Combine(directoryPath, filename);

            // Tối ưu: Đảm bảo thư mục tồn tại trước
            Directory.CreateDirectory(directoryPath);

            // Xóa file cũ nếu tồn tại
            if (File.Exists(filePath))
            {
                DateTime lastWriteTime = File.GetLastWriteTime(filePath);
                TimeSpan timeDifference = DateTime.Now - lastWriteTime;

                if (timeDifference.TotalMinutes > 30)
                {
                    File.Delete(filePath);
                    Console.WriteLine($"Đã xóa file: {filePath}");
                }
                else
                {
                    Console.WriteLine($"File chưa đủ 30 phút để xóa. Thời gian còn lại: {30 - timeDifference.TotalMinutes:F1} phút");
                    return;
                }
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                try
                {
                    // Tối ưu: Bỏ Thread.Sleep không cần thiết
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    progressPanel1.Caption = $"Đang tải {filePath} ";
                    Application.DoEvents();
                    response.EnsureSuccessStatusCode();

                    var fileBytes = response.Content.ReadAsByteArrayAsync().Result;
                    File.WriteAllBytes(filePath, fileBytes);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");
                }
            }
        }
    }
}