using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using System;

namespace QuanLyThuVien.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        // TÍCH HỢP: Thêm RoleManager để quản lý các nhóm quyền (Admin, User...)
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        // TÍCH HỢP: Inject RoleManager vào Constructor
        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context) // THÊM THAM SỐ NÀY)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context; // GÁN GIÁ TRỊ VÀO ĐÂY
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            return View(user);
        }

        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            // TÍCH HỢP: PasswordSignInAsync kiểm tra email/password và tạo Cookie xác thực
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                // === DÁN ĐOẠN MÃ CỦA BẠN VÀO ĐÂY ===
                var roles = await _userManager.GetRolesAsync(user!);
                if (roles.Contains("Admin"))
                {
                    // Nếu là Admin, chuyển hướng thẳng đến trang quản lý sách
                    return RedirectToAction("Index", "BookManagement");
                }

                // TÍCH HỢP: Xử lý ReturnUrl để quay lại trang người dùng đang xem trước khi đăng nhập
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Email hoặc mật khẩu không đúng!";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp!");
                return View();
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                //// 1. Tạo bản ghi ThanhVien để liên kết với tài khoản vừa tạo
                //var moiThanhVien = new ThanhVien
                //{
                //    FullName = fullName,
                //    Email = email,
                //    JoinDate = DateTime.Now,
                //    UserId = user.Id // Khóa ngoại liên kết với tài khoản đăng nhập
                //};

                //// Lưu vào database (Giả sử bạn dùng _context cho EF Core)
                //_context.Members.Add(moiThanhVien);
                //await _context.SaveChangesAsync();
                //// 1. Chỉ gán Role cơ bản "User" để họ có thể đăng nhập
                //if (!await _roleManager.RoleExistsAsync("User"))
                //{
                //    await _roleManager.CreateAsync(new IdentityRole("User"));
                //}
                ////Dòng này sẽ cho khi đăng ký tk sẽ tự động gán User / vai trò mà chưa duyệt
                ////await _userManager.AddToRoleAsync(user, "User");

                //// 2. Thông báo cho người dùng biết họ cần đợi duyệt
                //TempData["Message"] = "Đăng ký thành công! Tài khoản của bạn đang chờ Admin cấp thẻ thành viên.";

                // Đăng nhập ngay sau khi đăng ký
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // TÍCH HỢP: Xóa Cookie xác thực của người dùng
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        public async Task<IActionResult> ChangeTaiKhoan()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeTaiKhoan(AppUser model, IFormFile? ImageFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // TÍCH HỢP: Xử lý Upload ảnh đại diện (Avatar)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");

                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                user.AvatarUrl = "/images/avatars/" + fileName;
            }

            // TÍCH HỢP: Cập nhật thông tin User vào Database thông qua UserManager
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // TÍCH HỢP: RefreshSignInAsync giúp cập nhật lại các Claims (như FullName) trên Layout ngay lập tức
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(user);
        }
    }
}