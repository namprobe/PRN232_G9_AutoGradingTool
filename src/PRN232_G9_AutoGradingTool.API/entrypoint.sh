#!/bin/sh
set -e

echo "== PRN232_G9_AutoGradingTool API entrypoint =="

# Wait for database to be reachable (uses netcat installed in runtime image)
DB_HOST=${DB_HOST:-postgres}
DB_PORT=${DB_PORT:-5432}
COUNT=0
echo "Waiting for database ${DB_HOST}:${DB_PORT}..."
while ! nc -z "$DB_HOST" "$DB_PORT"; do
  COUNT=$((COUNT+1))
  echo "  waiting for db (${COUNT})..."
  sleep 2
  if [ $COUNT -gt 30 ]; then
    echo "  timed out waiting for database; continuing anyway"
    break
  fi
done

echo "Starting application"
exec "$@"
