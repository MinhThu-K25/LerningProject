using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyThuVien.Controllers.Admin
{
    [Authorize(Roles = "Admin")] // Đảm bảo chỉ Admin mới truy cập được
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
