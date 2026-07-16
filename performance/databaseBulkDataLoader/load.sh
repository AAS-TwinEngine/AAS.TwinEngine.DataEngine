#!/bin/sh
set -e

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname "$0")" && pwd)
WORKSPACE_DIR=/workspace

PG_CONN_STRING="${PG_CONN_STRING:?PG_CONN_STRING environment variable is required}"
ASSET_COUNT="${ASSET_COUNT:-1000}"
BATCH_SIZE="${BATCH_SIZE:-100}"

PG_CONN_STRING=$(printf '%s' "$PG_CONN_STRING" | sed -E 's#(://[^/@]*@?)localhost([:/])#\1host.docker.internal\2#')

CONN_STRING=$PG_CONN_STRING
TOTAL=$ASSET_COUNT

MAX_ATTEMPTS=10
SQL_DIR=$WORKSPACE_DIR/sql
SCHEMA_SOURCE_DIR=$WORKSPACE_DIR/schema

if [ ! -d "$SCHEMA_SOURCE_DIR" ]; then
    echo "ERROR: schema source directory not found: $SCHEMA_SOURCE_DIR"
    exit 1
fi

if [ ! -f "$SQL_DIR/check-schema.sql" ] || [ ! -f "$SQL_DIR/truncate-db.sql" ] || [ ! -f "$SQL_DIR/schema-generator.sql" ] || [ ! -f "$SQL_DIR/load.sql" ]; then
    echo "ERROR: required SQL files not found under: $SQL_DIR"
    exit 1
fi

psql_exec() {
    sql_file=$1
    shift
    psql "$PG_CONN_STRING" -v ON_ERROR_STOP=1 "$@" -f "$sql_file"
}

echo "========================================="
echo " PostgreSQL Bulk Data Loader (via Docker)"
echo "========================================="
echo "Connection   : $CONN_STRING"
echo "Total Assets : $TOTAL"
echo "Batch Size   : $BATCH_SIZE"
echo "========================================="

echo "Checking PostgreSQL connectivity..."
ATTEMPT=0

until psql "$PG_CONN_STRING" -c "SELECT 1;" >/dev/null 2>&1
do
    ATTEMPT=$((ATTEMPT + 1))
    if [ "$ATTEMPT" -ge "$MAX_ATTEMPTS" ]; then
        echo ""
        echo "ERROR: Could not reach PostgreSQL after $MAX_ATTEMPTS attempts."
        echo "Debug with: psql \"$PG_CONN_STRING\" -c \"SELECT 1;\""
        exit 1
    fi
    echo "Attempt $ATTEMPT/$MAX_ATTEMPTS failed, retrying in 2s..."
    sleep 2
done

echo "PostgreSQL is ready."
echo "Checking schema..."

TABLE_EXISTS=$(psql_exec "$SQL_DIR/check-schema.sql" -tA)
TABLE_EXISTS=$(printf '%s' "$TABLE_EXISTS" | tr -d '[:space:]')

if [ "$TABLE_EXISTS" = "t" ]; then
    echo "Schema already exists. Truncating existing data..."
    psql_exec "$SQL_DIR/truncate-db.sql"
else
    echo "Schema not found. Creating..."
    psql_exec "$SQL_DIR/schema-generator.sql"
fi

START=1

while [ "$START" -le "$TOTAL" ]
do
    END=$((START + BATCH_SIZE - 1))
    if [ "$END" -gt "$TOTAL" ]; then
        END=$TOTAL
    fi
    CURRENT_BATCH_COUNT=$((END - START + 1))

    echo ""
    echo "========================================="
    echo "Loading Batch : $START -> $END ($CURRENT_BATCH_COUNT assets)"
    echo "========================================="

    psql "$PG_CONN_STRING" \
        -v ON_ERROR_STOP=1 \
        -v batch_start="$START" \
        -v batch_end="$END" \
        -c "SET app.asset_count = $CURRENT_BATCH_COUNT;" \
        -f "$SQL_DIR/load.sql"

    echo "Batch $START -> $END completed."
    START=$((END + 1))
done

echo ""
echo "========================================="
echo "Bulk loading completed successfully."
echo "========================================="