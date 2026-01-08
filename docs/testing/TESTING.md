# Testing & Environment Guide

This document explains how to switch between Development and Testing modes, and how to perform manual or automated verification of Project65.

## 🏁 Environment Switching

Project65 uses the standard `ASPNETCORE_ENVIRONMENT` variable to toggle between live integrations and mock/test data.

### 1. Development Mode (Default)
Used for your active work with real data and real API keys.
- **Command**: `dotnet run --project Project65.Web/Project65.Web.csproj`
- **Config**: Reads `appsettings.Development.json`
- **Database**: `project65.db`
- **Services**: Uses real `MuxVideoService` and `StripePaymentService`.

### 2. Testing Mode
Used for automated E2E tests or "safe" manual verification where you don't want to spend real Mux credits or process real payments.
- **Command**: `ASPNETCORE_ENVIRONMENT=Testing dotnet run --project Project65.Web/Project65.Web.csproj`
- **Config**: Reads `appsettings.Testing.json`
- **Database**: `project65_test.db` (Clean isolation)
- **Services**: Uses `FakeVideoService` (signed IDs start with `fake_`) and `FakePaymentService`.

---

## 🧪 Testing Strategies

### Automated Smoke Tests (Playwright)
Run the full automated suite to protect the "Money Path."
```bash
dotnet test tests/Project65.E2ETests/Project65.E2ETests.csproj
```
Tests included:
- **Discovery**: Home page loads, search works, and media elements appear.
- **Revenue**: Cart persistence, Promo codes, and Volume Discounts (25% off 3+ items).
- **Checkout**: Stripe redirection pulse.
- **Integrity**: R2 signed thumbnail URLs and Mux hover previews.

### Manual "Money Path" Verification
To test yourself as a user:
1. Start the app in **Testing** mode (see command above).
2. Open `http://localhost:5094`.
3. Select an Event and use the **Quick Add** buttons on clips.
4. Verify the **Cart** updates in the top navigation.
5. In the Cart page:
    - Add 3 items to see the **Volume Discount Unlocked** message.
    - Check the subtotal math.
6. Click **Checkout** and verify you are redirected (in Testing mode, it will typically simulate the redirect).

---

## 🛠 Troubleshooting
- **Port Conflicts**: Ensure only one instance of the app is running (check for existing `dotnet run` processes).
- **Database Sync**: If `project65_test.db` gets corrupted, simply delete it and the app will re-create/re-seed it on the next run in Testing mode.

### Summary Table
Goal	        Command	                  Database	            Services
-------------------------------------------------------------------------
Normal Dev	    dotnet run	              project65.db          Real Mux 
Safe Testing	ASPNETCORE_ENVIRONMENT
                =Testing dotnet run	      project65_test.db     Fake Mux
Auto Suite	    dotnet test	              project65_test.db    Playwright
-------------------------------------------------------------------------