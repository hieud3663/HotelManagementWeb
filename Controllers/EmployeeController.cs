using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly HotelManagementContext _context;

        public EmployeeController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        // GET: Employee
        public async Task<IActionResult> Index()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            var employees = await _context.Employees
                .Where(e => e.IsActivate == "ACTIVATE")
                .ToListAsync();
            return View(employees);
        }

        // GET: Employee/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.EmployeeID == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // GET: Employee/Create
        public IActionResult Create()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeID,FullName,PhoneNumber,Email,Address,Gender,IdCardNumber,Dob,Position")] Employee employee)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                employee.EmployeeID = await _context.GenerateID("EMP-", "Employee");
                employee.IsActivate = "ACTIVATE";

                try
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_InsertEmployee @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8",
                        employee.EmployeeID,
                        employee.FullName,
                        employee.PhoneNumber,
                        employee.Email,
                        employee.Address,
                        employee.Gender,
                        employee.IdCardNumber,
                        employee.Dob,
                        employee.Position
                    );
                    TempData["Success"] = "Thêm nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(employee);
        }

        // GET: Employee/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        // POST: Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("EmployeeID,FullName,PhoneNumber,Email,Address,Gender,IdCardNumber,Dob,Position,IsActivate")] Employee employee)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id != employee.EmployeeID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_UpdateEmployee @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9",
                        employee.EmployeeID,
                        employee.FullName,
                        employee.PhoneNumber,
                        employee.Email,
                        employee.Address,
                        employee.Gender,
                        employee.IdCardNumber,
                        employee.Dob,
                        employee.Position,
                        employee.IsActivate
                    );
                    TempData["Success"] = "Cập nhật nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(employee);
        }

        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.EmployeeID == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsActivate = "DEACTIVATE";
                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa nhân viên thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(string id)
        {
            return _context.Employees.Any(e => e.EmployeeID == id);
        }
    }
}
