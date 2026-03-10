using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;
using System.IO;
using EmployeeManagementApp.Data;
using EmployeeManagementApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementApp.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _db;

        public EmployeesController(AppDbContext db)
        {
            _db = db;
        }


        public async Task<IActionResult> Index(
            string? fullName,
            string? position,
            string? department,
            string? phone,
            string? email,
            string? passport,
            string? address,
            DateTime? hireDateFrom,
            DateTime? hireDateTo,
            DateTime? dismissDateFrom,
            DateTime? dismissDateTo
        )
        {
            var employees = _db.Employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(fullName))
                employees = employees.Where(e => EF.Functions.Like(e.FullName, $"%{fullName.Trim()}%"));

            if (!string.IsNullOrWhiteSpace(position))
                employees = employees.Where(e => EF.Functions.Like(e.Position, $"%{position.Trim()}%"));

            if (!string.IsNullOrWhiteSpace(department))
                employees = employees.Where(e => EF.Functions.Like(e.Department, $"%{department.Trim()}%"));

            if (!string.IsNullOrWhiteSpace(phone))
                employees = employees.Where(e => EF.Functions.Like(e.Phone, $"%{phone.Trim()}%"));

            if (!string.IsNullOrWhiteSpace(email))
                employees = employees.Where(e => EF.Functions.Like(e.Email, $"%{email.Trim()}%"));

            if (!string.IsNullOrWhiteSpace(passport))
            {
                var p = $"%{passport.Trim()}%";
                employees = employees.Where(e =>
                    EF.Functions.Like(e.PassportSeries, p) ||
                    EF.Functions.Like(e.PassportNumber, p) ||
                    EF.Functions.Like(e.PassportIssuedBy, p));
            }

            if (!string.IsNullOrWhiteSpace(address))
                employees = employees.Where(e => EF.Functions.Like(e.Address, $"%{address.Trim()}%"));

            if (hireDateFrom.HasValue)
                employees = employees.Where(e => e.HireDate >= hireDateFrom);

            if (hireDateTo.HasValue)
                employees = employees.Where(e => e.HireDate <= hireDateTo);

            if (dismissDateFrom.HasValue)
                employees = employees.Where(e => e.DismissalDate >= dismissDateFrom);

            if (dismissDateTo.HasValue)
                employees = employees.Where(e => e.DismissalDate <= dismissDateTo);

            return View(await employees.OrderBy(e => e.FullName).ToListAsync());
        }


        [HttpGet]
        public async Task<IActionResult> ExportToExcel()
        {
            var employees = await _db.Employees.OrderBy(e => e.FullName).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Сотрудники");

            string[] headers =
            {
                "ID","ФИО","Должность","Отдел","Дата приёма","Дата увольнения","Оклад",
                "Телефон","Email","Дата рождения","Серия паспорта","Номер паспорта",
                "Кем выдан","Дата выдачи","Адрес","Образование","Опыт работы"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var row = 1;
            foreach (var e in employees)
            {
                row++;
                ws.Cell(row, 1).Value = e.Id;
                ws.Cell(row, 2).Value = e.FullName;
                ws.Cell(row, 3).Value = e.Position;
                ws.Cell(row, 4).Value = e.Department;
                ws.Cell(row, 5).Value = e.HireDate;
                ws.Cell(row, 6).Value = e.DismissalDate;
                ws.Cell(row, 7).Value = e.Salary;
                ws.Cell(row, 8).Value = e.Phone;
                ws.Cell(row, 9).Value = e.Email;
                ws.Cell(row, 10).Value = e.BirthDate;
                ws.Cell(row, 11).Value = e.PassportSeries;
                ws.Cell(row, 12).Value = e.PassportNumber;
                ws.Cell(row, 13).Value = e.PassportIssuedBy;
                ws.Cell(row, 14).Value = e.PassportIssueDate;
                ws.Cell(row, 15).Value = e.Address;
                ws.Cell(row, 16).Value = e.Education;
                ws.Cell(row, 17).Value = e.Experience;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"employees_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _db.Employees.FindAsync(id);
            return employee == null ? NotFound() : View(employee);
        }

   
        public IActionResult Create()
            => View(new Employee { HireDate = DateTime.Today });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee, IFormFile? Photo)
        {
            if (!ModelState.IsValid) return View(employee);

            await SavePhoto(employee, Photo);

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

      
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _db.Employees.FindAsync(id);
            return employee == null ? NotFound() : View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee, IFormFile? Photo)
        {
            if (id != employee.Id) return NotFound();
            if (!ModelState.IsValid) return View(employee);

            await SavePhoto(employee, Photo);

            _db.Update(employee);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _db.Employees.FindAsync(id);
            return employee == null ? NotFound() : View(employee);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee != null)
            {
                _db.Employees.Remove(employee);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

       
        private async Task SavePhoto(Employee employee, IFormFile? photo)
        {
            if (photo == null || photo.Length == 0) return;

            if (!photo.ContentType.StartsWith("image/"))
                throw new Exception("Можно загружать только изображения");

            var folder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/uploads/employees"
            );

            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await photo.CopyToAsync(stream);

            employee.PhotoPath = $"/uploads/employees/{fileName}";
        }
    }
}
