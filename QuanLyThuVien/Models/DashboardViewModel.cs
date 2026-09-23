namespace QuanLyThuVien.Models
{
    public class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalMembers { get; set; }
        public int BorrowingCount { get; set; }
        public decimal TotalFines { get; set; }

        // Danh sách quá hạn
        public List<PhieuMuon> OverdueLoans { get; set; }

        // Phiếu phạt gần đây
        public List<PhieuPhat> RecentFines { get; set; }

        // Dữ liệu cho biểu đồ (Ví dụ đơn giản)
        public List<int> MonthlyBorrowData { get; set; }
        public List<int> FineReasonData { get; set; }
    }
}
