# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy csproj and restore
COPY backend.csproj .
RUN dotnet restore ./backend.csproj

# Copy full source
COPY . .

# Build app
RUN dotnet publish ./backend.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime

# Install dependencies for yt-dlp + ffmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg python3-pip && \
    pip3 install yt-dlp

WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Add yt-dlp explicitly to PATH (just in case)
ENV PATH="/root/.local/bin:${PATH}"

ENTRYPOINT ["dotnet", "backend.dll"]
