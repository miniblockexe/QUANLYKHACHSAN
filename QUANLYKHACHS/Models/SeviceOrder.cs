using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHSAN.Models;

public partial class SeviceOrder
{
    [Key]
    public int OrderId { get; set; }

    public int BookingId { get; set; }

    [Column("ServiceID")]
    public int ServiceId { get; set; }

    public int? Quantity { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? PriceAtOrder { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? OrderTime { get; set; }

    [ForeignKey("BookingId")]
    [InverseProperty("SeviceOrders")]
    public virtual Booking Booking { get; set; } = null!;

    [ForeignKey("ServiceId")]
    [InverseProperty("SeviceOrders")]
    public virtual Servicess Service { get; set; } = null!;
}
