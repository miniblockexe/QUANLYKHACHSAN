using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHSAN.Models;

[Index("UserName", Name = "UQ__Users__C9F28456561B1F52", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string UserName { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string Passwords { get; set; } = null!;

    [Column("fullname")]
    [StringLength(255)]
    public string? Fullname { get; set; }

    [Column("roles")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Roles { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Balance { get; set; }

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    [InverseProperty("User")]
    public virtual ICollection<DepositTransaction> DepositTransactions { get; set; } = new List<DepositTransaction>();

    [InverseProperty("User")]
    public virtual ICollection<PromoCodeUsage> PromoCodeUsages { get; set; } = new List<PromoCodeUsage>();

    [InverseProperty("User")]
    public virtual UserAvatar? UserAvatar { get; set; }
}
