using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Project65.Web.Models;

namespace Project65.Web.Services;

public class CartService
{
    private readonly ProtectedLocalStorage _storage;
    private List<CartItem> _cart = new();
    private bool _isInitialized = false;

    public event Action? OnChange;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CartService(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _semaphore.WaitAsync();
        try
        {
            if (_isInitialized) return;

            var result = await _storage.GetAsync<List<CartItem>>("cart_items");
            if (result.Success && result.Value != null)
            {
                _cart = result.Value;
            }
            _isInitialized = true;
            NotifyStateChanged();
        }
        catch
        {
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

        if (!_cart.Any(c => c.Id == item.Id))
        {
            _cart.Add(item);
            await SaveCartAsync();
            NotifyStateChanged();
        }
    }

    public async Task RemoveAsync(string id)
    {
        await InitializeAsync();

        var item = _cart.FirstOrDefault(c => c.Id == id);
        if (item != null)
        {
            _cart.Remove(item);
            await SaveCartAsync();
            NotifyStateChanged();
        }
    }

    public async Task ClearAsync()
    {
        _cart.Clear();
        await _storage.DeleteAsync("cart_items");
        NotifyStateChanged();
    }

    public List<CartItem> GetItems() => _cart;
    
    public int Count => _cart.Count;
    
    public long TotalPriceCents => _cart.Sum(c => c.PriceCents);

    private async Task SaveCartAsync()
    {
        try 
        {
            await _storage.SetAsync("cart_items", _cart);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CART-ERROR] Failed to save cart: {ex.Message}");
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
