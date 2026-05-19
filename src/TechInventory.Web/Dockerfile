# Tech Inventory Web — Multi-stage build
# Build SvelteKit static output, then serve it with nginx and proxy /api/* to the API container.

# --- Build stage ---
FROM node:22-alpine AS build
WORKDIR /app

RUN corepack enable

COPY src/TechInventory.Web/package.json src/TechInventory.Web/pnpm-lock.yaml* ./
RUN pnpm install --frozen-lockfile || pnpm install

COPY src/TechInventory.Web/ ./
RUN pnpm run build

# --- Runtime stage ---
FROM nginx:1.27.5-alpine AS runtime

RUN apk add --no-cache wget

COPY src/TechInventory.Web/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/build/ /usr/share/nginx/html/

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
