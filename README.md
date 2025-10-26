# GOFUS - Multiplayer MMORPG Game

A hybrid architecture MMORPG game inspired by Dofus, built with modern technologies for scalability and real-time gameplay.

## 🎮 Project Status

**Version**: 1.0.0
**Status**: ✅ **FULLY DEPLOYED AND OPERATIONAL**
**Deployment Date**: October 26, 2025
**Unity Client**: ✅ **PHASES 1-7 COMPLETE**

---

## 🏗️ Architecture

GOFUS uses a **hybrid architecture** combining serverless backend with a stateful game server:

- **Backend API** (Vercel): Serverless REST API for user management, marketplace, guilds, etc.
- **Game Server** (Railway): Stateful WebSocket server for real-time gameplay, combat, and multiplayer interactions
- **Database** (Supabase): PostgreSQL for persistent data storage
- **Cache** (Redis Cloud): Session management and real-time state caching

### Architecture Diagram

```
Unity/Web Client
      │
      ├─── HTTPS ────► Vercel Backend (REST API)
      │                     │
      └─── WebSocket ──► Railway Game Server
                            │
                    ┌───────┴────────┐
              Supabase DB      Redis Cache
```

See [docs/architecture/GOFUS_HYBRID_ARCHITECTURE.md](docs/architecture/GOFUS_HYBRID_ARCHITECTURE.md) for details.

---

## 🚀 Live URLs

### Backend API
- **URL**: https://gofus-backend-qyakdcmwa-andres-munozs-projects-fe137bcd.vercel.app
- **Health**: https://gofus-backend-qyakdcmwa-andres-munozs-projects-fe137bcd.vercel.app/api/health
- **API Docs**: https://gofus-backend-qyakdcmwa-andres-munozs-projects-fe137bcd.vercel.app/api/swagger

### Game Server
- **URL**: https://gofus-game-server-production.up.railway.app
- **WebSocket**: wss://gofus-game-server-production.up.railway.app
- **Health**: https://gofus-game-server-production.up.railway.app/health

---

## 📁 Project Structure

```
gofus/
├── gofus-backend/          # Vercel serverless backend (Next.js)
│   ├── app/api/           # API routes (28 endpoints)
│   ├── lib/               # Business logic, AI, combat system
│   ├── db/                # Drizzle ORM schemas (45 tables)
│   └── __tests__/         # Jest tests for AI system
│
├── gofus-game-server/     # Railway stateful game server (Node.js + Socket.IO)
│   ├── src/
│   │   ├── core/          # GameServer, managers
│   │   ├── managers/      # Player, Map, Combat, AI managers
│   │   ├── config/        # Server configuration
│   │   └── utils/         # Logger, helpers
│   ├── Dockerfile         # Production Docker configuration
│   └── railway.json       # Railway deployment config
│
└── docs/                  # Project documentation
    ├── deployment/        # Deployment guides and status
    ├── architecture/      # Architecture documentation
    ├── backend/           # Backend-specific docs
    ├── game-server/       # Game server-specific docs
    └── unity-client/      # Unity client documentation
```

---

## 🎯 Features

### Implemented ✅

#### Backend API (28 endpoints)
- **Authentication**: JWT-based auth, user registration, login
- **Characters**: Character creation, stats, leveling
- **Inventory**: Item management, equipment
- **Guilds**: Guild creation, membership, management
- **Marketplace**: Item listing, buying, selling
- **Trading**: Player-to-player trading
- **Chat**: Public and private messaging
- **Combat**: Turn-based and real-time combat system

#### AI Combat System
- **Influence Calculator**: Evaluates tactical positioning
- **Behavior Tree**: Decision-making AI for NPCs
- **ML Prediction Engine**: Predicts player actions
- **6 Difficulty Presets**: From easy to nightmare

#### Game Server (Real-time)
- **WebSocket**: Socket.IO for real-time communication
- **Game Loop**: 20 TPS tick rate
- **Managers**: Player, Map, Combat, Movement, AI, Chat
- **World State**: Synchronized game state
- **Day/Night Cycle**: Dynamic time system
- **Weather System**: Environmental effects

### Database Schema
- **45 Tables**: Users, characters, items, guilds, marketplace, combat, AI
- **AI Tables**: Combat presets, influence maps, behavior trees, ML models

---

## 🛠️ Technology Stack

