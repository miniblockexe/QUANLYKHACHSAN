using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHSAN.Data;
using QUANLYKHACHSAN.Models;

namespace QUANLYKHACHS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomersController : ControllerBase
{
    private readonly HotelContext _context;

    public CustomersController(HotelContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        try
        {
            var customers = await _context.Customers
                .Select(c => new
                {
                    c.Customerid,
                    c.Fullname,
                    c.Cccd,
                    c.Sdt,
                    RoomNumber = _context.Bookings
                        .Where(b => b.CustomerId == c.Customerid && (b.StatusRoom == "active" || b.StatusRoom == "Reserved"))
                        .OrderByDescending(b => b.BookingId) 
                        .Select(b => b.Room.SoPhong)
                        .FirstOrDefault(),
                    BookingStatus = _context.Bookings
                        .Where(b => b.CustomerId == c.Customerid && (b.StatusRoom == "active" || b.StatusRoom == "Reserved"))
                        .OrderByDescending(b => b.BookingId)
                        .Select(b => b.StatusRoom)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(customers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetById(int id)
    {
        try
        {
            var customer = await _context.Customers
                .FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }
        catch(Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult> SearchByCCCD(string cccd)
    {
        if (string.IsNullOrWhiteSpace(cccd))
        {
            return BadRequest("Vui lòng cung cấp CCCD để tìm kiếm");
        }

        var customers = await _context.Customers
            .Where(c => c.Cccd.ToLower().Contains(cccd.ToLower()))
            .Select(c => new
            {
                c.Customerid,
                c.Fullname,
                c.Cccd,
                c.Sdt,
                RoomNumber = _context.Bookings
                    .Where(b => b.CustomerId == c.Customerid && (b.StatusRoom == "active" || b.StatusRoom == "Reserved"))
                    .OrderByDescending(b => b.BookingId)
                    .Select(b => b.Room.SoPhong)
                    .FirstOrDefault(),

                BookingStatus = _context.Bookings
                    .Where(b => b.CustomerId == c.Customerid && (b.StatusRoom == "active" || b.StatusRoom == "Reserved"))
                    .OrderByDescending(b => b.BookingId)
                    .Select(b => b.StatusRoom)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(customers);
    }

    //[HttpPost]
    //public async Task<ActionResult<Customer>> Create([FromBody] CustomerCreateDto dto)
    //{

    //    if (await _context.Customers.AnyAsync(c => c.Cccd == dto.Cccd))
    //    {
    //        return BadRequest(new { message = "CCCD này đã được đăng ký trước đó" });
    //    }

    //    var customer = new Customer
    //    {
    //        Fullname = dto.Fullname,
    //        Cccd = dto.Cccd,
    //        Sdt = dto.Sdt
    //    };

    //    _context.Customers.Add(customer);
    //    await _context.SaveChangesAsync();

    //    return CreatedAtAction(nameof(GetById), new { id = customer.Customerid }, customer);
    //}

    [HttpPost("checkin")]
    public async Task<ActionResult> CheckInCustomer([FromBody] CheckInModel checkInModel)
    {
        if (string.IsNullOrWhiteSpace(checkInModel.Cccd))
        {
            return BadRequest(new { message = "CCCD là bắt buộc!" });
        }

        var room = await _context.Rooms.FindAsync(checkInModel.RoomId);
        if (room == null)
        {
            return BadRequest(new { message = "The room does not exist!" });
        }

        if (room.TrangThai == "Occupied")
        {
            return BadRequest(new { message = "The room is already booked!" });
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Cccd == checkInModel.Cccd);
        if (customer == null)
        {
            customer = new Customer
            {
                Fullname = checkInModel.Fullname,
                Cccd = checkInModel.Cccd,
                Sdt = checkInModel.Sdt
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        else if (customer.Fullname != checkInModel.Fullname || customer.Sdt != checkInModel.Sdt)
        {
            customer.Fullname = checkInModel.Fullname;
            customer.Sdt = checkInModel.Sdt;
            await _context.SaveChangesAsync();
        }

        room.TrangThai = "Reserved";
        _context.Entry(room).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        var booking = new Booking
        {
            CustomerId = customer.Customerid,
            RoomId = room.RoomId,
            Checkin = DateTime.Now,
            Checkout = checkInModel.Checkout,
            TotalRoomRice = 0,
            StatusRoom = "Reserved",
            CreatedByUserId = checkInModel.UserId
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Check-in thành công!", BookingId = booking.BookingId });
    }

    [HttpGet("{customerId}/active-booking")]
    public async Task<ActionResult> GetActiveBooking(int customerId)
    {
        var booking = await _context.Bookings
            .OrderByDescending(b => b.BookingId)
            .FirstOrDefaultAsync(b => b.CustomerId == customerId &&
                                     (b.StatusRoom == "active" || b.StatusRoom == "Reserved"));

        if (booking == null)
        {
            return NotFound(new { message = "Khách hàng này hiện không có phòng đang thuê hoặc đặt trước." });
        }

        return Ok(new { bookingId = booking.BookingId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerCreateDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        if (dto.Cccd != customer.Cccd &&
            await _context.Customers.AnyAsync(c => c.Cccd == dto.Cccd))
        {
            return BadRequest(new { message = "CCCD này đã được sử dụng bởi khách hàng khác" });
        }

        customer.Fullname = dto.Fullname;
        customer.Cccd = dto.Cccd;
        customer.Sdt = dto.Sdt;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Bookings)
                .ThenInclude(b => b.SeviceOrders) 
            .FirstOrDefaultAsync(c => c.Customerid == id);

        if (customer == null)
        {
            return NotFound(new { message = "Không tìm thấy khách hàng này!" });
        }
        
        var activeBooking = customer.Bookings.FirstOrDefault(b => b.StatusRoom == "Active");

        if (activeBooking != null)
        {
            return BadRequest(new
            {
                message = $"Khách hàng đang ở phòng {activeBooking.RoomId}. Vui lòng Checkout trước khi xóa!"
            });
        }

        try
        {
            foreach (var booking in customer.Bookings)
            {
                if (booking.SeviceOrders != null && booking.SeviceOrders.Any())
                {
                    _context.SeviceOrders.RemoveRange(booking.SeviceOrders);
                }
            }

            _context.Bookings.RemoveRange(customer.Bookings);

            _context.Customers.Remove(customer);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa khách hàng và toàn bộ lịch sử dịch vụ thành công!" });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return StatusCode(500, new { message = "Lỗi khi xóa dữ liệu liên kết. Hãy đảm bảo đã xóa hết ServiceOrders trước!" });
        }
    }

    public class CustomerCreateDto
    {
        public string Fullname { get; set; } = null!;
        public string Cccd { get; set; } = null!;
        public string? Sdt { get; set; }
    }
}