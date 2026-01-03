# Platform Cost Analysis (The "Balanced" Plan)

This is the recommended architecture that balances **Developer Experience (Function)** with **Price**.

## The Strategy
1.  **Compute**: Don't use Lightsail Containers (they are slow to deploy and don't auto-scale well). Use **AWS App Runner**. It handles load balancing, SSL, and scaling automatically.
2.  **Database**: Don't pay for RDS yet. Use **Lightsail Managed DB**. It's the same Postgres engine but half the price.
3.  **Storage**: Keep **Cloudflare R2**. It's non-negotiable for video profitability.

## Fixed Costs Breakdown

| Component | Service | Cost | Why this balance? |
| :--- | :--- | :--- | :--- |
| **App Compute** | **AWS App Runner** | **$15 - $25** | **Function Win**: Zero-downtime deploys, auto-to-zero scaling, logs included. Much better than managing a Lightsail instance. |
| **Database** | **AWS Lightsail DB** | **$15.00** | **Cost Win**: Robust Managed Postgres. Connecting App Runner (VPC) to Lightsail is easy (VPC Peering). Saves ~$20/mo vs RDS. |
| **Domain/DNS** | **Cloudflare** | **Free** | Industry standard performance. |
| **Email** | **Postmark** | **$10.00** | Best deliverability. |
| **Total** | | **~$45 / month** | *A perfect middle ground.* |

## Comparison

| Feature | Lightsail (Saver) | **Balanced Plan** | Pure AWS (Enterprise) |
| :--- | :--- | :--- | :--- |
| **Monthly Cost** | ~$32 | **~$45** | ~$100+ |
| **Auto-Scaling** | Manual | **Automatic** | Automatic |
| **Deploy Speed** | Slow | **Fast** | Fast |
| **Video Profit** | High (R2) | **High (R2)** | Low (S3 Fees) |

## Recommendation
**Go with the Balanced Plan.**
For an extra **$13/month**, you get auto-scaling and a much better deployment pipeline (App Runner) while still keeping the database and storage costs low.

## Next Steps
This architecture decision affects **Deployment** (DevOps), not **Development** (Code).
The code we are writing (Middleware, Multi-Tenancy) works identically on all three plans.

**Ready to build the Middleware?**
