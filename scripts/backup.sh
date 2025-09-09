#!/bin/bash

# Database Backup Script
set -e

BACKUP_DIR="./backups"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="transport_bot_backup_${DATE}.sql"

echo "🗄️ Starting database backup..."

# Create backup directory
mkdir -p $BACKUP_DIR

# Create database backup
docker exec transport-bot-postgres pg_dump -U postgres transport_bot > "${BACKUP_DIR}/${BACKUP_FILE}"

# Compress backup
gzip "${BACKUP_DIR}/${BACKUP_FILE}"

echo "✅ Backup completed: ${BACKUP_DIR}/${BACKUP_FILE}.gz"

# Keep only last 7 backups
find $BACKUP_DIR -name "transport_bot_backup_*.sql.gz" -mtime +7 -delete

echo "🧹 Old backups cleaned up"
