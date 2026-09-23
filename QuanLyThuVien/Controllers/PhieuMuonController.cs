using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

using QuanLyThuVien.Models;
using QuanLyThuVien.Migrations;

namespace QuanLyThuVien.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PhieuMuonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        /// Sửa hàm khởi tạo(Constructor)
        public PhieuMuonController(ApplicationDbContext context,
                            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Danh sách phiếu mượn
        // GET: Borrow/Index
        // GET: Borrow/Index
        public async Task<IActionResult> Index(string status, string searchString, DateTime? borrowDate)
        {

            // Cập nhật trạng thái Overdue
            var overdueRecords = await _context.PhieuMuons
                .Where(b => (b.Status == "Borrowing" || b.Status == "Overdue")
                            && b.DueDate < DateTime.Today)
                .ToListAsync();

            if (overdueRecords.Any())
            {
                foreach (var r in overdueRecords)
                {
                    r.Status = "Overdue";

                    var existingFine = await _context.PhieuPhats
                        .AnyAsync(f => f.BorrowId == r.BorrowId);

                    if (!existingFine)
                    {
                        var overdueDays = (DateTime.Today - r.DueDate.Date).Days; // thêm .Date
                        decimal finePerDay = 2000m;
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
                }
                await _context.SaveChangesAsync();
            }

            // 2. Truy vấn dữ liệu kèm các bảng liên quan (Include)
            // 1. Khởi tạo query ban đầu
            var query = _context.PhieuMuons
                .Include(b => b.Member)
                .Include(b => b.BorrowDetails)
                    .ThenInclude(d => d.Book)
                .AsQueryable();

            // 2. Lọc theo Status (nếu có)
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            // 3. Lọc theo từ khóa tìm kiếm (Giả sử bạn có biến 'searchString')
            if (!string.IsNullOrEmpty(searchString))
            {
                var s = searchString.ToLower().Trim();
                string search = searchString.ToLower();

                query = query.Where(b =>
                    // 1. Tìm theo Member
                    b.Member.FullName.ToLower().Contains(s) ||
                    b.Member.PhoneNumber.Contains(s) ||
                    b.Member.CMND.Contains(s) ||

                    // 2. Tìm theo Tên Sách (trong danh sách BorrowDetails)
                    b.BorrowDetails.Any(d => d.Book.TenSach.ToLower().Contains(s)) ||

                    // 3. Tìm theo Ngày mượn (Chuyển ngày mượn thành chuỗi để tìm kiếm nhanh)
                    b.BorrowDate.ToString().Contains(s)
                );
            }

            ViewBag.Status = status;
            ViewBag.CurrentFilter = searchString; // Đồng bộ với value="@ViewBag.CurrentFilter" ở View

            var data = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return View(data);
        }

        // 2. Tạo mới phiếu mượn
        public async Task<IActionResult> Create()
        {
            // --- XỬ LÝ THÀNH VIÊN ---
            var members = await _context.Members.ToListAsync();

            if (members != null && members.Any())
            {
                // Nếu CÓ dữ liệu thật: Đổ dữ liệu thật vào
                ViewBag.Members = new SelectList(members, "MemberId", "FullName");
            }
            else
            {
                // Nếu KHÔNG CÓ dữ liệu thật: Tạo 2 dữ liệu mẫu để test giao diện
                var mockMembers = new List<SelectListItem>
        {
            new SelectListItem { Value = "0", Text = "Mẫu: Nguyễn Văn Anh (Chưa có data thật)" },
            new SelectListItem { Value = "-1", Text = "Mẫu: Trần Thị Bình (Chưa có data thật)" }
        };
                ViewBag.Members = mockMembers;
            }

            var booksFromDb = await _context.Books.Where(b => b.Kho > 0).ToListAsync();

            List<BookManagement> displayBooks;

            if (booksFromDb == null || !booksFromDb.Any())
            {
                // Tạo sách mẫu
                displayBooks = new List<BookManagement> {
        new BookManagement { MaSach = "1", TenSach = "Sách Mẫu 1", TacGia = "TG Mẫu", Kho = 10 },
        new BookManagement { MaSach = "2", TenSach = "Sách Mẫu 2", TacGia = "TG Mẫu", Kho = 5 }
    };
            }
            else
            {
                // Chuyển đổi từ kiểu Book sang BookManagement (Mapping)
                displayBooks = booksFromDb.Select(b => new BookManagement
                {
                    MaSach = b.MaSach.ToString(), // Đảm bảo khớp kiểu string/int
                    TenSach = b.TenSach,
                    TacGia = b.TacGia,
                    Kho = b.Kho,
                    NamXuatBan = b.NamXuatBan
                }).ToList();
            }

            ViewBag.Books = displayBooks;

            return View();
        }

        // POST: Borrow/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int memberId, List<string> bookIds, DateTime dueDate, string? notes)
        {
            // Lưu ý: Tên tham số 'bookIds' phải khớp với thuộc tính 'name' ở View
            if (bookIds == null || !bookIds.Any())
            {
                TempData["Warning"] = "Vui lòng chọn ít nhất 1 cuốn sách!";
                return RedirectToAction(nameof(Create));
            }

            try
            {
                var record = new PhieuMuon
                {
                    MemberId = memberId,
                    BorrowDate = DateTime.Today,
                    DueDate = dueDate,
                    Status = "Borrowing",
                    Notes = notes,
                    CreatedByUserId = User.Identity?.Name,
                    CreatedAt = DateTime.Now // Quan trọng để Index sắp xếp OrderByDescending
                };

                _context.PhieuMuons.Add(record);
                await _context.SaveChangesAsync();

                foreach (var maSach in bookIds)
                {
                    _context.PhieuMuonChiTiets.Add(new PhieuMuonChiTiet
                    {
                        BorrowId = record.BorrowId,
                        BookId = maSach,
                        Quantity = 1,
                        ReturnedQty = 0
                    });

                    // Trừ tồn kho
                    var book = await _context.Books.FindAsync(maSach);
                    if (book != null) book.Kho--;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo phiếu mượn thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Lấy thông báo lỗi sâu nhất (InnerException)
                var innerMessage = ex.InnerException != null
                    ? ex.InnerException.Message
                    : ex.Message;

                if (ex.InnerException?.InnerException != null)
                    innerMessage = ex.InnerException.InnerException.Message;

                TempData["Warning"] = "Lỗi chi tiết: " + innerMessage;
                return RedirectToAction(nameof(Create));
            }
        }

        // 3. Xử lý trả sách
        public async Task<IActionResult> Return(int id)
        {
            var record = await _context.PhieuMuons
                .Include(b => b.BorrowDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(b => b.BorrowId == id);

            if (record == null) return NotFound();
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id, string? notes)
        {
            var record = await _context.PhieuMuons
        .Include(m => m.Member)
        .Include(m => m.BorrowDetails)
            .ThenInclude(d => d.Book)
        .Include(m => m.FineRecord) // BẮT BUỘC PHẢI CÓ DÒNG NÀY để nạp dữ liệu phiếu phạt
        .FirstOrDefaultAsync(m => m.BorrowId == id);

            if (record == null) return NotFound();

            // DÁN ĐOẠN NÀY VÀO: Kiểm tra phiếu phạt chưa thanh toán
            var unpaidFine = await _context.PhieuPhats
                .FirstOrDefaultAsync(f => f.BorrowId == id && !f.IsPaid);

            if (unpaidFine != null)
            {
                TempData["Warning"] = $"Không thể trả sách. Phiếu phạt {unpaidFine.TotalFine:N0} VNĐ chưa được thanh toán!";
                return RedirectToAction(nameof(Index)); // Hoặc quay lại trang Return
            }


            // Xử lý trả sách bình thường
            record.ReturnDate = DateTime.Today;
            record.Status = "Returned";
            record.Notes = notes;

            foreach (var detail in record.BorrowDetails)
            {
                detail.ReturnedQty = detail.Quantity;
                if (detail.Book != null)
                    detail.Book.Kho += detail.Quantity;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Trả sách thành công!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detail(int id)
        {
            var record = await _context.PhieuMuons
                .Include(b => b.Member) // Lấy thông tin người mượn
                .Include(b => b.BorrowDetails) // Lấy danh sách sách mượn
                    .ThenInclude(d => d.Book) // Lấy tên sách từ bảng Books
                .FirstOrDefaultAsync(b => b.BorrowId == id);

            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }
        /* TẠM ĐÓNG PHẦN MEMBER - SẼ MỞ LẠI SAU
       
        */
        [AllowAnonymous]
        public async Task<IActionResult> MyHistory()
        {
            // 1. Lấy ID của tài khoản đang đăng nhập
            var userId = _userManager.GetUserId(User);


            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // 2. Tìm thông tin Thành viên tương ứng
            // Quan trọng: Hãy chắc chắn UserId trong DB (như ảnh image_9180aa.png) 
            // khớp hoàn toàn với ID của tài khoản thinh123@gmail.com đang đăng nhập.
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (member == null)
            {
                ViewBag.NoMember = true;
                return View(new List<PhieuMuon>());
            }

            // 3. Lấy danh sách phiếu mượn
            var records = await _context.PhieuMuons
                .Include(b => b.Member)
                .Include(b => b.BorrowDetails)
                    .ThenInclude(d => d.Book)
                .Include(b => b.FineRecord)
                .Where(b => b.MemberId == member.MemberId)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            return View(records);
        }
    }
}