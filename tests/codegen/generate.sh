#!/usr/bin/env bash

# Get schema
curl http://localhost:8080/swagger/v1/swagger.json \
    > ./backend.schema.json

# Generate frontend client
orval --config ./orval.config.ts
