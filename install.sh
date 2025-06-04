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

echo "---- Build Docker containers ----"
docker-compose build

echo "---- Starting containers ----"
docker-compose up -d


echo "---- All done ----"
