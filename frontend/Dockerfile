# =============================================================================
# Tendex AI - Frontend Dockerfile (Multi-stage Build)
# =============================================================================
# Produces a minimal Nginx-based image serving the Vue.js 3 SPA.
# Stage 1: Install dependencies & build (Node.js)
# Stage 2: Serve static files (Nginx Alpine)
# =============================================================================

# ---------------------------------------------------------------------------
# Stage 1: Build the Vue.js application
# ---------------------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app

# Install pnpm
RUN corepack enable && corepack prepare pnpm@latest --activate

# Copy dependency files first for better caching
COPY package.json pnpm-lock.yaml ./

# Install dependencies
RUN pnpm install --frozen-lockfile --prod=false

# Copy source code
COPY . .

# Build for production
ARG VITE_API_BASE_URL
ARG VITE_APP_TITLE="Tendex AI"
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_APP_TITLE=${VITE_APP_TITLE}

RUN pnpm build

# ---------------------------------------------------------------------------
# Stage 2: Serve with Nginx
# ---------------------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime

# Remove default nginx config
RUN rm -rf /etc/nginx/conf.d/default.conf

# Copy custom nginx config for SPA routing
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

# Security: Remove server tokens
RUN sed -i 's/# server_tokens off;/server_tokens off;/' /etc/nginx/nginx.conf || true

# Add security headers config
RUN echo 'server_tokens off;' > /etc/nginx/conf.d/security.conf

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
