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

- `src/lib/tokens.css` - Design tokens (CSS custom properties)
- `src/lib/i18n/` - Internationalization catalogs
- `src/lib/auth/` - MSAL.js auth (Phase 2)
- `src/lib/api/` - Generated TypeScript API client (Phase 1)
- `src/routes/` - SvelteKit routes

## TODO

- [ ] Generate TS API client from OpenAPI spec (after Hicks lands Phase 1 endpoints)
- [ ] Wire MSAL.js auth (Phase 2, Bishop owns design)
- [ ] Service worker for offline support (PRD §U22)
