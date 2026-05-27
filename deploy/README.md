# Deploying Credo CMS

This directory contains the Bicep template and deployment guide for provisioning
the Azure resources Credo CMS needs in production.

---

## Prerequisites

- An Azure subscription with sufficient quota.
- The Azure CLI (`az`) — version 2.55+.
- Permission to create resource groups (Owner or Contributor + User Access
  Administrator on the subscription).
- A strong SQL administrator password meeting Azure SQL's requirements
  (12+ chars, upper, lower, digit, symbol).

---

## One-Time Setup

```bash
# Sign in
az login

# Pick your subscription
az account set --subscription "Your Subscription Name"

# Create a resource group (region of your choice)
az group create \
  --name credocms-rg \
  --location centralus
```

---

## Deploy

1. Copy `parameters.example.json` to `parameters.json` and fill in real values
   (especially `sqlAdminPassword` and, if you have one, `customDomain`):

   ```bash
   cp parameters.example.json parameters.json
   $EDITOR parameters.json
   ```

2. Run the deployment:

   ```bash
   az deployment group create \
     --resource-group credocms-rg \
     --template-file main.bicep \
     --parameters @parameters.json
   ```

   First run takes 5–8 minutes. Resources created:
   - App Service Plan (Linux)
   - App Service (the API; serves the SPA from `wwwroot/`)
   - Azure SQL Server + Database
   - Storage Account + `images` container
   - Azure SignalR Service (Free F1)
   - Application Insights + Log Analytics workspace

3. The deployment outputs the App Service URL. Visit it to confirm it boots.
   The SPA returns the homepage; sign in with the seeded `admin@example.com`
   account using the `Identity__DefaultAdminPassword` you supplied. **You will
   be forced to change the password on first sign-in.** Update the application
   setting via the Azure portal or CLI immediately afterwards so future
   redeployments do not surface the original placeholder.

---

## Custom Domain + TLS

After the resources are provisioned:

```bash
APP_NAME=credocms-app-production
RG=credocms-rg
DOMAIN=example.org

# 1. Add an A record at your DNS provider:
#    A    @     <App Service inbound IP>
#    TXT  asuid <App Service custom-domain verification id>
#
#    Both are visible via:
az webapp show -g $RG -n $APP_NAME --query 'inboundIpAddress'
az webapp config hostname get-external-ip -g $RG --webapp-name $APP_NAME

# 2. Bind the hostname:
az webapp config hostname add -g $RG --webapp-name $APP_NAME --hostname $DOMAIN

# 3. Provision a managed certificate (App Service issues a free cert):
az webapp config ssl create -g $RG --name $APP_NAME --hostname $DOMAIN

# 4. Bind the cert to enforce HTTPS:
THUMBPRINT=$(az webapp config ssl list -g $RG --query "[?subjectName=='$DOMAIN'].thumbprint" -o tsv)
az webapp config ssl bind -g $RG --name $APP_NAME --certificate-thumbprint $THUMBPRINT --ssl-type SNI
```

When you add a custom domain, redeploy the Bicep template with `customDomain`
set to the same hostname so `PublicSite:BaseUrl` updates accordingly.

---

## Sizing

| Tier | App Service | SQL | SignalR | Approximate monthly cost (USD) |
|---|---|---|---|---|
| **Dev** | B1 (`appServiceSku: B1`) | Basic | Free F1 | ~$30 |
| **Small-church production (recommended Phase 1)** | S1 | S0 | Free F1 | ~$110 |
| **Larger congregation** | P1v3 | S2 | Standard S1 | ~$330 |

Real-time SignalR Free F1 is capped at 20 concurrent connections. For
deployments with more than ~20 concurrently-online members in the admin/member
areas, switch to `Standard_S1`.

---

## Alternative: SPA on Azure Static Web Apps

The Phase 1 default is "single deployment" — the SPA is built into
`api/CredoCms.Api/wwwroot/` and served by the App Service. This minimises
moving parts.

If you prefer to host the SPA on **Azure Static Web Apps** (free CDN, easier
custom domains for marketing pages, separate scaling), the steps are:

1. Skip building the SPA into the API publish artifact.
2. Create a Static Web App resource pointing at the SPA's GitHub repo path
   (`/spa`). Build command: `npm ci && npm run build`. Output path: `dist`.
3. Set the Static Web App's **API backend** to the App Service URL so that
   `/api/*` requests proxy to the API.
4. Set CORS on the App Service to permit the Static Web App's hostname:
   `Authentication:Cors:DevAllowedOrigins` (despite the dev-tinted name) is
   not exposed in production by default — for SWA you instead add the host
   to `Authentication:Cors:ProdAllowedOrigins` (a Phase-2 surface; for now,
   add it as an explicit CORS policy in `Program.cs`).

For Phase 1, single-deployment is simpler and is what the GitHub Actions
workflow at `.github/workflows/deploy.yml` produces.

---

## Cleanup

```bash
az group delete --name credocms-rg --yes --no-wait
```

Note that this deletes the SQL database **and all data**. Take a backup before
deleting if there's anything you want to keep.
