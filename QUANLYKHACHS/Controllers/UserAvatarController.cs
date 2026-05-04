using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHS.Data;
using QUANLYKHACHS.Models;

namespace QUANLYKHACHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAvatarController : ControllerBase
    {
        private readonly HotelContext _context;

        public UserAvatarController(HotelContext context)
        {
            _context = context;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAvatar(int userId)
        {
            var avatar = await _context.UserAvatars
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (avatar == null || avatar.ImageData == null)
                return NotFound("Chưa có avatar.");

            return File(avatar.ImageData, "image/jpeg");
        }

        [HttpPost("{userId}/upload")]
        public async Task<IActionResult> UploadAvatar(int userId, [FromForm] AvatarUploadDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("Vui lòng chọn ảnh.");

            if (dto.File.Length > 5 * 1024 * 1024)
                return BadRequest("Ảnh không được vượt quá 5MB.");

            using var ms = new MemoryStream();
            await dto.File.CopyToAsync(ms);

            var existing = await _context.UserAvatars
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (existing != null)
            {
                existing.ImageData = ms.ToArray();
                existing.FileName = dto.File.FileName;
                existing.UploadedDate = DateTime.Now;
                _context.UserAvatars.Update(existing);
            }
            else
            {
                _context.UserAvatars.Add(new UserAvatar
                {
                    UserId = userId,
                    FileName = dto.File.FileName,
                    ImageData = ms.ToArray(),
                    UploadedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật avatar thành công!" });
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteAvatar(int userId)
        {
            var avatar = await _context.UserAvatars
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (avatar == null) return NotFound();

            _context.UserAvatars.Remove(avatar);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa avatar." });
        }
    }

    public class AvatarUploadDto
    {
        public IFormFile File { get; set; } = null!;
    }
}