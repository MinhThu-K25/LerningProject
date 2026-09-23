using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Models
{
    public class ThanhVien
    {
        [Key]
        public int MemberId { get; set; }

        public string? UserId { get; set; } = string.Empty;

        
        public string? MemberCode { get; set; } = string.Empty;

        [Required, StringLength(200)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10, ErrorMessage = "Số điện thoại phải đúng 10 chữ số.")] // Giới hạn độ dài tại VN thường là 10 số
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (Phải bắt đầu bằng số 0 và có 10 chữ số).")] // Kiểm tra định dạng
        [DataType(DataType.PhoneNumber)] // Xác định kiểu dữ liệu là số điện thoại
        [Display(Name = "Số điện thoại")] // Tên hiển thị trên giao diện (Label)
        public string? PhoneNumber { get; set; }

        [StringLength(12)]
        [RegularExpression(@"^\d{9}(\d{3})?$", ErrorMessage = "CMND phải có 9 hoặc 12 chữ số.")]
        [Display(Name = "CMND/CCCD")]
        public string? CMND { get; set; }

        [StringLength(500)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [StringLength(256), EmailAddress]
        public string? Email { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày tham gia")]
        public DateTime JoinDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Ngày hết hạn thẻ")]
        public DateTime? ExpireDate { get; set; }
        // Thêm vào class ThanhVien

        public bool IsActive { get; set; } = true;

        public virtual ICollection<PhieuMuon> PhieuMuons { get; set; } = new List<PhieuMuon>();
    }
}
