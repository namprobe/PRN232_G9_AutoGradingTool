# syntax=docker/dockerfile:1
# Build static site; runtime = nginx (cùng docker-compose với API)
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN --mount=type=cache,target=/root/.npm \
    npm ci

COPY . .

# Rỗng = browser gọi /api/... cùng origin, nginx proxy sang autogradingtool-api
ARG VITE_API_BASE_URL=
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

# Trong Docker mặc định gọi API thật qua nginx; chỉ bật true khi cần demo offline
ARG VITE_USE_API_MOCK=false
ENV VITE_USE_API_MOCK=${VITE_USE_API_MOCK}

RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
