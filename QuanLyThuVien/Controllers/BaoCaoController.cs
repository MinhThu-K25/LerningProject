using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using System.Linq;

namespace QuanLyThuVien.Controllers
{
    public class BaoCaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();

            // 1. Thống kê tổng quan (Cards)
            viewModel.TotalBooks = await _context.Books.CountAsync();
            viewModel.TotalMembers = await _context.Members.CountAsync();
            viewModel.BorrowingCount = await _context.PhieuMuons
                                            .CountAsync(p => p.Status == "Borrowing");

            // Tổng tiền phạt đã thu (hoặc tổng tất cả)
            viewModel.TotalFines = await _context.PhieuPhats.SumAsync(p => p.TotalFine);

            // 2. Lấy danh sách phiếu mượn quá hạn (DueDate < Today và chưa trả)
            viewModel.OverdueLoans = await _context.PhieuMuons
                                            .Include(p => p.Member)
                                            .Include(p => p.BorrowDetails)
                                            .Where(p => p.DueDate < DateTime.Today && p.ReturnDate == null)
                                            .OrderBy(p => p.DueDate)
                                            .Take(5)
                                            .ToListAsync();

            // 3. Lấy phiếu phạt gần đây
            viewModel.RecentFines = await _context.PhieuPhats
                                            .Include(p => p.Member)
                                            .OrderByDescending(p => p.CreatedAt)
                                            .Take(5)
                                            .ToListAsync();

            // 4. Dữ liệu cho biểu đồ mặc định (Năm hiện tại)
            // -- MonthlyBorrowData: đếm số phiếu mượn theo BorrowDate từng tháng trong năm nay
            var currentYear = DateTime.Today.Year;
            viewModel.MonthlyBorrowData = Enumerable.Range(1, 12)
                .Select(m => _context.PhieuMuons.Count(p => p.BorrowDate.Year == currentYear && p.BorrowDate.Month == m))
                .ToList();

            // -- FineReasonData: đếm số phiếu phạt theo lý do [Quá hạn, Mất sách, Hư hỏng]
            // "Quá hạn"  = PhieuPhat có BorrowRecord với DueDate < ReturnDate (trả trễ)
            // "Mất sách" = PhieuPhat có OverdueDays == 0 và TotalFine > 0 (phạt không do trả trễ) — tạm dùng heuristic
            // "Hư hỏng"  = chưa có trường phân biệt trong model, tạm để 0
            viewModel.FineReasonData = new List<int>
            {
                await _context.PhieuPhats
                    .Include(p => p.BorrowRecord)
                    .CountAsync(p => p.BorrowRecord != null
                                  && p.BorrowRecord.ReturnDate != null
                                  && p.BorrowRecord.DueDate < p.BorrowRecord.ReturnDate),   // Quá hạn

                await _context.PhieuPhats
                    .CountAsync(p => p.OverdueDays == 0 && p.TotalFine > 0),                 // Mất sách (heuristic)

                0  // Hư hỏng — PhieuPhat chưa có trường Reason để phân biệt
            };

