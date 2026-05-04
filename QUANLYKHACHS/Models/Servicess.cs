using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHS.Models;

[Table("Servicess")]
public partial class Servicess
{
    [Key]
    public int ServicessId { get; set; }

    [StringLength(100)]
    public string Servicename { get; set; } = null!;

    [StringLength(20)]
    public string? Unit { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? Price { get; set; }

    [InverseProperty("Service")]
    public virtual ICollection<SeviceOrder> SeviceOrders { get; set; } = new List<SeviceOrder>();
}
