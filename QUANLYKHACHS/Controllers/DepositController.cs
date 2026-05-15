using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUANLYKHACHSAN.Data;
using QUANLYKHACHSAN.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace QUANLYKHACHS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepositController : ControllerBase
    {
        private readonly HotelContext _context;

        public DepositController(HotelContext context)
        {
            _context = context;
        }

        [HttpPost("request")]
        [Authorize]
        public async Task<IActionResult> CreateDepositRequest([FromBody] CreateDepositRequestDto dto)
        {
            if (dto.Amount <= 0 || dto.Amount % 2000 != 0)
                return BadRequest(new { success = false, message = "Số tiền phải là bội số của 2.000 VNĐ" });

            var userIdStr = User.FindFirstValue("id");
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Không xác định được người dùng" });

            if (await _context.DepositTransactions.AnyAsync(d => d.TransactionCode == dto.TransactionCode.Trim()))
                return BadRequest(new { success = false, message = "Mã giao dịch đã tồn tại" });

            if (await _context.DepositTransactions.AnyAsync(d => d.UserId == userId && d.Status == "Pending"))
                return BadRequest(new { success = false, message = "Bạn đang có yêu cầu chờ xử lý, vui lòng chờ admin duyệt." });

            PromoCode? promo = null;
            decimal discountPercent = 0;

            if (!string.IsNullOrWhiteSpace(dto.PromoCode))
            {
                promo = await _context.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code == dto.PromoCode.Trim().ToUpper() && p.IsActive);

                if (promo == null)
                    return BadRequest(new { success = false, message = "Mã khuyến mãi không hợp lệ hoặc đã hết hiệu lực" });

                if (promo.ExpiredAt.HasValue && promo.ExpiredAt < DateTime.Now)
                    return BadRequest(new { success = false, message = "Mã khuyến mãi đã hết hạn" });

                var alreadyUsed = await _context.PromoCodeUsages
                    .AnyAsync(u => u.PromoId == promo.PromoId && u.UserId == userId);
                if (alreadyUsed)
                    return BadRequest(new { success = false, message = $"Bạn đã sử dụng mã '{promo.Code}' trước đó" });

                discountPercent = promo.DiscountPercent;
            }

            long baseCoins  = (long)(dto.Amount * 100);
            long bonusCoins = (long)(baseCoins * discountPercent / 100);
            long totalCoins = baseCoins + bonusCoins;

            var deposit = new DepositTransaction
            {
                UserId          = userId,
                Amount          = dto.Amount,
                TransactionCode = dto.TransactionCode.Trim(),
                Status          = "Pending",
                CreatedAt       = DateTime.Now,
                ApprovedAt      = null
            };

            _context.DepositTransactions.Add(deposit);
            await _context.SaveChangesAsync();

            if (promo != null)
            {
                _context.PromoCodeUsages.Add(new PromoCodeUsage
                {
                    PromoId         = promo.PromoId,
                    UserId          = userId,
                    TransactionId   = deposit.TransactionId,
                    DiscountPercent = discountPercent,
                    BonusCoins      = bonusCoins,
                    UsedAt          = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message = "Yêu cầu nạp tiền đã được gửi. Vui lòng chờ admin xác nhận.",
                data = new
                {
                    transactionId   = deposit.TransactionId,
                    amount          = deposit.Amount,
                    transactionCode = deposit.TransactionCode,
                    status          = deposit.Status,
                    baseCoins,
                    bonusCoins,
                    totalCoins,
                    discountPercent,
                    promoCode       = promo?.Code,
                    note            = promo != null
                        ? $"Nạp {dto.Amount:#,##0} ₫ → {baseCoins:#,##0} đ + {bonusCoins:#,##0} đ bonus ({discountPercent}%) = {totalCoins:#,##0} đ"
                        : $"Nạp {dto.Amount:#,##0} ₫ → {totalCoins:#,##0} đ",
                    createdAt       = deposit.CreatedAt
                }
            });
        }

        [HttpPost("admin/approve/{transactionId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ApproveDeposit(int transactionId)
        {
            await using var dbTx = await _context.Database.BeginTransactionAsync();
            try
            {
                var deposit = await _context.DepositTransactions
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.TransactionId == transactionId);

                if (deposit == null)
                    return NotFound(new { success = false, message = "Không tìm thấy giao dịch" });

                if (deposit.Status == "Approved")
                    return BadRequest(new { success = false, message = "Giao dịch đã được duyệt trước đó" });

                if (deposit.Status == "Rejected")
                    return BadRequest(new { success = false, message = "Giao dịch đã bị từ chối" });

                long baseCoins = (long)(deposit.Amount * 100);

                var promoUsage = await _context.PromoCodeUsages
                    .Include(u => u.Promo)
                    .FirstOrDefaultAsync(u => u.TransactionId == transactionId);

                long bonusCoins = promoUsage?.BonusCoins ?? 0;
                long totalCoins = baseCoins + bonusCoins;

                deposit.User.Balance += totalCoins;
                deposit.Status        = "Approved";
                deposit.ApprovedAt    = DateTime.Now;

                await _context.SaveChangesAsync();
                await dbTx.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Duyệt thành công! Đã cộng {totalCoins:#,##0} đ cho {deposit.User.UserName}",
                    data = new
                    {
                        transactionId   = deposit.TransactionId,
                        userId          = deposit.UserId,
                        userName        = deposit.User.UserName,
                        amountDeposited = deposit.Amount,
                        baseCoins,
                        bonusCoins,
                        totalCoins,
                        promoCode       = promoUsage?.Promo.Code,
                        discountPercent = promoUsage?.DiscountPercent ?? 0,
                        newBalance      = deposit.User.Balance,
                        approvedAt      = deposit.ApprovedAt
                    }
                });
            }
            catch (Exception ex)
            {
                await dbTx.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống", detail = ex.Message });
            }
        }

        [HttpPost("validate-promo")]
        [Authorize]
        public async Task<IActionResult> ValidatePromo([FromBody] ValidatePromoDto dto)
        {
            var userIdStr = User.FindFirstValue("id");
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var promo = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == dto.PromoCode.Trim().ToUpper() && p.IsActive);

            if (promo == null)
                return Ok(new { valid = false, message = "Mã không tồn tại hoặc đã hết hiệu lực" });

            if (promo.ExpiredAt.HasValue && promo.ExpiredAt < DateTime.Now)
                return Ok(new { valid = false, message = "Mã đã hết hạn" });

            var alreadyUsed = await _context.PromoCodeUsages
                .AnyAsync(u => u.PromoId == promo.PromoId && u.UserId == userId);
            if (alreadyUsed)
                return Ok(new { valid = false, message = $"Bạn đã sử dụng mã '{promo.Code}' rồi" });

            return Ok(new
            {
                valid           = true,
                code            = promo.Code,
                discountPercent = promo.DiscountPercent,
                description     = promo.Description,
                message         = $"Mã hợp lệ! Cộng thêm {promo.DiscountPercent}% đ"
            });
        }

        [HttpGet("my-history")]
        [Authorize]
        public async Task<IActionResult> GetMyHistory()
        {
            var userIdStr = User.FindFirstValue("id");
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var history = await _context.DepositTransactions
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.TransactionId,
                    d.Amount,
                    BaseCoins     = (long)(d.Amount * 100),
                    BonusCoins    = _context.PromoCodeUsages
                                    .Where(u => u.TransactionId == d.TransactionId)
                                    .Select(u => u.BonusCoins)
                                    .FirstOrDefault(),
                    TotalCoins    = d.Status == "Approved"
                                    ? (long)(d.Amount * 100) + _context.PromoCodeUsages
                                        .Where(u => u.TransactionId == d.TransactionId)
                                        .Select(u => u.BonusCoins)
                                        .FirstOrDefault()
                                    : (long)0,
                    PromoCode     = _context.PromoCodeUsages
                                    .Where(u => u.TransactionId == d.TransactionId)
                                    .Select(u => u.Promo.Code)
                                    .FirstOrDefault(),
                    d.TransactionCode,
                    d.Status,
                    d.CreatedAt,
                    d.ApprovedAt
                })
                .ToListAsync();

            return Ok(new { success = true, total = history.Count, data = history });
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllDeposits([FromQuery] string? status = null)
        {
            var query = _context.DepositTransactions.Include(d => d.User).AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(d => d.Status == status);

            var deposits = await query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.TransactionId,
                    d.UserId,
                    UserName        = d.User.UserName,
                    FullName        = d.User.Fullname,
                    d.Amount,
                    BaseCoins       = (long)(d.Amount * 100),
                    BonusCoins      = _context.PromoCodeUsages
                                        .Where(u => u.TransactionId == d.TransactionId)
                                        .Select(u => u.BonusCoins).FirstOrDefault(),
                    PromoCode       = _context.PromoCodeUsages
                                        .Where(u => u.TransactionId == d.TransactionId)
                                        .Select(u => u.Promo.Code).FirstOrDefault(),
                    DiscountPercent = _context.PromoCodeUsages
                                        .Where(u => u.TransactionId == d.TransactionId)
                                        .Select(u => u.DiscountPercent).FirstOrDefault(),
                    d.TransactionCode,
                    d.Status,
                    d.CreatedAt,
                    d.ApprovedAt
                })
                .ToListAsync();

            return Ok(new { success = true, total = deposits.Count, data = deposits });
        }

        [HttpPost("admin/reject/{transactionId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RejectDeposit(int transactionId, [FromBody] RejectDepositDto dto)
        {
            var deposit = await _context.DepositTransactions
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.TransactionId == transactionId);

            if (deposit == null)
                return NotFound(new { success = false, message = "Không tìm thấy giao dịch" });

            if (deposit.Status != "Pending")
                return BadRequest(new { success = false, message = $"Trạng thái '{deposit.Status}', không thể từ chối" });

            deposit.Status     = "Rejected";
            deposit.ApprovedAt = DateTime.Now;

            var promoUsage = await _context.PromoCodeUsages
                .FirstOrDefaultAsync(u => u.TransactionId == transactionId);
            if (promoUsage != null)
                _context.PromoCodeUsages.Remove(promoUsage);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Đã từ chối yêu cầu của {deposit.User.UserName}",
                data    = new { transactionId, status = deposit.Status, reason = dto.Reason }
            });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class PromoCodeController : ControllerBase
    {
        private readonly HotelContext _context;
        public PromoCodeController(HotelContext context) => _context = context;

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll()
        {
            var codes = await _context.PromoCodes
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.PromoId,
                    p.Code,
                    p.Description,
                    p.DiscountPercent,
                    p.IsActive,
                    p.ExpiredAt,
                    p.CreatedAt,
                    UsageCount = _context.PromoCodeUsages.Count(u => u.PromoId == p.PromoId)
                })
                .ToListAsync();

            return Ok(new { success = true, data = codes });
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] CreatePromoDto dto)
        {
            var existed = await _context.PromoCodes
                .AnyAsync(p => p.Code == dto.Code.Trim().ToUpper());
            if (existed)
                return BadRequest(new { success = false, message = "Mã này đã tồn tại" });

            var promo = new PromoCode
            {
                Code            = dto.Code.Trim().ToUpper(),
                Description     = dto.Description,
                DiscountPercent = dto.DiscountPercent,
                IsActive        = true,
                ExpiredAt       = dto.ExpiredAt,
                CreatedAt       = DateTime.Now
            };

            _context.PromoCodes.Add(promo);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Tạo mã '{promo.Code}' thành công", data = promo });
        }

        [HttpPatch("{id:int}/toggle")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Toggle(int id)
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null)
                return NotFound(new { success = false, message = "Không tìm thấy mã" });

            promo.IsActive = !promo.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Mã '{promo.Code}' đã {(promo.IsActive ? "bật" : "tắt")}",
                isActive = promo.IsActive
            });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null)
                return NotFound();

            _context.PromoCodes.Remove(promo);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Đã xóa mã '{promo.Code}'" });
        }
    }

    public class CreateDepositRequestDto
    {
        [Required]
        [Range(2000, 100_000_000)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 4)]
        public string TransactionCode { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PromoCode { get; set; }
    }

    public class ValidatePromoDto
    {
        [Required]
        public string PromoCode { get; set; } = string.Empty;
    }

    public class RejectDepositDto
    {
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class CreatePromoDto
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 1000)]
        public decimal DiscountPercent { get; set; }

        public DateTime? ExpiredAt { get; set; }
    }
}