using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class CustomerController : BaseController
    {
        private readonly HotelManagementContext _context;

        public CustomerController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            var query = _context.Customers
                .Where(c => c.IsActivate == "ACTIVATE")
                .OrderBy(c => c.FullName);
            
            var customers = await PagedList<Customer>.CreateAsync(query, page, pageSize);
            
            ViewBag.PageSize = pageSize;
            return View(customers);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerID == id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        public IActionResult Create()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerID,FullName,PhoneNumber,Email,Address,Gender,IdCardNumber,Dob")] Customer customer)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                if (ModelState.IsValid)
                {
                    if (CustomerExists(customer.PhoneNumber) ||CustomerExists(customer.IdCardNumber))
                    {
                        TempData["Error"] = "Khách hàng với Số điện thoại hoặc CMND/CCCD đã tồn tại!";
                        return View(customer);
                    }
                    customer.CustomerID = await _context.GenerateID("CUS-", "Customer");
                    customer.IsActivate = "ACTIVATE";

                    _context.Add(customer);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Thêm khách hàng thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    TempData["Error"] = "Lỗi cơ sở dữ liệu: " + sqlEx.Message;
                }
                else
                {
                    TempData["Error"] = "Lỗi cơ sở dữ liệu: " + dbEx.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi thêm khách hàng: " + ex.Message;
            }
            return View(customer);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("CustomerID,FullName,PhoneNumber,Email,Address,Gender,IdCardNumber,Dob,IsActivate")] Customer customer)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id != customer.CustomerID) return NotFound();

            if (ModelState.IsValid)
            {   


                try
                {
                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật khách hàng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerID))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerID == id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                customer.IsActivate = "DEACTIVATE";
                _context.Update(customer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa khách hàng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Quick Create for AJAX
        [HttpPost]
        public async Task<IActionResult> QuickCreate([FromForm] Customer customer)
        {
            if (!CheckAuth()) 
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                if (CustomerExists(customer.PhoneNumber) ||CustomerExists(customer.IdCardNumber))
                {
                    return Json(new { success = false, message = "Khách hàng với Số điện thoại hoặc CMND/CCCD đã tồn tại!" });
                }
                // Sử dụng fn_GenerateID từ database
                customer.CustomerID = await _context.GenerateID("CUS-", "Customer");
                customer.IsActivate = "ACTIVATE";

                _context.Add(customer);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    customerId = customer.CustomerID,
                    customerName = customer.FullName
                });
            }
            catch (DbUpdateException dbEx)
            {
                // Kiểm tra lỗi SQL chi tiết
                if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    // Trả về thông báo lỗi chi tiết
                    return Json(new { success = false, message = sqlEx.Message });
                }

                // Trả về lỗi chung nếu không phải lỗi SQL
                return Json(new { success = false, message = "Lỗi cơ sở dữ liệu: " + dbEx.Message });
            }
            catch (Exception ex)
            {
                // Trả về lỗi chung
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool CustomerExists(string any)
        {
            return _context.Customers.Any(
                e => e.CustomerID == any || e.Email == any || e.PhoneNumber == any || e.IdCardNumber == any
            );
        }
    }
}
