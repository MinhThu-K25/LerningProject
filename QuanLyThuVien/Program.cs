using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Cấu hình Database (Phải có dòng này trước) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


// --- 2. ĐẶT DÒNG IDENTITY Ở ĐÂY ---
// Vị trí: Sau AddDbContext và trước builder.Build()
// Thay IdentityUser bằng AppUser
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // 1. Chỉ cho 1 email được đăng ký 1 lần
    options.User.RequireUniqueEmail = true;

    // 2. Mật khẩu ít nhất 8 ký tự
    options.Password.RequiredLength = 8;

    // 3. Ít nhất 1 ký tự số
    options.Password.RequireDigit = true;

    // 4. Ít nhất 1 ký tự đặc biệt (ví dụ: @, #, !, ...)
    options.Password.RequireNonAlphanumeric = true;

    // 5. Yêu cầu chữ thường và chữ hoa
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;

    // Các thiết lập khác
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();


// 3. Cấu hình đường dẫn Login/Logout (Rất quan trọng để điều hướng khi truy cập trái phép)
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied"; // Trang báo lỗi khi không đủ quyền
});
builder.Services.AddControllersWithViews();

// --- KẾT THÚC PHẦN CẤU HÌNH DỊCH VỤ ---
var app = builder.Build();

// --- 4. Seed Data (Khởi tạo Role và Admin) ---
using (var scope = app.Services.CreateScope()) // Tạo một phạm vi dịch vụ tạm thời
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // TÍCH HỢP: Khởi tạo các Role (Vai trò)
    string[] roleNames = { "Admin", "User" };
    foreach (var roleName in roleNames)
    {
        // Nếu Role chưa tồn tại trong Database thì tạo mới
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // TÍCH HỢP: Tạo tài khoản Admin mặc định
    var adminEmail = "admin@thuvien.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Quản trị viên",
            EmailConfirmed = true // Xác nhận email để tránh bị chặn đăng nhập
        };

        // Tạo Admin với mật khẩu mặc định
        var createAdmin = await userManager.CreateAsync(admin, "Admin@123");

        if (createAdmin.Succeeded)
        {
            // TÍCH HỢP: Gán quyền Admin cho tài khoản vừa tạo
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();