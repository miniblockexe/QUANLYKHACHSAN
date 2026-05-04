using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHS.Data;

namespace QUANLYKHACHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly HotelContext _context;

        public DashboardController(HotelContext context)
        {
            _context = context;
        }

        [HttpGet("gialap")]
        public IActionResult GetStats()
        {
            // GIẢ LẬP DỮ LIỆU
            var data = new
            {
                totalCustomers = 3782,
                totalBookings = 5359,
                pieData = new[] { 50, 25, 15, 10 },
                barLabels = new[] { "00", "01", "02", "03", "04", "05" },
                barData = new[] { 30, 45, 35, 50, 70, 90 }
            };

            return Ok(data);
        }
        [HttpGet("dulieuthat")]
        public async Task<IActionResult> Dulieu()
        {
            try
            {
                var totalCustomers = await _context.Customers.CountAsync();
                var totalBookings = await _context.Bookings.CountAsync();

                var recentCustomers = await _context.Customers
                    .OrderByDescending(c => c.Customerid)
                    .Take(5)
                    .Select(c => new
                    {
                        id = c.Customerid,
                        name = c.Fullname,
                        cccd = c.Cccd,
                        phone = c.Sdt
                    })
                    .ToListAsync();

                var startDate = DateTime.Today.AddDays(-6);
                var endDate = DateTime.Today.AddDays(1);

                var checkinData = await _context.Bookings
                    .Where(b => b.Checkin >= startDate && b.Checkin < endDate)
                    .GroupBy(b => b.Checkin.Value.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var barLabels = new List<string>();
                var barData = new List<int>();

                for (int i = 0; i < 7; i++)
                {
                    var date = startDate.AddDays(i);
                    barLabels.Add(date.ToString("dd/MM")); 

                    var match = checkinData.FirstOrDefault(x => x.Date == date);
                    barData.Add(match != null ? match.Count : 0);
                }

                return Ok(new
                {
                    totalCustomers = totalCustomers,
                    totalBookings = totalBookings,
                    pieData = new[] {
                        await _context.Rooms.CountAsync(r => r.TrangThai == "Available"),
                        await _context.Rooms.CountAsync(r => r.TrangThai == "Occupied"),
                        await _context.Rooms.CountAsync(r => r.TrangThai == "Cleaning"),
                        await _context.Rooms.CountAsync(r => r.TrangThai == "Reserved")
                            },
                    recentCustomers = recentCustomers,
                    barLabels = barLabels,
                    barData = barData
                });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}