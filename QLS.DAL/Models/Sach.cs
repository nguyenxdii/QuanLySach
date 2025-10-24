using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLS.DAL.Models
{
    [Table("Sach")]
    public class Sach
    {
        // MaSach: PK, CHAR(6)
        [Key]
        [Column(TypeName = "char")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã sách phải đúng 6 ký tự.")]
        public string MaSach { get; set; }

        [Required, StringLength(200)]
        public string TenSach { get; set; }

        public int NamXB { get; set; }

        // FK
        [ForeignKey(nameof(Loai))]
        public int MaLoai { get; set; }

        [StringLength(255)]
        public string HinhAnh { get; set; }   // chỉ lưu tên file ảnh

        public virtual LoaiSach Loai { get; set; }
    }
}
