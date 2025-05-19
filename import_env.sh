#!/bin/bash

if [ ! -f ".env" ]; then
  echo "File .env not found!"
  exit 1
fi

while IFS='=' read -r key value; do
  if [[ -n "$key" && ! "$key" =~ ^# ]]; then
    key=$(echo "$key" | xargs)
    value=$(echo "$value" | xargs)
    flyctl secrets set "$key=$value"
  fi
done < .env

echo "All vars set"
