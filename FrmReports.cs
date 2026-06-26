using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using DevExpress.XtraEditors;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Table = CrystalDecisions.CrystalReports.Engine.Table;

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
        string[] rootPath;
        string reportpath;

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

        private void FrmReports_Load(object sender, EventArgs e)
        {
            try
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string root = Path.GetFullPath(Path.Combine(exeDir, @"..\.."));

                string pathFile = Path.Combine(root, "hoadon", "invoice.txt");
                reportpath = File.ReadAllText(pathFile).Trim();
                rootPath = reportpath.Split('|');

                connectionString = ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;
                this.WindowState = FormWindowState.Maximized;

                var query = @"SELECT * FROM Sqlinfo";
                var dt = ExecuteQuery(query, null);
                if (dt != null && dt.Rows.Count > 0)
                {
                    LoadReport(rootPath[0],
                        dt.Rows[0]["SqlIp"].ToString(),
                        dt.Rows[0]["SqlDatabase"].ToString(),
                        dt.Rows[0]["SqlUsername"].ToString(),
                        dt.Rows[0]["SqlPassword"].ToString());
                }
                else
                {
                    MessageBox.Show("Không tìm thấy thông tin kết nối!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
            crystalReportViewer1.ToolPanelView = CrystalDecisions.Windows.Forms.ToolPanelViewType.None;
        }

        private void LoadReport(string rptPath, string server, string db, string user, string pass)
        {
            try
            {
                // ✅ Giải phóng report cũ
                if (rpt != null)
                {
                    try { rpt.Close(); rpt.Dispose(); } catch { }
                    rpt = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                rpt = new ReportDocument();
                rpt.Load(rptPath);

                // ✅ Set Database Logon
                SetDBLogonForReport(rpt, server, db, user, pass);

                // ✅ Load Parameters
                Loadparameter();

                // ✅ Gán ReportSource
                crystalReportViewer1.ReportSource = rpt;
                crystalReportViewer1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải report: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void SetDBLogonForReport(ReportDocument reportDoc, string server, string db, string user, string pass)
        {
            try
            {
                ConnectionInfo crConn = new ConnectionInfo();
                crConn.ServerName = server;
                crConn.DatabaseName = db;
                crConn.UserID = user;
                crConn.Password = pass;
                crConn.IntegratedSecurity = false;
                crConn.Type = ConnectionInfoType.SQL;

                foreach (Table table in reportDoc.Database.Tables)
                {
                    TableLogOnInfo logon = table.LogOnInfo;
                    logon.ConnectionInfo = crConn;
                    table.ApplyLogOnInfo(logon);

                    string tableName = table.Name;
                    if (tableName.Contains("."))
                    {
                        tableName = tableName.Substring(tableName.LastIndexOf(".") + 1);
                    }
                    table.Location = tableName;
                }

                foreach (ReportDocument sub in reportDoc.Subreports)
                {
                    SetDBLogonForReport(sub, server, db, user, pass);
                }

                // ✅ Bỏ VerifyDatabase nếu gây lỗi
                // try
                // {
                //     reportDoc.VerifyDatabase();
                // }
                // catch (Exception ex)
                // {
                //     System.Diagnostics.Debug.WriteLine($"VerifyDatabase lỗi: {ex.Message}");
                // }

                // ✅ Refresh (không Verify)
                try
                {
                    reportDoc.Refresh();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Refresh lỗi: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi SetDBLogon: {ex.Message}");
                throw;
            }

        }

        private void Loadparameter()
        {
            try
            {
                if (rpt == null) return;

                int fromdate = int.Parse(rootPath[1].Split('/')[0]);
                int todate = int.Parse(rootPath[2].Split('/')[0]);
                string thoiGianValue = TinhThoiGian(fromdate, todate, int.Parse(rootPath[1].Split('/')[1]));

                SetFormulaIfExists(rpt, "thoigian", thoiGianValue);
                SetFormulaIfExists(rpt, "ThoiGian", thoiGianValue);


                DateTime ngayCuoiThang = new DateTime(DateTime.Now.Year, todate, 1)
                            .AddMonths(1)
                            .AddDays(-1);

                string ketQua = $"Ngµy {ngayCuoiThang.Day} th¸ng {ngayCuoiThang.Month} n¨m {ngayCuoiThang.Year}";
                SetFormulaIfExists(rpt, "Ngay", ketQua);

                // License
                DataTable tbLicense = ExecuteQuery("SELECT * FROM License", null);
                if (tbLicense != null && tbLicense.Rows.Count > 0)
                {
                    SetFormulaString(rpt, "TenCty", tbLicense.Rows[0]["TenCty"].ToString());
                    SetFormulaString(rpt, "MSThue", tbLicense.Rows[0]["MaSoThue"].ToString());
                    SetFormulaString(rpt, "TenCn", "MST: " + tbLicense.Rows[0]["MaSoThue"].ToString());
                }

                // KhoHang
                DataTable tbKhoHang = ExecuteQuery("SELECT * FROM KhoHang", null);
                if (tbKhoHang != null && tbKhoHang.Rows.Count > 0)
                {
                    SetFormulaIfExists(rpt, "TenKho", "Kho: " + tbKhoHang.Rows[0]["tenKho"].ToString());
                }

                // Sqlinfo
                DataTable dtSqlinfo = ExecuteQuery("SELECT * FROM Sqlinfo", null);
                if (dtSqlinfo != null && dtSqlinfo.Rows.Count > 0)
                {
                    SetFormulaIfExists(rpt, "DkLuong", dtSqlinfo.Rows[0]["tondauky"].ToString());
                    SetFormulaIfExists(rpt, "DkTien", dtSqlinfo.Rows[0]["tiendauky"].ToString());
                }

                // THEKHO
                if (rootPath[0].Contains("THEKHO"))
                {
                    DataTable dt = ExecuteQuery("SELECT * FROM TheKho", null);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        SetFormulaIfExists(rpt, "TenVt", dt.Rows[0]["TenVt"].ToString());
                    }
                }

                // SOCAI
                if (rootPath[0].ToLower().Contains("socai"))
                {
                    DataTable dt = ExecuteQuery("SELECT * FROM Socai", null);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        SetFormulaString(rpt, "SoHieuTK", dt.Rows[0]["SoHieuTK"].ToString());
                        SetFormulaIfExists(rpt, "NoDk", dt.Rows[0]["NoDk"].ToString());
                        SetFormulaIfExists(rpt, "Kieu", dt.Rows[0]["Kieu"].ToString());
                        SetFormulaIfExists(rpt, "TenTk", dt.Rows[0]["TenTk"].ToString());
                        SetFormulaIfExists(rpt, "NoLK", dt.Rows[0]["NoLK"].ToString());
                        SetFormulaIfExists(rpt, "CoLK", dt.Rows[0]["CoLK"].ToString());
                    }
                }

                // CTTKCN
                if (rootPath[0].Contains("CTTKCN"))
                {
                    DataTable dt = ExecuteQuery("SELECT * FROM Congno", null);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        SetFormulaString(rpt, "SoHieuTK", dt.Rows[0]["SoHieuTK"].ToString());
                        SetFormulaIfExists(rpt, "MaCN", dt.Rows[0]["MaCN"].ToString());
                        SetFormulaIfExists(rpt, "NoLK", dt.Rows[0]["NoLK"].ToString());
                        SetFormulaIfExists(rpt, "CoLK", dt.Rows[0]["CoLK"].ToString());
                        SetFormulaIfExists(rpt, "TenTk", dt.Rows[0]["TenTk"].ToString());
                        SetFormulaIfExists(rpt, "NoDK", dt.Rows[0]["NoDK"].ToString());
                        SetFormulaIfExists(rpt, "Kieu", dt.Rows[0]["Kieu"].ToString());
                    }
                }

                // TOKHAI
                if (rootPath[0].Contains("TOKHAI"))
                {
                    DataTable dt = ExecuteQuery("SELECT * FROM vw_ToKhaiVAT", null);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        SetFormulaString(rpt, "DiaChi", dt.Rows[0]["DiaChi"].ToString());
                        SetFormulaString(rpt, "MSThue", dt.Rows[0]["MSThue"].ToString());
                        SetFormulaString(rpt, "SoHieuTK", dt.Rows[0]["SoHieuTK"].ToString());
                        SetFormulaString(rpt, "Quan", dt.Rows[0]["Quan"].ToString());

                        SetFormulaIfExists(rpt, "Thang", dt.Rows[0]["Thang"].ToString());
                        SetFormulaIfExists(rpt, "ThangCuoi", dt.Rows[0]["ThangCuoi"].ToString());
                        SetFormulaIfExists(rpt, "DTKCT", dt.Rows[0]["DTKCT"].ToString());
                        SetFormulaIfExists(rpt, "Vat0", dt.Rows[0]["Vat0"].ToString());
                        SetFormulaIfExists(rpt, "Vat10DT", dt.Rows[0]["Vat10DT"].ToString());
                        SetFormulaIfExists(rpt, "Vat10", dt.Rows[0]["Vat10"].ToString());
                        SetFormulaIfExists(rpt, "TongVaoV", dt.Rows[0]["TongVaoV"].ToString());
                        SetFormulaIfExists(rpt, "KyTruoc", dt.Rows[0]["KyTruoc"].ToString());
                        SetFormulaIfExists(rpt, "TongVao", dt.Rows[0]["TongVao"].ToString());
                        SetFormulaIfExists(rpt, "TongVATx", dt.Rows[0]["TongVATx"].ToString());
                        SetFormulaIfExists(rpt, "TongVATV", dt.Rows[0]["TongVATV"].ToString());
                        SetFormulaIfExists(rpt, "TongDoanhThu", dt.Rows[0]["TongDoanhThu"].ToString());
                        SetFormulaIfExists(rpt, "TongVAT", dt.Rows[0]["TongVAT"].ToString());
                    }
                }

                // SOQUY
                if (rootPath[0].Contains("SOQUY"))
                {
                    DataTable dt = ExecuteQuery("SELECT * FROM SoQuy", null);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        SetFormulaString(rpt, "SoHieuTK", dt.Rows[0]["SoHieuTK"].ToString());
                        SetFormulaIfExists(rpt, "NoDK", dt.Rows[0]["NoDK"].ToString());
                        SetFormulaIfExists(rpt, "CoDK", dt.Rows[0]["CoDK"].ToString());
                        SetFormulaIfExists(rpt, "Kieu", dt.Rows[0]["Kieu"].ToString());
                        SetFormulaString(rpt, "TenTK", dt.Rows[0]["TenTK"].ToString());
                        SetFormulaIfExists(rpt, "NoLK", dt.Rows[0]["NoLK"].ToString());
                        SetFormulaIfExists(rpt, "CoLK", dt.Rows[0]["CoLK"].ToString());
                        SetFormulaIfExists(rpt, "SoDuCK", dt.Rows[0]["SoDuCK"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi Loadparameter: {ex.Message}");
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

        private void SetFormulaIfExists(ReportDocument rpt, string formulaName, string value)
        {
            try
            {
                FormulaFieldDefinition formula = rpt.DataDefinition.FormulaFields[formulaName];
                double so;
                bool laSo = double.TryParse(value, out so);

                if (laSo)
                {
                    formula.Text = value;
                }
                else
                {
                    formula.Text = "'" + value + "'";
                }
            }
            catch { }
        }

        private DataTable ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
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

        private int ExecuteQueryResult(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    if (query.Trim().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                    {
                        string insertWithIdentity = query.TrimEnd() + "; SELECT SCOPE_IDENTITY();";
                        command.CommandText = insertWithIdentity;
                        object result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return Convert.ToInt32(result);
                        return 0;
                    }
                    else
                    {
                        return command.ExecuteNonQuery();
                    }
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (rpt != null)
            {
                try
                {
                    rpt.Close();
                    rpt.Dispose();
                }
                catch { }
                rpt = null;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}