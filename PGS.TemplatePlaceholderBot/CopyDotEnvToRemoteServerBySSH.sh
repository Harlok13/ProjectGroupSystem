#!/bin/bash

if [ -f .env ]; then
  export $(grep -v '^#' .env | xargs)
else
  echo "Error: .env file not found!"
  exit 1
fi

if [ ! -f "$ENV_FILE_PATH" ]; then
    echo "Error: The .env file was not found at the specified path: $ENV_FILE_PATH"
    exit 1
fi

if [ -z "$REMOTE_USER" ] || [ -z "$REMOTE_HOST" ] || [ -z "$REMOTE_DIR" ] || [ -z "$REMOTE_PASSWORD" ]; then
  echo "Error: REMOTE_USER, REMOTE_HOST, REMOTE_DIR, or REMOTE_PASSWORD not found in .env file!"
  exit 1
fi

sshpass -p "$REMOTE_PASSWORD" scp "$ENV_FILE_PATH" "$REMOTE_USER@$REMOTE_HOST:$REMOTE_DIR/.env"

if [ $? -eq 0 ]; then
    echo "Success: The .env file was successfully copied to the remote server."
else
    echo "Error: An error occurred while copying the .env file to the remote server."
    exit 1
fi
