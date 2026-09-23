using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien.Models
{
    public class PhieuMuonChiTiet
    {
        [Key]
        public int DetailId { get; set; }
        public int BorrowId { get; set; }
        // Đây là cột lưu giá trị mã sách (ví dụ: "S001")
        public string BookId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public int ReturnedQty { get; set; } = 0;

        [ForeignKey("BorrowId")]
        public virtual PhieuMuon? BorrowRecord { get; set; }

        // KHẮC PHỤC TẠI ĐÂY:
        // Báo cho EF biết BookId ở đây liên kết với MaSach ở bảng BookManagement
        [ForeignKey("BookId")]
        public virtual BookManagement? Book { get; set; }
    }
}
