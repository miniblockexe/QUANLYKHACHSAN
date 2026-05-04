using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHS.Models;

public partial class RoomImage
{
    [Key]
    public int ImageId { get; set; }

    public int RoomId { get; set; }

    [StringLength(100)]
    public string? ImageLabel { get; set; }

    public byte[] ImageData { get; set; } = null!;

    public bool? IsPrimary { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UploadedDate { get; set; }

    [ForeignKey("RoomId")]
    [InverseProperty("RoomImages")]
    public virtual Room Room { get; set; } = null!;
}
