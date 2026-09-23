using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // Thay bằng Namespace DbContext của bạn
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Controllers
{
    // Chỉ Admin mới được quản lý danh sách thành viên
    [Authorize(Roles = "Admin")]
    public class ThanhVienController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        // Đổi IdentityUser thành AppUser


        public ThanhVienController(ApplicationDbContext context, UserManager<AppUser> userManager) // Sửa ở đây)
        {
            _context = context;
            _userManager = userManager;

        }

        // --- 1. DANH SÁCH THÀNH VIÊN ---
        public async Task<IActionResult> Index(string searchString)
        {
            // Truy vấn danh sách thành viên từ Database
            var query = _context.Members.AsQueryable();


            // Xử lý tìm kiếm nếu có nhập chuỗi tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                // 1. Chuẩn hóa chuỗi tìm kiếm (loại bỏ khoảng trắng thừa)
                string search = searchString.Trim().ToLower();

                query = query.Where(m =>
                       (m.FullName != null && m.FullName.ToLower().Contains(search))
                    || (m.PhoneNumber != null && m.PhoneNumber.Contains(search))
                    || (m.Email != null && m.Email.ToLower().Contains(search))
                    || (m.CMND != null && m.CMND.Contains(search))
                );
            }

            ViewBag.CurrentFilter = searchString;
            // Trả về View danh sách đã lọc (hoặc tất cả)
            return View(await query.OrderByDescending(m => m.JoinDate).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetCMND(int memberId)
        {
            var member = await _context.Members.FindAsync(memberId);
            if (member == null) return NotFound();
            return Json(new { cmnd = member.CMND ?? "Chưa có" });
        }

        // GET: ThanhVien/Create
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách tất cả user chưa có thẻ thành viên
            var existingUserIds = await _context.Members
                .Where(m => m.UserId != null)
                .Select(m => m.UserId)
                .ToListAsync();

            var availableUsers = await _userManager.Users
                .Where(u => !existingUserIds.Contains(u.Id))
                .ToListAsync();

            ViewBag.Users = new SelectList(availableUsers, "Id", "Email");
            return View();
        }

        // POST: ThanhVien/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThanhVien model, string selectedUserId)
        {
            if (ModelState.IsValid)
            {
                var existingMember = await _context.Members
                    .AnyAsync(m => m.UserId == selectedUserId);

                if (existingMember)
                {
                    ModelState.AddModelError("", "Tài khoản này đã được liên kết với một thẻ thành viên.");
                    return View(model);
                }

                model.UserId = selectedUserId; // Gán UserId của member được chọn
                model.JoinDate = DateTime.Today;
                if (model.ExpireDate == null)
                    model.ExpireDate = DateTime.Today.AddYears(1);

                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, load lại dropdown
            var existingUserIds2 = await _context.Members
                .Where(m => m.UserId != null)
                .Select(m => m.UserId)
                .ToListAsync();
            var availableUsers2 = await _userManager.Users
                .Where(u => !existingUserIds2.Contains(u.Id))
                .ToListAsync();
            ViewBag.Users = new SelectList(availableUsers2, "Id", "Email");

            return View(model);
        }

        // --- 4. CHỈNH SỬA (GIAO DIỆN) ---
        // GET: ThanhVien/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Tìm thành viên trong bảng ThanhVien dựa trên MemberId
            var member = await _context.Members.FindAsync(id);

            if (member == null)
            {
                return NotFound();
            }

            // Trả về View với dữ liệu thành viên, bao gồm cả cột CMND mới thêm (nếu có)
            return View(member);
        }

        // --- 5. XỬ LÝ CẬP NHẬT & CẤP VAI TRÒ ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ThanhVien model)
        {
            // 1. Kiểm tra ID trên URL và ID trong Form có khớp nhau không (Bảo mật)
            if (id != model.MemberId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 2. Lấy dữ liệu gốc từ Database (AsNoTracking để tránh xung đột theo dõi thực thể)
                    var originalMember = await _context.Members
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.MemberId == id);

                    if (originalMember == null)
                    {
                        return NotFound();
                    }

                    // 3. QUAN TRỌNG: Gán lại các giá trị không được phép thay đổi hoặc dễ bị NULL
                    model.UserId = originalMember.UserId;   // Giữ nguyên mã liên kết ead11478...
                    model.JoinDate = originalMember.JoinDate; // Giữ nguyên ngày tham gia gốc

                    // Nếu IsActive không có trên form, hãy giữ nguyên trạng thái cũ
                    // model.IsActive = originalMember.IsActive; 

                    // 4. Tiến hành cập nhật
                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Members.Any(e => e.MemberId == model.MemberId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // Hiển thị lỗi cụ thể nếu có vấn đề phát sinh
                    ModelState.AddModelError("", "Không thể lưu thay đổi: " + ex.Message);
                }
            }

            // Nếu có lỗi, trả về View cùng với model để người dùng sửa
            return View(model);
        }

        // Hàm phụ hỗ trợ kiểm tra tồn tại
        private bool MemberExists(int id)
        {
            return _context.Members.Any(e => e.MemberId == id);
        }


        // --- 6. KHÓA / MỞ KHÓA TÀI KHOẢN ---
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member != null)
            {
                // Đảo ngược trạng thái IsActive (true -> false và ngược lại)
                member.IsActive = !member.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- 7. XÓA THÀNH VIÊN ---
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member != null)
            {
                _context.Members.Remove(member);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}