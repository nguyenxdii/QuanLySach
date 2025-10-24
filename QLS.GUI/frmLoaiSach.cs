using QLS.DAL;
using QLS.DAL.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QLS.GUI
{
    public partial class frmLoaiSach : Form
    {
        private List<LoaiSach> _currentLoai = new List<LoaiSach>();
        private string _sortColumn = "MaLoai";
        private bool _sortAsc = true;
        private readonly Timer _searchTimer = new Timer { Interval = 220 };

        public frmLoaiSach()
        {
            InitializeComponent();
            Load += FrmLoaiSach_Load;
        }

        private void FrmLoaiSach_Load(object sender, EventArgs e)
        {
            txtMaLoai.ReadOnly = true;       // mã loại tự tăng, không cho nhập
            ConfigureGrid();
            LoadGrid();
            WireEvents();
            ToggleEditMode(false);
        }

        // ========================== GRID ==========================

        private void ConfigureGrid()
        {
            var g = dgvLoaiSach;
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
            g.AllowUserToOrderColumns = true;
            g.AllowUserToResizeColumns = true;

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

            g.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaLoai",
                DataPropertyName = "MaLoai",
                HeaderText = "Mã loại",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
            g.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TenLoai",
                DataPropertyName = "TenLoai",
                HeaderText = "Tên loại",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });

            try
            {
                g.GetType().GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                 ?.SetValue(g, true, null);
            }
            catch { }

            g.ColumnHeaderMouseClick += DgvLoaiSach_ColumnHeaderMouseClick;
        }

        // ========================== LOAD & SORT ==========================

        private void LoadGrid()
        {
            using (var db = new QLSDbContext())
            {
                _currentLoai = db.LoaiSaches.OrderBy(x => x.MaLoai).ToList();
                BindData(_currentLoai);
            }
        }

        private void BindData(IEnumerable<LoaiSach> data)
        {
            dgvLoaiSach.DataSource = null;
            dgvLoaiSach.DataSource = data.ToList();
        }

        private void DgvLoaiSach_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var col = dgvLoaiSach.Columns[e.ColumnIndex];
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

            foreach (DataGridViewColumn c in dgvLoaiSach.Columns)
                c.HeaderCell.SortGlyphDirection = SortOrder.None;
            col.HeaderCell.SortGlyphDirection = _sortAsc ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void SortAndBind()
        {
            IEnumerable<LoaiSach> q = _currentLoai;
            switch (_sortColumn)
            {
                case "MaLoai":
                    q = _sortAsc ? q.OrderBy(x => x.MaLoai)
                                 : q.OrderByDescending(x => x.MaLoai);
                    break;
                case "TenLoai":
                    q = _sortAsc ? q.OrderBy(x => x.TenLoai, StringComparer.CurrentCultureIgnoreCase)
                                 : q.OrderByDescending(x => x.TenLoai, StringComparer.CurrentCultureIgnoreCase);
                    break;
            }
            BindData(q);
        }

        // ========================== EVENT & SEARCH ==========================

        private void WireEvents()
        {
            dgvLoaiSach.SelectionChanged += (s, ev) => FillForm();

            btnThem.Click += (s, ev) => { ResetForm(); ToggleEditMode(true); };
            btnSua.Click += (s, ev) => ToggleEditMode(true);
            btnXoa.Click += (s, ev) => DeleteLoai();

            btnLuu.Click += (s, ev) => ConfirmSave();
            btnKhongLuu.Click += (s, ev) => CancelEdit();

            // Live search
            _searchTimer.Tick += (s, ev) =>
            {
                _searchTimer.Stop();
                string key = txtTimKiem.Text.Trim();
                ApplySearch(key);
            };
            txtTimKiem.TextChanged += (s, ev) =>
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            };
        }

        private void ApplySearch(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                BindData(_currentLoai);
                return;
            }

            key = key.Trim();

            var filtered = _currentLoai.Where(x =>
                x.MaLoai.ToString().IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0
                || (!string.IsNullOrEmpty(x.TenLoai)
                    && x.TenLoai.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();

            BindData(filtered);
        }

        // ========================== LƯU / HỦY ==========================

        private void ToggleEditMode(bool editing)
        {
            btnThem.Visible = !editing;
            btnSua.Visible = !editing;
            btnXoa.Visible = !editing;

            btnLuu.Visible = editing;
            btnKhongLuu.Visible = editing;
        }

        private void ConfirmSave()
        {
            var confirm = MessageBox.Show("Bạn có chắc muốn lưu thay đổi không?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                MessageBox.Show("Đã hủy lưu thay đổi.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Kiểm tra trùng tên khi thêm mới
            if (string.IsNullOrWhiteSpace(txtMaLoai.Text))
            {
                string ten = (txtTenLoai.Text ?? "").Trim();
                if (string.IsNullOrEmpty(ten))
                {
                    MessageBox.Show("Vui lòng nhập tên loại!", "Thiếu dữ liệu",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var db = new QLSDbContext())
                {
                    if (db.LoaiSaches.Any(x => x.TenLoai.Equals(ten, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        MessageBox.Show("Tên loại đã tồn tại! Vui lòng nhập tên khác.",
                            "Trùng tên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            SaveLoai();
            ToggleEditMode(false);
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

        private void SaveLoai()
        {
            var ten = (txtTenLoai.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(ten))
            {
                MessageBox.Show("Vui lòng nhập tên loại!", "Thiếu dữ liệu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var db = new QLSDbContext())
                {
                    if (string.IsNullOrWhiteSpace(txtMaLoai.Text)) // THÊM
                    {
                        db.LoaiSaches.Add(new LoaiSach { TenLoai = ten });
                    }
                    else // SỬA
                    {
                        int id = int.Parse(txtMaLoai.Text);
                        var loai = db.LoaiSaches.FirstOrDefault(x => x.MaLoai == id);
                        if (loai == null)
                        {
                            MessageBox.Show("Loại sách không tồn tại!");
                            return;
                        }
                        loai.TenLoai = ten;
                    }

                    db.SaveChanges();
                }

                LoadGrid();
                ResetForm();
                MessageBox.Show("Lưu loại sách thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message, "Lỗi khi lưu",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteLoai()
        {
            if (string.IsNullOrWhiteSpace(txtMaLoai.Text)) return;
            int id = int.Parse(txtMaLoai.Text);

            using (var db = new QLSDbContext())
            {
                var loai = db.LoaiSaches.FirstOrDefault(x => x.MaLoai == id);
                if (loai == null)
                {
                    MessageBox.Show("Loại sách không tồn tại!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var sachs = db.Saches.Where(s => s.MaLoai == id).ToList();
                if (sachs.Any())
                {
                    var confirmWithBooks = MessageBox.Show(
                        $"Loại \"{loai.TenLoai}\" hiện có {sachs.Count} sách đang được gán.\n\n" +
                        $"Bạn có chắc muốn xóa loại này và toàn bộ {sachs.Count} sách đó không?",
                        "Xác nhận xóa loại và sách",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirmWithBooks != DialogResult.Yes)
                    {
                        MessageBox.Show("Đã hủy thao tác xóa.", "Thông báo",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    db.Saches.RemoveRange(sachs);
                }
                else
                {
                    var confirm = MessageBox.Show(
                        $"Bạn có chắc muốn xóa loại \"{loai.TenLoai}\"?",
                        "Xác nhận xóa loại",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm != DialogResult.Yes)
                        return;
                }

                db.LoaiSaches.Remove(loai);
                db.SaveChanges();
            }

            LoadGrid();
            ResetForm();
            MessageBox.Show("Đã xóa loại sách thành công!", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ========================== FORM ==========================

        private void FillForm()
        {
            if (dgvLoaiSach.CurrentRow?.DataBoundItem is LoaiSach loai)
            {
                txtMaLoai.Text = loai.MaLoai.ToString();
                txtTenLoai.Text = loai.TenLoai;
            }
        }

        private void ResetForm()
        {
            txtMaLoai.Clear();
            txtTenLoai.Clear();
            txtTimKiem.Clear();
            dgvLoaiSach.ClearSelection();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
