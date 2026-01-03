using Project65.Core.Entities;
using Xunit;

namespace Project65.UnitTests;

public class PromoCodeTests
{
    [Fact]
    public void IsValid_WithActiveCode_ReturnsTrue()
    {
        // Arrange
        var promo = new PromoCode
        {
            Code = "SAVE25",
            IsActive = true,
            ExpiryDate = DateTime.UtcNow.AddDays(1)
        };

        // Act & Assert
        Assert.True(promo.IsValid());
    }

    [Fact]
    public void IsValid_WithExpiredCode_ReturnsFalse()
    {
        // Arrange
        var promo = new PromoCode
        {
            Code = "EXPIRED",
            IsActive = true,
            ExpiryDate = DateTime.UtcNow.AddDays(-5)
        };

        // Act & Assert
        Assert.False(promo.IsValid());
    }

    [Theory]
    [InlineData(10000, 25, 2500)]   // 25% of $100 = $25
    [InlineData(10000, 10, 1000)]   // 10% of $100 = $10
    [InlineData(5000, 50, 2500)]    // 50% of $50 = $25
    [InlineData(10000, 0, 0)]       // 0% of $100 = $0
    public void CalculateDiscount_Percentage_ReturnsCorrectAmount(long subtotal, int value, long expectedDiscount)
    {
        // Arrange
        var promo = new PromoCode { DiscountType = DiscountType.Percentage, Value = value };

        // Act
        var result = promo.CalculateDiscount(subtotal);

        // Assert
        Assert.Equal(expectedDiscount, result);
    }

    [Theory]
    [InlineData(10000, 1000, 1000)] // $10 off $100 = $10
    [InlineData(5000, 1000, 1000)]  // $10 off $50 = $10
    [InlineData(500, 1000, 500)]    // $10 off $5 (limited by subtotal) = $5
    public void CalculateDiscount_FixedAmount_ReturnsCorrectAmount(long subtotal, int value, long expectedDiscount)
    {
        // Arrange
        var promo = new PromoCode { DiscountType = DiscountType.FixedAmount, Value = value };

        // Act
        var result = promo.CalculateDiscount(subtotal);

        // Assert
        Assert.Equal(expectedDiscount, result);
    }
}
