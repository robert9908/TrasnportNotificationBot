#!/bin/bash

# Monitoring Script for Transport Notification Bot
set -e

echo "📊 Transport Bot Monitoring Dashboard"
echo "======================================"

# Check container status
echo "🐳 Container Status:"
docker-compose -f docker-compose.prod.yml ps

echo ""

# Check application health
echo "🏥 Application Health:"
if curl -s http://localhost/health | jq . 2>/dev/null; then
    echo "✅ Application is healthy"
else
    echo "❌ Application health check failed"
fi

echo ""

# Check database connection
echo "🗄️ Database Status:"
if docker exec transport-bot-postgres pg_isready -U postgres > /dev/null 2>&1; then
    echo "✅ PostgreSQL is ready"
    
    # Show database stats
    echo "📈 Database Statistics:"
    docker exec transport-bot-postgres psql -U postgres -d transport_bot -c "
    SELECT 
        schemaname,
        tablename,
        n_tup_ins as inserts,
        n_tup_upd as updates,
        n_tup_del as deletes
    FROM pg_stat_user_tables 
    ORDER BY n_tup_ins DESC;
    " 2>/dev/null || echo "Could not fetch database stats"
else
    echo "❌ PostgreSQL is not ready"
fi

echo ""

# Check Redis status
echo "🔴 Redis Status:"
if docker exec transport-bot-redis redis-cli ping > /dev/null 2>&1; then
    echo "✅ Redis is ready"
    
    # Show Redis info
    echo "📊 Redis Info:"
    docker exec transport-bot-redis redis-cli info memory | grep used_memory_human || echo "Could not fetch Redis stats"
else
    echo "❌ Redis is not ready"
fi

echo ""

# Show recent logs
echo "📋 Recent Application Logs (last 20 lines):"
docker-compose -f docker-compose.prod.yml logs --tail=20 app

echo ""

# Show system resources
echo "💻 System Resources:"
echo "CPU Usage:"
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}" | grep transport-bot

echo ""
echo "📊 Monitoring completed at $(date)"
