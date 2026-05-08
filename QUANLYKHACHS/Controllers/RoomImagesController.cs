using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHSAN.Data;
using QUANLYKHACHSAN.Models;


namespace QUANLYKHACHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomImagesController : Controller
    {
        private readonly HotelContext _context;

        public RoomImagesController(HotelContext context)
        {
            _context = context;
        }
        [HttpPost("{roomId}/upload-image")]
        public async Task<IActionResult> UploadRoomImage(int roomId, [FromForm] RoomImageUploadDto dto)
        {
            var roomExists = await _context.Rooms.AnyAsync(r => r.RoomId == roomId);
            if (!roomExists)
            {
                return NotFound($"Lỗi: Không tìm thấy phòng với ID = {roomId}. Hãy kiểm tra lại ID phòng.");
            }

            if (dto.File == null || dto.File.Length == 0)
            {
                return BadRequest("Vui lòng chọn một file ảnh.");
            }

            using (var memoryStream = new MemoryStream())
            {
                await dto.File.CopyToAsync(memoryStream);

                var newImage = new RoomImage
                {
                    RoomId = roomId, 
                    ImageLabel = dto.Label,
                    ImageData = memoryStream.ToArray(),
                    IsPrimary = dto.IsPrimary
                };

                _context.RoomImages.Add(newImage);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Lưu ảnh thành công!", imageId = newImage.ImageId });
            }
        }

        [HttpGet("{imageId}")]
        public async Task<IActionResult> GetImage(int imageId)
        {
            var image = await _context.RoomImages.FindAsync(imageId);

            if (image == null || image.ImageData == null)
            {
                return NotFound("Không tìm thấy hình ảnh.");
            }

            return File(image.ImageData, "image/jpeg");
        }

        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetImagesByRoom(int roomId)
        {
            var images = await _context.RoomImages
                .Where(img => img.RoomId == roomId)
                .Select(img => new {
                    img.ImageId,
                    img.ImageLabel,
                    img.IsPrimary
                })
                .ToListAsync();

            return Ok(images);
        }

        [HttpDelete("{imageId}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.RoomImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound("Không tìm thấy ảnh để xóa.");
            }

            _context.RoomImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa ảnh thành công." });
        }

        [HttpPut("{imageId}")]
        public async Task<IActionResult> UpdateImage(int imageId, [FromForm] RoomImageUploadDto dto)
        {
            var existingImage = await _context.RoomImages.FindAsync(imageId);
            if (existingImage == null)
            {
                return NotFound("Không tìm thấy ảnh để cập nhật.");
            }

            existingImage.ImageLabel = dto.Label;
            existingImage.IsPrimary = dto.IsPrimary;

            if (dto.File != null && dto.File.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await dto.File.CopyToAsync(memoryStream);
                    existingImage.ImageData = memoryStream.ToArray();
                }
            }

            _context.RoomImages.Update(existingImage);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật ảnh thành công!" });
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetRoomsOverview()
        {
            var rooms = await _context.Rooms.ToListAsync();

            var result = new List<object>();
            foreach (var room in rooms)
            {
                var images = await _context.RoomImages
                    .Where(img => img.RoomId == room.RoomId)
                    .Select(img => new {
                        img.ImageId,
                        img.ImageLabel,
                        img.IsPrimary
                    })
                    .ToListAsync();

                result.Add(new
                {
                    room.RoomId,
                    room.SoPhong,
                    ImageCount = images.Count,
                    Images = images
                });
            }

            return Ok(result);
        }

        public class RoomImageUploadDto
        {
            public IFormFile File { get; set; }
            public string? Label { get; set; }
            public bool IsPrimary { get; set; }
        }
    }
}
