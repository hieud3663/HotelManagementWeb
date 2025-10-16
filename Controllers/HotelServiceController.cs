using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class HotelServiceController : Controller
    {
        private readonly HotelManagementContext _context;

        public HotelServiceController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        // GET: HotelService
        public async Task<IActionResult> Index(string? searchString, string? categoryFilter)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var services = from s in _context.HotelServices
                          .Include(s => s.ServiceCategory)
                          select s;

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                services = services.Where(s => s.ServiceName.Contains(searchString) 
                                            || s.Description!.Contains(searchString));
            }

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                services = services.Where(s => s.ServiceCategoryID == categoryFilter);
            }

            // Lấy danh sách categories cho dropdown
            ViewBag.ServiceCategories = await _context.ServiceCategories
                .Where(sc => sc.IsActivate == "ACTIVATE")
                .ToListAsync();
            
            ViewBag.SearchString = searchString;
            ViewBag.CategoryFilter = categoryFilter;

            return View(await services.OrderBy(s => s.ServiceCategoryID)
                                     .ThenBy(s => s.ServiceName)
                                     .ToListAsync());
        }

        // GET: HotelService/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return NotFound();
            }

            var hotelService = await _context.HotelServices
                .Include(h => h.ServiceCategory)
                .FirstOrDefaultAsync(m => m.HotelServiceId == id);
            
            if (hotelService == null)
            {
                return NotFound();
            }

            return View(hotelService);
        }

        // GET: HotelService/Create
        public async Task<IActionResult> Create()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            ViewData["ServiceCategoryID"] = new SelectList(
                await _context.ServiceCategories.Where(sc => sc.IsActivate == "ACTIVATE").ToListAsync(), 
                "ServiceCategoryID", "ServiceCategoryName");
            
            return View();
        }

        // POST: HotelService/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceName,Description,ServicePrice,ServiceCategoryID")] HotelService hotelService)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                // Tạo ID mới
                hotelService.HotelServiceId = await _context.GenerateID("HS-", "HotelService");
                hotelService.IsActivate = "ACTIVATE";
                
                _context.Add(hotelService);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $" Thêm dịch vụ '{hotelService.ServiceName}' thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["ServiceCategoryID"] = new SelectList(
                await _context.ServiceCategories.Where(sc => sc.IsActivate == "ACTIVATE").ToListAsync(), 
                "ServiceCategoryID", "ServiceCategoryName", hotelService.ServiceCategoryID);
            
            return View(hotelService);
        }

        // GET: HotelService/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return NotFound();
            }

            var hotelService = await _context.HotelServices.FindAsync(id);
            if (hotelService == null)
            {
                return NotFound();
            }
            
            ViewData["ServiceCategoryID"] = new SelectList(
                await _context.ServiceCategories.Where(sc => sc.IsActivate == "ACTIVATE").ToListAsync(), 
                "ServiceCategoryID", "ServiceCategoryName", hotelService.ServiceCategoryID);
            
            return View(hotelService);
        }

        // POST: HotelService/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("HotelServiceId,ServiceName,Description,ServicePrice,ServiceCategoryID,IsActivate")] HotelService hotelService)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (id != hotelService.HotelServiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hotelService);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $" Cập nhật dịch vụ '{hotelService.ServiceName}' thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HotelServiceExists(hotelService.HotelServiceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["ServiceCategoryID"] = new SelectList(
                await _context.ServiceCategories.Where(sc => sc.IsActivate == "ACTIVATE").ToListAsync(), 
                "ServiceCategoryID", "ServiceCategoryName", hotelService.ServiceCategoryID);
            
            return View(hotelService);
        }

        // GET: HotelService/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return NotFound();
            }

            var hotelService = await _context.HotelServices
                .Include(h => h.ServiceCategory)
                .FirstOrDefaultAsync(m => m.HotelServiceId == id);
            
            if (hotelService == null)
            {
                return NotFound();
            }

            return View(hotelService);
        }

        // POST: HotelService/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var hotelService = await _context.HotelServices.FindAsync(id);
            if (hotelService != null)
            {
                // Kiểm tra xem dịch vụ có đang được sử dụng không
                var isUsed = await _context.RoomUsageServices
                    .AnyAsync(rus => rus.HotelServiceId == id);

                if (isUsed)
                {
                    TempData["Error"] = "⚠️ Không thể xóa dịch vụ đang được sử dụng! Hãy vô hiệu hóa thay thế.";
                    return RedirectToAction(nameof(Index));
                }

                _context.HotelServices.Remove(hotelService);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $" Đã xóa dịch vụ '{hotelService.ServiceName}'!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: HotelService/ToggleActivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivate(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var hotelService = await _context.HotelServices.FindAsync(id);
            if (hotelService != null)
            {
                hotelService.IsActivate = hotelService.IsActivate == "ACTIVATE" ? "DEACTIVATE" : "ACTIVATE";
                _context.Update(hotelService);
                await _context.SaveChangesAsync();

                var status = hotelService.IsActivate == "ACTIVATE" ? "kích hoạt" : "vô hiệu hóa";
                TempData["Success"] = $" Đã {status} dịch vụ '{hotelService.ServiceName}'!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: HotelService/CreateCategory (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CreateCategory(string categoryName)
        {
            if (!CheckAuth())
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(new { success = false, message = "Tên danh mục không được để trống" });
            }

            // Kiểm tra trùng tên
            if (await _context.ServiceCategories.AnyAsync(sc => sc.ServiceCategoryName == categoryName))
            {
                return Json(new { success = false, message = "Tên danh mục đã tồn tại" });
            }

            try
            {
                var serviceCategory = new ServiceCategory
                {
                    ServiceCategoryID = await _context.GenerateID("SC-", "ServiceCategory"),
                    ServiceCategoryName = categoryName,
                    IsActivate = "ACTIVATE"
                };

                _context.ServiceCategories.Add(serviceCategory);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    categoryId = serviceCategory.ServiceCategoryID,
                    categoryName = serviceCategory.ServiceCategoryName
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool HotelServiceExists(string id)
        {
            return _context.HotelServices.Any(e => e.HotelServiceId == id);
        }
    }
}
