using BarcodeStandard;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Type = BarcodeStandard.Type;

namespace SaovietTax
{
    public partial class frmQrcode : XtraForm
    {
        private Bitmap printBitmap; // Lưu ảnh để in

        public frmQrcode()
        {
            InitializeComponent();
        }

        public static Image GenerateBarcode(string text)
        {
            try
            {
                // Tạo đối tượng Barcode từ thư viện BarcodeStandard
                var barcode = new Barcode();

                // Tùy chọn hiển thị label bên dưới mã vạch
                barcode.IncludeLabel = true;
                barcode.LabelFont = new SKFont(SKTypeface.Default, 14);

                // Mã hóa chuỗi text thành mã vạch CODE128
                // Trả về SKImage, sau đó chuyển sang System.Drawing.Image
                using (var skImage = barcode.Encode(Type.Code128, text, SKColors.Black, SKColors.White, 500, 120))
                {
                    // Chuyển SKImage -> SKBitmap -> System.Drawing.Bitmap
                    using (var bitmap = SKBitmap.FromImage(skImage))
                    {
                        using (var image = SKImage.FromBitmap(bitmap))
                        {
                            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                            {
                                using (var stream = new MemoryStream(data.ToArray()))
                                {
                                    return Image.FromStream(stream);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                return null;
            }
        }

        private void frmQrcode_Load(object sender, EventArgs e)
        {
            
            try
            {
                string appPath = Assembly.GetExecutingAssembly().Location;
                string directoryPath = Path.GetDirectoryName(appPath);

                string rootDirectory =
                    Path.GetFullPath(
                        Path.Combine(directoryPath, @"..\.."));

                string filePath =
                    Path.Combine(
                        rootDirectory,
                        "Hoadon",
                        "invoice.txt");

                // Kiểm tra file tồn tại
                if (!File.Exists(filePath))
                {
                    XtraMessageBox.Show("Không tìm thấy file invoice.txt!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string content = File.ReadAllText(
                    filePath,
                    Encoding.Default).Trim();

                // Kiểm tra nội dung không rỗng trước khi tạo barcode
                if (!string.IsNullOrEmpty(content))
                {
                    var getsplit = content.Split('|');

                    // Lấy phần tử đầu tiên để tạo barcode
                    string barcodeText = getsplit.Length > 0 ? getsplit[0] : content;

                    // Hiển thị barcode
                    pictureBoxQR.Image = GenerateBarcode(barcodeText);
                    pictureBoxQR.SizeMode = PictureBoxSizeMode.Zoom;
                }

                // Nếu file có dạng:
                // MaVach|TenCongTy|DiaChi
                var getsplit2 = content.Split('|');

                if (getsplit2.Length > 0)
                    labelControl1.Text = getsplit2[0];

                if (getsplit2.Length > 1)
                    labelControl2.Text =
                        Helpers.ConvertVniToUnicode(getsplit2[1]);

                if (getsplit2.Length > 2)
                    labelControl3.Text =
                        Helpers.ConvertVniToUnicode(getsplit2[2]);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void panelControl1_Paint(object sender, PaintEventArgs e)
        {
        }

        // ============= CHỨC NĂNG IN =============

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            try
            {
                // Chụp ảnh panelControl1
                printBitmap = CapturePanel(panelControl1);

                if (printBitmap == null)
                {
                    XtraMessageBox.Show("Không thể chụp ảnh để in!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Hiển thị hộp thoại in
                PrintDialog printDialog = new PrintDialog();
                printDialog.Document = new PrintDocument();
                printDialog.Document.PrintPage += new PrintPageEventHandler(Document_PrintPage);

                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    printDialog.Document.Print();
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi in: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Chụp ảnh một Control
        /// </summary>
        private Bitmap CapturePanel(Control control)
        {
            try
            {
                // Tạo bitmap với kích thước của control
                Bitmap bitmap = new Bitmap(control.Width, control.Height);

                // Vẽ control lên bitmap
                control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CapturePanel Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sự kiện in trang
        /// </summary>
        private void Document_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                if (printBitmap == null)
                    return;

                // Lấy kích thước trang in
                Rectangle marginBounds = e.MarginBounds;

                // Tính toán tỷ lệ để ảnh vừa trong lề in
                float scaleX = (float)marginBounds.Width / printBitmap.Width;
                float scaleY = (float)marginBounds.Height / printBitmap.Height;
                float scale = Math.Min(scaleX, scaleY) * 0.95f; // 95% để có lề

                // Tính kích thước mới
                int newWidth = (int)(printBitmap.Width * scale);
                int newHeight = (int)(printBitmap.Height * scale);

                // Tính vị trí để canh giữa
                int x = marginBounds.Left + (marginBounds.Width - newWidth) / 2;
                int y = marginBounds.Top + (marginBounds.Height - newHeight) / 2;

                // Vẽ ảnh lên trang in
                e.Graphics.DrawImage(printBitmap, x, y, newWidth, newHeight);

                // Không cần in thêm trang
                e.HasMorePages = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Document_PrintPage Error: {ex.Message}");
                e.HasMorePages = false;
            }
        }
    }
}