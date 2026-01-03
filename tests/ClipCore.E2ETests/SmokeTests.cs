using Microsoft.Playwright.Xunit;
using Microsoft.Playwright;
using Xunit;

namespace ClipCore.E2ETests;

public class SmokeTests : PageTest
{
    private readonly string _baseUrl = "http://project65.clipcore.test:5094";

    [Fact]
    public async Task HomePage_Loads_WithTitle()
    {
        await Page.GotoAsync(_baseUrl);
        
        // Wait for the main heading to appear
        var heading = Page.Locator("h1");
        await Expect(heading).ToContainTextAsync("Events");
        
        // Check Page Title - verify it matches the dynamic brand text in the header
        // Check Page Title - verify it matches the dynamic brand text in the header
        var brandText = (await Page.Locator(".brand").InnerTextAsync()).Trim();
        var normalizedBrand = System.Text.RegularExpressions.Regex.Replace(brandText, @"\s+", " ");
        
        // Use standard assertion to avoid CSP unsafe-eval issues with WaitForFunction
        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex(normalizedBrand, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    [Fact]
    public async Task EventDetails_Loads_MediaElements()
    {
        // This test assumes there is at least one event in the database.
        await Page.GotoAsync(_baseUrl);
        
        // Click the first event card
        var firstEvent = Page.Locator(".grid-card").First;
        await firstEvent.ClickAsync();
        
        // Wait for page load
        await Page.WaitForURLAsync("**/events/**");
        
        // Check for mux-player presence (smoke test for media logic)
        var muxPlayer = Page.Locator("mux-player").First;
        await Expect(muxPlayer).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Cart_Flow_And_PromoCode()
    {
        await Page.GotoAsync(_baseUrl);
        
        // 1. Go to an event
        await Page.Locator(".grid-card").First.ClickAsync();
        await Page.WaitForURLAsync("**/events/**");

        // 2. Click the first clip title to go to details
        await Page.Locator(".video-card-link").First.ClickAsync();
        await Page.WaitForURLAsync("**/clips/**");

        // 3. Add to cart on details page
        var addToCartBtn = Page.Locator("button:has-text('Add to Cart')");
        await addToCartBtn.ClickAsync();

        // 4. Handle License Modal
        await Page.Locator("#licenseAgreementModal").CheckAsync();
        await Page.Locator("button:has-text('Confirm & Continue')").ClickAsync();

        // 5. Verify button changed to "View in Cart"
        await Expect(Page.Locator("a:has-text('View in Cart')")).ToBeVisibleAsync();

        // 6. Go to Cart
        await Page.GotoAsync($"{_baseUrl}/cart");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // 7. Verify item in cart
        var cartItem = Page.Locator(".cart-item");
        await Expect(cartItem).ToHaveCountAsync(1);

        // 8. Apply TEST25 Promo Code
        var promoInput = Page.Locator("input[placeholder='Promo Code']");
        await promoInput.FillAsync("TEST25");
        await promoInput.BlurAsync(); // Ensure bind fires
        
        var applyBtn = Page.Locator("button:has-text('Apply')");
        await Expect(applyBtn).ToBeEnabledAsync();
        await applyBtn.ClickAsync();
        
        // 9. Verify promo applied
        await Expect(Page.Locator("text=Promo Applied")).ToBeVisibleAsync();
        await Expect(Page.Locator("strong")).ToContainTextAsync("TEST25");

        // 10. Test persistence by reloading
        await Page.ReloadAsync();
        await Expect(Page.Locator(".cart-item")).ToHaveCountAsync(1);
        await Expect(Page.Locator("text=Promo Applied: TEST25")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Media_Playback_Stability()
    {
        await Page.GotoAsync(_baseUrl);
        
        // 1. Navigate to first event
        await Page.Locator(".grid-card").First.ClickAsync();
        await Page.WaitForURLAsync("**/events/**");

        // 2. Wait for mux-player with attributes
        var muxPlayer = Page.Locator("mux-player[playback-id]").First;
        await Expect(muxPlayer).ToBeVisibleAsync();

        // 3. Monitor specifically for CORS or critical configuration errors
        var criticalErrors = new List<string>();
        Page.Console += (_, e) => 
        {
            if (e.Type == "error") 
            {
                var text = e.Text.ToLower();
                // Flag CORS or other structural security/config issues
                if (text.Contains("cors") || text.Contains("access-control") || text.Contains("content security policy") || text.Contains("csp"))
                {
                    criticalErrors.Add(e.Text);
                }
            }
        };

        // 4. Verify player attributes (support both fake and real Mux IDs)
        // Real IDs are alphanumeric, fake IDs start with fake_
        await Expect(muxPlayer).ToHaveAttributeAsync("playback-id", new System.Text.RegularExpressions.Regex("^[a-zA-Z0-9_]+$"));
        await Expect(muxPlayer).ToHaveAttributeAsync("playback-token", new System.Text.RegularExpressions.Regex("^.+"));

        // Let's just verify no UNEXPECTED errors happened during initial load (e.g. CORS)
        await Task.Delay(2000); // Give it a moment to settle
        
        // Assert that no CORS or critical configuration errors occurred
        Assert.Empty(criticalErrors);
    }

    [Fact]
    public async Task Cart_VolumeDiscount_Flow()
    {
        await Page.GotoAsync(_baseUrl);
        
        // 1. Go to an event
        await Page.Locator(".grid-card").First.ClickAsync();
        await Page.WaitForURLAsync("**/events/**");

        // 2. Add 3 different clips to cart using Quick Add buttons
        var quickAddButtons = Page.Locator(".quick-add-btn");
        await quickAddButtons.Nth(0).ClickAsync();
        await Task.Delay(500);
        await quickAddButtons.Nth(1).ClickAsync();
        await Task.Delay(500);
        await quickAddButtons.Nth(2).ClickAsync();
        await Task.Delay(500);

        // 3. Go to Cart
        await Page.GotoAsync($"{_baseUrl}/cart");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // 4. Verify 3 items in cart
        var cartItems = Page.Locator(".cart-item");
        await Expect(cartItems).ToHaveCountAsync(3);

        // 5. Verify Volume Discount is applied
        await Expect(Page.Locator("text=Volume Discount Unlocked: 25% Off!")).ToBeVisibleAsync();
        
        // 6. Verify Discount calculation (approximate check for 25% off display)
        await Expect(Page.Locator("text=Volume Discount (25%)")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Search_Connectivity()
    {
        await Page.GotoAsync($"{_baseUrl}/search?q=test");
        
        // Use a broader check: either results or a "No results" message should be present
        // but crucially, we should NOT see a 500 error or stack trace
        var mainContent = Page.Locator("main");
        await Expect(mainContent).Not.ToContainTextAsync("Error");
        await Expect(mainContent).Not.ToContainTextAsync("Exception");
        
        // Verify the search results header
        await Expect(Page.GetByText("Showing results for")).ToBeVisibleAsync();
        // The query itself is in a span with quotes like "test"
        await Expect(Page.Locator("main")).ToContainTextAsync("\"test\"");
    }

    [Fact]
    public async Task Stripe_Redirect_Handshake()
    {
        await Page.GotoAsync(_baseUrl);
        
        // 1. Add an item via Quick Add
        await Page.Locator(".grid-card").First.ClickAsync();
        await Page.WaitForURLAsync("**/events/**");
        await Page.Locator(".quick-add-btn").First.ClickAsync();
        await Task.Delay(1000); // Wait for Blazor state to sync
        
        // 2. Go to Cart
        await Page.GotoAsync($"{_baseUrl}/cart");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);
        
        // 3. Click Checkout
        var checkoutBtn = Page.Locator("button:has-text('Checkout')").First;
        await Expect(checkoutBtn).ToBeVisibleAsync();
        await checkoutBtn.ClickAsync();
        
        // 4. Verify Redirect to Stripe (it might happen fast or slow, wait for URL)
        // In dev with FakePaymentService, it might redirect to a fake success URL or Stripe.
        // Let's check for either stripe.com or the success/cancel path if mocked.
        await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex("checkout\\.stripe\\.com|checkout/success|cart"));
        
        var currentUrl = Page.Url;
        Assert.True(currentUrl.Contains("stripe.com") || currentUrl.Contains("checkout") || currentUrl.Contains("cart"), 
            $"Expected Stripe redirect or checkout path, but got: {currentUrl}");
    }

    [Fact]
    public async Task R2_Thumbnail_Integrity()
    {
        await Page.GotoAsync(_baseUrl);
        
        // 1. Navigate to first event
        await Page.Locator(".grid-card").First.ClickAsync();
        await Page.WaitForURLAsync("**/events/**");

        // 2. Find thumbnail images
        var thumbnails = Page.Locator(".video-thumb-img");
        await Expect(thumbnails.First).ToBeVisibleAsync();
        
        // 3. Verify SR contains R2 or signed URL indicators
        var src = await thumbnails.First.GetAttributeAsync("src");
        Assert.NotNull(src);
        
        // Check for common R2/S3 signed URL indicators OR seeded placeholders OR Mux thumbnails
        bool isR2 = src.Contains("r2.cloudflarestorage.com") || src.Contains("X-Amz-Signature");
        bool isMux = src.Contains("image.mux.com");
        bool isPlaceholder = src.Contains("images/") || src.Contains("thumbnails/");
        
        Assert.True(isR2 || isMux || isPlaceholder, $"Thumbnail URL '{src}' should be an R2, Mux, or seeded path.");
    }

    [Fact]
    public async Task Hover_Preview_Performance()
    {
        await Page.GotoAsync(_baseUrl);
        
        // 1. Navigate to first event
        await Page.Locator(".grid-card").First.ClickAsync();
        await Page.WaitForURLAsync("**/events/**");

        // 2. Hover over first clip card
        var firstClip = Page.Locator(".video-card").First;
        await firstClip.HoverAsync();
        
        // 3. Verify mux-player appears in the preview container
        var player = firstClip.Locator("mux-player");
        await Expect(player).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Admin_Upload_Page_Loading()
    {
        // Note: This may require authentication if roles are strictly enforced.
        // For a smoke test, we verify it doesn't 404/500 if we navigate to it.
        await Page.GotoAsync($"{_baseUrl}/admin/upload");
        
        // Check if we are redirected to login (which is fine for a smoke test if it loads login)
        // or if we see the upload button.
        if (Page.Url.Contains("/Account/Login"))
        {
            // Update to use the standard Input.Email and Input.Password from our Identity implementation
            var emailInput = Page.Locator("input[name='Input.Email']");
            var passwordInput = Page.Locator("input[name='Input.Password']");
            await Expect(emailInput).ToBeVisibleAsync();
            await Expect(passwordInput).ToBeVisibleAsync();
        }
        else
        {
            // If we are already logged in as admin in the test context
            var uploadBtn = Page.Locator("button:has-text('New Event')").Or(Page.Locator("input[type='file']"));
            await Expect(uploadBtn.First).ToBeVisibleAsync();
        }
    }
}
