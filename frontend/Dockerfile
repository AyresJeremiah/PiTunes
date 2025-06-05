# Stage 1: Build Angular browser app
FROM node:20-alpine AS build

# Set working directory inside container
WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm install

# Copy full frontend source code
COPY . .

# Build ONLY the browser version (no SSR, no prerendering)
RUN npx ng build --configuration=production --output-hashing=all

# Stage 2: Serve with nginx
FROM nginx:alpine AS runtime

# Clean nginx default content
RUN rm -rf /usr/share/nginx/html/*

# Copy built Angular app from build stage
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

# Copy your custom nginx configuration if you have one
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose nginx port
EXPOSE 80

# Start nginx in foreground
CMD ["nginx", "-g", "daemon off;"]
