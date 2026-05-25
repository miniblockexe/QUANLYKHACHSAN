using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHSAN.Data;
using QUANLYKHACHSAN.Models;

namespace QUANLYKHACHS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BookingController : ControllerBase
{
    private readonly HotelContext _context;
    private readonly ILogger<BookingController> _logger;

    public BookingController(HotelContext context, ILogger<BookingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var bookings = await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.Customer)
            .Select(b => new {
                b.BookingId,
                b.Checkin,
                b.Checkout,
                b.IsHourly,
                b.StatusRoom,
                b.TotalRoomRice,
                CustomerName = b.Customer.Fullname,
                RoomNumber = b.Room.SoPhong
            })
            .ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Booking>> GetById(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.Customer)
            .Include(b => b.SeviceOrders).ThenInclude(so => so.Service)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null) return NotFound();
        return Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<Booking>> Create([FromBody] BookingCreateDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var room = await _context.Rooms.FindAsync(dto.RoomId);
            if (room == null) return BadRequest("Phòng không tồn tại");

            if (room.TrangThai != "Available")
                return BadRequest($"Phòng này hiện đang {room.TrangThai}, không thể đặt.");

            if (!await _context.Customers.AnyAsync(c => c.Customerid == dto.CustomerId))
                return BadRequest("Khách hàng không tồn tại");

            var booking = new Booking
            {
                RoomId = dto.RoomId,
                CustomerId = dto.CustomerId,
                Checkin = dto.Checkin ?? DateTime.Now,
                Checkout = dto.Checkout,
                IsHourly = dto.IsHourly,
                StatusRoom = "Reserved",
                TotalRoomRice = 0
            };

            room.TrangThai = "Reserved";

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync(); 

            return CreatedAtAction(nameof(GetById), new { id = booking.BookingId }, booking);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi tạo đặt phòng");
            return StatusCode(500, "Lỗi khi xử lý đặt phòng");
        }
    }

    [HttpPost("{id}/confirm-checkin")]
    public async Task<IActionResult> ConfirmActualCheckIn(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null) return NotFound("Không tìm thấy đơn đặt phòng.");

        if (booking.StatusRoom != "Reserved" && booking.StatusRoom != "PendingCash")
            return BadRequest("Đơn đặt phòng này không ở trạng thái chờ nhận phòng.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            booking.StatusRoom = "active";

            booking.Checkin = DateTime.Now;

            if (booking.Room != null)
            {
                booking.Room.TrangThai = "Occupied";
            }

            var otherPending = await _context.Bookings
            .Where(b => b.RoomId == booking.RoomId
                     && b.BookingId != booking.BookingId
                     && b.StatusRoom == "PendingCash")
            .ToListAsync();

            foreach (var other in otherPending)
                other.StatusRoom = "cancelled";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Khách đã nhận phòng thành công!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Lỗi khi xác nhận: " + ex.Message);
        }
    }

    [HttpPost("{id}/checkout")]
    public async Task<IActionResult> CheckOut(int id, [FromBody] CheckoutRequestDto? request = null)
    {
        var paymentMethod = request?.PaymentMethod ?? "cash";
        var booking = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r.LoaiPhongNavigation)
            .Include(b => b.Customer)
            .Include(b => b.SeviceOrders).ThenInclude(so => so.Service)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null) return NotFound("Không tìm thấy đơn đặt phòng.");
        if (booking.StatusRoom == "completed") return BadRequest("Đơn này đã thanh toán rồi.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            booking.Checkout = DateTime.Now;
            var timeSpan = booking.Checkout.Value - booking.Checkin.Value;

            decimal roomTotal = 0;
            double totalUnits = 0;

            if (booking.IsHourly)
            {
                totalUnits = Math.Ceiling(timeSpan.TotalHours);
                if (totalUnits < 1) totalUnits = 1; 

                decimal giaGio = booking.Room?.LoaiPhongNavigation?.GiaTheoGio ?? 0;
                roomTotal = (decimal)totalUnits * giaGio;
            }
            else
            {
                totalUnits = Math.Ceiling(timeSpan.TotalDays);
                if (totalUnits < 1) totalUnits = 1;

                decimal giaDem = booking.Room?.LoaiPhongNavigation?.GiaTheoDem ?? 0;
                roomTotal = (decimal)totalUnits * giaDem;
            }

            decimal servicesTotal = booking.SeviceOrders.Sum(so => (so.PriceAtOrder ?? 0) * (so.Quantity ?? 0));

            //booking.TotalRoomRice = roomTotal + servicesTotal;
            decimal grandTotal = roomTotal + servicesTotal;
            decimal depositPaid = booking.DepositAmount ?? 0;
            decimal remaining = grandTotal - depositPaid;

            if (paymentMethod == "balance")
            {
                if (!booking.CreatedByUserId.HasValue)
                    return BadRequest(new { message = "Không tìm thấy tài khoản khách hàng." });

                var user = await _context.Users.FindAsync(booking.CreatedByUserId.Value);
                if (user == null)
                    return BadRequest(new { message = "Không tìm thấy tài khoản." });
                if (user.Balance < remaining)
                    return BadRequest(new { message = $"Số dư không đủ! Cần {remaining:#,##0} ₫, hiện có {user.Balance:#,##0} ₫." });

                user.Balance -= remaining;
            }

            booking.TotalRoomRice = grandTotal;
            booking.StatusRoom = "completed";

            if (booking.Room != null)
            {
                booking.Room.TrangThai = "Cleaning";
            }
            
            /*
            if (remaining > 0 && booking.CreatedByUserId.HasValue)
            {
                var user = await _context.Users.FindAsync(booking.CreatedByUserId.Value);
                if (user != null)
                {
                    if (user.Balance < remaining)
                        return BadRequest(new { message = $"Số dư không đủ để thanh toán. Cần {remaining:#,##0} xu, hiện có {user.Balance:#,##0} xu." });

                    user.Balance -= remaining;
                }
            }
            */

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new CheckoutResponse
            {
                BookingId = booking.BookingId,
                RoomNumber = booking.Room?.SoPhong ?? "N/A",
                CustomerName = booking.Customer?.Fullname ?? "Khách lẻ",
                CheckIn = booking.Checkin.Value,
                CheckOut = booking.Checkout.Value,
                TotalUnits = totalUnits,
                UnitName = booking.IsHourly ? "Giờ" : "Ngày",
                RoomPrice = roomTotal,
                ServicesTotal = servicesTotal,
                DepositPaid = depositPaid,
                RemainingCharged = remaining,
                //GrandTotal = booking.TotalRoomRice ?? 0,
                GrandTotal = grandTotal,
                PaymentMethod = paymentMethod,
                Details = booking.SeviceOrders.Select(so => new ServiceDetailDto
                {
                    ServiceName = so.Service?.Servicename ?? "Dịch vụ",
                    Quantity = so.Quantity ?? 0,
                    Price = so.PriceAtOrder ?? 0
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Lỗi thanh toán: " + ex.Message);
        }
    }

    [HttpGet("{userId}/booking-history")]
    public async Task<IActionResult> GetBookingHistory(int userId)
    {
        var history = await _context.Bookings
            .Where(b => b.CreatedByUserId == userId) 
            .OrderByDescending(b => b.BookingId)
            .Select(b => new {
                b.BookingId,
                RoomNumber = b.Room.SoPhong,
                RoomType = b.Room.LoaiPhongNavigation.TypeName,
                b.Checkin,
                b.Checkout,
                b.StatusRoom,
                b.IsHourly,
                TotalPrice = b.TotalRoomRice
            })
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost("book-with-deposit")]
    [Authorize]
    public async Task<IActionResult> BookWithDeposit([FromBody] BookWithDepositDto dto)
    {
        var userIdStr = User.FindFirstValue("id");
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        var room = await _context.Rooms
            .Include(r => r.LoaiPhongNavigation)
            .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);
        if (room == null) return BadRequest(new { message = "Phòng không tồn tại" });
        if (room.TrangThai != "Available")
            return BadRequest(new { message = "Phòng không còn trống" });

        decimal priceBase = dto.IsHourly
            ? (room.LoaiPhongNavigation?.GiaTheoGio ?? 0)
            : (room.LoaiPhongNavigation?.GiaTheoDem ?? 0);
        decimal depositAmount = Math.Round(priceBase * 0.30m, 0);

        if (user.Balance < depositAmount)
            return BadRequest(new { message = $"Số dư không đủ để đặt cọc. Cần {depositAmount:#,##0} xu, hiện có {user.Balance:#,##0} xu." });

        if (dto.ExpectedCheckin <= DateTime.Now)
            return BadRequest(new { message = "Ngày hẹn phải là ngày trong tương lai" });

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Cccd == dto.Cccd);
            if (customer == null)
            {
                customer = new Customer { Fullname = dto.Fullname, Cccd = dto.Cccd, Sdt = dto.Sdt };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            user.Balance -= depositAmount;

            var booking = new Booking
            {
                RoomId = dto.RoomId,
                CustomerId = customer.Customerid,
                CreatedByUserId = userId,
                Checkin = dto.ExpectedCheckin,  
                IsHourly = dto.IsHourly,
                StatusRoom = "Reserved",
                DepositAmount = depositAmount,
                ExpectedCheckin = dto.ExpectedCheckin,
                ExpectedCheckout = dto.ExpectedCheckout,
                DepositRefunded = false,
                TotalRoomRice = 0
            };

            room.TrangThai = "Reserved";
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                message = $"Đặt phòng thành công! Đã cọc {depositAmount:#,##0} xu.",
                bookingId = booking.BookingId,
                depositAmount,
                expectedCheckin = dto.ExpectedCheckin,
                remainingBalance = user.Balance
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpDelete("cancel/{bookingId}")]
    public async Task<IActionResult> CancelByUser(int bookingId, [FromQuery] int userId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CreatedByUserId == userId);

        if (booking == null)
            return NotFound(new { message = "Không tìm thấy đặt phòng!" });

        if (booking.StatusRoom != "Reserved")
            return BadRequest(new { message = "Chỉ có thể hủy phòng đang ở trạng thái đặt trước!" });

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (booking.DepositAmount.HasValue && booking.DepositAmount > 0 && !booking.DepositRefunded)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.Balance += booking.DepositAmount.Value;
                    booking.DepositRefunded = true;
                }
            }

            booking.StatusRoom = "cancelled";
            if (booking.Room != null)
                booking.Room.TrangThai = "Available";

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                message = "Hủy đặt phòng thành công!" +
                          (booking.DepositAmount > 0 ? $" Đã hoàn {booking.DepositAmount:#,##0}đ vào tài khoản." : "")
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpPost("book-cash-reserve")]
    public async Task<IActionResult> BookCashReserve([FromBody] BookCashReserveDto dto)
    {
        var room = await _context.Rooms
            .Include(r => r.LoaiPhongNavigation)
            .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId);

        if (room == null) return BadRequest(new { message = "Phòng không tồn tại" });
        //if (room.TrangThai != "Available")
        //    return BadRequest(new { message = "Phòng không còn trống" });

        if (dto.ExpectedCheckin <= DateTime.Now)
            return BadRequest(new { message = "Ngày hẹn phải là ngày trong tương lai" });

        decimal priceBase = dto.IsHourly
        ? (room.LoaiPhongNavigation?.GiaTheoGio ?? 0)
        : (room.LoaiPhongNavigation?.GiaTheoDem ?? 0);
        decimal depositAmount = Math.Round(priceBase * 0.30m, 0);

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Cccd == dto.Cccd);
            if (customer == null)
            {
                customer = new Customer
                {
                    Fullname = dto.Fullname,
                    Cccd = dto.Cccd,
                    Sdt = dto.Sdt
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var booking = new Booking
            {
                RoomId = dto.RoomId,
                CustomerId = customer.Customerid,
                CreatedByUserId = dto.UserId > 0 ? dto.UserId : null,
                Checkin = dto.ExpectedCheckin,
                IsHourly = dto.IsHourly,
                StatusRoom = "PendingCash",  
                DepositAmount = depositAmount,
                ExpectedCheckin = dto.ExpectedCheckin,
                ExpectedCheckout = dto.ExpectedCheckout,
                DepositRefunded = false,
                TotalRoomRice = 0
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                message = "Đặt trước thành công!",
                bookingId = booking.BookingId,
                expectedCheckin = dto.ExpectedCheckin,
                depositAmount = depositAmount,
                status = "PendingCash"
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var booking = await _context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.BookingId == id);
        if (booking == null) return NotFound();

        if (booking.Room != null)
        {
            booking.Room.TrangThai = "Available";
        }

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("admin/schedule")]
    public async Task<IActionResult> GetSchedule()
    {
        var list = await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.Customer)
            .OrderByDescending(b => b.BookingId)
            .Select(b => new {
                b.BookingId,
                RoomNumber = b.Room.SoPhong,
                CustomerName = b.Customer.Fullname,
                b.Checkin,
                b.Checkout,
                b.ExpectedCheckin,
                b.ExpectedCheckout,
                b.IsHourly,
                b.StatusRoom,
                b.DepositAmount,
                b.TotalRoomRice
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost("{id}/confirm-cash-deposit")]
    public async Task<IActionResult> ConfirmCashDeposit(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .ThenInclude(r => r.LoaiPhongNavigation)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null) return NotFound("Không tìm thấy đơn đặt phòng.");
        if (booking.StatusRoom != "PendingCash")
            return BadRequest("Chỉ xác nhận được đơn đang chờ tiền mặt.");

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            decimal priceBase = booking.IsHourly
                ? (booking.Room?.LoaiPhongNavigation?.GiaTheoGio ?? 0)
                : (booking.Room?.LoaiPhongNavigation?.GiaTheoDem ?? 0);
            booking.DepositAmount = Math.Round(priceBase * 0.30m, 0);

            booking.StatusRoom = "Reserved";
            booking.Room!.TrangThai = "Reserved";

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { message = "Xác nhận đóng cọc thành công!" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, "Lỗi: " + ex.Message);
        }
    }

    public class BookingCreateDto
    {
        public int RoomId { get; set; }
        public int CustomerId { get; set; }
        public DateTime? Checkin { get; set; }
        public DateTime? Checkout { get; set; }
        public bool IsHourly { get; set; }
        public string? StatusRoom { get; set; }
        public decimal? TotalRoomPrice { get; set; }
    }

    public class CheckoutResponse
    {
        public int BookingId { get; set; }
        public string RoomNumber { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public double TotalUnits { get; set; } 
        public string UnitName { get; set; } = ""; 
        public decimal RoomPrice { get; set; }
        public decimal ServicesTotal { get; set; }
        public decimal DepositPaid { get; set; }
        public decimal RemainingCharged { get; set; }
        public decimal GrandTotal { get; set; }
        public string PaymentMethod { get; set; } = "";
        public List<ServiceDetailDto> Details { get; set; } = new();
    }

    public class CheckoutRequestDto
    {
        public string PaymentMethod { get; set; } = "cash";
    }

    public class BookWithDepositDto
    {
        public int RoomId { get; set; }
        public string Fullname { get; set; } = "";
        public string Cccd { get; set; } = "";
        public string? Sdt { get; set; }
        public bool IsHourly { get; set; }
        public DateTime ExpectedCheckin { get; set; }
        public DateTime? ExpectedCheckout { get; set; }
    }

    public class ServiceDetailDto
    {
        public string ServiceName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
    public class BookCashReserveDto
    {
        public int RoomId { get; set; }
        public string Fullname { get; set; } = "";
        public string Cccd { get; set; } = "";
        public string? Sdt { get; set; }
        public bool IsHourly { get; set; }
        public DateTime ExpectedCheckin { get; set; }
        public DateTime? ExpectedCheckout { get; set; }
        public int UserId { get; set; } 
    }
}