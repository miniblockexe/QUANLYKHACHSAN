using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHS.Data;
using QUANLYKHACHS.Models;

namespace QUANLYKHACHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly HotelContext _context;

        public RoomsController(HotelContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomStoredProcModel>>> GetRooms()
        {
            var rooms = await _context.Rooms.Select(r => new
            {
                RoomID = r.RoomId,
                RoomNumber = r.SoPhong,
                RoomStatus = r.TrangThai,
                RoomTypeID = r.LoaiPhong ?? 0,
                TypeName = r.LoaiPhongNavigation != null ? r.LoaiPhongNavigation.TypeName : "N/A",

                PricePerNight = r.LoaiPhongNavigation != null ? r.LoaiPhongNavigation.GiaTheoDem : 0,
                PricePerHour = r.LoaiPhongNavigation != null ? r.LoaiPhongNavigation.GiaTheoGio : 0,

                BookingId = r.Bookings
                    .Where(b => (b.StatusRoom == "Reserved" || b.StatusRoom == "active") && b.Checkout == null)
                    .OrderByDescending(b => b.BookingId)
                    .Select(b => b.BookingId)
                    .FirstOrDefault(),
                PrimaryImageId = r.RoomImages
                    .Where(img => img.IsPrimary == true)
                    .Select(img => (int?)img.ImageId)
                    .FirstOrDefault()
            }).ToListAsync();

            return Ok(rooms);
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<RoomStoredProcModel>>> GetAvailableRooms()
        {
            var availableRooms = await _context.Rooms
                .Include(r => r.LoaiPhongNavigation)
                .Where(r => r.TrangThai == "Available")
                .Select(r => new RoomStoredProcModel
                {
                    RoomID = r.RoomId,
                    RoomNumber = r.SoPhong,
                    RoomStatus = r.TrangThai,
                    RoomTypeID = r.LoaiPhong ?? 0,
                    TypeName = r.LoaiPhongNavigation.TypeName,
                    PricePerNight = r.LoaiPhongNavigation.GiaTheoDem ?? 0,
                    PricePerHour = r.LoaiPhongNavigation.GiaTheoGio ?? 0
                })
                .ToListAsync();

            return Ok(availableRooms);
        }

        [HttpGet("needs-cleaning")]
        public async Task<ActionResult<IEnumerable<Room>>> GetRoomsToClean()
        {
            var dirtyRooms = await _context.Rooms
                .Where(r => r.TrangThai == "Cleaning" || r.TrangThai == "Dirty")
                .Include(r => r.LoaiPhongNavigation)
                .ToListAsync();
            return Ok(dirtyRooms);
        }

        [HttpPost("{id}/finish-cleaning")]
        public async Task<IActionResult> FinishCleaning(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound("Không tìm thấy phòng.");

            if (room.TrangThai != "Cleaning" && room.TrangThai != "Dirty")
                return BadRequest("Phòng này hiện không cần dọn dẹp.");

            room.TrangThai = "Available"; 
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Phòng {room.SoPhong} dọn xong, đã sẵn sàng!" });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetById(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.LoaiPhongNavigation)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null) return NotFound();
            return Ok(room);
        }

        [HttpPost]
        public async Task<ActionResult<Room>> PostRoom(RoomInputModel input)
        {
            if (!await _context.RoomTypes.AnyAsync(rt => rt.RoomTypeId == input.RoomTypeID))
                return BadRequest("Loại phòng không tồn tại!");

            var newRoom = new Room
            {
                SoPhong = input.RoomNumber,
                LoaiPhong = input.RoomTypeID,
                TrangThai = input.RoomStatus ?? "Available"
            };

            _context.Rooms.Add(newRoom);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), 
                new { id = newRoom.RoomId }, 
            new
            {
                roomID = newRoom.RoomId,
                roomNumber = newRoom.SoPhong,
                roomStatus = newRoom.TrangThai
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RoomInputModel input)
        {
            var room = await _context.Rooms.Include(r => r.LoaiPhongNavigation)
                .FirstOrDefaultAsync(r => r.RoomId == id);
            if (room == null) return NotFound();

            if (input.RoomTypeID.HasValue)
            {
                var exists = await _context.RoomTypes.AnyAsync(rt => rt.RoomTypeId == input.RoomTypeID);
                if (!exists) return BadRequest("Loại phòng không hợp lệ");
                room.LoaiPhong = input.RoomTypeID;
            }

            if (!string.IsNullOrWhiteSpace(input.RoomNumber))
                room.SoPhong = input.RoomNumber;

            if (!string.IsNullOrWhiteSpace(input.RoomStatus))
                room.TrangThai = input.RoomStatus;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class RoomInputModel
    {
        public string? RoomNumber { get; set; }
        public int? RoomTypeID { get; set; }
        public string? RoomStatus { get; set; }
    }

    public class RoomStoredProcModel
    {
        public int RoomID { get; set; }
        public string? RoomNumber { get; set; }
        public string? RoomStatus { get; set; }
        public int RoomTypeID { get; set; }
        public string? TypeName { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal PricePerHour { get; set; }
    }
}