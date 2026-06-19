using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using DevExpress.Map.Kml.Model;
using DevExpress.Map.Native;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaovietTax
{
    public partial class FrmReports : DevExpress.XtraEditors.XtraForm
    {
        private ReportDocument rpt;

        public FrmReports()
        {
            InitializeComponent();
        }
        public string connectionString;
        private string TinhThoiGian(int thangDau, int thangCuoi, int nam)
        {
            if (thangDau == thangCuoi)
            {
                return "Th¸ng " + thangDau + "/" + nam;
            }
            else
            {
                return "Tõ Th¸ng " + thangDau + " ®Õn Th¸ng " + thangCuoi + " n¨m " + nam;
            }
        }
        string[] rootPath ;
        string reportpath;
        private void FrmReports_Load(object sender, EventArgs e)
        {
            string exeDir = Path.GetDirectoryName(
               Assembly.GetExecutingAssembly().Location);

            string root = Path.GetFullPath(
                    Path.Combine(exeDir, @"..\.."));
            var pah = Path.Combine(root, "REPORTS");

            string pathFile = Path.Combine(root, "hoadon", "invoice.txt");
            reportpath = File.ReadAllText(pathFile).Trim();
            rootPath = reportpath.Split('|');

            connectionString =
    ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;
            this.WindowState = FormWindowState.Maximized;

            var query = @"SELECT * FROM Sqlinfo";
            var dt = ExecuteQuery(query, null);
            if (dt != null)
            {
              
                ReportDocument rpt = new ReportDocument();
                rpt.Load(rootPath[0]);
                //MessageBox.Show("Integrated: " + rpt.Database.Tables[0].LogOnInfo.ConnectionInfo.IntegratedSecurity.ToString() +
                //                "\nServer: " + rpt.Database.Tables[0].LogOnInfo.ConnectionInfo.ServerName +
                //                "\nDB: " + rpt.Database.Tables[0].LogOnInfo.ConnectionInfo.DatabaseName);

                LoadReport(rootPath[0], dt.Rows[0]["SqlIp"].ToString(), dt.Rows[0]["SqlDatabase"].ToString(), dt.Rows[0]["SqlUsername"].ToString(),dt.Rows[0]["SqlPassword"].ToString());
            } 
        }
        private void Loadparameter()
        {
            int fromdate = int.Parse(rootPath[1].Split('/')[0]);
            int todate = int.Parse(rootPath[2].Split('/')[0]);
            string thoiGianValue = TinhThoiGian(fromdate, todate, int.Parse(rootPath[1].Split('/')[1]));

            // Set formula "thoigian" (có kiểm tra)
            SetFormulaIfExists(rpt, "thoigian", thoiGianValue);
            SetFormulaIfExists(rpt, "ThoiGian", thoiGianValue);
            SetFormulaIfExists(rpt, "Ngay", DateTime.Now.ToShortDateString());
            // Load thông tin công ty
            DataTable tbLicnese = new DataTable();
            var query = @"SELECT * FROM License";
            tbLicnese = ExecuteQuery(query, null);

            if (tbLicnese != null && tbLicnese.Rows.Count > 0)
            {
                string tenCty = tbLicnese.Rows[0]["TenCty"].ToString();
                string mstCty = tbLicnese.Rows[0]["MaSoThue"].ToString();

                // Set formula "TenCty" (chỉ set nếu có)
                SetFormulaString(rpt, "TenCty", tenCty);

                // Set formula "MSThue" (chỉ set nếu có)
                SetFormulaString(rpt, "MSThue", mstCty);
                SetFormulaString(rpt, "TenCn", $"MST: {mstCty}");
            }

            //
            var qrkho = @"SELECT * FROM KhoHang";
            DataTable  tbKhohang = ExecuteQuery(qrkho, null);
            if(tbKhohang != null)
            {
                SetFormulaIfExists(rpt, "TenKho", $"Kho: {tbKhohang.Rows[0]["tenKho"].ToString()}");
            }
            //Tồn kho
            var qrTonkho = @"SELECT * FROM Sqlinfo";
            var dt = ExecuteQuery(qrTonkho, null);
            if (dt != null)
            {
                SetFormulaIfExists(rpt, "DkLuong", dt.Rows[0]["tondauky"].ToString());
                SetFormulaIfExists(rpt, "DkTien", dt.Rows[0]["tiendauky"].ToString());
            }
            if (rootPath[0].Contains("THEKHO"))
            {
                var qrTheKho = @"SELECT * FROM TheKho";
                var getqrTheKho = ExecuteQuery(qrTheKho, null);
                SetFormulaIfExists(rpt, "TenVt", getqrTheKho.Rows[0]["TenVt"].ToString());
            }
            if (rootPath[0].ToLower().Contains("socai"))
            {
                var qrTheKho = @"SELECT * FROM Socai";
                var getqrTheKho = ExecuteQuery(qrTheKho, null);
                var test = getqrTheKho.Rows[0]["SoHieuTK"].ToString();
                SetFormulaString(rpt, "SoHieuTK", getqrTheKho.Rows[0]["SoHieuTK"].ToString());
                SetFormulaIfExists(rpt, "NoDk", getqrTheKho.Rows[0]["NoDk"].ToString());
                SetFormulaIfExists(rpt, "Kieu", getqrTheKho.Rows[0]["Kieu"].ToString());
                SetFormulaIfExists(rpt, "TenTk", getqrTheKho.Rows[0]["TenTk"].ToString());
                SetFormulaIfExists(rpt, "NoLK", getqrTheKho.Rows[0]["NoLK"].ToString());
                SetFormulaIfExists(rpt, "CoLK", getqrTheKho.Rows[0]["CoLK"].ToString());
            }
            if (rootPath[0].Contains("CTTKCN"))
            {

                var qrTheKho = @"SELECT * FROM Congno";
                var getqrTheKho = ExecuteQuery(qrTheKho, null);
                var test = getqrTheKho.Rows[0]["SoHieuTK"].ToString();
                SetFormulaString(rpt, "SoHieuTK", getqrTheKho.Rows[0]["SoHieuTK"].ToString());
                SetFormulaIfExists(rpt, "MaCN", getqrTheKho.Rows[0]["MaCN"].ToString());
                SetFormulaIfExists(rpt, "NoLK", getqrTheKho.Rows[0]["NoLK"].ToString());
                SetFormulaIfExists(rpt, "CoLK", getqrTheKho.Rows[0]["CoLK"].ToString());
                SetFormulaIfExists(rpt, "TenTk", getqrTheKho.Rows[0]["TenTk"].ToString());
                SetFormulaIfExists(rpt, "NoDK", getqrTheKho.Rows[0]["NoDK"].ToString());
                SetFormulaIfExists(rpt, "Kieu", getqrTheKho.Rows[0]["Kieu"].ToString());
            }
            if (rootPath[0].Contains("TOKHAI"))
            {

                var qrTheKho = @"SELECT * FROM vw_ToKhaiVAT";
                var getqrTheKho = ExecuteQuery(qrTheKho, null);
                var ddd = getqrTheKho.Rows[0]["Diachi"].ToString();
                // Gán dữ liệu - Giữ nguyên kiểu getqrTheKho
                SetFormulaString(rpt, "DiaChi", getqrTheKho.Rows[0]["DiaChi"].ToString());
                SetFormulaString(rpt, "MSThue", getqrTheKho.Rows[0]["MSThue"].ToString());
                SetFormulaString(rpt, "SoHieuTK", getqrTheKho.Rows[0]["SoHieuTK"].ToString());
                SetFormulaString(rpt, "Quan", getqrTheKho.Rows[0]["Quan"].ToString());

                // Gán số - Không Convert, để nguyên
                SetFormulaIfExists(rpt, "Thang", getqrTheKho.Rows[0]["Thang"].ToString());
                SetFormulaIfExists(rpt, "ThangCuoi", getqrTheKho.Rows[0]["ThangCuoi"].ToString());
                SetFormulaIfExists(rpt, "DTKCT", getqrTheKho.Rows[0]["DTKCT"].ToString());
                SetFormulaIfExists(rpt, "Vat0", getqrTheKho.Rows[0]["Vat0"].ToString());
                SetFormulaIfExists(rpt, "Vat10DT", getqrTheKho.Rows[0]["Vat10DT"].ToString());
                SetFormulaIfExists(rpt, "Vat10", getqrTheKho.Rows[0]["Vat10"].ToString());
                SetFormulaIfExists(rpt, "TongVaoV", getqrTheKho.Rows[0]["TongVaoV"].ToString());
                SetFormulaIfExists(rpt, "KyTruoc", getqrTheKho.Rows[0]["KyTruoc"].ToString());
                SetFormulaIfExists(rpt, "TongVao", getqrTheKho.Rows[0]["TongVao"].ToString());
                SetFormulaIfExists(rpt, "TongVATx", getqrTheKho.Rows[0]["TongVATx"].ToString());
                SetFormulaIfExists(rpt, "TongVATV", getqrTheKho.Rows[0]["TongVATV"].ToString());
                SetFormulaIfExists(rpt, "TongDoanhThu", getqrTheKho.Rows[0]["TongDoanhThu"].ToString());
                SetFormulaIfExists(rpt, "TongVAT", getqrTheKho.Rows[0]["TongVAT"].ToString());
            }
            if (rootPath[0].Contains("SOQUY"))
            {
                var qrTheKho = @"SELECT * FROM SoQuy";
                var getqrTheKho = ExecuteQuery(qrTheKho, null);
                SetFormulaString(rpt, "SoHieuTK", getqrTheKho.Rows[0]["SoHieuTK"].ToString());
                SetFormulaIfExists(rpt, "NoDK", getqrTheKho.Rows[0]["NoDK"].ToString());
                SetFormulaIfExists(rpt, "CoDK", getqrTheKho.Rows[0]["CoDK"].ToString());
                SetFormulaIfExists(rpt, "Kieu", getqrTheKho.Rows[0]["Kieu"].ToString());
                SetFormulaString(rpt, "TenTK", getqrTheKho.Rows[0]["TenTK"].ToString());
                SetFormulaIfExists(rpt, "NoLK", getqrTheKho.Rows[0]["NoLK"].ToString());
                SetFormulaIfExists(rpt, "CoLK", getqrTheKho.Rows[0]["CoLK"].ToString());
                SetFormulaIfExists(rpt, "SoDuCK", getqrTheKho.Rows[0]["SoDuCK"].ToString());
            }
        }
        private void SetFormulaString(ReportDocument rpt, string formulaName, string value)
        {
            try
            {
                FormulaFieldDefinition formula = rpt.DataDefinition.FormulaFields[formulaName];

                formula.Text = "'" + value + "'";
            }
            catch { }
        }

        // Hàm kiểm tra formula có tồn tại không trước khi set
        private void SetFormulaIfExists(ReportDocument rpt, string formulaName, string value)
        {
            try
            {
                FormulaFieldDefinition formula = rpt.DataDefinition.FormulaFields[formulaName];

                // Thử chuyển thành số
                double so;
                bool laSo = double.TryParse(value, out so);

                if (laSo)
                {
                    // Nếu là số -> không thêm nháy đơn
                    formula.Text = value;
                }
                else
                {
                    // Nếu là chuỗi -> thêm nháy đơn
                    formula.Text = "'" + value + "'";
                }
            }
            catch { }
        }
         

        private void LoadReport(string rptPath, string server, string db, string user, string pass)
        {
            try
            {
                rpt = new ReportDocument();
                rpt.Load(rptPath);
                SetDBLogonForReport(rpt, server, db, user, pass);
                rpt.SetDatabaseLogon(user, pass, server, db, false);

                Loadparameter();


                crystalReportViewer1.ReportSource = rpt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
                this.Close();
            }
        }
        private DataTable ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
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

            return dataTable; // Trả về DataTable chứa dữ liệu
        } 
        private int ExecuteQueryResult(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Kết nối đến cơ sở dữ liệu thành công! " + query);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    // Kiểm tra nếu là INSERT thì lấy ID, nếu không thì chỉ Execute
                    if (query.Trim().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                    {
                        // Gộp SELECT SCOPE_IDENTITY() vào câu lệnh INSERT
                        string insertWithIdentity = query.TrimEnd() + "; SELECT SCOPE_IDENTITY();";
                        command.CommandText = insertWithIdentity;

                        object result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return Convert.ToInt32(result);
                        return 0;
                    }
                    else
                    {
                        // Với UPDATE/DELETE, chỉ Execute và trả về số dòng ảnh hưởng
                        return command.ExecuteNonQuery();
                    }
                }
            }
        }
        private void SetDBLogonForReport(ReportDocument reportDoc, string server, string db, string user, string pass)
        {
            ConnectionInfo crConn = new ConnectionInfo();
            crConn.ServerName = server;
            crConn.DatabaseName = db;
            crConn.UserID = user;
            crConn.Password = pass;
            crConn.IntegratedSecurity = false; // Bắt buộc tắt Windows Auth
            crConn.Type = ConnectionInfoType.SQL; // Ép dùng OLE DB cho SQL

            foreach (Table table in reportDoc.Database.Tables)
            {
                TableLogOnInfo logon = table.LogOnInfo;
                logon.ConnectionInfo = crConn;
                table.ApplyLogOnInfo(logon);

                // Quan trọng: Xóa schema cũ dba.dbo.
                table.Location = table.Name;
            }

            foreach (ReportDocument sub in reportDoc.Subreports)
            {
                SetDBLogonForReport(sub, server, db, user, pass);
            }
        }

    }
}