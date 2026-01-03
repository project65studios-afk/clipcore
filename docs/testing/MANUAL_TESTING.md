# Manual Testing Guide: Multi-Tenancy

This guide details how to verify that ClipCore correctly handles multiple storefronts (tenants) on your local development machine.

## 1. Prerequisites (The "Trick")
Since we are running locally (`localhost`), we need to trick the browser into thinking we are visiting different subdomains. We do this by editing the `hosts` file.

### Edit Hosts File
1.  Open your terminal.
2.  Run: `sudo nano /etc/hosts`
3.  Enter your password if prompted.
4.  Add the following lines to the bottom of the file:
    ```text
    127.0.0.1   project65.clipcore.test
    127.0.0.1   racing.clipcore.test
    127.0.0.1   demo.clipcore.test
    ```
5.  **Save & Exit**: Press `Ctrl+O`, then `Enter`, then `Ctrl+X`.

---

## 2. Verification Scenarios

Ensure the application is running (`dotnet run`).

### Scenario A: The "Production" Store (Project65)
*   **URL**: [http://project65.clipcore.test:5095](http://project65.clipcore.test:5095)
*   **What to Check**:
    1.  **Header**: Top left logo/text should read **"PROJECT65 STUDIOS"**.
    2.  **Content**: You should see your full library of clips (e.g., "Urban Neon", "Midnight Highway").
    3.  **Isolation**: You should **NOT** see any "Drifting" clips.

### Scenario B: The "Client" Store (Speed Racing)
*   **URL**: [http://racing.clipcore.test:5095](http://racing.clipcore.test:5095)
*   **What to Check**:
    1.  **Header**: Top left logo/text should read **"SPEED RACING"**.
    2.  **Content**: You should see **ONLY** the "Formula Drift" event.
    3.  **Isolation**: You should **NOT** see any Project65 clips ("Urban Neon", etc).

### Scenario C: Invalid Store (404)
*   **URL**: [http://invalid.clipcore.test:5095](http://invalid.clipcore.test:5095)
*   **Expected Result**: A 404 "Not Found" page or message saying the store does not exist.

---

## 3. troubleshooting

*   **"Site can't be reached"**: Ensure the app is actually running on port `5095`. Check your terminal output.
*   **"Connection Refused"**: The app might be running on HTTPS only or a different port. Check `launchSettings.json`.
*   **No Branding Change**: If the header always says "ClipCore", the `TenantResolutionMiddleware` might not be detecting the host header correctly. Check the server logs for `[TenantMiddleware] Resolved Subdomain: ...`.
