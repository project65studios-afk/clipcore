# Identity Implementation Overview

## Architecture

The Identity system in Project65 is built on **ASP.NET Core Identity** using Scaffolding for the UI, integrated into a **Blazor Server** application.

### Scaffolding Strategy
We have scaffolded specific pages within `Areas/Identity/Pages/Account/Manage` to customize the user experience while relying on the default implementation for logic where possible.

**Key Customizations:**
*   **Layout Integration**: Identity pages use `_ManageLayout.cshtml`, which wraps the content in a consistent "glassmorphism" container (`.identity-container`) and includes the sidebar.
*   **Navigation**: A custom `_ManageNav.cshtml` partial replaces the default, providing path-based active link detection and Bootstrap Icons.
*   **Styling**: `app.css` overrides Bootstrap defaults to match the application's dark theme, using translucent backgrounds and custom form inputs.

## Security Configurations

### Antiforgery Token Validation
To ensure secure form submissions within the Blazor/Razor hybrid environment, we enforced explicit Antiforgery token generation:
1.  **Tag Helpers**: Explicitly registered `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` in `_ViewImports.cshtml` to activate the Form Tag Helper.
2.  **Explicit Injection**: Used `@Html.AntiForgeryToken()` in `Index`, `Email`, `ChangePassword`, and `SetPassword` forms to guarantee token presence.
3.  **Middleware Defaulting**: Reverted custom `AntiforgeryOptions` in `Program.cs` to default settings to avoid conflicts between strict header requirements and standard form token submission.

### Content Security Policy (CSP)
The application implements a strict CSP in `Program.cs`. For Identity pages:
*   **Fonts**: Allowed `https://fonts.gstatic.com` and `https://cdn.jsdelivr.net` (Bootstrap Icons).
*   **Scripts**: Allowed `jquery` and `jquery-validation` from `cdn.jsdelivr.net` (and local fallbacks).
*   **Subresource Integrity (SRI)**: Note that strict SRI checks for `bootstrap-icons` were relaxed to resolve persistent cross-origin blocking issues during development.

## Asset Management

Identity pages are server-rendered (Razor Pages) and require specific static assets not loaded by Blazor's router:
*   **jQuery**: Explicitly injected into `_ManageLayout.cshtml` (before `blazor.web.js`) to support unobtrusive validation.
*   **Validation**: `jquery.validate` and `jquery.validate.unobtrusive` are loaded in the `Scripts` section of the layout.

## Validated Flows
*   **Profile Update**: Phone number limits and basic profile data.
*   **Email Management**: Displaying confirmed email and requesting changes.
*   **Password Management**: Changing existing password or setting a local password for external logins.
