using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien.Models
{
    public class PhieuPhat
    {
        [Key]
        public int FineId { get; set; }
        // Chỉ định rõ ràng đây là Foreign Key cho BorrowRecord
        [ForeignKey("BorrowRecord")]
        public int BorrowId { get; set; }
        public int MemberId { get; set; }

        [Display(Name = "Số ngày trễ")]
        public int OverdueDays { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Phạt/ngày (VNĐ)")]
        public decimal FinePerDay { get; set; } = 5000;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Tổng tiền phạt")]
        public decimal TotalFine { get; set; } = 0;

        [Display(Name = "Đã thanh toán")]
        public bool IsPaid { get; set; } = false;

        public DateTime? PaidAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public PhieuMuon? BorrowRecord { get; set; }
        public ThanhVien? Member { get; set; }
    }
}
