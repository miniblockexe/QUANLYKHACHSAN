using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHS.Data;
using QUANLYKHACHS.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QUANLYKHACHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly HotelContext _context;
        public UserController(HotelContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<User>> DanhSach()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return user;
        }
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (_context.Users.Any(u => u.UserName == user.UserName))
            {
                return BadRequest(new { Message = "Tên người dùng đã tồn tại" });
            }
            user.UserId = 0;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> PutUser(int id, User user)
        {
            var u = await _context.Users.FindAsync(id);
            if (u == null)
            {
                return NotFound();
            }
            u.UserName = user.UserName;
            u.Passwords = user.Passwords;
            u.Fullname = user.Fullname;
            u.Roles = user.Roles;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<User>> Xoa(int id)
        {
            var u = await _context.Users.FindAsync(id);
            if (u == null)
            {
                return NotFound();
            }
            _context.Users.Remove(u);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == req.Username))
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });

            if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 3)
                return BadRequest(new { message = "Mật khẩu phải có ít nhất 3 ký tự!" });

            var newUser = new User
            {
                UserName = req.Username,
                Passwords = req.Password,
                Fullname = req.Fullname,
                Roles = "nv" 
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công!", userId = newUser.UserId });
        }

        [HttpGet("check-username")]
        [ResponseCache(Duration = 30)] 
        public async Task<IActionResult> CheckUsername([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                return BadRequest(new { message = "Username quá ngắn" });

            var exists = await _context.Users.AnyAsync(
                u => u.UserName.ToLower() == username.ToLower()
            );
            return Ok(new { available = !exists });
        }


        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == req.Username && u.Passwords == req.Password);

            if (user == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            return Ok(new
            {
                accessToken = GenerateJwtToken(user),
                username = user.UserName,
                role = user.Roles,
                fullname = user.Fullname,
                userId = user.UserId
            });
        }

        [HttpGet("{id}/profile")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var customer = await _context.Bookings
                .Where(b => b.CreatedByUserId == id)
                .OrderByDescending(b => b.BookingId)
                .Select(b => b.Customer)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                userId = user.UserId,
                username = user.UserName,
                fullname = user.Fullname,
                balance = user.Balance,
                cccd = customer != null ? customer.Cccd : "",
                sdt = customer != null ? customer.Sdt : "",
            });
        }

        [HttpPut("{id}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Fullname = dto.Fullname;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(dto.Cccd))
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Cccd == dto.Cccd);

                if (customer != null)
                {
                    customer.Fullname = dto.Fullname;
                    customer.Sdt = dto.Sdt;
                }
                else
                {
                    _context.Customers.Add(new Customer
                    {
                        Fullname = dto.Fullname ?? "",
                        Cccd = dto.Cccd,
                        Sdt = dto.Sdt
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Cập nhật thành công!" });
        }



        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Chuoi_Key_Bi_Mat_Cuc_Ky_Dai_Va_An_Toan"));
            var token = new JwtSecurityToken(
            claims: new[] {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("id", user.UserId.ToString()),
            new Claim("role", user.Roles ?? "nv")
                },
                expires: DateTime.Now.AddDays(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class UpdateProfileDto
        {
            public string? Fullname { get; set; }
            public string? Cccd { get; set; } 
            public string? Sdt { get; set; }   
        }

        public class RegisterRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? Fullname { get; set; }
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}