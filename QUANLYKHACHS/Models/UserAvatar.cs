using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHS.Models;

[Index("UserId", Name = "UQ__UserAvat__1788CCADA7F97D56", IsUnique = true)]
public partial class UserAvatar
{
    [Key]
    public int AvatarId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(255)]
    public string? FileName { get; set; }

    public byte[] ImageData { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? UploadedDate { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserAvatar")]
    public virtual User User { get; set; } = null!;
}
