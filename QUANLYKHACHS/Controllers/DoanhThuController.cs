using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHS.Data;

namespace QUANLYKHACHS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DoanhThuController : ControllerBase
{
    private readonly HotelContext _context;

    public DoanhThuController(HotelContext context)
    {
        _context = context;
    }

    [HttpGet("tong-quan")]
    public async Task<IActionResult> TongQuan([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.Now.AddMonths(-1);
        var toDate = to ?? DateTime.Now;

        var bookings = await _context.Bookings
            .Where(b => b.StatusRoom == "completed"
                     && b.Checkout >= fromDate
                     && b.Checkout <= toDate)
            .Include(b => b.SeviceOrders).ThenInclude(so => so.Service)
            .ToListAsync();

        decimal tongDoanhThuPhong = bookings.Sum(b => b.TotalRoomRice ?? 0);

        decimal tongDoanhThuDichVu = bookings
            .SelectMany(b => b.SeviceOrders)
            .Sum(so => (so.PriceAtOrder ?? 0) * (so.Quantity ?? 0));

        decimal tongDeposit = await _context.DepositTransactions
            .Where(d => d.Status == "Approved"
                     && d.ApprovedAt >= fromDate
                     && d.ApprovedAt <= toDate)
            .SumAsync(d => d.Amount);

        return Ok(new
        {
            tuNgay = fromDate,
            denNgay = toDate,
            soBookingHoanThanh = bookings.Count,
            tongDoanhThuPhong,
            tongDoanhThuDichVu,
            tongDoanhThu = tongDoanhThuPhong + tongDoanhThuDichVu,
            tongNapTien = tongDeposit
        });
    }

    [HttpGet("theo-ngay")]
    public async Task<IActionResult> TheoNgay([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.Now.AddDays(-30);
        var toDate = to ?? DateTime.Now;

        var data = await _context.Bookings
            .Where(b => b.StatusRoom == "completed"
                     && b.Checkout >= fromDate
                     && b.Checkout <= toDate)
            .GroupBy(b => b.Checkout!.Value.Date)
            .Select(g => new
            {
                ngay = g.Key,
                soBooking = g.Count(),
                doanhThu = g.Sum(b => b.TotalRoomRice ?? 0)
            })
            .OrderBy(x => x.ngay)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("theo-thang")]
    public async Task<IActionResult> TheoThang([FromQuery] int? year)
    {
        int nam = year ?? DateTime.Now.Year;

        var data = await _context.Bookings
            .Where(b => b.StatusRoom == "completed"
                     && b.Checkout!.Value.Year == nam)
            .GroupBy(b => b.Checkout!.Value.Month)
            .Select(g => new
            {
                thang = g.Key,
                soBooking = g.Count(),
                doanhThu = g.Sum(b => b.TotalRoomRice ?? 0)
            })
            .OrderBy(x => x.thang)
            .ToListAsync();

        var result = Enumerable.Range(1, 12).Select(m => new
        {
            thang = m,
            soBooking = data.FirstOrDefault(d => d.thang == m)?.soBooking ?? 0,
            doanhThu = data.FirstOrDefault(d => d.thang == m)?.doanhThu ?? 0
        });

        return Ok(new { nam, data = result });
    }

    [HttpGet("theo-loai-phong")]
    public async Task<IActionResult> TheoLoaiPhong([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.Now.AddMonths(-1);
        var toDate = to ?? DateTime.Now;

        var data = await _context.Bookings
            .Where(b => b.StatusRoom == "completed"
                     && b.Checkout >= fromDate
                     && b.Checkout <= toDate)
            .Include(b => b.Room).ThenInclude(r => r.LoaiPhongNavigation)
            .GroupBy(b => b.Room.LoaiPhongNavigation.TypeName)
            .Select(g => new
            {
                loaiPhong = g.Key,
                soBooking = g.Count(),
                doanhThu = g.Sum(b => b.TotalRoomRice ?? 0)
            })
            .OrderByDescending(x => x.doanhThu)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("dich-vu")]
    public async Task<IActionResult> DoanhThuDichVu([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.Now.AddMonths(-1);
        var toDate = to ?? DateTime.Now;

        var data = await _context.SeviceOrders
            .Include(so => so.Service)
            .Include(so => so.Booking)
            .Where(so => so.Booking.StatusRoom == "completed"
                      && so.Booking.Checkout >= fromDate
                      && so.Booking.Checkout <= toDate)
            .GroupBy(so => so.Service.Servicename)
            .Select(g => new
            {
                tenDichVu = g.Key,
                soLuong = g.Sum(so => so.Quantity ?? 0),
                doanhThu = g.Sum(so => (so.PriceAtOrder ?? 0) * (so.Quantity ?? 0))
            })
            .OrderByDescending(x => x.doanhThu)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("top-khach-hang")]
    public async Task<IActionResult> TopKhachHang([FromQuery] int top = 10)
    {
        var data = await _context.Bookings
            .Where(b => b.StatusRoom == "completed")
            .Include(b => b.Customer)
            .GroupBy(b => new { b.CustomerId, b.Customer.Fullname })
            .Select(g => new
            {
                customerId = g.Key.CustomerId,
                tenKhach = g.Key.Fullname,
                soLanDat = g.Count(),
                tongChiTieu = g.Sum(b => b.TotalRoomRice ?? 0)
            })
            .OrderByDescending(x => x.tongChiTieu)
            .Take(top)
            .ToListAsync();

        return Ok(data);
    }
}