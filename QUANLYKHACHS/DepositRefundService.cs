using Microsoft.EntityFrameworkCore;
using QUANLYKHACHS.Data;

namespace QUANLYKHACHS
{
    public class DepositRefundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DepositRefundService> _logger;

        public DepositRefundService(IServiceScopeFactory scopeFactory, ILogger<DepositRefundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RefundExpiredDeposits();
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); 
            }
        }

        private async Task RefundExpiredDeposits()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelContext>();

            var expiredBookings = await context.Bookings
                .Include(b => b.Room)
                .Where(b =>
                    b.StatusRoom == "Reserved" &&
                    b.DepositAmount > 0 &&
                    !b.DepositRefunded &&
                    b.ExpectedCheckin.HasValue &&
                    b.ExpectedCheckin.Value.Date < DateTime.Today &&
                    b.CreatedByUserId.HasValue)
                .ToListAsync();

            foreach (var booking in expiredBookings)
            {
                using var tx = await context.Database.BeginTransactionAsync();
                try
                {
                    var user = await context.Users.FindAsync(booking.CreatedByUserId!.Value);
                    if (user != null)
                        user.Balance += booking.DepositAmount ?? 0;

                    booking.StatusRoom = "cancelled";
                    booking.DepositRefunded = true;

                    if (booking.Room != null)
                        booking.Room.TrangThai = "Available";

                    await context.SaveChangesAsync();
                    await tx.CommitAsync();
                    _logger.LogInformation($"Hoàn cọc {booking.DepositAmount} cho user {booking.CreatedByUserId}, booking #{booking.BookingId}");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, $"Lỗi hoàn cọc booking #{booking.BookingId}");
                }
            }
        }
    }
}
