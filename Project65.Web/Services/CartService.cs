using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Project65.Core.Entities;

namespace Project65.Web.Services;

public class CartService
{
    private readonly ProtectedLocalStorage _storage;
    private List<Clip> _cart = new();
    private bool _isInitialized = false;

    public event Action? OnChange;

    public CartService(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var result = await _storage.GetAsync<List<Clip>>("cart_items");
            if (result.Success && result.Value != null)
            {
                _cart = result.Value;
            }
        }
        catch
        {
            // If storage fails or data is corrupt, start with empty cart
        }
        finally
        {
            _isInitialized = true;
            NotifyStateChanged();
        }
    }

    public async Task AddAsync(Clip clip)
    {
        await InitializeAsync();

        if (!_cart.Any(c => c.Id == clip.Id))
        {
            _cart.Add(clip);
            await SaveCartAsync();
            NotifyStateChanged();
        }
    }

    public async Task RemoveAsync(Clip clip)
    {
        await InitializeAsync();

        var item = _cart.FirstOrDefault(c => c.Id == clip.Id);
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

    public List<Clip> GetItems() => _cart;
    
    public int Count => _cart.Count;
    
    public long TotalPriceCents => _cart.Sum(c => c.PriceCents);

    private async Task SaveCartAsync()
    {
        await _storage.SetAsync("cart_items", _cart);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
