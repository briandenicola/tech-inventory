# TechInventory.Web

SvelteKit PWA for Tech Inventory.

## Commands

```bash
# Install dependencies (pnpm, not npm)
pnpm install

# Development server
pnpm run dev

# Type checking
pnpm run check

# Lint
pnpm run lint

# Format
pnpm run format

# Unit tests
pnpm run test

# Single test file
pnpm run test -- --run src/lib/MyFile.test.ts

# Build for production
pnpm run build

# Preview production build
pnpm run preview
```

## Structure

- `src/lib/tokens.css` - Design tokens (CSS custom properties) — D-137 / D-138 visual baseline.
- `src/lib/i18n/` - Internationalization catalogs.
- `src/lib/auth/` - MSAL.js (Workforce-tenant Entra) and the local-auth client (sessionStorage tokens
  `ti_local_token` / `ti_local_meta`) — see `docs/auth-design.md` §6 and ADR D-140.
- `src/lib/api/` - Generated TypeScript API client.
- `src/routes/` - SvelteKit routes (notably `auth/change-password` for the F025 v1b force-rotation flow).

## Auth surface (F025 v1b)

The sign-in page renders MSAL by default and offers a **"Use a local account
instead"** toggle (`LocalLoginForm`) that calls `POST /api/v1/auth/local/login`,
stores the returned JWT under `ti_local_token` plus metadata under `ti_local_meta`
in **sessionStorage only** (per Constitution §6 / D-002 / security-baseline §1),
and routes through the same `auth` store the Entra flow uses. If the local
token carries `mustChangePassword: true`, a root-layout `$effect` redirects to
`/auth/change-password`, which submits to `POST /api/v1/auth/local/change-password`
and clears the flag on success. End-to-end design lives in `docs/auth-design.md`
§6; operator runbook lives in `docs/operations.md`.

## TODO

- [ ] Service worker for offline support (PRD §U22).
