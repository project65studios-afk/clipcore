# AWS Deployment Guide: Project65 (Beginner's Edition)

Welcome! This guide is designed for **absolute beginners**. We will take the Project65 web application from your computer to the AWS Cloud, step-by-step.

We will focus on **keeping costs low** (approx. $22/month) while using professional, production-ready services.

---

## 📚 Glossary for Humans (Read this first!)

- **AWS Console**: The website where you log in to manage your cloud servers.
- **Region**: The physical location of the data center. We will use **`us-east-1` (N. Virginia)** because it has the most features and often the lowest prices.
- **RDS (Relational Database Service)**: A managed database cloud. Instead of running a database on your laptop, AWS runs it for you, backs it up, and keeps it safe.
- **App Runner**: The magic "runner" for your code. You give it your code, and it runs it. It handles SSL (https://) and scaling automatically.
- **Parameter Store**: A secure vault for your detailed secrets (like API keys) so they aren't written in plain text.
- **CI/CD (Continuous Integration/Deployment)**: A robot that watches your code. When you save changes, the robot automatically updates your live website.

## 🧠 Understanding Configuration: Where does it go?

Your application needs different settings for different environments. Here is the simple breakdown:

1.  **The Switch (Environment Variables)**
    *   **Where**: App Runner Console.
    *   **What**: `ASPNETCORE_ENVIRONMENT` = `Production`.
    *   **Why**: Tells the app to "Act like Production" (Use PostgreSQL, send real emails).

2.  **The Secrets (Parameter Store)**
    *   **Where**: AWS Systems Manager (The Vault).
    *   **What**: Passwords, API Keys, Connection Strings.
    *   **Why**: **Security**. We never write passwords in code.

3.  **The Defaults (appsettings.json)**
    *   **Where**: In your source code.
    *   **What**: Boring stuff like Logging levels.
    *   **Why**: Safe to be public.

## 💰 The Plan (Budget: ~$22/mo)

We are building a "Cost-Optimized" infrastructure:

1.  **Database**: Amazon RDS (PostgreSQL) - The brain of your data. (~$15/mo)
2.  **Web Server**: AWS App Runner - The actual website. (~$7/mo, scales to $0 when no one visits)
3.  **Secrets**: AWS Parameter Store - **FREE** security.
4.  **CDN**: Amazon CloudFront - **FREE** global speed boost.

---

## 🌐 Step 0: Buy Your Domain (Optional but Recommended) ✅ Completed

Using a custom domain (like `project65video.com`) looks professional and is easier to manage if bought in AWS.

1.  Log in to the **AWS Console** and search for **Route 53**.
2.  Click **Registered domains** on the left menu.
3.  Click the orange **Register Domain** button.
4.  Search for your desired name (e.g., `project65video.com`).
5.  Add to cart and complete the purchase (~$12-15/year).
    *   *Note: Using AWS Route 53 simplifies connecting your specific "App Runner" service later because AWS handles the DNS automatically.*

---

## 🚀 Step 1: Create the Database (RDS) ✅ Completed

1.  Log in to the [AWS Console](https://console.aws.amazon.com/).
2.  In the top search bar, type `RDS` and click **RDS**.
3.  Click the orange **Create database** button.
4.  **Choose a database creation method**: Select `Standard create`.
5.  **Engine options**: Select `PostgreSQL`.
6.  **Templates**: Select `Free tier` (if available) or `Production` if not.
7.  **DB Instance Identifier**: Type `project65-db`.
8.  **Master username**: Type `postgres`.
9.  **Master password**: Type a strong password (write this down!).
10. **Instance configuration**: Select `Burstable classes` -> `db.t4g.micro` (This is the cheap, good one).
11. **Connectivity**:
    - **Public access**: `No`. (Secure!)
    - **VPC Security Group**: `Create new`. Name it `project65-db-sg`.
12. Click **Create database** (It takes about 10 minutes to start).

> **Save this for later**: Once created, click on the database name and copy the **Internal Endpoint** (it looks like `project65-db.xyz.us-east-1.rds.amazonaws.com`).

---

## 🔐 Step 2: Secure Your Secrets (Parameter Store) ✅ Completed

Don't put passwords in your code! We'll put them in the vault.

1.  Search for **Systems Manager** in the top bar.
2.  On the left menu, scroll down to **Application Management** and click **Parameter Store**.
3.  Click **Create parameter**.
4.  **Name**: `/project65/ConnectionStrings/DefaultConnection`
5.  **Type**: `SecureString`
6.  **Value**: 
    ```text
    Host=YOUR_RDS_ENDPOINT;Database=project65;Username=postgres;Password=YOUR_PASSWORD
    ```
    (Replace `YOUR_RDS_ENDPOINT` and `YOUR_PASSWORD` with values from Step 1).
7.  Click **Create parameter**.

**Repeat for these other keys:**
- Name: `/project65/Stripe/SecretKey` -> Value: `sk_test_...`
- Name: `/project65/Mux/TokenId` -> Value: `...`
- Name: `/project65/Mux/TokenSecret` -> Value: `...`
- Name: `/project65/SEED_ADMIN_PASSWORD` -> Value: `YourStrongPasswordHere!` (For Admin Account Creation)

---

## 🔑 Step 2.1: Google Login Setup (Optional)

To allow users to sign in with Google, you need to create credentials and add them to the Parameter Store.

1.  **Get Credentials**:
    *   Go to **[Google Cloud Console](https://console.cloud.google.com/apis/credentials)**.
    *   Create a Project (e.g. `Project65`).
    *   Click **Create Credentials** -> **OAuth Client ID**.
    *   **Application Type**: Web Application.
    *   **Authorized Redirect URIs**:
        *   `https://localhost:7192/signin-google` (Local testing)
        *   `https://project65video.com/signin-google` (Production - Root Domain)
        *   `https://www.project65video.com/signin-google` (Production - WWW Domain - **CRITICAL**)
        *   `https://[YOUR_APP_RUNNER_URL].awsapprunner.com/signin-google` (Production fallback)
    *   Copy the **Client ID** and **Client Secret**.

2.  **Add to AWS Parameter Store**:
    *   Create `/project65/Google/ClientId` -> Value: `[Your Client ID]` (Type: `SecureString`)
    *   Create `/project65/Google/ClientSecret` -> Value: `[Your Client Secret]` (Type: `SecureString`)

*The application will automatically enable Google Login once these keys are present.*

---

## 🐳 Step 2.5: Containerize with Docker (Optional but Recommended) ✅ Completed

"Containerizing" means wrapping your app in a box (Container) so it runs exactly the same on your laptop as it does in the cloud. We have included a `Dockerfile` in the project.

### How to use it:

1.  **Install Docker Desktop**: Download and install it on your computer.
2.  **Build the Image**: Open your terminal in the project folder and run:
    ```bash
    docker build -t project65-local .
    ```
3.  **Run it Locally**:
    ```bash
    docker run -p 8080:8080 project65-local
    ```
4.  **Visit**: Go to `http://localhost:8080` in your browser.

**Why do this?** AWS App Runner uses this exact same process to run your site. If it works in Docker on your machine, it will work on AWS.

---

## ☁️ Step 3: Configure The Web Server (App Runner) ✅ Completed

1.  Search for **App Runner**.
2.  Click **Create service**.
3.  **Source**: Select `Source code repository`.
4.  **Connect GitHub**: Follow the prompts to authorize AWS to see your repo.
5.  **Repository**: Select `project65`. Branch: `main`.
6.  **Configuration**: Select `Configure all settings here`.
7.  **Runtime**: Select `Docker`.
8.  **Build settings**:
    - **Port**: `8080` (This is important for .NET).
9.  Click **Next**.
10. **Service name**: `project65-web`.
11. **Environment variables**: Use the "SSM Parameter" reference syntax (e.g. `Amazon.Extensions.Configuration.SystemsManager` setup in code) OR just manually add them here if you want to be quick for now:
    - Key: `ASPNETCORE_ENVIRONMENT` -> Value: `Production`
    - Key: `AllowedOrigins__0` -> Value: `https://your-app-runner-url.awsapprunner.com` (Add this AFTER you get your URL!)
    - Key: `AllowedOrigins__1` -> Value: `https://your-custom-domain.com` (Optional)

    > **Why is "AllowedOrigins" needed?** 
    > Think of your R2 Storage as a secure vault. By default, it blocks everyone. When you deploy, your new website address (e.g., `https://myapp.awsapprunner.com`) is a stranger. You must add it to this list so the app can introduce itself to the vault and say "It's okay to show images to me!"

12. Click **Next** -> **Create & deploy**.

*Grab a coffee ☕. It takes about 5 minutes for your site to go live.*

---

## 💳 Step 3.5: Finish Stripe Setup (The "Chicken and Egg" Problem)

You couldn't set up the Webhook **before** proper deployment because Stripe needs your **URL**. Now that you have it:

1.  **Copy your App Runner URL** (e.g., `https://xyz.awsapprunner.com`).
2.  Go to **[Stripe Dashboard > Developers > Webhooks](https://dashboard.stripe.com/test/webhooks)**.
3.  Click **Add Endpoint**.
4.  **Endpoint URL**: Paste your URL and add `/api/webhooks/stripe` to the end.
    *   Example: `https://xyz.awsapprunner.com/api/webhooks/stripe`
5.  **Select events**:
    *   `checkout.session.completed` (Crucial for fulfilling orders!)
6.  Click **Add endpoint**.
7.  **Reveal Signing Secret**: Look for `Current secret` (starts with `whsec_...`). Copy it.
8.  **Update AWS**:
    *   Go back to **AWS Parameter Store**.
    *   Create a new parameter: `/project65/Stripe/WebhookSecret`.
    *   Value: Paste the `whsec_...` key.
    *   **Restart App Runner**: Go to App Runner service -> "Actions" -> "Deploy" (to pick up the new secret).

---

## 🔒 Step 3.6: Allow Your Domain in Cloudflare R2 (CORS) ✅ Completed

Your images/videos are stored in Cloudflare R2. By default, it might block your new website from displaying them.

1.  Log in to the **[Cloudflare Dashboard](https://dash.cloudflare.com/)**.
2.  Go to **R2** -> Select your bucket (`project65-files` or similar).
3.  Click **Settings** tab.
4.  Scroll down to **CORS Policy**.
5.  Click **Edit CORS Policy** and ensure your new domains are listed. It should look like this:

```json
[
  {
    "AllowedOrigins": [
      "http://localhost:5094",
      "https://project65video.com",
      "https://www.project65video.com",
      "https://your-app-runner-url.awsapprunner.com" 
    ],
    "AllowedMethods": [
      "GET",
      "PUT",
      "POST",
      "DELETE",
      "HEAD"
    ],
    "AllowedHeaders": [
      "*"
    ]
  }
]
```
6.  Click **Save**.

---

## 🔄 Step 4: Automate It (GitHub Actions) ✅ Completed

We have set up a fully automated pipeline so you don't need to manually deploy ever again.

👉 **[Read the Full Automation Guide](AUTOMATION_GUIDE.md)**

**Summary:**
*   **Trigger**: Just run `git push` to `main`.
*   **Mechanism**: GitHub Actions builds your Docker image and updates AWS App Runner.
*   **Status**: Check the **Actions** tab in your GitHub repo.



---

## 📧 Step 6: Email Deliverability (SES Sandbox)

**CRITICAL:** By default, AWS places all new SES accounts in a **Sandbox**.

**Restrictions:**
1.  You can **only send TO verified email addresses** (e.g., your own). Customer emails will fail.
2.  You have a limit of 200 emails per day.

**How to Fix (Request Production Access):**
1.  Log in to the **AWS Console** and search for **Amazon SES**.
2.  Go to **Account Dashboard** (left menu).
3.  Look for the yellow banner saying "Your account is in the sandbox."
4.  Click **Request production access** (or "Edit account details").
5.  **Fill out the form**:
    *   **Mail Type**: Transactional.
    *   **Website URL**: Your App Runner URL (e.g., `https://xyz.awsapprunner.com`).
    *   **Use Case Description**: 
        > "I am building a video storefront where users buy digital clips. The system sends them a receipt and a secure download link immediately after checkout. We only send requested transactional emails to customers who have made a purchase."
6.  Click **Submit**.
    *   *Note: Approval usually takes 24 hours. Until then, you can only test with your own verified email.*

---

## 📈 Step 7: Monitoring (Where are my logs?)

Once your app is in the cloud, you can't see the console window anymore. Here is how you debug issues:

1.  Go to **[AWS App Runner Console](https://console.aws.amazon.com/apprunner/home)**.
2.  Click on your service (`project65-web`).
3.  Click the **Logs** tab.
4.  **Event Logs**: Shows you deployment steps ("Pulling image", "Starting container"). Check here if your deployment fails.
5.  **Application Logs**: Click "View in CloudWatch". This shows you your standard `Console.WriteLine` output!
    *   This is where you will see "User X logged in" or "Exception: Database connection failed".

---

## 🆘 Troubleshooting: "Help, it broke!"

**Problem**: The site says "502 Bad Gateway".
**Fix**: Did you set the Port to **8080**? Blazor on Docker usually needs 8080 or 80. Check your `Dockerfile` and App Runner settings.

**Problem**: "Database connection failed".
**Fix**: Go to specific RDS Security Group (created in Step 1) and add an **Inbound Rule** allowing traffic from "Anywhere" (temporary test) or from the App Runner VPC security group (production fix).

**Problem**: "Videos aren't playing".
**Fix**: Check your Browser Console (F12). If you see "CORS" errors, make sure you added your new App Runner URL (e.g., `https://xyz.awsapprunner.com`) to your Mux and R2 Allowed Origins.

---

## 🎉 You did it!

Your app is now cloud-native.
- **URL**: Found on the App Runner dashboard.
- **Updates**: Push to main, wait 5 mins, refresh.
- **Cost**: Check "Billing Dashboard" occasionally to ensure you stay near $22.

**Need help?** Check the main specific docs or ask an AI helper "How do I fix X in AWS App Runner?".
