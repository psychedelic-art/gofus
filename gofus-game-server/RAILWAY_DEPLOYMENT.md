# 🚀 GOFUS Game Server - Railway Deployment Guide

## Overview

This guide will help you deploy the GOFUS Game Server to Railway.

## Prerequisites

- Railway account (https://railway.app)
- Railway CLI installed ✅ (already installed)
- Project built successfully ✅ (dist/ folder created)

## Deployment Steps

### Step 1: Login to Railway

Since this is a CLI environment, you'll need to login via browser:

```bash
railway login
```

This will open a browser window for authentication.

**Alternative**: If you cannot open a browser, you can set up Railway via the web dashboard and link the project.

### Step 2: Initialize Railway Project

```bash
cd C:\Users\HardM\Desktop\Enterprise\gofus\gofus-game-server
railway init
```

When prompted:
- Choose "Empty Project" or "Create new project"
- Name it: `gofus-game-server`

### Step 3: Link to Railway Project (if already exists)

If you already created a Railway project via the web dashboard:

```bash
railway link [project-id]
```

### Step 4: Set Environment Variables

Set all required environment variables:

```bash
# Database (Supabase)
railway variables set DATABASE_URL="postgresql://postgres.tfjlapqczjafecblvxjp:AjMwQR6*S.n.V3E@aws-1-us-east-2.pooler.supabase.com:6543/postgres?pgbouncer=true"

railway variables set DIRECT_URL="postgresql://postgres:AjMwQR6*S.n.V3E@db.tfjlapqczjafecblvxjp.supabase.co:5432/postgres"

# Redis
railway variables set REDIS_URL="redis://default:wjrrU55bUrBm4ErJmjWEX40m1agpMYGB@redis-16598.c103.us-east-1-mz.ec2.redns.redis-cloud.com:16598"

# Server Config
railway variables set NODE_ENV=production
railway variables set PORT=3001
railway variables set GAME_SERVER_ID=gs-railway-001
railway variables set REGION=us-east-1

# JWT
railway variables set JWT_SECRET="gofus-production-jwt-secret-2024"

# Backend API
railway variables set API_URL="https://gofus-backend-qyakdcmwa-andres-munozs-projects-fe137bcd.vercel.app"

# Game Configuration
railway variables set MAX_PLAYERS_PER_MAP=50
railway variables set TICK_RATE=20
railway variables set SAVE_INTERVAL=300000
railway variables set COMBAT_TIMEOUT=120000

# Performance
railway variables set MAX_CONNECTIONS=5000
railway variables set WORKER_THREADS=4
railway variables set USE_CLUSTERING=false

# Monitoring
railway variables set METRICS_PORT=9090
railway variables set LOG_LEVEL=info

# Security
railway variables set RATE_LIMIT_WINDOW=60000
railway variables set RATE_LIMIT_MAX_REQUESTS=100
railway variables set MAX_PACKET_SIZE=1024

# Production Settings
railway variables set DEBUG=false
railway variables set ENABLE_HOT_RELOAD=false
```

### Step 5: Deploy

```bash
railway up
```

This will:
1. Build the Docker image from the Dockerfile
2. Push to Railway's registry
3. Deploy the container
4. Start the game server

### Step 6: Check Deployment Status

```bash
# View deployment logs
railway logs

# Check service status
railway status

# Open project dashboard
railway open
```

### Step 7: Get the Public URL

```bash
# Generate a Railway domain
railway domain
```

Or via the dashboard:
1. Go to https://railway.app/dashboard
2. Select your `gofus-game-server` project
3. Click on "Settings" → "Networking"
4. Generate a domain or add a custom domain

### Step 8: Verify Deployment

Test the health endpoint:

```bash
curl https://your-railway-domain.railway.app/health
```

Expected response:
```json
{
  "status": "ok",
  "timestamp": "2024-10-25T...",
  "uptime": 123,
  "metrics": {
    "onlinePlayers": 0,
    "activeMaps": 0,
    "activeBattles": 0,
    "tickCount": 2460,
    "lastTickDuration": 5
  }
}
```

## Alternative: Deploy via Web Dashboard

### Option 1: GitHub Integration

1. Push your code to GitHub
2. Go to https://railway.app/dashboard
3. Click "New Project"
4. Select "Deploy from GitHub repo"
5. Select your `gofus-game-server` repository
6. Railway will automatically detect the Dockerfile
7. Add environment variables in the "Variables" tab
8. Click "Deploy"

### Option 2: Use Railway's REST API

You can deploy programmatically using the Railway API token:

```bash
# See deploy-to-railway.sh script
```

## Files Created for Deployment

✅ **Dockerfile** - Multi-stage build for optimized deployment
✅ **.dockerignore** - Excludes unnecessary files from Docker build
✅ **railway.json** - Railway configuration
✅ **.env.prod** - Production environment variables template
✅ **Health endpoint** - Added to GameServer.ts (/health and /metrics)

## Environment Variables Summary

All required environment variables are configured in `.env.prod`. They include:

- **Database**: PostgreSQL (Supabase) connection strings
- **Redis**: Cache and session store
- **Authentication**: JWT secrets and API URLs
- **Game Settings**: Player limits, tick rates, timeouts
- **Performance**: Connection limits, worker threads
- **Monitoring**: Metrics, logging levels
- **Security**: Rate limiting, packet size limits

## Architecture

```
┌─────────────────────────────────────────────────┐
│                  CLIENT                          │
│           Unity/Web Game Client                  │
└─────────────────┬───────────────────────────────┘
                  │
          WebSocket + HTTPS
                  │
      ┌───────────┴────────────┐
      │                        │
      ▼                        ▼
┌──────────────┐      ┌────────────────┐
│   RAILWAY    │      │    VERCEL      │
│ Game Server  │◄────►│   Backend API  │
│  (Stateful)  │      │  (Serverless)  │
│              │      │                │
│ • WebSocket  │      │ • REST API     │
│ • Game Loop  │      │ • Auth         │
│ • Combat     │      │ • Marketplace  │
│ • AI System  │      │ • Guilds       │
└──────┬───────┘      └────────┬───────┘
       │                       │
       └───────┬───────────────┘
               │
       ┌───────▼────────┐
       │   SUPABASE     │
       │   PostgreSQL   │
       └────────────────┘
```

## Health Checks

Railway will automatically monitor the `/health` endpoint:

- **URL**: `GET /health`
- **Expected**: HTTP 200 with JSON payload
- **Timeout**: 300s (configured in railway.json)
- **Check Interval**: Every 30s (Docker HEALTHCHECK)

## Scaling

To scale the game server:

```bash
# Increase replicas (if needed in the future)
railway scale --replicas 2
```

**Note**: Currently configured for 1 replica. Multi-server synchronization would need additional work.

## Monitoring

### View Logs

```bash
# Real-time logs
railway logs --follow

# Last 100 lines
railway logs --tail 100
```

### Metrics Endpoint

Access server metrics:
```bash
curl https://your-domain.railway.app/metrics
```

Returns detailed server statistics including:
- Uptime
- Online players
- Active map instances
- Active battles
- Memory usage
- Tick performance

## Troubleshooting

### Deployment Fails

```bash
# Check build logs
railway logs --build

# Check deployment logs
railway logs --deployment
```

### Connection Issues

1. Verify environment variables are set correctly
2. Check database connectivity
3. Verify Redis connection
4. Check Railway service status

### Performance Issues

1. Monitor `/metrics` endpoint
2. Check tick duration (should be < 50ms)
3. Review Railway metrics dashboard
4. Consider increasing worker threads

## Next Steps

After successful deployment:

1. ✅ Update frontend to use Railway WebSocket URL
2. ✅ Configure custom domain (optional)
3. ✅ Set up monitoring alerts
4. ✅ Configure backups
5. ✅ Test all game features
6. ✅ Load testing with multiple clients

## Support

- Railway Docs: https://docs.railway.app
- Railway Discord: https://discord.gg/railway
- GOFUS Issues: [Your repository issues]

---

**Deployment Status**: Ready to deploy ✅
**Build Status**: Successful ✅
**Configuration**: Complete ✅
