using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Thêm dòng này

namespace QuanLyThuVien.Models
{
    public class BookManagement
    {
        [Key]
        [Display(Name = "Mã Sách")]
        public string MaSach { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên sách không được để trống")]
        [Display(Name = "Tên Sách")]
        public string TenSach { get; set; } = string.Empty;

        [Display(Name = "Thể Loại")]
        public string? TheLoai { get; set; }

        [Display(Name = "Tái Bản")]
        public string? TaiBan { get; set; }

        [Display(Name = "Kho")]
        public int Kho { get; set; }

        [Display(Name = "Năm Xuất Bản")]
        public int? NamXuatBan { get; set; }

        [Display(Name = "Nhà Xuất Bản")]
        public string? NhaXuatBan { get; set; }

        [Display(Name = "Tác Giả")]
        public string? TacGia { get; set; }

        [Display(Name = "Ảnh Bìa Sách")]
        public string? AnhBiaSach { get; set; }

        [Display(Name = "Mô Tả Nội Dung")]
        public string? MoTaNoiDung { get; set; }


        
        public virtual AppUser? AppUser { get; set; }
    }
}