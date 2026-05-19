# Tech Inventory Web — Multi-stage build
# Build SvelteKit static output, then serve it with nginx and proxy /api/* to the API container.

# --- Build stage ---
FROM node:22-alpine AS build
WORKDIR /app

RUN corepack enable

# Tell pnpm we're in CI so `pnpm run`'s implicit deps-status check doesn't
# try to interactively confirm a modules-directory purge (no TTY → abort).
# See ERR_PNPM_ABORTED_REMOVE_MODULES_DIR_NO_TTY.
ENV CI=true

COPY src/TechInventory.Web/package.json src/TechInventory.Web/pnpm-lock.yaml* src/TechInventory.Web/pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile || pnpm install

COPY src/TechInventory.Web/ ./
RUN pnpm run build

# --- Runtime stage ---
# nginx-unprivileged: runs as uid 101, listens on 8080, /var/cache/nginx +
# /var/log/nginx + /tmp are writable by 101. Satisfies Trivy DS-0002 (no
# root execution) without sacrificing the official nginx image.
FROM nginxinc/nginx-unprivileged:1.27.5-alpine AS runtime

USER root
# Pull the latest patched alpine packages on top of the pinned base tag.
# Without this, libcrypto3 / busybox / curl drift behind nginxinc's release
# cadence and Trivy gates HIGH/CRITICAL CVEs (CVE-2025-15467, etc.) even
# though fixes exist in the alpine repository. `--no-cache` keeps the layer
# from carrying the apk index.
RUN apk upgrade --no-cache && apk add --no-cache wget
USER 101

COPY src/TechInventory.Web/nginx.conf /etc/nginx/nginx.conf
COPY src/TechInventory.Web/templates/upstream.conf.template /etc/nginx/templates/upstream.conf.template
COPY --from=build /app/build/ /usr/share/nginx/html/

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
