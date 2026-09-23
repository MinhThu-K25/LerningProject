using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
 // Thay bằng Namespace chứa ApplicationDbContext của bạn

namespace QuanLyThuVien.Controllers
{
    public class PhanLoaiSachsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PhanLoaiSachsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: PhanLoaiSachs/Index
        public async Task<IActionResult> Index()
        {
            // Lấy toàn bộ danh sách phân loại sách từ Database
            var danhSach = await _context.PhanLoaiSachs.ToListAsync();
            return View(danhSach);
        }

        // 2. GET: PhanLoaiSachs/AddPhanLoai
        public IActionResult AddPhanLoai()
        {
            return View();
        }

        // 2. POST: PhanLoaiSachs/AddPhanLoai
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhanLoai(PhanLoaiSachs phanLoai)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng mã trước khi thêm
                bool isExist = await _context.PhanLoaiSachs.AnyAsync(x => x.MaPhanLoaiSach == phanLoai.MaPhanLoaiSach);
                if (isExist)
                {
                    ModelState.AddModelError("MaPhanLoaiSach", "Mã phân loại này đã tồn tại.");
                    return View(phanLoai);
                }

                _context.Add(phanLoai);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(phanLoai);
        }

        // 3. GET: PhanLoaiSachs/EditPhanLoai/5
        public async Task<IActionResult> EditPhanLoai(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var phanLoai = await _context.PhanLoaiSachs.FindAsync(id);
            if (phanLoai == null) return NotFound();

            return View(phanLoai);
        }

        // 3. POST: PhanLoaiSachs/EditPhanLoai/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPhanLoai(string id, PhanLoaiSachs phanLoai)
        {
            if (id != phanLoai.MaPhanLoaiSach) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phanLoai);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhanLoaiExists(phanLoai.MaPhanLoaiSach)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(phanLoai);
        }

        // 4. POST: PhanLoaiSachs/DeletePhanLoai (Xử lý nút xóa qua Ajax)
        [HttpPost]
        public async Task<IActionResult> DeletePhanLoai(string id)
        {
            var phanLoai = await _context.PhanLoaiSachs.FindAsync(id);
            if (phanLoai == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã phân loại này." });
            }

            try
            {
                _context.PhanLoaiSachs.Remove(phanLoai);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        // Hàm hỗ trợ kiểm tra tồn tại
        private bool PhanLoaiExists(string id)
        {
            return _context.PhanLoaiSachs.Any(e => e.MaPhanLoaiSach == id);
        }
    }
}