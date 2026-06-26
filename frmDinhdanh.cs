using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using iText.StyledXmlParser.Jsoup.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SaovietTax.frmKiemtrahethong;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace SaovietTax
{
	public partial class frmDinhdanh: DevExpress.XtraEditors.XtraForm
	{
        public class PhanLoaiVattu
        {
            public int MaSo { get; set; }
            public string SoHieu { get; set; }  
            public string TenPhanLoai { get; set; }
            public string GhiChu { get; set; }
            public string TKCo { get; set; }
            public string TKNo { get; set; }
        }
        public frmDinhdanh()
		{
            InitializeComponent();
		}
        string dbPath = "";
        public DataTable result { get; set; }
        public frmMain frmMain { get; set; }
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
        DataTable PLHH;
        List<PhanLoaiVattu> PLHHList = new List<PhanLoaiVattu>();
        BindingSource BindingSource = new BindingSource();
        private bool Kiemtrataikhoancon(string tk)
        {
            string query = @"
    SELECT * FROM HeThongTK WHERE SoHieu = @SoHieu";

            var resultkm = ExecuteQuery(query, new SqlParameter("@SoHieu", tk));

            if (resultkm.Rows.Count > 0)
            {
                string getTK_ID2 = resultkm.Rows[0]["MaSo"].ToString();

                query = @"SELECT * FROM HeThongTK WHERE TKCha0 = @TKCha0";
                resultkm = ExecuteQuery(query, new SqlParameter("@TKCha0", getTK_ID2));

                if (resultkm.Rows.Count > 0)
                    return true;
            }
            return false;
        }
        private void frmDinhdanh_Load(object sender, EventArgs e)
        {
           
            //   comboBoxEdit1.Properties.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Combo;

            //   comboBoxEdit1.Properties.Items.AddRange(new string[]
            //{
            //   "Thấp",
            //   "Cao" 
            //});
            //   comboBoxEdit1.SelectedIndex = 0;
            //   comboBoxEdit1.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

            InitDB();
            LoadDataDinhDanh();
            LoadMacdinhVattu();
            string querykh = @" SELECT *  FROM PhanLoaiVattu"; // Sử dụng ? thay cho @mst trong OleDb

            PLHH = ExecuteQuery(querykh,null);  
            foreach(DataRow item in PLHH.Rows)
            {
                PhanLoaiVattu PhanLoaiVattu=new PhanLoaiVattu();
                PhanLoaiVattu.MaSo = item.Field<int>("MaSo");
                PhanLoaiVattu.SoHieu = item.Field<string>("SoHieu");
                PhanLoaiVattu.TenPhanLoai = Helpers.ConvertVniToUnicode(item.Field<string>("TenPhanLoai"));
                PhanLoaiVattu.GhiChu = item.Field<string>("GhiChu");
                PhanLoaiVattu.TKCo = item.Field<string>("TKCo");
                PhanLoaiVattu.TKNo = item.Field<string>("TKNo");
                PLHHList.Add(PhanLoaiVattu);
            }
            BindingSource.DataSource = PLHHList; 

        
            RepositoryItemButtonEdit buttonEdit = new RepositoryItemButtonEdit();
            buttonEdit.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
            buttonEdit.Buttons[0].Caption = "Xóa";
            buttonEdit.ButtonClick += ButtonEdit_ButtonClick;
            
            gridView2.Columns["colDelete"].ColumnEdit = buttonEdit;


            RepositoryItemButtonEdit buttonEdit2 = new RepositoryItemButtonEdit();
            buttonEdit2.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
            buttonEdit2.Buttons[0].Caption = "Xóa";
            buttonEdit2.ButtonClick += ButtonEdit_ButtonClick;

            gridView3.Columns["colDelete"].ColumnEdit = buttonEdit2;

            gridControl1.DataSource = BindingSource;


            //

            DevExpress.XtraGrid.Views.Grid.GridView view = gcDinhdanh.MainView as DevExpress.XtraGrid.Views.Grid.GridView;
            DevExpress.XtraGrid.Views.Grid.GridView view2 = gridControl2.MainView as DevExpress.XtraGrid.Views.Grid.GridView;
            if (frmMain != null)
            {
                if (!string.IsNullOrEmpty(frmMain.keyMST))
                {
                    for (int i = 0; i < view.RowCount; i++)
                    {
                        // Lấy giá trị của cột STT
                        if (view.GetRowCellValue(i, "KeyValue").ToString().ToLower() == frmMain.keyMST)
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                if (gridView2.RowCount > i) // Kiểm tra số lượng dòng
                                {
                                    gridView2.FocusedRowHandle = i; // Đặt focus
                                    gridView2.MakeRowVisible(i); // Cuộn đến dòng
                                    gridView2.SelectRow(i); // Chọn dòng 
                                }
                            });
                            return;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(frmMain.KeyDG))
                {
                    for (int i = 0; i < view.RowCount; i++)
                    {
                        // Lấy giá trị của cột STT
                        if (view.GetRowCellValue(i, "Type").ToString().ToLower() == frmMain.KeyDG.ToLower())
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                if (gridView2.RowCount > i) // Kiểm tra số lượng dòng
                                {
                                    gridView2.FocusedRowHandle = i; // Đặt focus
                                    gridView2.MakeRowVisible(i); // Cuộn đến dòng
                                    gridView2.SelectRow(i); // Chọn dòng 
                                }
                            });
                            return;
                        }
                    }
                }
                //
                if (!string.IsNullOrEmpty(frmMain.keyMST2))
                {
                    for (int i = 0; i < view2.RowCount; i++)
                    {
                        // Lấy giá trị của cột STT
                        if (view2.GetRowCellValue(i, "KeyValue").ToString().ToLower() == frmMain.keyMST2)
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                if (gridView3.RowCount > i) // Kiểm tra số lượng dòng
                                {
                                    gridView3.FocusedRowHandle = i; // Đặt focus
                                    gridView3.MakeRowVisible(i); // Cuộn đến dòng
                                    gridView3.SelectRow(i); // Chọn dòng 
                                }
                            });
                            return;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(frmMain.KeyDG2))
                {
                    for (int i = 0; i < view2.RowCount; i++)
                    {
                        // Lấy giá trị của cột STT
                        if (view2.GetRowCellValue(i, "Type").ToString().ToLower() == frmMain.KeyDG2.ToLower())
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                if (gridView3.RowCount > i) // Kiểm tra số lượng dòng
                                {
                                    gridView3.FocusedRowHandle = i; // Đặt focus
                                    gridView3.MakeRowVisible(i); // Cuộn đến dòng
                                    gridView3.SelectRow(i); // Chọn dòng 
                                }
                            });
                            return;
                        }
                    }
                }
            }


        }
        private void ButtonEdit_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            // Xử lý sự kiện khi nút "Xóa" được nhấn
            // Ví dụ: Xóa hàng hiện tại trong GridView
            int focusedRowHandle = gridView1.FocusedRowHandle;
            if (focusedRowHandle >= 0)
            {
                gridView1.DeleteRow(focusedRowHandle);
            }
        }
        private void LoadMacdinhVattu()
        {
            string querykh = @" SELECT *  FROM tbRegister"; // Sử dụng ? thay cho @mst trong OleDb

            result = ExecuteQuery(querykh, null);
            string col1 = result.Rows[0]["Col1"].ToString();
            string col2 = result.Rows[0]["Col2"].ToString();
            if(col1=="1")
            {
                checkEdit1.Checked = true;
            }
            if (col2 == "1")
            {
                checkEdit2.Checked = true;
            }
        }
        private void LoadDataDinhDanh()
        {
          
            if ( string.IsNullOrEmpty(dbPath))
                return;
            string querykh = @" SELECT *  FROM tbDinhdanhtaikhoan"; // Sử dụng ? thay cho @mst trong OleDb

            result = ExecuteQuery(querykh, null);


            //Cập nhật loại
            foreach (DataRow row in result.Rows)
            {
                if(row.Field<string>("KeyValue").Contains("Ưu tiên vào"))
                {
                    string sql = "UPDATE tbDinhdanhtaikhoan SET Loai = @Loai  WHERE ID = @ID";
                    SqlParameter[] parameters = new SqlParameter[]
                    { 
                       new SqlParameter("Loai","1"),
                             new SqlParameter("ID",row.Field<int>("ID").ToString()), 
                    };
                    int resl = ExecuteQueryResult(sql, parameters);
                }
                else
                {
                    if (row.Field<string>("KeyValue").Contains("Ưu tiên ra"))
                    {
                        string sql = "UPDATE tbDinhdanhtaikhoan SET Loai = @Loai  WHERE ID = @ID";
                        SqlParameter[] parameters = new SqlParameter[]
                        {
                       new SqlParameter("@Loai","2"),
                             new SqlParameter("@ID",row.Field<int>("ID").ToString()),
                        };
                        int resl = ExecuteQueryResult(sql, parameters);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(row.Field<string>("Loai")))
                        {
                            if(row.Field<string>("TKNo").Contains("642") || row.Field<string>("TKNo").Contains("15"))
                            {
                                string sql = "UPDATE tbDinhdanhtaikhoan SET Loai = @Loai  WHERE ID = @ID";
                                SqlParameter[] parameters = new SqlParameter[]
                                {
                       new SqlParameter("@Loai","1"),
                             new SqlParameter("@ID",row.Field<int>("ID").ToString()),
                                };
                                int resl = ExecuteQueryResult(sql, parameters);
                            }
                            else
                            {
                                string sql = "UPDATE tbDinhdanhtaikhoan SET Loai = @Loai  WHERE ID = @ID";
                                SqlParameter[] parameters = new SqlParameter[]
                                {
                       new SqlParameter("@Loai","2"),
                             new SqlParameter("@ID",row.Field<int>("ID").ToString()),
                                };
                                int resl = ExecuteQueryResult(sql, parameters);
                            }
                        }
                       
                    }
                }
                
            }


            //Lọc lại
             querykh = @" SELECT *  FROM tbDinhdanhtaikhoan"; // Sử dụng ? thay cho @mst trong OleDb
             result = ExecuteQuery(querykh,null);

            DataTable data = null;

            // Check if result is not null and has rows
            if (result != null && result.Rows.Count > 0)
            {
                data = result.AsEnumerable()
                             .Where(m => m.Field<string>("Loai") == "1")
                             .CopyToDataTable();
            }
            GridView gridView = gcDinhdanh.MainView as GridView;
            // Assign to the DataSource only if data is not null
            if (data != null)
            { 
                gcDinhdanh.MainView.BeginUpdate(); // Bắt đầu cập nhật

                gcDinhdanh.DataSource = data;
                gcDinhdanh.MainView.EndUpdate(); // Kết thúc cập nhật

                gridView.MoveLast();

                // Hoặc bạn có thể sử dụng BestFocusedRowHandle để đảm bảo hàng cuối cùng được focus
                gridView.FocusedRowHandle = gridView.RowCount - 1;
            } 

            // Tạo cột xóa
            
            gridView.CustomUnboundColumnData += gridView_CustomUnboundColumnData;
            gridView.RowCellClick += gridView_RowCellClick;
            gridView.CellValueChanged += GridView_CellValueChanged;


            // Filter the result for "Loai" == "2" and copy to DataTable
            var filteredData = result.AsEnumerable()
                                     .Where(m => m.Field<string>("Loai") == "2")
                                     .ToList();

            // Check if there are any matching rows before copying to DataTable
            if (filteredData.Count > 0)
            {
                data = filteredData.CopyToDataTable();
            }
            else
                data = null;

            // Assign to the DataSource only if data is not null
            if (data != null)
            {
                data.Columns.Add("STT", typeof(int));
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    data.Rows[i]["STT"] = i + 1; // Gán giá trị STT
                }

                gridControl2.DataSource = data;
            }

            GridView gridView2= gridControl2.MainView as GridView;

            RepositoryItemButtonEdit buttonEdit = new RepositoryItemButtonEdit();
            buttonEdit.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
            buttonEdit.Buttons[0].Caption = "Xóa"; // Văn bản trên nút
            buttonEdit.ButtonClick += ButtonEdit_ButtonClick;

           
            gridView2.CustomUnboundColumnData += gridView_CustomUnboundColumnData2;
            gridView2.RowCellClick += gridView_RowCellClick2;
            // Tạo cột xóa

        }
        private void ChangedValue(GridView gridView,int rowHandle,string columnName)
        {
           
            // Lấy thông tin về hàng và cột của ô đã thay đổi
           
            //Lấy current data row
            int ID = int.Parse(gridView.GetRowCellValue(rowHandle, gridView.Columns["ID"]).ToString());
            string Type = gridView.GetRowCellValue(rowHandle, gridView.Columns["Type"]).ToString();
            string KeyValue = gridView.GetRowCellValue(rowHandle, gridView.Columns["KeyValue"]).ToString();
            string TKNo = gridView.GetRowCellValue(rowHandle, gridView.Columns["TKNo"]).ToString();
            string TKCo = gridView.GetRowCellValue(rowHandle, gridView.Columns["TKCo"]).ToString();
            string TKThue = gridView.GetRowCellValue(rowHandle, gridView.Columns["TKThue"]).ToString();
            string sql = "UPDATE tbDinhdanhtaikhoan SET Type = @Type, KeyValue = @KeyValue, TKNo = @TKNo, TKCo = @TKCo, TKThue = @TKThue WHERE ID = @ID";

            SqlParameter[] parameters = new SqlParameter[]
            {
    new SqlParameter("@Type", Type),
    new SqlParameter("@KeyValue", KeyValue),
    new SqlParameter("@TKNo", TKNo),
    new SqlParameter("@TKCo", TKCo),
    new SqlParameter("@TKThue", TKThue),
    new SqlParameter("@ID", ID)
            };
            int resl = ExecuteQueryResult(sql, parameters);
        }
        private void GridView_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            GridView gridView = gcDinhdanh.MainView as GridView;
            int rowHandle = e.RowHandle;
            string columnName = e.Column.FieldName; // Tên cột
            ChangedValue(gridView, rowHandle, columnName);
        }

        private void gridView_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "colDelete" )
            {
                e.Value = "Xóa";
            }
        }
        private void gridView_CustomUnboundColumnData2(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "colDelete")
            {
                e.Value = "Xóa";
            }
        }
        private void gridView_RowCellClick(object sender, DevExpress.XtraGrid.Views.Grid.RowCellClickEventArgs e)
        {
            if (e.Column.FieldName == "colDelete" )
            {
                var rowHandle = e.RowHandle;
                GridView gridView = gcDinhdanh.MainView as GridView;
                if (gridView.GetRowCellValue(rowHandle, "ID") == null)
                    return;
                // Ví dụ: Lấy giá trị của một cột có tên "Name" từ hàng hiện tại
                string nameValue = gridView.GetRowCellValue(rowHandle, "ID").ToString();
                if (XtraMessageBox.Show("Bạn có chắc chắn muốn xóa hàng này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string sql = "DELETE FROM tbDinhdanhtaikhoan WHERE ID = @AccountID";
                    SqlParameter[] parameters = new SqlParameter[]
                {
        new SqlParameter("@AccountID", nameValue),
                };
                    int resl = ExecuteQueryResult(sql, parameters);
                    LoadDataDinhDanh();
                }
                else
                {
                    return;
                }
            }
        }
        private void gridView_RowCellClick2(object sender, DevExpress.XtraGrid.Views.Grid.RowCellClickEventArgs e)
        {
            if (e.Column.FieldName == "colDelete")
            {
                var rowHandle = e.RowHandle;
                GridView gridView = gridControl2.MainView as GridView;
                if (gridView.GetRowCellValue(rowHandle, "ID") == null)
                    return;
                // Ví dụ: Lấy giá trị của một cột có tên "Name" từ hàng hiện tại
                string nameValue = gridView.GetRowCellValue(rowHandle, "ID").ToString();
                if (XtraMessageBox.Show("Bạn có chắc chắn muốn xóa hàng này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string sql = "DELETE FROM tbDinhdanhtaikhoan WHERE ID = @AccountID";
                    SqlParameter[] parameters = new SqlParameter[]
                {
        new SqlParameter("@AccountID", nameValue),
                };
                    int resl = ExecuteQueryResult(sql, parameters);
                    LoadDataDinhDanh();
                }
                else
                {
                    return;
                }
            }
        }
        private void InitDB()
        {
            string appPath = Assembly.GetExecutingAssembly().Location;

            // Lấy thư mục chứa ứng dụng
            string directoryPath = Path.GetDirectoryName(appPath);

            // Xóa phần \bin\Debug để lấy đường dẫn gốc
            string rootDirectory = Path.GetFullPath(Path.Combine(directoryPath, @"..\.."));

            // Tạo đường dẫn đến file dpPath.txt trong thư mục hoadon
            string filePaths = Path.Combine(rootDirectory, "hoadon", "dpPath.txt");
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
            connectionString = ConfigurationManager.ConnectionStrings["SqlConn"].ConnectionString;
            //connectionString = $@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={dbPath};Jet OLEDB:Database Password={password};";
            // connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Jet OLEDB:Database";
            //connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\S.T.E 25\S.T.E 25\DATA\importData.accdb;Persist Security Info=False";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Có lỗi xảy ra: {ex.Message}");
            }

        }
        string password, connectionString;
        private void btnLuudinhdanh_Click(object sender, EventArgs e)
        {
            //Kiểm tra tài khoản con
            if (txtTukhoa.Text.Contains("Ưu tiên vào"))
            {
                string querydinhdanh = @"SELECT * FROM HeThongTK WHERE SoHieu LIKE @SoHieu";
                var resultkm = ExecuteQuery(querydinhdanh, new SqlParameter("@SoHieu", txtTKNo.Text + "%"));
                if (resultkm.Rows.Count > 1)
                {
                    XtraMessageBox.Show("Tài khoản " + txtTKNo.Text + " có tài khoản con, vui lòng kiểm tra lại!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (string.IsNullOrEmpty(txtTukhoa.Text) || string.IsNullOrEmpty(txtTKNo.Text) || string.IsNullOrEmpty(txtTKCo.Text) || string.IsNullOrEmpty(txtTKThue.Text))
                {
                    XtraMessageBox.Show("Vui lòng nhập thông tin!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            if (txtTukhoa.Text.Contains("Ưu tiên ra"))
            {
                string querydinhdanh = @"SELECT * FROM HeThongTK WHERE SoHieu LIKE @SoHieu";
                var resultkm = ExecuteQuery(querydinhdanh, new SqlParameter("@SoHieu", txtTKCo.Text + "%"));
                if (resultkm.Rows.Count > 1)
                {
                    XtraMessageBox.Show("Tài khoản " + txtTKCo.Text + " có tài khoản con, vui lòng kiểm tra lại!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (string.IsNullOrEmpty(txtTukhoa.Text) || string.IsNullOrEmpty(txtTKNo.Text) || string.IsNullOrEmpty(txtTKCo.Text) || string.IsNullOrEmpty(txtTKThue.Text))
                {
                    XtraMessageBox.Show("Vui lòng nhập thông tin!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string query = @"
    INSERT INTO tbDinhdanhtaikhoan (KeyValue, TKNo, TKCo, TKThue, Type)
    VALUES (@KeyValue, @TKNo, @TKCo, @TKThue, @Type)";

            SqlParameter[] parameters = new SqlParameter[]
            {
    new SqlParameter("@KeyValue", txtTukhoa.Text),
    new SqlParameter("@TKNo", txtTKNo.Text),
    new SqlParameter("@TKCo", txtTKCo.Text),
    new SqlParameter("@TKThue", txtTKThue.Text),
    new SqlParameter("@Type", txtDiengiai.Text)
            };

            // Thực thi truy vấn và lấy kết quả
            int a = ExecuteQueryResult(query, parameters);
            LoadDataDinhDanh();
        }

        private void txtTKNo_EditValueChanging(object sender, DevExpress.XtraEditors.Controls.ChangingEventArgs e)
        {

        }

        private void checkEdit1_CheckedChanged(object sender, EventArgs e)
        {
            string sql = "UPDATE tbRegister set col1= @col1";
            SqlParameter[] parameters = new SqlParameter[]
        {
        new SqlParameter("@col1", checkEdit1.Checked==true?"1":""),
        };
            int resl = ExecuteQueryResult(sql, parameters);
        }

        private void checkEdit2_CheckedChanged(object sender, EventArgs e)
        {
            string sql = "UPDATE tbRegister set col2= @col2";
            SqlParameter[] parameters = new SqlParameter[]
        {
        new SqlParameter("@col2", checkEdit2.Checked==true?"1":""),
        };
            int resl = ExecuteQueryResult(sql, parameters);
        }

        private void gridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            int rowHandle = e.RowHandle;
            var gridView = sender as GridView;
            PhanLoaiVattu rowData = gridView.GetRow(rowHandle) as PhanLoaiVattu;
            string sql = "UPDATE PhanLoaiVattu SET GhiChu = @GhiChu, TKCo = @TKCo, TKNo = @TKNo WHERE MaSo = @MaSo";

            SqlParameter[] parameters = new SqlParameter[]
            {
    new SqlParameter("@GhiChu", rowData.GhiChu ?? (object)DBNull.Value),
    new SqlParameter("@TKCo", rowData.TKCo ?? (object)DBNull.Value),
    new SqlParameter("@TKNo", rowData.TKNo ?? (object)DBNull.Value),
    new SqlParameter("@MaSo", rowData.MaSo.ToString())
            };
            int resl = ExecuteQueryResult(sql, parameters);

        }

        private void gcDinhdanh_Click(object sender, EventArgs e)
        {

        }

        private void gridView2_RowCountChanged(object sender, EventArgs e)
        {

        }

        public void addNewrow(int Type)
        {
            string query = @"
    INSERT INTO tbDinhdanhtaikhoan (KeyValue, TKNo, TKCo, TKThue, Type, Loai)
    VALUES (@KeyValue, @TKNo, @TKCo, @TKThue, @Type, @Loai)";

            SqlParameter[] parameters = new SqlParameter[]
            {
    new SqlParameter("@KeyValue", txtTukhoa.Text ?? (object)DBNull.Value),
    new SqlParameter("@TKNo", txtTKNo.Text ?? (object)DBNull.Value),
    new SqlParameter("@TKCo", txtTKCo.Text ?? (object)DBNull.Value),
    new SqlParameter("@TKThue", txtTKThue.Text ?? (object)DBNull.Value),
    new SqlParameter("@Type", txtDiengiai.Text ?? (object)DBNull.Value),
    new SqlParameter("@Loai", Type.ToString())
            };

            // Thực thi truy vấn và lấy kết quả
            int a = ExecuteQueryResult(query, parameters);
            LoadDataDinhDanh();
        }
        private void btnAddnewrow_Click(object sender, EventArgs e)
        {
            addNewrow(1);
        }

        private void gridView3_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            GridView gridView = gridControl2.MainView as GridView;
            int rowHandle = e.RowHandle;
            string columnName = e.Column.FieldName; // Tên cột
            ChangedValue(gridView, rowHandle, columnName);
            if (columnName == "TKNo")
            {
                if(Kiemtrataikhoancon(gridView.GetRowCellValue(rowHandle,"TKNo").ToString()))
                    {
                    XtraMessageBox.Show($"Tài khoản {gridView.GetRowCellValue(rowHandle, "TKNo")} có chi tiết");
                }
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            addNewrow(2);
        }

        private void gridView2_CustomDrawGroupPanel(object sender, DevExpress.XtraGrid.Views.Base.CustomDrawEventArgs e)
        {
            //e.Handled = true; // Ngăn chặn việc vẽ văn bản mặc định

            //// Vẽ text tùy chỉnh
            //string customText = "Kéo cột vào đây để nhóm";
            //Font font = new Font("Tahoma", 8);
            //Brush brush = Brushes.DarkBlue;
            //e.Graphics.DrawString(customText, font, brush, e.Bounds);
        }

        private void gridView2_Click(object sender, EventArgs e)
        {   

        }

        private void gridView2_MouseDown(object sender, MouseEventArgs e)
        {
            // Tính toán thông tin hit tại vị trí click
            var hitInfo = gridView2.CalcHitInfo(e.Location);

            // Kiểm tra nếu click vào một cell hợp lệ
            if ( hitInfo.RowHandle >= 0)
            {
                // Kích hoạt ô và bôi đen văn bản
                gridView2.ShowEditor();
                var editor = gridView2.ActiveEditor as DevExpress.XtraEditors.TextEdit;
                if (editor != null)
                {
                    editor.SelectAll(); // Bôi đen toàn bộ văn bản
                    editor.Focus(); // Đặt tiêu điểm vào editor
                }
            }
        }

        private void gridView2_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
          
        }

        private void gridView2_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "STT" && e.IsGetData)
            {
                e.Value = e.ListSourceRowIndex + 1; // Gán giá trị STT
            }
        }

        private void gridView3_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "STT" && e.IsGetData)
            {
                e.Value = e.ListSourceRowIndex + 1; // Gán giá trị STT
            }
        }

        private void gridControl2_Click(object sender, EventArgs e)
        {

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
    }
}