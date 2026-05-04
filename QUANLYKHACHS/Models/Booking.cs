using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHS.Models;

[Table("Booking")]
public partial class Booking
{
    [Key]
    public int BookingId { get; set; }

    public int RoomId { get; set; }

    public int CustomerId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Checkin { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Checkout { get; set; }

    [StringLength(20)]
    public string? StatusRoom { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? TotalRoomRice { get; set; }

    public bool IsHourly { get; set; }

    public int? CreatedByUserId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? DepositAmount { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ExpectedCheckin { get; set; }

    public bool DepositRefunded { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Bookings")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Bookings")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("RoomId")]
    [InverseProperty("Bookings")]
    public virtual Room Room { get; set; } = null!;

    [InverseProperty("Booking")]
    public virtual ICollection<SeviceOrder> SeviceOrders { get; set; } = new List<SeviceOrder>();
}
