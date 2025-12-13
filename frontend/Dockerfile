FROM node:18 AS build
WORKDIR /app
COPY package*.json ./
# npm ci ensures reproducible builds by installing exact versions from package-lock.json
# It also installs optional dependencies by default, including native bindings
RUN npm install
COPY . .
RUN npm run build

FROM node:18-slim AS runtime
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