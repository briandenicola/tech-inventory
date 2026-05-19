# Tech Inventory Web — Multi-stage build
# Build SvelteKit static output, then serve it with nginx and proxy /api/* to the API container.

# --- Build stage ---
FROM node:22-alpine AS build
WORKDIR /app

# When VITE_AUTH_DEV_BYPASS=true is passed at build time, the bundle skips MSAL
# and lets the backend DevBypass authenticate (used by `task test:e2e` and local
# dev sessions). Default is false so production builds keep real Entra auth.
ARG VITE_AUTH_DEV_BYPASS=false
ENV VITE_AUTH_DEV_BYPASS=$VITE_AUTH_DEV_BYPASS

RUN corepack enable

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
RUN apk add --no-cache wget
USER 101

COPY src/TechInventory.Web/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/build/ /usr/share/nginx/html/

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
