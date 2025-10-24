using QLS.BUS;
using QLS.DAL.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace QLS.GUI
{
    public partial class frmSach : Form
    {
        private readonly LoaiSachService _loaiSvc = new LoaiSachService();
        private readonly SachService _sachSvc = new SachService();
        private readonly Timer _searchTimer = new Timer { Interval = 220 };
        private readonly string _imageFolder = Path.Combine(Application.StartupPath, "Images");
        private List<SachService.SachListItem> _currentData = new List<SachService.SachListItem>();
        private string _sortColumn = "MaSach";
        private bool _sortAsc = true;
        private bool _isEditing = false; // trạng thái: đang sửa hay thêm mới

        public frmSach()
        {
            InitializeComponent();
            this.Load += FrmSach_Load;
            // tạo nút xuất PDF nếu bạn chưa có trong Designer
            var btnXuatPdf = new Button
            {
                Name = "btnXuatPdf",
                Text = "Xuất PDF",
                AutoSize = true
            };
            // đặt vị trí tạm (bạn chỉnh lại cho đẹp)
            btnXuatPdf.Location = new Point( /*x*/ 20, /*y*/ this.ClientSize.Height - 50);
            btnXuatPdf.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(btnXuatPdf);
            btnXuatPdf.Click += BtnXuatPdf_Click;

        }

        private void FrmSach_Load(object sender, EventArgs e)
        {
            ConfigureGrid();
            PrepareFolder();
            LoadLoaiToCombo();
            LoadGrid(_sachSvc.GetAll());
            WireEvents();
            ToggleEditMode(false);
        }

        // ========================== KHỞI TẠO ==========================

        private void PrepareFolder()
        {
            if (!Directory.Exists(_imageFolder))
                Directory.CreateDirectory(_imageFolder);
        }

        private void WireEvents()
        {
            // Live Search
            _searchTimer.Tick += (s, ev) =>
            {
                _searchTimer.Stop();
                string key = txtTimKiem.Text.Trim();
                LoadGrid(string.IsNullOrWhiteSpace(key)
                    ? _sachSvc.GetAll()
                    : _sachSvc.Search(key));
            };
            txtTimKiem.TextChanged += (s, ev) =>
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            };

            // Chỉ nhập số cho Năm XB
            txtNamXB.KeyPress += (s, ev) =>
            {
                if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar))
                    ev.Handled = true;
            };
            txtNamXB.MaxLength = 4;

            dgvSach.SelectionChanged += (s, ev) => FillFormFromGrid();

            // Nút chức năng
            btnThem.Click += (s, ev) => { ResetForm(); _isEditing = false; ToggleEditMode(true); };
            btnSua.Click += (s, ev) => { _isEditing = true; ToggleEditMode(true); };
            btnXoa.Click += (s, ev) => DeleteBook();
            btnLamMoi.Click += (s, ev) => ResetForm();

            btnChonAnh.Click += (s, ev) => ChonAnh();

            btnLuu.Click += (s, ev) => ConfirmSave();
            btnKhongLuu.Click += (s, ev) => CancelEdit();

            // MenuStrip
            mnuThoat.Click += (s, ev) => this.Close();
            mnuLoaiSach.Click += MnuLoaiSach_Click;

            // Phím tắt
            this.KeyPreview = true;
            this.KeyDown += FrmSach_KeyDown;
        }

        private void MnuLoaiSach_Click(object sender, EventArgs e)
        {
            // Ẩn form hiện tại
            this.Hide();
            var f = new frmLoaiSach();
            f.FormClosed += (s, ev) =>
            {
                this.Show();
                LoadLoaiToCombo(); // reload lại danh sách loại
            };
            f.ShowDialog();
        }

        private void FrmSach_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.N) btnThem.PerformClick();
            if (e.Control && e.KeyCode == Keys.S && btnLuu.Visible) btnLuu.PerformClick();
            if (e.KeyCode == Keys.F5) btnLamMoi.PerformClick();
        }

        private void ToggleEditMode(bool editing)
        {
            btnThem.Visible = !editing;
            btnSua.Visible = !editing;
            btnXoa.Visible = !editing;
            btnLamMoi.Visible = !editing;

            btnLuu.Visible = editing;
            btnKhongLuu.Visible = editing;
        }

        // ========================== GRID ==========================

        private void ConfigureGrid()
        {
            var g = dgvSach;
            g.Columns.Clear();
            g.AutoGenerateColumns = false;
            g.AllowUserToAddRows = false;
            g.ReadOnly = true;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.BorderStyle = BorderStyle.FixedSingle;
            g.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            g.EnableHeadersVisualStyles = false;
            g.GridColor = Color.Gainsboro;

            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(31, 113, 185);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            g.ColumnHeadersHeight = 36;

            g.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            g.DefaultCellStyle.SelectionBackColor = Color.LightSteelBlue;
            g.DefaultCellStyle.SelectionForeColor = Color.Black;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);

            // các cột đều SortMode = Programmatic
            g.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaSach",
                DataPropertyName = "MaSach",
                HeaderText = "Mã sách",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
            g.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TenSach",
                DataPropertyName = "TenSach",
                HeaderText = "Tên sách",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
            g.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TenLoai",
                DataPropertyName = "TenLoai",
                HeaderText = "Thể loại",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
            g.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NamXB",
                DataPropertyName = "NamXB",
                HeaderText = "Năm XB",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                SortMode = DataGridViewColumnSortMode.Programmatic
            });

            try
            {
                g.GetType().GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                 ?.SetValue(g, true, null);
            }
            catch { }

            g.ColumnHeaderMouseClick += DgvSach_ColumnHeaderMouseClick;
        }

        private void DgvSach_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var col = dgvSach.Columns[e.ColumnIndex];
            if (string.IsNullOrEmpty(col.DataPropertyName)) return;

            string newCol = col.DataPropertyName;
            if (_sortColumn == newCol)
                _sortAsc = !_sortAsc;
            else
            {
                _sortColumn = newCol;
                _sortAsc = true;
            }

            SortAndBind();

            foreach (DataGridViewColumn c in dgvSach.Columns)
                c.HeaderCell.SortGlyphDirection = SortOrder.None;
            col.HeaderCell.SortGlyphDirection = _sortAsc ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void SortAndBind()
        {
            IEnumerable<SachService.SachListItem> q = _currentData;

            switch (_sortColumn)
            {
                case "MaSach":
                    q = _sortAsc ? q.OrderBy(x => x.MaSach, StringComparer.CurrentCultureIgnoreCase)
                                 : q.OrderByDescending(x => x.MaSach, StringComparer.CurrentCultureIgnoreCase);
                    break;
                case "TenSach":
                    q = _sortAsc ? q.OrderBy(x => x.TenSach, StringComparer.CurrentCultureIgnoreCase)
                                 : q.OrderByDescending(x => x.TenSach, StringComparer.CurrentCultureIgnoreCase);
                    break;
                case "TenLoai":
                    q = _sortAsc ? q.OrderBy(x => x.TenLoai, StringComparer.CurrentCultureIgnoreCase)
                                 : q.OrderByDescending(x => x.TenLoai, StringComparer.CurrentCultureIgnoreCase);
                    break;
                case "NamXB":
                    q = _sortAsc ? q.OrderBy(x => x.NamXB)
                                 : q.OrderByDescending(x => x.NamXB);
                    break;
            }

            BindData(q);
        }

        private void LoadGrid(List<SachService.SachListItem> data)
        {
            _currentData = data ?? new List<SachService.SachListItem>();

            // sort mặc định theo mã sách
            if (_sortColumn == "MaSach" && _sortAsc)
                _currentData = _currentData.OrderBy(x => x.MaSach, StringComparer.CurrentCultureIgnoreCase).ToList();

            BindData(_currentData);
        }

        private void BindData(IEnumerable<SachService.SachListItem> data)
        {
            dgvSach.DataSource = null;
            dgvSach.DataSource = data.ToList();
        }

        // ========================== LOẠI SÁCH ==========================

        private void LoadLoaiToCombo()
        {
            var loais = _loaiSvc.GetAll();
            cboLoaiSach.DisplayMember = "TenLoai";
            cboLoaiSach.ValueMember = "MaLoai";
            cboLoaiSach.DataSource = loais;
        }

        // ========================== LƯU / HỦY ==========================

        private void ConfirmSave()
        {
            var confirm = MessageBox.Show("Bạn có chắc muốn lưu thay đổi không?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                SaveBook();
                ToggleEditMode(false);
            }
            else
            {
                MessageBox.Show("Đã hủy lưu thay đổi.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CancelEdit()
        {
            var confirm = MessageBox.Show("Hủy thay đổi hiện tại?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                ResetForm();
                ToggleEditMode(false);
            }
        }

        // ========================== CRUD ==========================

        private void SaveBook()
        {
            if (string.IsNullOrWhiteSpace(txtMaSach.Text)
                || string.IsNullOrWhiteSpace(txtTenSach.Text)
                || string.IsNullOrWhiteSpace(txtNamXB.Text)
                || cboLoaiSach.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin sách!",
                    "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtMaSach.Text.Length != 6)
            {
                MessageBox.Show("Mã sách phải đúng 6 ký tự.",
                    "Sai định dạng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtNamXB.Text, out int nam))
            {
                MessageBox.Show("Năm XB phải là số hợp lệ.",
                    "Sai định dạng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ma = txtMaSach.Text.Trim();

            if (!_isEditing && _sachSvc.GetById(ma) != null)
            {
                MessageBox.Show("Mã sách đã tồn tại, vui lòng nhập mã khác.",
                    "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sach = new Sach
            {
                MaSach = ma,
                TenSach = txtTenSach.Text.Trim(),
                NamXB = nam,
                MaLoai = (int)cboLoaiSach.SelectedValue,
                HinhAnh = picAnhBia.Tag as string
            };

            try
            {
                _sachSvc.AddOrUpdate(sach);
                LoadGrid(_sachSvc.GetAll());
                MessageBox.Show(_isEditing ? "Cập nhật thành công!" : "Thêm mới thành công!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetForm();
                _isEditing = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteBook()
        {
            if (dgvSach.CurrentRow?.DataBoundItem is SachService.SachListItem it)
            {
                var confirm = MessageBox.Show(
                    $"Xóa sách {it.TenSach} ({it.MaSach})?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    bool ok = _sachSvc.Delete(it.MaSach);
                    if (ok)
                    {
                        LoadGrid(_sachSvc.GetAll());
                        ResetForm();
                        MessageBox.Show("Đã xóa thành công!");
                    }
                    else
                    {
                        MessageBox.Show("Sách cần xóa không tồn tại!", "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void ResetForm()
        {
            txtMaSach.Clear();
            txtTenSach.Clear();
            txtNamXB.Clear();
            cboLoaiSach.SelectedIndex = 0;
            picAnhBia.Image = null;
            picAnhBia.Tag = null;
            txtTimKiem.Clear();
            dgvSach.ClearSelection();
        }

        private void FillFormFromGrid()
        {
            if (dgvSach.CurrentRow?.DataBoundItem is SachService.SachListItem it)
            {
                txtMaSach.Text = it.MaSach;
                txtTenSach.Text = it.TenSach;
                txtNamXB.Text = it.NamXB.ToString();
                cboLoaiSach.SelectedValue = it.MaLoai;
                LoadImageToPictureBox(it.HinhAnh);
            }
        }

        // ========================== ẢNH ==========================

        private void ChonAnh()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Ảnh (*.jpg;*.png)|*.jpg;*.png";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string fileName = Path.GetFileName(ofd.FileName);
                    string destPath = Path.Combine(_imageFolder, fileName);

                    try
                    {
                        if (!File.Exists(destPath))
                            File.Copy(ofd.FileName, destPath, true);

                        picAnhBia.Image = Image.FromFile(destPath);
                        picAnhBia.SizeMode = PictureBoxSizeMode.Zoom;
                        picAnhBia.Tag = fileName;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi copy ảnh: {ex.Message}");
                    }
                }
            }
        }

        private void LoadImageToPictureBox(string fileName)
        {
            picAnhBia.Image = null;
            picAnhBia.Tag = fileName;
            if (string.IsNullOrWhiteSpace(fileName)) return;

            string path = Path.Combine(_imageFolder, fileName);
            if (File.Exists(path))
            {
                picAnhBia.Image = Image.FromFile(path);
                picAnhBia.SizeMode = PictureBoxSizeMode.Zoom;
            }
        }

        //===============================
        private void BtnXuatPdf_Click(object sender, EventArgs e)
        {
            if (dgvSach.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF file (*.pdf)|*.pdf";
                sfd.FileName = $"DanhSachSach_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ExportGridToPdf(dgvSach, sfd.FileName, "DANH SÁCH SÁCH");
                        MessageBox.Show("Xuất PDF thành công!", "OK",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi xuất PDF: " + ex.Message, "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportGridToPdf(DataGridView grid, string filePath, string title)
        {
            var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4.Rotate(), 24, 24, 24, 24);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
                doc.Open();

                // Font Unicode (Việt): dùng Arial
                var bf = iTextSharp.text.pdf.BaseFont.CreateFont(
                    @"C:\Windows\Fonts\arial.ttf",
                    iTextSharp.text.pdf.BaseFont.IDENTITY_H,
                    iTextSharp.text.pdf.BaseFont.EMBEDDED);

                var fontTitle = new iTextSharp.text.Font(bf, 16, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);
                var fontHeader = new iTextSharp.text.Font(bf, 11, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.WHITE);
                var fontCell = new iTextSharp.text.Font(bf, 11, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.BLACK);

                // Tiêu đề
                var pTitle = new iTextSharp.text.Paragraph(title, fontTitle)
                {
                    Alignment = iTextSharp.text.Element.ALIGN_CENTER,
                    SpacingAfter = 8f
                };
                doc.Add(pTitle);

                // Ngày giờ
                var pTime = new iTextSharp.text.Paragraph(
                    "Ngày xuất: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    new iTextSharp.text.Font(bf, 10))
                {
                    Alignment = iTextSharp.text.Element.ALIGN_RIGHT,
                    SpacingAfter = 10f
                };
                doc.Add(pTime);

                // Cột hiển thị theo DisplayIndex
                var visibleCols = grid.Columns
                                      .Cast<DataGridViewColumn>()
                                      .Where(c => c.Visible)
                                      .OrderBy(c => c.DisplayIndex)
                                      .ToList();

                var table = new iTextSharp.text.pdf.PdfPTable(visibleCols.Count)
                {
                    WidthPercentage = 100
                };

                // Header
                foreach (var col in visibleCols)
                {
                    var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(col.HeaderText, fontHeader))
                    {
                        HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER,
                        VerticalAlignment = iTextSharp.text.Element.ALIGN_MIDDLE,
                        BackgroundColor = new iTextSharp.text.BaseColor(31, 113, 185), // giống màu header của grid
                        Padding = 6,
                        BorderWidth = 0.5f
                    };
                    table.AddCell(cell);
                }

                // Dữ liệu
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;

                    foreach (var col in visibleCols)
                    {
                        var val = row.Cells[col.Name].Value?.ToString() ?? "";

                        int hAlign = iTextSharp.text.Element.ALIGN_LEFT;
                        if (string.Equals(col.DataPropertyName, "NamXB", StringComparison.OrdinalIgnoreCase))
                            hAlign = iTextSharp.text.Element.ALIGN_CENTER;

                        var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(val, fontCell))
                        {
                            HorizontalAlignment = hAlign,
                            VerticalAlignment = iTextSharp.text.Element.ALIGN_MIDDLE,
                            Padding = 5,
                            BorderWidth = 0.5f
                        };
                        table.AddCell(cell);
                    }
                }

                doc.Add(table);
                doc.Close();
            }
        }


    }
}
