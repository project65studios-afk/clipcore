# Blazor Server on AWS App Runner: Constraints & Strategy

## The Constraint: Sticky Sessions
Blazor Server maintains a stateful connection ("Circuit") with the client. It relies on two things:
1.  **WebSockets** (Preferred for performance).
2.  **Sticky Sessions** (Required for scaling): If a user disconnects, they must reconnect to the *same* server that holds their state in RAM.

**AWS App Runner does NOT support sticky sessions.**
Running multiple instances of Blazor Server on App Runner leads to random disconnections and "Circuit Validation" errors because requests bounce between servers randomly.

## Our Solution: The "Start-Up" Config
To run reliably on App Runner, we apply strict constraints:

### 1. Single Instance Only
We lock the infrastructure to **1 Instance Max**.
- **Pros**: Solves the "Sticky Session" problem (there is only one server).
- **Cons**: Limited scalability. Can handle ~100-300 concurrent active users.
- **Cost**: Low (~$5 - $35/mo).

### 2. Force Long Polling
We explicitly disable WebSockets or accept Long Polling fallbacks.
- **Why**: App Runner's Envoy proxy often drops or blocks WebSocket upgrades. Long Polling is robust HTTP requests.
- **Impact**: Slightly higher latency, more network traffic, but **stable behavior**.

## The Upgrade Path: When to Switch?
You should move to **AWS Elastic Beanstalk** or **ECS Fargate** when:
1.  **Revenue**: Site generates ~$500/mo (covers the higher infrastructure floor price).
2.  **Traffic**: You sustain >100 concurrent users active at once.
3.  **Performance**: Users complain about "Reconnecting..." messages.

### Migration Overview (Future)
When upgrading to Elastic Beanstalk:
1.  **Load Balancer**: You will provision an Application Load Balancer (ALB).
2.  **Stickiness**: You will enable "Stickiness" (Cookies) on the ALB Target Group.
3.  **Scaling**: You can then run 2, 5, or 10 instances freely.
4.  **WebSockets**: ALB supports native WebSockets.

---

## Technical Implementation Details
**File**: `main.tf`
```hcl
instance_configuration {
    # ...
}
# Future Auto-Scaling Configuration would need to be locked here
```

**File**: `Project65.Web/Components/App.razor`
```html
<script src="@Assets["_framework/blazor.web.js"]" autostart="false"></script>
<script>
    Blazor.start({
        circuit: {
            configureSignalR: function (builder) {
                // Force Long Polling to prevent "WebSocket failed" errors in console
                builder.withUrl("/_blazor", {
                    transport: 4 // HttpTransportType.LongPolling
                });
            }
        }
    });
</script>
```
