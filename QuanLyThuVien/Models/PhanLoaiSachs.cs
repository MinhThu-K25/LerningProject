using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Models
{
    public class PhanLoaiSachs
    {
        [Key]
        public string MaPhanLoaiSach { get; set; } = string.Empty;

        [Required]
        public string TheLoai { get; set; } = string.Empty;

        [Required]
        public string TenKe { get; set; } = string.Empty;

        public string Hang { get; set; } = string.Empty;

        public int DayNgang { get; set; }
        public int DayDoc { get; set; }
        public int Tang { get; set; }

        public int SucChua { get; set; }
        public string TrangThai { get; set; } = string.Empty;

        [StringLength(500)]
        public string MoTa { get; set; } = string.Empty;
    }
}
