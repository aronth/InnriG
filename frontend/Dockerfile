FROM node:18-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:18-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache wget
ENV NODE_ENV=production
ENV NUXT_HOST=0.0.0.0
ENV NUXT_PORT=3000
EXPOSE 3000
COPY --from=build /app/.output ./.output
COPY --from=build /app/package*.json ./
RUN npm ci --omit=dev
CMD ["node", ".output/server/index.mjs"]
