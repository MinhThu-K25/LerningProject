using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore; // Quan trọng nhất để sửa lỗi ToListAsync
using QuanLyThuVien.Models; 
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

public class ApplicationDbContext : IdentityDbContext<AppUser> // Phải có <IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        
    }
    public DbSet<BookManagement> Books { get; set; }
    public DbSet<PhanLoaiSachs> PhanLoaiSachs { get; set; }
    public DbSet<PhieuPhat> PhieuPhats { get; set; }
    public DbSet<PhieuMuon> PhieuMuons { get; set; }
    public DbSet<PhieuMuonChiTiet> PhieuMuonChiTiets { get; set; }
    public DbSet<ThanhVien> Members { get; set; }

    public DbSet<AppUser> AppUser { get; set; }

    // ĐÂY LÀ NƠI BẠN CẦN DÁN CODE
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Rất quan trọng: Phải có dòng base này nếu dùng Identity
        base.OnModelCreating(modelBuilder);

        // Cấu hình để tránh lỗi "cycles or multiple cascade paths"
        modelBuilder.Entity<PhieuPhat>()
            .HasOne(p => p.Member)
            .WithMany() // Hoặc .WithMany(m => m.PhieuPhats) nếu có ICollection
            .HasForeignKey(p => p.MemberId)
            .OnDelete(DeleteBehavior.NoAction); // Ngăn chặn xóa dây chuyền gây lỗi SQL

    }


}