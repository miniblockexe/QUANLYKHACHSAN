using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QUANLYKHACHS.Controllers
{
    public class CheckInModel
    {
        public int RoomId { get; set; }
        public string Fullname { get; set; } = null!;
        public string Cccd { get; set; } = null!;
        public string? Sdt { get; set; }
        public bool IsHourly { get; set; }
        public DateTime? Checkout { get; set; }
        public string? Status { get; set; }
        public int? UserId { get; set; } 
    }
}