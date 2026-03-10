using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace EmployeeManagementApp.Models
{
    public class UserAccount
    {
        public int Id { get; set; }

        [Required, Display(Name = "Логин")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Отображаемое имя")]
        public string? DisplayName { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Display(Name = "Аватар")]
        public string? AvatarPath { get; set; }
    }

    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}
