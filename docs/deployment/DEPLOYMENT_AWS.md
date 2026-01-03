# AWS Deployment Guide: ClipCore (Beginner's Edition)

Welcome! This guide is designed for **absolute beginners**. We will take the ClipCore web application from your computer to the AWS Cloud, step-by-step.

We will focus on **keeping costs low** (approx. $22/month) while using professional, production-ready services.

---

## 📚 Glossary for Humans (Read this first!)

- **AWS Console**: The website where you log in to manage your cloud servers.
- **Region**: The physical location of the data center. We will use **`us-east-1` (N. Virginia)** because it has the most features and often the lowest prices.
- **RDS (Relational Database Service)**: A managed database cloud. Instead of running a database on your laptop, AWS runs it for you, backs it up, and keeps it safe.
- **App Runner**: The magic "runner" for your code. You give it your code, and it runs it. It handles SSL (https://) and scaling automatically.
- **Parameter Store**: A secure vault for your detailed secrets (like API keys) so they aren't written in plain text.
- **CI/CD (Continuous Integration/Deployment)**: A robot that watches your code. When you save changes, the robot automatically updates your live website.

---

## 💰 The Plan (Budget: ~$22/mo)

We are building a "Cost-Optimized" infrastructure:

1.  **Database**: Amazon RDS (PostgreSQL) - The brain of your data. (~$15/mo)
2.  **Web Server**: AWS App Runner - The actual website. (~$7/mo, scales to $0 when no one visits)
3.  **Secrets**: AWS Parameter Store - **FREE** security.
4.  **CDN**: Amazon CloudFront - **FREE** global speed boost.

---

## 🚀 Step 1: Create the Database (RDS)

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

## 🔐 Step 2: Secure Your Secrets (Parameter Store)

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

---

## 🐳 Step 2.5: Containerize with Docker (Optional but Recommended)

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

## ☁️ Step 3: Configure The Web Server (App Runner)

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

## 🔄 Step 4: Automate It (GitHub Actions)

We want the site to update automatically when you push code.

1.  In your project, create a file: `.github/workflows/deploy.yml`.
2.  Paste this content:

```yaml
name: Deploy to Production
on:
  push:
    branches: [ "main" ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Deploy to App Runner
        uses: awslabs/amazon-app-runner-deploy@v1
        with:
          service: project65-web
          region: us-east-1
          # You need to set AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY 
          # in your GitHub Repo Settings -> Secrets
```

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
