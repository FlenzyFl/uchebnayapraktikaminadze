using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EmployeeManagementApp.Data;
using EmployeeManagementApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AccountController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
                return View(model);
            }

            await SignInUserAsync(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Employees");
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _db.UserAccounts.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError(nameof(model.Username), "Пользователь с таким логином уже существует");
                return View(model);
            }

            var user = new UserAccount
            {
                Username = model.Username,
                DisplayName = model.DisplayName,
                PasswordHash = PasswordHelper.HashPassword(model.Password)
            };

            _db.UserAccounts.Add(user);
            await _db.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var username = User.Identity!.Name!;
            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                Username = user.Username,
                DisplayName = user.DisplayName ?? user.Username,
                ExistingAvatarPath = user.AvatarPath
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var username = User.Identity!.Name!;
            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            user.DisplayName = model.DisplayName;

            if (model.AvatarFile is not null && model.AvatarFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "avatars");
                Directory.CreateDirectory(uploadsDir);

                var ext = Path.GetExtension(model.AvatarFile.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

                var fileName = $"{user.Id}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }

                user.AvatarPath = $"/avatars/{fileName}";
            }

            await _db.SaveChangesAsync();
            await SignInUserAsync(user);

            TempData["ProfileUpdated"] = true;
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var username = User.Identity!.Name!;
            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Текущий пароль указан неверно");
                return View(model);
            }

            user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            await _db.SaveChangesAsync();

            TempData["PasswordChanged"] = true;
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private async Task SignInUserAsync(UserAccount user)
        {
            var displayName = user.DisplayName ?? user.Username;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("displayName", displayName),
                new Claim("avatarPath", user.AvatarPath ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }
    }

    public class LoginViewModel
    {
        [Required, Display(Name = "Логин")]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required, Display(Name = "Логин")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Отображаемое имя")]
        public string? DisplayName { get; set; }

        [Required, DataType(DataType.Password), Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Подтверждение пароля"), Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        public string Username { get; set; } = string.Empty;

        [Required, Display(Name = "Отображаемое имя")]
        public string DisplayName { get; set; } = string.Empty;

        public string? ExistingAvatarPath { get; set; }

        [Display(Name = "Новый аватар")]
        public IFormFile? AvatarFile { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password), Display(Name = "Текущий пароль")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Новый пароль")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Подтверждение нового пароля"), Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
