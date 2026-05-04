using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHS.Models;

[Index("TypeName", Name = "UQ__RoomType__D4E7DFA87E51BE8A", IsUnique = true)]
public partial class RoomType
{
    [Key]
    public int RoomTypeId { get; set; }

    [StringLength(100)]
    public string TypeName { get; set; } = null!;

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? GiaTheoGio { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? GiaTheoDem { get; set; }

    [InverseProperty("LoaiPhongNavigation")]
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
