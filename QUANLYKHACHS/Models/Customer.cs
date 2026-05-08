using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHSAN.Models;

public partial class Customer
{
    [Key]
    public int Customerid { get; set; }

    [Column("fullname")]
    [StringLength(100)]
    public string Fullname { get; set; } = null!;

    [Column("CCCD")]
    [StringLength(20)]
    [Unicode(false)]
    public string Cccd { get; set; } = null!;

    [StringLength(15)]
    [Unicode(false)]
    public string? Sdt { get; set; }

    [InverseProperty("Customer")]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
