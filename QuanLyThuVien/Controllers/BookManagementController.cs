using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using QuanLyThuVien.Models;
using static System.Reflection.Metadata.BlobBuilder;

namespace QuanLyThuVien.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BookManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        // 1. Khai báo biến private để sử dụng trong toàn bộ Controller
        private readonly IWebHostEnvironment _hostEnvironment;

        // 2. Tiêm (Inject) dịch vụ vào hàm khởi tạo (Constructor)
        public BookManagementController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment; // Gán giá trị được hệ thống cung cấp vào biến private
        }

        // 1. Lấy danh sách sách từ Database BookManagement
        // GET: BookManagement/Index
        public async Task<IActionResult> Index(string searchString)
        {
            // 1. Lấy toàn bộ danh sách sách dưới dạng Queryable (chưa truy vấn DB)
            var books = from b in _context.Books
                        select b;

            // 2. Kiểm tra nếu có chuỗi tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                // Chuyển về chữ thường và xóa khoảng trắng thừa
                searchString = searchString.ToLower().Trim();

                // Lọc theo Tên Sách, Tác Giả, hoặc Thể Loại dựa trên Model của bạn
                books = books.Where(s => s.TenSach.ToLower().Contains(searchString)
                                       || (s.TacGia != null && s.TacGia.ToLower().Contains(searchString))
                                       || (s.TheLoai != null && s.TheLoai.ToLower().Contains(searchString))
                                       || s.MaSach.ToLower().Contains(searchString));
            }

            // 3. Sắp xếp theo tên sách mặc định và gửi về View
            var result = await books.OrderBy(s => s.TenSach).ToListAsync();

            // Lưu lại từ khóa tìm kiếm để hiển thị lại trên ô Input ở Layout
            ViewBag.CurrentSearch = searchString;

            return View(result);
        }

        // 2. Xử lý Xóa từ Modal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync(); // Lưu thay đổi vào DB
                TempData["Success"] = "Xóa sách thành công!";
            }
            return RedirectToAction(nameof(Index));

        }
        // GET: Hiển thị trang thêm sách
        [HttpGet]
        public IActionResult AddBooks() => View();

        // POST: Xử lý dữ liệu khi nhấn nút "Lưu thông tin"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBooks(BookManagement book, IFormFile? fileAnh)
        {
            // Kiểm tra tính hợp lệ của dữ liệu (các trường Required)
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Xử lý tải ảnh bìa lên (Upload Image)
                    if (fileAnh != null && fileAnh.Length > 0)
                    {
                        // Tạo đường dẫn lưu file trong wwwroot/uploads/images
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fileAnh.FileName);
                        string path = Path.Combine(wwwRootPath, @"uploads\images");

                        // Tạo thư mục nếu chưa tồn tại
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        using (var fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                        {
                            await fileAnh.CopyToAsync(fileStream);
                        }

                        // Lưu đường dẫn ảnh vào thuộc tính AnhBiaSach
                        book.AnhBiaSach = @"\uploads\images\" + fileName;
                    }

                    // 2. Thêm dữ liệu vào Database
                    _context.Add(book);
                    await _context.SaveChangesAsync();

                    // Thông báo thành công (tùy chọn)
                    TempData["Success"] = "Đã thêm sách " + book.TenSach + " thành công!";

                    // 3. Chuyển hướng về trang danh sách (Index)
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu: " + ex.Message);
                }
            }

            // Nếu dữ liệu không hợp lệ, trả lại View kèm các thông báo lỗi
            return View(book);
        }
        // 1. GET: BookManagement/EditBooks/MS001
        // Hàm này dùng để load dữ liệu cũ lên Form
        [HttpGet]
        public async Task<IActionResult> EditBooks(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // Tìm sách trong database theo Primary Key (MaSach)
            var sach = await _context.Books.FindAsync(id);

            if (sach == null)
            {
                return NotFound(); // Trả về 404 nếu không tìm thấy mã sách này
            }

            return View(sach);
        }

        // 2. CHỨC NĂNG CẬP NHẬT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBooks(string id, [Bind("MaSach,TenSach,TheLoai,TaiBan,Kho,NamXuatBan,NhaXuatBan,TacGia,AnhBiaSach,MoTaNoiDung")] BookManagement sach, IFormFile? fileAnh)
        {
            if (id != sach.MaSach) return NotFound();

            if (ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                try
                {
                    // XỬ LÝ FILE ẢNH
                    if (fileAnh != null && fileAnh.Length > 0)
                    {
                        // Tạo tên file duy nhất
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fileAnh.FileName);
                        string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                        // Lưu file vào thư mục wwwroot/images
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await fileAnh.CopyToAsync(stream);
                        }

                        // Gán đường dẫn mới vào thuộc tính AnhBiaSach (kiểu string) để lưu DB
                        sach.AnhBiaSach = "/images/" + fileName;
                    }

                    _context.Update(sach);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Books.Any(e => e.MaSach == sach.MaSach)) return NotFound();
                    else throw;
                }
            }
            return View(sach);
        }

        // Hàm kiểm tra sự tồn tại của sách
        private bool SachExists(string id)
        {
            return _context.Books.Any(e => e.MaSach == id);
        }
    }
}
