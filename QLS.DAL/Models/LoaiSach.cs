using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLS.DAL.Models
{
    [Table("LoaiSach")]
    public class LoaiSach
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaLoai { get; set; }

        [Required, StringLength(100)]
        public string TenLoai { get; set; }

        public virtual ICollection<Sach> Saches { get; set; } = new HashSet<Sach>();
    }
}