### Backend
- **Framework**: Next.js 16 (App Router)
- **Runtime**: Node.js 20
- **Database ORM**: Drizzle ORM
- **Validation**: Zod
- **Testing**: Jest (51 AI system tests)
- **Deployment**: Vercel

### Game Server
- **Runtime**: Node.js 20
- **WebSocket**: Socket.IO
- **Database**: PostgreSQL (via Drizzle ORM)
- **Cache**: ioredis
- **Logging**: Winston
- **Containerization**: Docker
- **Deployment**: Railway

### Infrastructure
- **Database**: Supabase (PostgreSQL)
- **Cache**: Redis Cloud
- **Backend Hosting**: Vercel (Serverless)
- **Game Server Hosting**: Railway (Containers)

---

## 📖 Documentation

### Deployment
- [**Deployment Success**](docs/deployment/DEPLOYMENT_SUCCESS.md) - Complete deployment summary ✅
- [Backend Deployment](docs/backend/DEPLOYMENT_COMPLETE.md) - Vercel deployment details
- [Game Server Deployment](docs/game-server/RAILWAY_DEPLOYMENT.md) - Railway deployment guide

### Architecture
- [Hybrid Architecture](docs/architecture/GOFUS_HYBRID_ARCHITECTURE.md) - System architecture overview
- [Technical Analysis](docs/architecture/technical-analysis.md) - Technical specifications
- [Migration Overview](docs/architecture/migration-overview.md) - Migration strategy

### Unity Client
- [Client Implementation](docs/unity-client/GOFUS_UNITY_CLIENT_IMPLEMENTATION.md) - Unity client guide
- [Client Plan](docs/unity-client/GOFUS_UNITY_CLIENT_PLAN.md) - Development plan
- [Test Components](docs/unity-client/GOFUS_UNITY_TESTS_COMPONENTS.md) - Testing guide

### Component Documentation
- [Backend README](gofus-backend/README.md) - Backend API documentation
- [Game Server README](gofus-game-server/README.md) - Game server documentation

---

## 🚦 Getting Started

### Prerequisites
- Node.js 20+
- Docker (for game server)
- PostgreSQL database (or Supabase account)
- Redis instance (or Redis Cloud account)

### Backend Development

```bash
cd gofus-backend
npm install
cp .env.example .env
# Edit .env with your credentials
npm run dev
```

Backend runs on `http://localhost:3000`

### Game Server Development

```bash
cd gofus-game-server
npm install
cp .env.example .env
# Edit .env with your credentials
npm run dev
```

Game server runs on `ws://localhost:3001`

### Running Tests

```bash
# Backend AI tests
cd gofus-backend
npm test

# Game server tests
cd gofus-game-server
npm test
```

---

## 🐳 Docker Deployment

### Game Server

```bash
cd gofus-game-server
docker build -t gofus-game-server .
docker run -p 3001:3001 --env-file .env gofus-game-server
```

See [docs/game-server/RAILWAY_DEPLOYMENT.md](docs/game-server/RAILWAY_DEPLOYMENT.md) for Railway deployment.

---

## 🔐 Environment Variables

### Backend (.env)
```env
DATABASE_URL=postgresql://...
REDIS_URL=redis://...
JWT_SECRET=your-secret-key
NEXT_PUBLIC_API_URL=https://...
```

### Game Server (.env)
```env
NODE_ENV=production
PORT=3001
DATABASE_URL=postgresql://...
REDIS_URL=redis://...
JWT_SECRET=your-secret-key
API_URL=https://...
GAME_SERVER_ID=gs-001
TICK_RATE=20
MAX_PLAYERS_PER_MAP=50
```

See [docs/game-server/SET_ENV_VARIABLES.md](docs/game-server/SET_ENV_VARIABLES.md) for complete list.

---

## 📊 Performance

**Backend API**:
- Response time: < 100ms (typical)
- Database connection pooling: 20 connections
- Redis caching enabled

**Game Server**:
- Tick rate: 20 TPS
- Tick duration: < 1ms
- Memory usage: ~14MB
- Max players per map: 50
- Concurrent battles: Unlimited

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📝 License

This project is proprietary. All rights reserved.

---

## 🙏 Acknowledgments

- Inspired by Dofus MMORPG
- Built with modern web technologies
- Deployed on Vercel and Railway

---

## 📞 Support

For issues and questions:
- Check the [documentation](docs/)
- Review [deployment guides](docs/deployment/)
- Test the [live servers](#-live-urls)

---

**Last Updated**: October 26, 2025
**Status**: ✅ OPERATIONAL
