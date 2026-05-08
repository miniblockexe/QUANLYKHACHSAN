using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHSAN.Models;

[Table("AppBackground")]
[Index("ThemeKey", Name = "IX_AppBackground_ThemeKey", IsUnique = true)]
[Index("ThemeKey", Name = "UQ__AppBackg__6CAE74C4BC618F1B", IsUnique = true)]
public partial class AppBackground
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    public string ThemeKey { get; set; } = null!;

    [StringLength(100)]
    public string FileName { get; set; } = null!;

    [StringLength(50)]
    public string MimeType { get; set; } = null!;

    public byte[] Data { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
