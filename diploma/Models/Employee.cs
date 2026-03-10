using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementApp.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required, Display(Name = "ФИО сотрудника")]
        public string FullName { get; set; } = string.Empty;

        [Required, Display(Name = "Должность")]
        public string Position { get; set; } = string.Empty;

        [Display(Name = "Отдел")]
        public string? Department { get; set; }

        [Display(Name = "Дата приема на работу"), DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [Display(Name = "Дата увольнения"), DataType(DataType.Date)]
        public DateTime? DismissalDate { get; set; }

        [Display(Name = "Оклад"), DataType(DataType.Currency)]
        public decimal Salary { get; set; }

        [Display(Name = "Дата рождения"), DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Телефон"), Phone]
        public string? Phone { get; set; }

        [Display(Name = "Email"), EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "Серия паспорта")]
        public string? PassportSeries { get; set; }

        [Display(Name = "Номер паспорта")]
        public string? PassportNumber { get; set; }

        [Display(Name = "Кем выдан паспорт")]
        public string? PassportIssuedBy { get; set; }

        [Display(Name = "Дата выдачи паспорта"), DataType(DataType.Date)]
        public DateTime? PassportIssueDate { get; set; }

        [Display(Name = "Адрес проживания")]
        public string? Address { get; set; }

        [Display(Name = "Образование")]
        public string? Education { get; set; }

        [Display(Name = "Опыт работы")]
        public string? Experience { get; set; }

        public string? PhotoPath { get; set; }

    }
}
