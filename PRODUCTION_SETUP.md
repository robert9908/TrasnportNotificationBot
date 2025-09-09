# 🚀 Production Setup Guide

## Prerequisites

- Docker & Docker Compose
- PostgreSQL 15+
- Redis 7+
- SSL Certificate (for HTTPS)

## Quick Start

1. **Clone and configure environment:**
```bash
git clone <repository-url>
cd TransportNotificationBot
cp .env.production .env
```

2. **Set environment variables in `.env`:**
```bash
POSTGRES_PASSWORD=your_secure_password
TELEGRAM_BOT_TOKEN=your_bot_token
MOSCOW_TRANSPORT_API_KEY=your_api_key
```

3. **Deploy:**
```bash
chmod +x scripts/deploy.sh
./scripts/deploy.sh
```

## Production Features

### 🔒 Security
- Rate limiting (100 req/min for API, 30 req/sec for Telegram)
- Non-root container user
- Environment variable secrets
- Input validation and sanitization

### 📊 Monitoring & Logging
- Structured logging with Serilog
- Health checks for all services
- Application metrics
- Log rotation (30 days retention)

### 🚀 Performance
- Redis caching for API responses
- Connection pooling
- Async/await throughout
- Optimized Docker images

### 🗄️ Database
- PostgreSQL with automatic migrations
- Connection resilience
- Backup scripts included

## API Endpoints

### Core Endpoints
- `GET /health` - Health check
- `GET /swagger` - API documentation
- `POST /api/telegram/webhook` - Telegram webhook

### Transport API
- `GET /api/transportstop` - Get all stops
- `GET /api/transportstop/nearby` - Find nearby stops
- `GET /api/route` - Get all routes
- `GET /api/route/by-stop/{id}` - Routes by stop

### User Management
- `POST /api/user/register` - Register user
- `GET /api/user/{id}` - Get user
- `PUT /api/user/{id}` - Update user

## Telegram Bot Commands

- `/start` - Welcome message with options
- `/moscow` - Show Moscow transport stops
- `/stops` - Show all available stops
- `/search <name>` - Search stops by name
- `/subscriptions` - Manage notifications
- `/help` - Show help information

## Monitoring

### Health Checks
```bash
curl http://localhost/health
```

### View Logs
```bash
docker-compose -f docker-compose.prod.yml logs -f app
```

### Monitor Resources
```bash
./scripts/monitor.sh
```

### Database Backup
```bash
./scripts/backup.sh
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_PASSWORD` | Database password | Required |
| `TELEGRAM_BOT_TOKEN` | Telegram bot token | Required |
| `MOSCOW_TRANSPORT_API_KEY` | Moscow transport API key | Optional |
| `ASPNETCORE_ENVIRONMENT` | Environment | Production |
| `DB_HOST` | Database host | postgres |
| `REDIS_HOST` | Redis host | redis |

### Rate Limiting
- API: 100 requests per minute
- Telegram: 30 requests per second
- Configurable in `Program.cs`

### Caching
- Redis for API responses
- 30-minute default TTL
- Configurable per endpoint

## Scaling

### Horizontal Scaling
```yaml
# In docker-compose.prod.yml
app:
  deploy:
    replicas: 3
```

### Load Balancer
Add nginx or traefik for load balancing:
```yaml
nginx:
  image: nginx:alpine
  ports:
    - "80:80"
    - "443:443"
  volumes:
    - ./nginx.conf:/etc/nginx/nginx.conf
```

## Troubleshooting

### Common Issues

1. **Bot not responding:**
   - Check `TELEGRAM_BOT_TOKEN`
   - Verify bot permissions
   - Check logs: `docker logs transport-bot-app`

2. **Database connection failed:**
   - Verify PostgreSQL is running
   - Check connection string
   - Run: `docker exec transport-bot-postgres pg_isready`

3. **High memory usage:**
   - Check Redis memory: `docker exec transport-bot-redis redis-cli info memory`
   - Review cache TTL settings
   - Monitor with `./scripts/monitor.sh`

### Performance Tuning

1. **Database:**
```sql
-- Optimize queries
CREATE INDEX idx_subscriptions_user_active ON subscriptions(user_id, is_active);
CREATE INDEX idx_transport_stops_location ON transport_stops USING GIST(location);
```

2. **Redis:**
```bash
# Increase memory limit
redis-cli CONFIG SET maxmemory 256mb
redis-cli CONFIG SET maxmemory-policy allkeys-lru
```

## Security Checklist

- [ ] Change default passwords
- [ ] Enable HTTPS with SSL certificates
- [ ] Configure firewall rules
- [ ] Regular security updates
- [ ] Monitor access logs
- [ ] Backup encryption
- [ ] API key rotation

## Maintenance

### Daily
- Monitor health checks
- Review error logs
- Check resource usage

### Weekly
- Database backup verification
- Security updates
- Performance metrics review

### Monthly
- Log cleanup
- Certificate renewal check
- Dependency updates

## Support

For issues and questions:
1. Check logs: `./scripts/monitor.sh`
2. Review health status: `curl /health`
3. Check GitHub issues
4. Contact development team

---

**Production Environment Status:** ✅ Ready for deployment
