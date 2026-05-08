using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHSAN.Models;

[Index("SoPhong", Name = "UQ__Rooms__7C736CA18535374B", IsUnique = true)]
public partial class Room
{
    [Key]
    public int RoomId { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string SoPhong { get; set; } = null!;

    public int? LoaiPhong { get; set; }

    [StringLength(50)]
    public string? TrangThai { get; set; }

    [InverseProperty("Room")]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    [ForeignKey("LoaiPhong")]
    [InverseProperty("Rooms")]
    public virtual RoomType? LoaiPhongNavigation { get; set; }

    [InverseProperty("Room")]
    public virtual ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();
}
