using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    public class BaseController : Controller
    {
        protected bool IsManager()
        {
            return HttpContext.Session.GetString("Role") == "MANAGER";
        }

        protected bool IsEmployee()
        {
            return HttpContext.Session.GetString("Role") == "EMPLOYEE";
        }
    }
}