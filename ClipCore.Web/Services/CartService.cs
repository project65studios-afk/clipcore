using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ClipCore.Web.Models;
using ClipCore.Core.Entities;

namespace ClipCore.Web.Services;

public class CartService
{
    private readonly ProtectedLocalStorage _storage;
    private readonly ILogger<CartService> _logger;
    internal List<CartItem> _cart = new();
    internal bool _isInitialized = false;

    public event Action? OnChange;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    internal PromoCode? _appliedPromo;
    public PromoCode? AppliedPromoCode => _appliedPromo;

    public CartService(ProtectedLocalStorage storage, ILogger<CartService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _semaphore.WaitAsync();
        try
        {
            if (_isInitialized) return;

            var cartResult = await _storage.GetAsync<List<CartItem>>("cart_items");
            if (cartResult.Success && cartResult.Value != null)
            {
                _cart = cartResult.Value;
            }

            var promoResult = await _storage.GetAsync<PromoCode>("applied_promo");
            if (promoResult.Success && promoResult.Value != null)
            {
                _appliedPromo = promoResult.Value;
            }

            _isInitialized = true;
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[CartService] Failed to load cart from storage: {ex.Message}. Resetting cart.");
            _cart = new List<CartItem>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddAsync(CartItem item)
    {
        await InitializeAsync();

        if (!_cart.Any(c => c.Id == item.Id && c.LicenseType == item.LicenseType && c.IsGif == item.IsGif))
        {
            _cart.Add(item);
            await SaveCartAsync();
            NotifyStateChanged();
        }
    }

    public async Task RemoveAsync(string id, LicenseType license, bool isGif = false)
    {
        await InitializeAsync();

        var item = _cart.FirstOrDefault(c => c.Id == id && c.LicenseType == license && c.IsGif == isGif);
        if (item != null)
        {
            _cart.Remove(item);
            await SaveCartAsync();
            NotifyStateChanged();
        }
    }

    public async Task ApplyPromoAsync(PromoCode promo)
    {
        _appliedPromo = promo;
        await _storage.SetAsync("applied_promo", _appliedPromo);
        NotifyStateChanged();
    }

    public async Task RemovePromoAsync()
    {
        _appliedPromo = null;
        await _storage.DeleteAsync("applied_promo");
        NotifyStateChanged();
    }

    public async Task ClearAsync()
    {
        _cart.Clear();
        _appliedPromo = null;
        await _storage.DeleteAsync("cart_items");
        await _storage.DeleteAsync("applied_promo");
        NotifyStateChanged();
    }

    public List<CartItem> GetItems() => _cart;

    public int Count => _cart.Count;

    public long SubTotalCents => _cart.Sum(c => c.PriceCents);

    public bool IsBundleDiscountApplied => Count >= 3;

    public long BundleDiscountCents => IsBundleDiscountApplied ? (long)(SubTotalCents * 0.25) : 0;

    public long PromoDiscountCents => _appliedPromo?.CalculateDiscount(SubTotalCents) ?? 0;

    public long TotalDiscountCents => Math.Max(BundleDiscountCents, PromoDiscountCents);

    public long TotalPriceCents => SubTotalCents - TotalDiscountCents;

    private async Task SaveCartAsync()
    {
        try
        {
            await _storage.SetAsync("cart_items", _cart);
        }
        catch (Exception)
        {
            // Failed to save cart
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
