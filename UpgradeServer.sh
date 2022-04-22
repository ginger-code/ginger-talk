#!/usr/bin/env zsh

docker-compose down
git reset origin/main --hard
docker-compose build
docker-compose up -d