            return View(viewModel);
        }

        public IActionResult BaoCao()
        {
            return View();
        }

        // ============================================================
        // ✅ [MỚI] API: Dữ liệu biểu đồ Hiệu suất Mượn Sách
        // Endpoint: GET /BaoCao/GetBorrowData?period=year|month|week
        // ============================================================
        [HttpGet]
        public IActionResult GetBorrowData(string period = "year")
        {
            var now = DateTime.Today;
            List<string> labels;
            List<int> data;

            if (period == "week")
            {
                // ✅ [MỚI] Chế độ Tuần: 7 ngày gần nhất, mỗi cột = 1 ngày
                labels = Enumerable.Range(0, 7)
                    .Select(i => now.AddDays(-6 + i).ToString("dd/MM"))
                    .ToList();

                data = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var day = now.AddDays(-6 + i).Date;
                        return _context.PhieuMuons.Count(p => p.BorrowDate.Date == day);
                    })
                    .ToList();
            }
            else if (period == "month")
            {
                // ✅ [MỚI] Chế độ Tháng: 30 ngày gần nhất, chia thành 4 tuần
                labels = new List<string> { "Tuần 1", "Tuần 2", "Tuần 3", "Tuần 4" };

                data = Enumerable.Range(0, 4)
                    .Select(i =>
                    {
                        var from = now.AddDays(-29 + i * 7).Date;
                        var to = from.AddDays(6);
                        return _context.PhieuMuons.Count(p =>
                            p.BorrowDate.Date >= from && p.BorrowDate.Date <= to);
                    })
                    .ToList();
            }
            else
            {
                // ✅ [MỚI] Chế độ Năm (mặc định): 12 tháng trong năm hiện tại
                labels = Enumerable.Range(1, 12).Select(m => $"Th.{m}").ToList();

                data = Enumerable.Range(1, 12)
                    .Select(m => _context.PhieuMuons.Count(p =>
                        p.BorrowDate.Year == now.Year && p.BorrowDate.Month == m))
                    .ToList();
            }

            return Json(new { labels, data });
        }

        // ============================================================
        // ✅ [MỚI] API: Dữ liệu biểu đồ Lý do bị Phạt
        // Endpoint: GET /BaoCao/GetFineReasonData?period=year|month|week
        // ============================================================
        [HttpGet]
        public IActionResult GetFineReasonData(string period = "year")
        {
            var now = DateTime.Today;

            // ✅ [MỚI] Xác định mốc thời gian bắt đầu theo period
            DateTime from = period == "week" ? now.AddDays(-6)
                          : period == "month" ? now.AddDays(-29)
                          : new DateTime(now.Year, 1, 1);

            // ✅ [MỚI] Đếm số phiếu phạt theo từng lý do trong khoảng thời gian
            // "Quá hạn"  = BorrowRecord.DueDate < BorrowRecord.ReturnDate (trả trễ)
            // "Mất sách" = OverdueDays == 0 && TotalFine > 0 (phạt không do trả trễ) — heuristic
            // "Hư hỏng"  = PhieuPhat chưa có trường Reason để phân biệt, tạm để 0
            var data = new[]
            {
                _context.PhieuPhats
                    .Include(p => p.BorrowRecord)
                    .Count(p => p.CreatedAt.Date >= from
                             && p.BorrowRecord != null
                             && p.BorrowRecord.ReturnDate != null
                             && p.BorrowRecord.DueDate < p.BorrowRecord.ReturnDate),   // Quá hạn

                _context.PhieuPhats
                    .Count(p => p.CreatedAt.Date >= from
                             && p.OverdueDays == 0 && p.TotalFine > 0),                 // Mất sách (heuristic)

                0  // Hư hỏng — chưa có trường phân biệt trong model
            };

            return Json(new { data });
        }

        // ============================================================
        // ✅ [MỚI] API: Danh sách Sách quá hạn chưa trả
        // Endpoint: GET /BaoCao/GetOverdueLoans?period=year|month|week
        // ============================================================
        [HttpGet]
        public IActionResult GetOverdueLoans(string period = "year")
        {
            var now = DateTime.Today;

            // ✅ [MỚI] Lọc các phiếu quá hạn có DueDate trong khoảng period được chọn
            DateTime from = period == "week" ? now.AddDays(-6)
                          : period == "month" ? now.AddDays(-29)
                          : new DateTime(now.Year, 1, 1);

            var loans = _context.PhieuMuons
                .Include(p => p.Member)
                .Where(p => p.DueDate < now          // Đã quá hạn
                         && p.ReturnDate == null      // Chưa trả
                         && p.DueDate.Date >= from)   // Trong khoảng period
                .OrderBy(p => p.DueDate)
                .Select(p => new
                {
                    // ✅ [MỚI] Trả về JSON để View render lại bảng
                    borrowId = p.BorrowId,
                    memberName = p.Member!.FullName,
                    dueDate = p.DueDate
                })
                .ToList();

            return Json(loans);
        }

        // ============================================================
        // ✅ [MỚI] API: Danh sách Phiếu phạt mới nhất
        // Endpoint: GET /BaoCao/GetRecentFines?period=year|month|week
        // ============================================================
        [HttpGet]
        public IActionResult GetRecentFines(string period = "year")
        {
            var now = DateTime.Today;

            // ✅ [MỚI] Lọc phiếu phạt được tạo trong khoảng period được chọn
            DateTime from = period == "week" ? now.AddDays(-6)
                          : period == "month" ? now.AddDays(-29)
                          : new DateTime(now.Year, 1, 1);

            var fines = _context.PhieuPhats
                .Include(p => p.Member)
                .Where(p => p.CreatedAt.Date >= from)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .Select(p => new
                {
                    // ✅ [MỚI] Trả về JSON để View render lại bảng
                    memberName = p.Member!.FullName,
                    totalFine = p.TotalFine,
                    createdAt = p.CreatedAt
                })
                .ToList();

            return Json(fines);
        }
    }
}