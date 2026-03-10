using System.Globalization;
using CondiService.Web.Data;
using CondiService.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Services;

public class SeedData
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IWebHostEnvironment _env;

    public const string RoleManagerName = "Менеджер";
    public const string RoleOperator = "Оператор";
    public const string RoleSpecialist = "Специалист";
    public const string RoleCustomer = "Заказчик";
    public const string RoleQualityManager = "Менеджер по качеству";

    public SeedData(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _env = env;
    }

    public async Task SeedAsync()
    {
        await EnsureRolesAsync();


        if (await _userManager.Users.AnyAsync(u => u.ExternalUserId != null))
            return;


        var importRoot = Path.Combine(_env.ContentRootPath, "Data", "Import");

        var usersPath = FindFirst(importRoot, "inputDataUsers.csv");
        var requestsPath = FindFirst(importRoot, "inputDataRequests.csv");
        var commentsPath = FindFirst(importRoot, "inputDataComments.csv");

        var importUsers = ReadCsv(usersPath);
        var importRequests = ReadCsv(requestsPath);
        var importComments = ReadCsv(commentsPath);

        if (importUsers.Count == 0)
        {
            await EnsureFallbackUsersAsync();
            return;
        }

        var usersByExternalId = new Dictionary<int, ApplicationUser>();

        foreach (var row in importUsers)
        {
            var externalId = int.Parse(row["userID"], CultureInfo.InvariantCulture);
            var fullName = row.GetValueOrDefault("fio", "");
            var phone = row.GetValueOrDefault("phone", "");
            var login = row.GetValueOrDefault("login", "");
            var password = row.GetValueOrDefault("password", "");
            var type = row.GetValueOrDefault("type", "");

            var user = new ApplicationUser
            {
                ExternalUserId = externalId,
                UserName = login,
                PhoneNumber = phone,
                FullName = fullName
            };

            var create = await _userManager.CreateAsync(user, password);
            if (!create.Succeeded)
                throw new InvalidOperationException("Ошибка создания пользователя: " + string.Join("; ", create.Errors.Select(e => e.Description)));

            var role = type switch
            {
                RoleManagerName => RoleManagerName,
                RoleOperator => RoleOperator,
                RoleSpecialist => RoleSpecialist,
                RoleCustomer => RoleCustomer,
                _ => RoleCustomer
            };

            await _userManager.AddToRoleAsync(user, role);
            usersByExternalId[externalId] = user;
        }

        foreach (var row in importRequests)
        {
            var id = int.Parse(row["requestID"], CultureInfo.InvariantCulture);
            var startDate = DateTime.Parse(row["startDate"], CultureInfo.InvariantCulture);

            var statusText = row.GetValueOrDefault("requestStatus", "");
            var status = MapStatus(statusText);

            DateTime? completionDate = null;
            if (DateTime.TryParse(row.GetValueOrDefault("completionDate", ""), CultureInfo.InvariantCulture, DateTimeStyles.None, out var cd))
                completionDate = cd;

            var masterIdText = row.GetValueOrDefault("masterID", "");
            var clientIdText = row.GetValueOrDefault("clientID", "");

            string? assignedSpecUserId = null;
            if (int.TryParse(masterIdText, out var masterExternalId) && usersByExternalId.TryGetValue(masterExternalId, out var specUser))
                assignedSpecUserId = specUser.Id;

            string? customerUserId = null;
            if (int.TryParse(clientIdText, out var clientExternalId) && usersByExternalId.TryGetValue(clientExternalId, out var clientUser))
                customerUserId = clientUser.Id;

            var customerName = usersByExternalId.Values.FirstOrDefault(u => u.ExternalUserId == (int.TryParse(clientIdText, out var tmp) ? tmp : -1))?.FullName ?? "";
            var customerPhone = usersByExternalId.Values.FirstOrDefault(u => u.ExternalUserId == (int.TryParse(clientIdText, out var tmp2) ? tmp2 : -1))?.PhoneNumber ?? "";

            var req = new RepairRequest
            {
                Id = id,
                CreatedAt = startDate,
                EquipmentType = row.GetValueOrDefault("climateTechType", ""),
                DeviceModel = row.GetValueOrDefault("climateTechModel", ""),
                ProblemDescription = row.GetValueOrDefault("problemDescryption", ""),
                Status = status,
                DueAt = startDate.AddDays(3),
                CompletedAt = completionDate,
                RepairParts = NullIfEmpty(row.GetValueOrDefault("repairParts", "")),
                AssignedSpecialistId = assignedSpecUserId,
                CustomerUserId = customerUserId,
                CustomerFullName = customerName,
                CustomerPhone = customerPhone
            };

            _db.RepairRequests.Add(req);
        }

        await _db.SaveChangesAsync();

        foreach (var row in importComments)
        {
            var requestId = int.Parse(row["requestID"], CultureInfo.InvariantCulture);
            var masterExternalId = int.Parse(row["masterID"], CultureInfo.InvariantCulture);

            if (!usersByExternalId.TryGetValue(masterExternalId, out var author))
                continue;

            var msg = row.GetValueOrDefault("message", "");
            if (string.IsNullOrWhiteSpace(msg))
                continue;

            _db.RequestComments.Add(new RequestComment
            {
                RepairRequestId = requestId,
                AuthorUserId = author.Id,
                Message = msg,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var role in new[] { RoleManagerName, RoleOperator, RoleSpecialist, RoleCustomer, RoleQualityManager })
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static RequestStatus MapStatus(string statusText)
    {
        return statusText.Trim() switch
        {
            "Новая заявка" => RequestStatus.New,
            "В процессе ремонта" => RequestStatus.InProgress,
            "Ожидание комплектующих" => RequestStatus.WaitingParts,
            "Готова к выдаче" => RequestStatus.Completed,
            "Завершена" => RequestStatus.Completed,
            _ => RequestStatus.New
        };
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static List<Dictionary<string, string>> ReadCsv(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new();

        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
            return new();

        var header = lines[0].Split(';').Select(h => h.Trim()).ToArray();
        var result = new List<Dictionary<string, string>>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(';');
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length && i < parts.Length; i++)
                row[header[i]] = parts[i].Trim();
            result.Add(row);
        }

        return result;
    }

    private static string? FindFirst(string root, string fileName)
    {
        try
        {
            if (!Directory.Exists(root))
                return null;

            return Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private async Task EnsureFallbackUsersAsync()
    {

        var users = new (string login, string pass, string role, string name)[]
        {
            ("login1", "pass1", RoleManagerName, "Менеджер (тест)"),
            ("login4", "pass4", RoleOperator, "Оператор (тест)"),
            ("login2", "pass2", RoleSpecialist, "Специалист (тест)"),
            ("login3", "pass3", RoleCustomer, "Заказчик (тест)"),
            ("login5", "pass5", RoleQualityManager, "Менеджер по качеству (тест)"),
        };

        foreach (var u in users)
        {
            var existing = await _userManager.FindByNameAsync(u.login);
            if (existing != null)
                continue;

            var user = new ApplicationUser
            {
                UserName = u.login,
                FullName = u.name,
                ExternalUserId = null
            };

            var create = await _userManager.CreateAsync(user, u.pass);
            if (!create.Succeeded)
                throw new InvalidOperationException("Ошибка создания fallback-пользователя: " + string.Join("; ", create.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, u.role);
        }
    }
}
