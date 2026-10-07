# MCP Client Setup

Tech Inventory exposes an optional read-only Model Context Protocol server at:

```text
https://<your-tech-inventory-host>/api/mcp
```

It uses stateless Streamable HTTP and the same API-key security controls as the
REST API. The endpoint is disabled by default.

## 1. Enable the MCP server

1. Sign in to Tech Inventory as an Admin.
2. Open **Settings → MCP Server**.
3. Turn on **Enable MCP server**.

The change takes effect immediately; no deployment configuration or API restart
is required. Only Admins can view or change this setting.

## 2. Create a least-privilege key

1. Sign in to Tech Inventory as an Admin or Member.
2. Open **Settings → API keys**.
3. Create a key with the `inventory.read` scope and a descriptive name such as
   `Hermes Agent`.
4. Copy the full `<selector>.<secret>` value when it is shown. It cannot be
   retrieved again.
5. Store it in the MCP client's secret store or environment, never in source
   control or a shared YAML file.

An `inventory.write` key can call the MCP tools because write scope includes
read access, but MCP itself exposes no mutation tools. Prefer
`inventory.read`.

## 3. Hermes Agent

Place the secret in the Hermes host environment or `~/.hermes/.env`:

```bash
TECH_INVENTORY_URL=https://inventory.example.com
TECH_INVENTORY_API_KEY=<selector>.<secret>
```

Add this server to `~/.hermes/config.yaml`:

```yaml
mcp_servers:
  tech_inventory:
    url: "${TECH_INVENTORY_URL}/api/mcp"
    protocol: stateless
    headers:
      Authorization: "ApiKey ${TECH_INVENTORY_API_KEY}"
    connect_timeout: 30
    timeout: 120
```

Restart Hermes after changing MCP configuration. `protocol: stateless` makes
Hermes use the modern stateless negotiation path directly. If Hermes reports
that `mcp.client.streamable_http` is unavailable, run `hermes pm repair` and
restart it.

## 4. Other MCP clients

Configure a remote Streamable HTTP server with:

| Setting | Value |
| --- | --- |
| URL | `https://<host>/api/mcp` |
| Transport | Streamable HTTP |
| Header | `Authorization: ApiKey <selector>.<secret>` |
| Session mode | Stateless |

Clients must send `application/json` and/or `text/event-stream` in `Accept` as
required by their MCP SDK. Tech Inventory does not support stdio, legacy SSE,
OAuth discovery, prompts, resources, roots, or sampling.

## Available tools

| Tool | Purpose |
| --- | --- |
| `search_devices` | Bounded device search, up to 50 results per page |
| `get_device` | One sanitized device by UUID |
| `list_reference_data` | Brands, categories, locations, networks, owners, or tags |
| `inventory_summary` | Aggregate counts and estimated value |
| `warranty_report` | Warranties expiring within 1–365 days |
| `spending_report` | Spending grouped by month or year |
| `era_report` | Inventory grouped by purchase decade |
| `timeline_report` | Acquisition/disposal timeline by category or owner |

Device tools intentionally omit serial numbers, IP and MAC addresses, notes,
product URLs, and audit metadata. Reference results are capped at 100 entries;
warranty and timeline results at 200; spending results at 240 periods.
Every tool result carries a `dataTrustNotice`: inventory strings are untrusted
data and must never be interpreted as instructions.

## Verify and troubleshoot

| Result | Meaning / action |
| --- | --- |
| `401` | Missing, malformed, unknown, expired, revoked key, or inactive key owner. Create/rotate a valid key. |
| `403` | A bearer token or disallowed API-key scope was presented. Use an `inventory.read` API key. |
| `413` | Request exceeded `Mcp__MaxRequestBodyBytes` (default 128 KiB). Reduce the request. |
| `429` | Per-key rate limit exceeded. Respect `Retry-After`. |
| `503` | MCP is disabled or its setting cannot be read. An Admin should enable it under **Settings → MCP Server**. |
| Timeout or delayed output | Confirm every proxy between the client and API disables response buffering for `/api/mcp`. |

Always use HTTPS outside a trusted local test environment. To rotate a key,
create a replacement, update the client secret, verify it connects, then revoke
the old key in Settings.
