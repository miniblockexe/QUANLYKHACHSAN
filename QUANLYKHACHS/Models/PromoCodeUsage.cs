using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QUANLYKHACHS.Models;

public partial class PromoCodeUsage
{
    [Key]
    public int UsageId { get; set; }

    public int PromoId { get; set; }

    public int UserId { get; set; }

    public int TransactionId { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal DiscountPercent { get; set; }

    public long BonusCoins { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UsedAt { get; set; }

    [ForeignKey("PromoId")]
    [InverseProperty("PromoCodeUsages")]
    public virtual PromoCode Promo { get; set; } = null!;

    [ForeignKey("TransactionId")]
    [InverseProperty("PromoCodeUsages")]
    public virtual DepositTransaction Transaction { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("PromoCodeUsages")]
    public virtual User User { get; set; } = null!;
}
