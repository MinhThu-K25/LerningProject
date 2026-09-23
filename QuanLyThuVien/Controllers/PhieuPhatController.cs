using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models; // Đảm bảo namespace này khớp với thư mục Models của bạn
// Thay bằng namespace DbContext của bạn

namespace QuanLyThuVien.Controllers
{
    public class PhieuPhatController : Controller
    {
        private readonly ApplicationDbContext _context;


        public PhieuPhatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Danh sách phiếu phạt với lọc trạng thái
        public async Task<IActionResult> Index(string? status)
        {
            var overdueRecords = await _context.PhieuMuons
      .Where(b => (b.Status == "Borrowing" || b.Status == "Overdue" || b.Status == null)
                  && b.DueDate.Date < DateTime.Today)
      .ToListAsync();

            if (overdueRecords.Any())
            {
                foreach (var r in overdueRecords)
                {
                    r.Status = "Overdue";

                    // Tìm phiếu phạt đã tồn tại
                    var currentFine = await _context.PhieuPhats
                        .FirstOrDefaultAsync(f => f.BorrowId == r.BorrowId);

                    var overdueDays = (DateTime.Today - r.DueDate).Days;
                    decimal finePerDay = 5000m;

                    if (currentFine == null)
                    {
                        // Tạo mới nếu chưa có
                        _context.PhieuPhats.Add(new PhieuPhat
                        {
                            BorrowId = r.BorrowId,
                            MemberId = r.MemberId,
                            OverdueDays = overdueDays,
                            FinePerDay = finePerDay,
                            TotalFine = overdueDays * finePerDay,
                            IsPaid = false,
                            CreatedAt = DateTime.Now
                        });
                    }
                    else if (!currentFine.IsPaid)
                    {
                        // Cập nhật lại số ngày và tiền nếu CHƯA thanh toán
                        currentFine.OverdueDays = overdueDays;
                        currentFine.TotalFine = overdueDays * finePerDay;
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Query dữ liệu bao gồm cả thông tin Thành viên, Phiếu mượn, Chi tiết phiếu mượn và Sách
            var query = _context.PhieuPhats
                .Include(f => f.Member)
                .Include(f => f.BorrowRecord)
                    .ThenInclude(b => b!.BorrowDetails) // Giả định tên là ChiTietPhieuMuons hoặc BorrowDetails
                        .ThenInclude(d => d.Book)           // Giả định tên là Sach hoặc Book
                .AsQueryable();

            // Lọc theo trạng thái thanh toán
            if (status == "unpaid")
            {
                query = query.Where(f => !f.IsPaid);
            }
            else if (status == "paid")
            {
                query = query.Where(f => f.IsPaid);
            }

            ViewBag.Status = status;

            // Lấy danh sách và sắp xếp mới nhất lên đầu
            var model = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();

            // Đảm bảo model không bao giờ null khi truyền qua View
            return View(model ?? new List<PhieuPhat>());
        }

        // 2. Xác nhận đã thu tiền phạt (Mark Paid)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var fine = await _context.PhieuPhats
        .Include(f => f.BorrowRecord)
        .FirstOrDefaultAsync(f => f.FineId == id);

            if (fine == null) return NotFound();

            if (!fine.IsPaid)
            {
                fine.IsPaid = true;
                fine.PaidAt = DateTime.Now;

                // Cập nhật phiếu mượn thành Returned sau khi thanh toán xong
                if (fine.BorrowRecord != null)
                {
                    fine.BorrowRecord.ReturnDate = DateTime.Today;
                    fine.BorrowRecord.Status = "Returned";
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thanh toán {fine.TotalFine:N0} VNĐ. Phiếu mượn đã được đóng.";
            }

            return RedirectToAction(nameof(Index));
        }

        // 3. Xem chi tiết phiếu phạt
        public async Task<IActionResult> Details(int id)
        {
            var fine = await _context.PhieuPhats
                .Include(f => f.Member)
                .Include(f => f.BorrowRecord)
                    .ThenInclude(b => b!.BorrowDetails)
                        .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(f => f.FineId == id);

            if (fine == null)
            {
                return NotFound();
            }

            return View(fine);
        }
    }
}