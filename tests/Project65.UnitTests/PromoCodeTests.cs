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
}
