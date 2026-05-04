
using Microsoft.AspNetCore.Mvc;
using QUANLYKHACHS.Data;
using QUANLYKHACHS.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks; 

namespace QUANLYKHACHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderServicesController : ControllerBase
    {
        private readonly HotelContext _context;
        public OrderServicesController(HotelContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> OrderServices([FromBody] Order o)
        {
            if (o.Quantity <= 0)
            {
                return BadRequest("Số lượng phải lớn hơn 0");
            }

            var booking = await _context.Bookings.FindAsync(o.BookingId);
            if (booking == null)
            {
                return NotFound("Không tìm thấy booking");
            }

            if (booking.Checkout != null && DateTime.Now >= booking.Checkout)
            {
                return BadRequest("Booking đã được check out");
            }

            var service = await _context.Servicesses.FindAsync(o.ServiceId);
            if (service == null)
            {
                return NotFound("Không tìm thấy dịch vụ");
            }
            decimal currentPrice;
            if (service.Price != null)
            {
                currentPrice = service.Price.Value;  
            }
            else
            {
                currentPrice = 0;
            }
            if (currentPrice == 0)
            {
                return BadRequest("Giá dịch vụ không hợp lệ");
            }

            var newOrder = new SeviceOrder 
            {
                BookingId = o.BookingId,
                ServiceId = o.ServiceId,
                Quantity = o.Quantity , 
                OrderTime = DateTime.Now,
                PriceAtOrder = currentPrice + (currentPrice * 10)/100
            };

                _context.SeviceOrders.Add(newOrder);
                await _context.SaveChangesAsync();
           
            return Ok(new
            {
                message = "Order hoàn tất",
                serviceName = service.Servicename,
                quantity = newOrder.Quantity,
                totalPrice = newOrder.PriceAtOrder * newOrder.Quantity,
                orderTime = newOrder.OrderTime
            });
        }
    }

    public class Order
    {
        public int BookingId { get; set; }
        public int ServiceId { get; set; }
        public int? Quantity { get; set; }
    }
}