# 🚨 CRITICAL: Set Environment Variables on Railway

## Status
✅ **Docker Build**: SUCCESSFUL (55.73 seconds)
⚠️ **Health Check**: FAILING - Missing environment variables
🔴 **Service**: Not starting - needs DATABASE_URL, REDIS_URL, etc.

---

## Quick Setup via Railway Dashboard (RECOMMENDED)

### Step 1: Open Railway Dashboard
Go to: https://railway.com/project/d982b8ed-6125-4176-aa35-98e4ae45c509

### Step 2: Navigate to Variables
1. Click on the `gofus-game-server` service
2. Click on the **"Variables"** tab
3. Click **"New Variable"** or **"Raw Editor"**

### Step 3: Copy ALL Variables from .env.prod

Use the **Raw Editor** mode and paste ALL of these:

```bash
NODE_ENV=production
PORT=3001
GAME_SERVER_ID=gs-railway-001
REGION=us-east-1
DATABASE_URL=postgresql://postgres.tfjlapqczjafecblvxjp:AjMwQR6*S.n.V3E@aws-1-us-east-2.pooler.supabase.com:6543/postgres?pgbouncer=true
DIRECT_URL=postgresql://postgres:AjMwQR6*S.n.V3E@db.tfjlapqczjafecblvxjp.supabase.co:5432/postgres
REDIS_URL=redis://default:wjrrU55bUrBm4ErJmjWEX40m1agpMYGB@redis-16598.c103.us-east-1-mz.ec2.redns.redis-cloud.com:16598
REDIS_HOST=redis-16598.c103.us-east-1-mz.ec2.redns.redis-cloud.com
REDIS_PORT=16598
REDIS_PASSWORD=wjrrU55bUrBm4ErJmjWEX40m1agpMYGB
JWT_SECRET=gofus-production-jwt-secret-2024
API_URL=https://gofus-backend.vercel.app
MAX_PLAYERS_PER_MAP=50
TICK_RATE=20
SAVE_INTERVAL=300000
COMBAT_TIMEOUT=120000
MAP_INSTANCE_TIMEOUT=600000
AI_UPDATE_INTERVAL=100
MAX_CONNECTIONS=5000
WORKER_THREADS=4
USE_CLUSTERING=false
METRICS_PORT=9090
LOG_LEVEL=info
LOG_FILE=game-server.log
RATE_LIMIT_WINDOW=60000
RATE_LIMIT_MAX_REQUESTS=100
MAX_PACKET_SIZE=1024
DEBUG=false
ENABLE_HOT_RELOAD=false
ALLOWED_ORIGINS=*
SUPABASE_URL=https://tfjlapqczjafecblvxjp.supabase.co
SUPABASE_ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InRmamxhcHFjemphZmVjYmx2eGpwIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjEzNDU4ODYsImV4cCI6MjA3NjkyMTg4Nn0.z-_A309HPhIBVEmkmBafpXudcDM5KLJESb0VE_wFdCU
SUPABASE_SERVICE_ROLE_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InRmamxhcHFjemphZmVjYmx2eGpwIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2MTM0NTg4NiwiZXhwIjoyMDc2OTIxODg2fQ.UbDcgBubXR9n7X16sjgJozPsGLDZVJ5pqjL0rutWBgw
WS_PING_INTERVAL=25000
WS_PING_TIMEOUT=60000
WS_MAX_PAYLOAD=1048576
COMBAT_DEFAULT_MODE=turn_based
COMBAT_MAX_CONCURRENT_REALTIME=100
COMBAT_TICK_RATE_MIN=50
COMBAT_TICK_RATE_MAX=500
COMBAT_ACTION_QUEUE_SIZE=5
COMBAT_ENABLE_MODE_SWITCHING=true
COMBAT_STATE_CACHE_TTL=5
COMBAT_BROADCAST_INTERVAL=100
COMBAT_MAX_PARTICIPANTS=10
REDIS_COMBAT_PREFIX=combat:
REDIS_COMBAT_TTL=3600
REDIS_SESSION_PREFIX=session:
REDIS_MAP_PREFIX=map:
REDIS_PLAYER_PREFIX=player:
```

### Step 4: Save and Wait for Redeploy
Railway will automatically redeploy the service with the new variables.

---

## What Happens Next

Once variables are set:
1. ⏳ Railway automatically redeploys (30-60 seconds)
2. ✅ Service starts successfully
3. ✅ Health checks pass (`/health` returns 200 OK)
4. ✅ Game server is ready to accept WebSocket connections

---

## Verify Deployment

### Check Logs
```bash
railway logs --service gofus-game-server
```

Look for:
```
✅ Game Server started on port 3001
Server ID: gs-railway-001
Region: us-east-1
Environment: production
Tick Rate: 20 TPS
```

### Test Health Endpoint
After Railway generates a domain:
```bash
curl https://your-domain.railway.app/health
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

### Generate Public Domain
```bash
railway domain
```

Or via dashboard: Settings → Networking → Generate Domain

---

## Current Build Status

✅ **Dockerfile**: Fixed and working
✅ **.dockerignore**: Fixed to include tsconfig.json
✅ **Build Stage**: Successfully compiled TypeScript
✅ **Production Image**: Created and pushed to Railway registry
✅ **Health Check Endpoint**: Configured at `/health`
✅ **Metrics Endpoint**: Available at `/metrics`

**Build Time**: 55.73 seconds
**Image Size**: Optimized with multi-stage build
**Region**: us-west1

---

## Troubleshooting

### If Health Checks Keep Failing:
1. Verify all environment variables are set correctly
2. Check that DATABASE_URL and REDIS_URL are valid
3. View deployment logs for specific errors

### If Service Crashes:
Common causes:
- Missing environment variables
- Invalid DATABASE_URL format
- Redis connection failed
- Database connection failed

Check logs:
```bash
railway logs --service gofus-game-server --deployment
```

---

## Files Reference

All environment variables are defined in:
- `.env.prod` - Production template (this file)
- `.env` - Development (already updated with real credentials)

---

**NEXT STEP**: Set the environment variables in Railway dashboard NOW! 🚀
