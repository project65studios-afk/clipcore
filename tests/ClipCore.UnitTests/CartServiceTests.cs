using ClipCore.Web.Services;
using ClipCore.Web.Models;
using ClipCore.Core.Entities;
using Xunit;
using System.Collections.Generic;

namespace ClipCore.UnitTests;

public class CartServiceTests
{
    private readonly CartService _cartService;

    public CartServiceTests()
    {
        // Pass null for storage since we won't call methods that use it (AddAsync, etc.)
        // We will manipulate the internal _cart and _appliedPromo directly for pricing logic tests.
        _cartService = new CartService(null!);
        _cartService._isInitialized = true;
    }

    [Fact]
    public void SubTotal_CalculatesCorrectly()
    {
        // Arrange
        _cartService._cart = new List<CartItem> 
        {
            new CartItem { Id = "1", PriceCents = 1000 },
            new CartItem { Id = "2", PriceCents = 2000 }
        };

        // Assert
        Assert.Equal(3000, _cartService.SubTotalCents);
    }

    [Fact]
    public void BundleDiscount_Applied_WhenThreeOrMoreItems()
    {
        // Arrange
        _cartService._cart = new List<CartItem> 
        {
            new CartItem { Id = "1", PriceCents = 1000 },
            new CartItem { Id = "2", PriceCents = 1000 },
            new CartItem { Id = "3", PriceCents = 1000 }
        };

        // Act & Assert
        Assert.True(_cartService.IsBundleDiscountApplied);
        Assert.Equal(750, _cartService.BundleDiscountCents); // 25% of 3000
    }

    [Fact]
    public void TotalDiscount_TakesMaxOfBundleOrPromo()
    {
        // Arrange
        // Subtotal = 4000
        // Bundle 25% = 1000
        _cartService._cart = new List<CartItem> 
        {
            new CartItem { Id = "1", PriceCents = 1000 },
            new CartItem { Id = "2", PriceCents = 1000 },
            new CartItem { Id = "3", PriceCents = 2000 }
        };

        // Promo 10% = 400
        _cartService._appliedPromo = new PromoCode { DiscountType = DiscountType.Percentage, Value = 10 };

        // Act & Assert
        Assert.Equal(1000, _cartService.BundleDiscountCents);
        Assert.Equal(400, _cartService.PromoDiscountCents);
        Assert.Equal(1000, _cartService.TotalDiscountCents); // Max(1000, 400)
    }

    [Fact]
    public void TotalDiscount_PrefersStrongerPromoOverBundle()
    {
        // Arrange
        // Subtotal = 3000
        // Bundle 25% = 750
        _cartService._cart = new List<CartItem> 
        {
            new CartItem { Id = "1", PriceCents = 1000 },
            new CartItem { Id = "2", PriceCents = 1000 },
            new CartItem { Id = "3", PriceCents = 1000 }
        };

        // Promo 50% = 1500
        _cartService._appliedPromo = new PromoCode { DiscountType = DiscountType.Percentage, Value = 50 };

        // Act & Assert
        Assert.Equal(1500, _cartService.TotalDiscountCents); // Max(750, 1500)
    }

    [Fact]
    public void EmptyCart_HasZeroPrices()
    {
        // Act & Assert
        Assert.Equal(0, _cartService.SubTotalCents);
        Assert.Equal(0, _cartService.TotalDiscountCents);
        Assert.Equal(0, _cartService.TotalPriceCents);
    }
}
