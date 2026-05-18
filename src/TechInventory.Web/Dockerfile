# Tech Inventory Web — Multi-stage build
# Base: Node 22 Alpine (SvelteKit + adapter-node)
# Non-root user (1000), minimal runtime

# --- Build stage ---
FROM node:22-alpine AS build
WORKDIR /app

# Enable pnpm via corepack
RUN corepack enable

# Copy package manifests (layer cache optimization)
# Defensive: use wildcard for pnpm-lock.yaml* in case it doesn't exist yet during parallel scaffolding
COPY src/TechInventory.Web/package.json src/TechInventory.Web/pnpm-lock.yaml* ./

# Install dependencies (frozen lockfile for reproducibility)
RUN pnpm install --frozen-lockfile || pnpm install

# Copy source
COPY src/TechInventory.Web/ ./

# Build the app (SvelteKit adapter-node produces /build)
RUN pnpm run build

# --- Runtime stage ---
FROM node:22-alpine AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup -g 1000 appgroup && \
    adduser -u 1000 -G appgroup -s /bin/sh -D appuser && \
    chown -R appuser:appgroup /app

# Install wget for healthcheck
RUN apk add --no-cache wget

# Copy built app from build stage
COPY --from=build --chown=appuser:appgroup /app/build ./build
COPY --from=build --chown=appuser:appgroup /app/package.json ./
COPY --from=build --chown=appuser:appgroup /app/node_modules ./node_modules

# Switch to non-root
USER 1000

# Expose web port (SvelteKit adapter-node default: 3000)
EXPOSE 3000

# Set Node environment
ENV NODE_ENV=production
ENV PORT=3000

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:3000/ || exit 1

# Start the app (adapter-node output)
CMD ["node", "build"]
