using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHSAN.Data;
using QUANLYKHACHSAN.Models;

namespace QUANLYKHACHSAN.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppBackgroundController : ControllerBase
    {
        private readonly HotelContext _db;

        public AppBackgroundController(HotelContext db)
        {
            _db = db;
        }

        [HttpGet("{themeKey}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBackground(string themeKey)
        {
            var key = themeKey.ToLower();
            if (key != "light" && key != "dark")
                return BadRequest("themeKey phải là 'light' hoặc 'dark'.");

            var bg = await _db.AppBackgrounds
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ThemeKey == key);

            if (bg == null || bg.Data.Length == 0)
                return NotFound("Chưa có ảnh nền cho theme này.");

            Response.Headers["Cache-Control"] = "public, max-age=86400";
            return File(bg.Data, bg.MimeType, bg.FileName);
        }

        [HttpPut("{themeKey}")]
        [Authorize(Roles = "admin")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadBackground(string themeKey, IFormFile file)
        {
            var key = themeKey.ToLower();
            if (key != "light" && key != "dark")
                return BadRequest("themeKey phải là 'light' hoặc 'dark'.");

            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn file SVG.");

            if (!file.FileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Chỉ chấp nhận file .svg.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var bg = await _db.AppBackgrounds
                .FirstOrDefaultAsync(x => x.ThemeKey == key);

            if (bg == null)
            {
                bg = new AppBackground
                {
                    ThemeKey = key,
                    FileName = file.FileName,
                    MimeType = "image/svg+xml",
                    Data = bytes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.AppBackgrounds.Add(bg);
            }
            else
            {
                bg.FileName = file.FileName;
                bg.Data = bytes;
                bg.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Upload thành công ảnh nền '{key}'.",
                themeKey = key,
                fileName = bg.FileName,
                sizeKb = Math.Round(bytes.Length / 1024.0, 1)
            });
        }

        [HttpDelete("{themeKey}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteBackground(string themeKey)
        {
            var bg = await _db.AppBackgrounds
                .FirstOrDefaultAsync(x => x.ThemeKey == themeKey.ToLower());

            if (bg == null) return NotFound();

            bg.Data = Array.Empty<byte>();
            bg.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Đã xoá ảnh nền '{themeKey}'." });
        }
    }
}