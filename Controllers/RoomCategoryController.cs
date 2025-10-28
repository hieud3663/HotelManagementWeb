using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Controllers
{
    public class RoomCategoryController : BaseController
    {
        private readonly HotelManagementContext _context;

        public RoomCategoryController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        // GET: RoomCategory
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var query = _context.RoomCategories
                .Include(rc => rc.Pricings)
                .Include(rc => rc.Rooms)
                .OrderBy(rc => rc.RoomCategoryName);

            var categories = await PagedList<RoomCategory>.CreateAsync(query, page, pageSize);
            
            ViewBag.PageSize = pageSize;
            return View(categories);
        }

        // GET: RoomCategory/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return NotFound();
            }

            var roomCategory = await _context.RoomCategories
                .Include(rc => rc.Pricings)
                .Include(rc => rc.Rooms)
                .FirstOrDefaultAsync(m => m.RoomCategoryID == id);

            if (roomCategory == null)
            {
                return NotFound();
            }

            return View(roomCategory);
        }

        // GET: RoomCategory/Create
        public IActionResult Create()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            return View();
        }

        // POST: RoomCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("RoomCategoryName,NumberOfBed")] RoomCategory roomCategory,
            decimal? priceHour,
            decimal? priceDay)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                try
                {
                    var newPricingHourID = "";
                    var newPricingDayID = "";
                    // Tạo ID cho RoomCategory
                    roomCategory.RoomCategoryID = await _context.GenerateID("RC-", "RoomCategory");
                    roomCategory.IsActivate = "ACTIVATE";

                    _context.Add(roomCategory);
                    await _context.SaveChangesAsync();

                    // Thêm Pricing cho HOUR (nếu có)
                    if (priceHour.HasValue && priceHour.Value > 0)
                    {
                        newPricingHourID = await _context.GenerateID("P-", "Pricing");
                        var pricingHour = new Pricing
                        {
                            PricingID = newPricingHourID,
                            PriceUnit = "HOUR",
                            Price = priceHour.Value,
                            RoomCategoryID = roomCategory.RoomCategoryID
                        };
                        _context.Pricings.Add(pricingHour);
                    }

                    // Thêm Pricing cho DAY (nếu có)
                    if (priceDay.HasValue && priceDay.Value > 0)
                    {
                        int num = int.Parse(newPricingHourID.Substring("P-".Length));
                        newPricingDayID = "P-" + (num + 1).ToString("D6");
                        var pricingDay = new Pricing
                        {
                            PricingID = newPricingDayID,
                            PriceUnit = "DAY",
                            Price = priceDay.Value,
                            RoomCategoryID = roomCategory.RoomCategoryID
                        };
                        _context.Pricings.Add(pricingDay);
                    }

                    await _context.SaveChangesAsync();

                    TempData["Success"] = $" Thêm loại phòng '{roomCategory.RoomCategoryName}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"❌ Lỗi: {ex.Message}";
                }
            }

            return View(roomCategory);
        }

        // GET: RoomCategory/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return NotFound();
            }

            var roomCategory = await _context.RoomCategories
                .Include(rc => rc.Pricings)
                .FirstOrDefaultAsync(rc => rc.RoomCategoryID == id);

            if (roomCategory == null)
            {
                return NotFound();
            }

            // Truyền giá hiện tại vào ViewBag
            ViewBag.PriceHour = roomCategory.Pricings?.FirstOrDefault(p => p.PriceUnit == "HOUR")?.Price;
            ViewBag.PriceDay = roomCategory.Pricings?.FirstOrDefault(p => p.PriceUnit == "DAY")?.Price;

            return View(roomCategory);
        }

        // POST: RoomCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("RoomCategoryID,RoomCategoryName,NumberOfBed,IsActivate")] RoomCategory roomCategory,
            decimal? priceHour,
            decimal? priceDay)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (id != roomCategory.RoomCategoryID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Load RoomCategory VỚI TRACKING (không dùng AsNoTracking)
                    var existingCategory = await _context.RoomCategories
                        .Include(rc => rc.Pricings)
                        .FirstOrDefaultAsync(rc => rc.RoomCategoryID == id);


                    if (existingCategory == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật thông tin RoomCategory
                    existingCategory.RoomCategoryName = roomCategory.RoomCategoryName;
                    existingCategory.NumberOfBed = roomCategory.NumberOfBed;
                    existingCategory.IsActivate = roomCategory.IsActivate;
                    
                    string newPricingHourID = null;
                    string newPricingDayID = null;

                    // Xử lý Pricing HOUR
                    var existingPricingHour = existingCategory.Pricings?
                        .FirstOrDefault(p => p.PriceUnit == "HOUR");

                    if (priceHour.HasValue && priceHour.Value > 0)
                    {
                        if (existingPricingHour != null)
                        {
                            // Đã có → Update
                            existingPricingHour.Price = priceHour.Value;
                        }
                        else
                        {
                            newPricingHourID = await _context.GenerateID("P-", "Pricing");
                            // Chưa có → Add mới
                            var newPricingHour = new Pricing
                            {
                                PricingID = newPricingHourID,
                                PriceUnit = "HOUR",
                                Price = priceHour.Value,
                                RoomCategoryID = id
                            };
                            _context.Pricings.Add(newPricingHour);
                        }
                    }

                    // Xử lý Pricing DAY
                    var existingPricingDay = existingCategory.Pricings?
                        .FirstOrDefault(p => p.PriceUnit == "DAY");

                    if (priceDay.HasValue && priceDay.Value > 0)
                    {
                        if (existingPricingDay != null)
                        {
                            // Đã có → Update
                            existingPricingDay.Price = priceDay.Value;
                        }
                        else
                        {
                            int num = int.Parse(newPricingHourID.Substring("P-".Length));
                            newPricingDayID = "P-" + (num + 1).ToString("D6");

                            // Chưa có → Add mới
                            var newPricingDay = new Pricing
                            {
                                PricingID = newPricingDayID,
                                PriceUnit = "DAY",
                                Price = priceDay.Value,
                                RoomCategoryID = id
                            };
                            _context.Pricings.Add(newPricingDay);
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"✅ Cập nhật loại phòng '{roomCategory.RoomCategoryName}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomCategoryExists(roomCategory.RoomCategoryID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"❌ Lỗi: {ex.Message}";
                }
            }

            // Nếu ModelState không valid, load lại giá để hiển thị
            var categoryWithPrices = await _context.RoomCategories
                .Include(rc => rc.Pricings)
                .FirstOrDefaultAsync(rc => rc.RoomCategoryID == id);

            ViewBag.PriceHour = categoryWithPrices?.Pricings?.FirstOrDefault(p => p.PriceUnit == "HOUR")?.Price;
            ViewBag.PriceDay = categoryWithPrices?.Pricings?.FirstOrDefault(p => p.PriceUnit == "DAY")?.Price;

            return View(roomCategory);
        }

        // GET: RoomCategory/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return NotFound();
            }

            var roomCategory = await _context.RoomCategories
                .Include(rc => rc.Pricings)
                .Include(rc => rc.Rooms)
                .FirstOrDefaultAsync(m => m.RoomCategoryID == id);

            if (roomCategory == null)
            {
                return NotFound();
            }

            return View(roomCategory);
        }

        // POST: RoomCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var roomCategory = await _context.RoomCategories
                .Include(rc => rc.Rooms)
                .Include(rc => rc.Pricings)
                .FirstOrDefaultAsync(rc => rc.RoomCategoryID == id);

            if (roomCategory != null)
            {
                // Kiểm tra xem có phòng nào đang sử dụng loại phòng này không
                if (roomCategory.Rooms != null && roomCategory.Rooms.Any())
                {
                    TempData["Error"] = $"⚠️ Không thể xóa loại phòng đang có {roomCategory.Rooms.Count} phòng!";
                    return RedirectToAction(nameof(Index));
                }

                // Xóa các pricing trước
                if (roomCategory.Pricings != null && roomCategory.Pricings.Any())
                {
                    _context.Pricings.RemoveRange(roomCategory.Pricings);
                }

                _context.RoomCategories.Remove(roomCategory);
                await _context.SaveChangesAsync();

                TempData["Success"] = $" Đã xóa loại phòng '{roomCategory.RoomCategoryName}'!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: RoomCategory/ToggleActivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivate(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var roomCategory = await _context.RoomCategories.FindAsync(id);
            if (roomCategory != null)
            {
                roomCategory.IsActivate = roomCategory.IsActivate == "ACTIVATE" ? "DEACTIVATE" : "ACTIVATE";
                _context.Update(roomCategory);
                await _context.SaveChangesAsync();

                var status = roomCategory.IsActivate == "ACTIVATE" ? "kích hoạt" : "vô hiệu hóa";
                TempData["Success"] = $" Đã {status} loại phòng '{roomCategory.RoomCategoryName}'!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoomCategoryExists(string id)
        {
            return _context.RoomCategories.Any(e => e.RoomCategoryID == id);
        }
    }
}
