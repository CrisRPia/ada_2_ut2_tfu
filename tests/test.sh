#! /usr/bin/env bash

docker_compose_file="../docker-compose.yml"
docker compose -f $docker_compose_file down -v && docker compose -f $docker_compose_file up  --build -d --wait

echo "Waiting for backend to start..."
sleep 10 # lmao
node --experimental-strip-types test.ts
