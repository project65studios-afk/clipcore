using Microsoft.Playwright.Xunit;
using Microsoft.Playwright;
using Xunit;

namespace ClipCore.E2ETests;

public class TenantIsolationTests : PageTest
{
    private readonly string _p65Url = "http://project65.clipcore.test:5094";
    private readonly string _racingUrl = "http://racing.clipcore.test:5094";

    [Fact(Skip = "Skipping due to .test TLD cookie restrictions in dev environment")]
    public async Task CrossSubdomain_SSO_Works()
    {
        // 1. Login at Project65
        await Page.GotoAsync($"{_p65Url}/Account/Login");
        await Page.FillAsync("input[name='Input.Email']", "owner@project65.com");
        await Page.FillAsync("input[name='Input.Password']", "Admin123!");
        await Page.ClickAsync("button[type='submit']");
        
        // Wait for redirect back to home (might have trailing slash or query params)
        await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex($"{_p65Url}/?.*"));
        
        // 2. Head over to Racing (different subdomain, same base domain)
        await Page.GotoAsync(_racingUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // 3. Verify we are still logged in (should see Logout button, not Login)
        // Using .First to avoid strict mode violation if mobile/desktop headers both exist
        var logoutBtn = Page.Locator("button:has-text('Logout')").First;
        await Expect(logoutBtn).ToBeVisibleAsync();
        
        var loginLink = Page.Locator("a:has-text('Login')");
        await Expect(loginLink).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Tenant_AdminAccess_IsRestricted()
    {
        // 0. Warm up - the first request can sometimes timeout during dev
        await Page.GotoAsync(_p65Url);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 1. Login as Project65 Owner
        await Page.GotoAsync($"{_p65Url}/Account/Login");
        await Page.FillAsync("input[name='Input.Email']", "owner@project65.com");
        await Page.FillAsync("input[name='Input.Password']", "Admin123!");
        await Page.ClickAsync("button[type='submit']");
        
        // Ensure redirect happens
        await Page.WaitForURLAsync(url => url.Contains(_p65Url) && !url.Contains("Login"));

        // 2. Try to access Project65 Admin (Should work)
        await Page.GotoAsync($"{_p65Url}/admin");
        await Expect(Page.Locator("h1")).ToContainTextAsync("Admin Portal");

        // 3. Try to access Racing Admin (Should fail with Access Denied UI)
        await Page.GotoAsync($"{_racingUrl}/admin");
        
        // Verified: The system shows "Access Denied" for unauthorized portal access
        await Expect(Page.Locator("h1")).ToContainTextAsync("Access Denied");
        await Expect(Page.Locator("h1")).Not.ToContainTextAsync("Admin Portal");
    }

    [Fact]
    public async Task Data_Isolation_Works()
    {
        // 1. Go to Racing - should see Formula Drift (from seed)
        await Page.GotoAsync(_racingUrl);
        // Specifically look for the card with this text
        await Expect(Page.Locator(".grid-card", new() { HasTextString = "Formula Drift" })).ToBeVisibleAsync();

        // 2. Go to Project65 - should NOT see Formula Drift
        await Page.GotoAsync(_p65Url);
        // Verify count of cards with this specific text is zero
        await Expect(Page.Locator(".grid-card", new() { HasTextString = "Formula Drift" })).ToHaveCountAsync(0);
        
        // Should see its own events instead
        await Expect(Page.Locator(".grid-card").First).ToContainTextAsync("LA Night Run");
    }
}
