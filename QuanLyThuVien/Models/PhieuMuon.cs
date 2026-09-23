using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien.Models
{
    [Table("PhieuMuon")]
    public class PhieuMuon
    {
        [Key]
        public int BorrowId { get; set; }

        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public virtual ThanhVien? Member { get; set; } // Giữ lại cái này (số ít)

        [DataType(DataType.Date)]
        public DateTime BorrowDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

        public DateTime? ReturnDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Borrowing";

        public string? Notes { get; set; }

        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // XÓA DÒNG NÀY: public ThanhVien? Members { get; set; } <--- ĐÂY LÀ NGUYÊN NHÂN LỖI

        public virtual ICollection<PhieuMuonChiTiet> BorrowDetails { get; set; } = new List<PhieuMuonChiTiet>();

        public virtual PhieuPhat? FineRecord { get; set; }
    }
}