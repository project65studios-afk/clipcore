# Automated Deployment (GitHub Actions)

This guide explains how the automated CI/CD pipeline works for Project65.

## 🔄 Overview

We use **GitHub Actions** to automatically deploy your application to AWS App Runner whenever you push code to the `main` branch.

### How it Works

1.  **Workflow File**: `.github/workflows/deploy.yml` is the "recipe" for deployment.
2.  **Trigger**: It watches for any `git push` to the `main` branch.
3.  **Process**:
    *   **Check out code**: Gets your latest changes.
    *   **Authentication**: Logs in to AWS using the dedicated `github-actions-deployer` IAM user.
    *   **Build**: Creates a new Docker container from your code.
    *   **Push**: Uploads the container to Amazon ECR (Elastic Container Registry) with two tags:
        *   `commit-sha`: For history/rollback (e.g., `a1b2c3d`).
        *   `latest`: The active version used by App Runner.
    *   **Deploy**: Triggers AWS App Runner to restart safely using the new `latest` image.

---

## ⚙️ Configuration

### 1. The Workflow File
The file is located at: `/.github/workflows/deploy.yml`.

### 2. AWS Permissions
The automation uses a special "Robot User" in AWS named `github-actions-deployer`. This user has restricted permissions only for:
*   Pushing images to ECR.
*   Starting App Runner deployments.

### 3. GitHub Secrets
For the automation to work, your GitHub Repository holds the credentials for the robot user.
(Settings -> Secrets and variables -> Actions)

*   `AWS_ACCESS_KEY_ID`: `AKIA...`
*   `AWS_SECRET_ACCESS_KEY`: `...` (Hidden)

---

## 🚀 How to Deploy

You don't need to run any manual commands anymore!

1.  **Make changes** to your code locally.
2.  **Commit and Push**:
    ```bash
    git add .
    git commit -m "My awesome update"
    git push
    ```
3.  **Monitor**:
    *   Go to your GitHub Repository page.
    *   Click the **Actions** tab.
    *   Watch the deployment turn **Green** ✅.
4.  **Verify**:
    *   Visit your site (e.g., `https://project65video.com`).

---

## 🆘 Troubleshooting

**If the Action fails (Red X):**
1.  Click on the failed run to see the logs.
2.  Common issues:
    *   **"Service not in RUNNING state"**: You pushed too fast! An update was already valid. Wait 5 mins and try again.
    *   **"Credentials invalid"**: You might need to rotate the IAM keys (rare).

**Manual Fallback:**
If automation is completely broken, you can always deploy manually from your local machine:

1.  **Authenticate**:
    ```bash
    aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 016981601583.dkr.ecr.us-east-1.amazonaws.com
    ```
2.  **Build & Push**:
    ```bash
    docker build -t project65-web .
    docker tag project65-web:latest 016981601583.dkr.ecr.us-east-1.amazonaws.com/project65-web:latest
    docker push 016981601583.dkr.ecr.us-east-1.amazonaws.com/project65-web:latest
    ```
3.  **Deploy**:
    *   Because `auto_deployments_enabled = true` is set in your Terraform, App Runner *should* automatically detect the new image digest and start deploying within minutes.
    *   **Force Deployment** (If it doesn't start or you are in a hurry):
        ```bash
        # Get the Service ARN
        SERVICE_ARN=$(aws apprunner list-services --query "ServiceSummaryList[?ServiceName=='project65-web-v2'].ServiceArn" --output text)
        
        # Start the deployment
        aws apprunner start-deployment --service-arn $SERVICE_ARN --region us-east-1
        ```
    *   *Note: We use AWS CLI here instead of Terraform. Terraform is for managing infrastructure structure (e.g., resizing CPU), not for triggering routine code deployments.*
