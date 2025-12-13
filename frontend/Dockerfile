FROM node:20 AS build
WORKDIR /app
COPY package*.json ./
# Install dependencies with npm ci for reproducible builds
# Use --ignore-scripts to prevent postinstall from running before optional deps are ready
RUN npm ci --ignore-scripts
# Explicitly install all required optional dependencies for linux-x64-gnu
# Nuxt 4 uses multiple oxc packages (parser, transform, minify) that need native bindings
# This ensures all native bindings are available before nuxt prepare runs
RUN npm install --no-save \
    @oxc-parser/binding-linux-x64-gnu@0.102.0 \
    @oxc-transform/binding-linux-x64-gnu@0.102.0 \
    @oxc-minify/binding-linux-x64-gnu@0.102.0 || true
# Now run postinstall script (nuxt prepare) after native bindings are installed
RUN npm run postinstall
COPY . .
RUN npm run build

FROM node:20-slim AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*
ENV NODE_ENV=production
ENV NUXT_HOST=0.0.0.0
ENV NUXT_PORT=3000
EXPOSE 3000
COPY --from=build /app/.output ./.output
COPY --from=build /app/package*.json ./
# Use npm ci for consistency with build stage - ensures exact same dependency versions
RUN npm ci --omit=dev
CMD ["node", ".output/server/index.mjs"]