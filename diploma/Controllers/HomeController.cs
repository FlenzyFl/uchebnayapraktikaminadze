using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagementApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
