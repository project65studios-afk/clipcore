using System;

namespace ClipCore.Core.Entities
{
    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }

    public class PromoCode
    {
        public Guid TenantId { get; set; }
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
        public int Value { get; set; } // Percentage (0-100) or Cents
        public int? MaxUsages { get; set; }
        public int UsageCount { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsValid()
        {
            if (!IsActive) return false;
            if (ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow) return false;
            if (MaxUsages.HasValue && UsageCount >= MaxUsages.Value) return false;
            return true;
        }

        public long CalculateDiscount(long subtotalCents)
        {
            if (DiscountType == DiscountType.Percentage)
            {
                return (long)Math.Floor(subtotalCents * (Value / 100.0));
            }
            else // FixedAmount
            {
                return Math.Min(Value, subtotalCents); // Don't discount more than the subtotal
            }
        }
    }
}
