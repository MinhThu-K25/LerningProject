using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Models
{
    public class AppUser : IdentityUser
    {
        public string? AvatarUrl { get; set; }
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        
    }
}
