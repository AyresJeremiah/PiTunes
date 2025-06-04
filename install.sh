#!/bin/bash

set -e

echo "---- Update system ----"
sudo apt-get update
sudo apt-get upgrade -y

echo "---- Install dependencies ----"
sudo apt-get install -y \
    docker.io \
    docker-compose \
    alsa-utils \
    ffmpeg \
    yt-dlp

# Enable user in docker group (so you don't need sudo for docker)
sudo usermod -aG docker $USER

# Check if Docker is running
sudo systemctl enable docker
sudo systemctl start docker

echo "---- Add permissions for sound ----"
sudo usermod -aG audio $USER

echo "---- Create project directory ----"
mkdir -p ~/jukebox
cd ~/jukebox

echo "---- Clone or copy your repo ----"
# If you host on Git just pull:
# git clone https://github.com/your-repo/jukebox.git .

# Otherwise manually copy your files here beforehand

echo "---- Build Docker containers ----"
docker-compose build

echo "---- Starting containers ----"
docker-compose up -d

echo "---- Adding Audio to the backend container ----"
docker run --rm \
  --device /dev/snd \
  --volume /etc/asound.conf:/etc/asound.conf \
  PiTunes-backend


echo "---- All done ----"
