# GOFUS Game Server - Stateful Real-time Engine

## Overview

The GOFUS Game Server is the stateful, real-time component of the hybrid architecture that handles:
- Real-time gameplay (movement, combat, interactions)
- WebSocket connections for live game state
- AI-driven NPCs and combat intelligence
- Map state management and synchronization
- Multi-server orchestration

This server works in conjunction with the serverless backend (Next.js) which handles:
- REST APIs (auth, marketplace, guilds)
- Persistent data storage
- Static content delivery
- Admin dashboards

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    CLIENT LAYER                              │
│  Unity 2D Client ←→ WebSocket ←→ Game Server                │
│                  ←→ HTTPS     ←→ Next.js API                │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                 HYBRID ARCHITECTURE                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Serverless (Next.js/Vercel)    Stateful (Node.js/VPS)     │
│  ┌──────────────────┐           ┌──────────────────┐       │
│  │ • Authentication │           │ • Game Loop      │       │
│  │ • Marketplace    │    API    │ • Combat Engine  │       │
│  │ • Guild System   │ ←────────→│ • Movement/Path  │       │
│  │ • Leaderboards   │           │ • AI System      │       │
│  │ • Admin Panel    │           │ • Chat/Real-time │       │
│  └──────────────────┘           └──────────────────┘       │
│           ↓                              ↓                   │
│  ┌────────────────────────────────────────────────┐        │
│  │        Shared Data Layer (PostgreSQL + Redis)   │        │
│  └────────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

```
gofus-game-server/
├── src/
│   ├── index.ts                 # Main server entry point
│   ├── config/
│   │   ├── server.config.ts     # Server configuration
│   │   ├── game.config.ts       # Game constants
│   │   └── database.config.ts   # DB connections
│   │
│   ├── core/
│   │   ├── GameServer.ts        # Main game server class
│   │   ├── GameLoop.ts          # Game loop manager
│   │   ├── WorldState.ts        # Global world state
│   │   └── PlayerManager.ts     # Player session handling
│   │
│   ├── managers/
│   │   ├── MapManager.ts        # Map instances & state
│   │   ├── CombatManager.ts     # Combat system
│   │   ├── MovementManager.ts   # Movement & pathfinding
│   │   ├── ChatManager.ts       # Chat system
│   │   └── AIManager.ts         # AI & NPC behavior
│   │
│   ├── systems/
│   │   ├── combat/
│   │   │   ├── Battle.ts        # Battle instance
│   │   │   ├── Fighter.ts       # Fighter entity
│   │   │   ├── SpellSystem.ts   # Spell mechanics
│   │   │   └── CombatAI.ts      # AI combat decisions
│   │   │
│   │   ├── ai/
│   │   │   ├── NPCBehavior.ts   # NPC AI behaviors
│   │   │   ├── MobAI.ts         # Monster AI
│   │   │   ├── PathfindingAI.ts # A* pathfinding
│   │   │   └── StrategicAI.ts   # Advanced combat AI
│   │   │
│   │   ├── physics/
│   │   │   ├── Movement.ts      # Movement validation
│   │   │   ├── Collision.ts     # Collision detection
│   │   │   └── LineOfSight.ts   # LoS calculations
│   │   │
│   │   └── synchronization/
│   │       ├── StateSyncManager.ts
│   │       ├── MultiServerSync.ts
│   │       └── ClientReconciliation.ts
│   │
│   ├── entities/
│   │   ├── Player.ts
│   │   ├── NPC.ts
│   │   ├── Monster.ts
│   │   └── GameObject.ts
│   │
│   ├── network/
│   │   ├── SocketHandler.ts     # Socket.IO handler
│   │   ├── PacketProcessor.ts   # Packet processing
│   │   ├── Protocol.ts          # Network protocol
│   │   └── RateLimiter.ts       # Connection limits
│   │
│   ├── utils/
│   │   ├── Formulas.ts          # Game formulas
│   │   ├── Logger.ts            # Logging system
│   │   ├── Performance.ts       # Performance monitoring
│   │   └── Validator.ts         # Input validation
│   │
│   └── jobs/
│       ├── SaveStateJob.ts      # Periodic saves
│       ├── MobRespawnJob.ts     # Mob respawning
│       └── MaintenanceJob.ts    # Server maintenance
│
├── tests/
│   ├── unit/
│   ├── integration/
│   └── load/
│
├── scripts/
│   ├── migrate-maps.ts          # Map data migration
│   ├── seed-npcs.ts            # NPC data seeding
│   └── performance-test.ts     # Load testing
│
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
│
├── .env.example
├── package.json
├── tsconfig.json
└── README.md
```

## Technology Stack

- **Runtime**: Node.js 20 LTS
- **Language**: TypeScript 5.x
- **WebSocket**: Socket.IO 4.x
- **Database**: PostgreSQL (Supabase) + Redis
- **Queue**: BullMQ
- **Monitoring**: Prometheus + Grafana
- **Testing**: Jest + Artillery (load testing)

## Game Modes Support

### PvP Mode (Player vs Player)
- Real-time combat with turn-based mechanics
- Matchmaking system
- ELO rating system
- Tournament support
- Anti-cheat measures

### PvE Mode (Player vs Environment)
- AI-driven monsters and NPCs
- Quest system integration
- Dungeon instances
- Boss mechanics
- Dynamic difficulty adjustment

## Installation

```bash
# Clone repository
git clone https://github.com/yourusername/gofus-game-server
cd gofus-game-server

# Install dependencies
npm install

# Copy environment file
cp .env.example .env

# Build TypeScript
npm run build

# Run development server
npm run dev

# Run production server
npm run start
```

## Environment Configuration

```env
# Server Configuration
NODE_ENV=development
PORT=3001
GAME_SERVER_ID=gs-001
REGION=us-east-1

# Database
DATABASE_URL=postgresql://...
REDIS_URL=redis://...

# Authentication
JWT_SECRET=your-secret-key
API_URL=http://localhost:3000

# Game Configuration
MAX_PLAYERS_PER_MAP=50
TICK_RATE=20
SAVE_INTERVAL=300000
COMBAT_TIMEOUT=120000

# Monitoring
METRICS_PORT=9090
LOG_LEVEL=info
```

## Development

```bash
# Run in development mode with hot reload
npm run dev

# Run tests
npm test

# Run specific test suite
npm test:unit
npm test:integration
npm test:load

# Lint code
npm run lint

# Format code
npm run format

# Generate documentation
npm run docs
```

## Deployment

See [DEPLOYMENT.md](./docs/DEPLOYMENT.md) for production deployment instructions.

## Documentation

- [Architecture Overview](./docs/ARCHITECTURE.md)
- [API Reference](./docs/API.md)
- [Game Mechanics](./docs/GAME_MECHANICS.md)
- [AI System](./docs/AI_SYSTEM.md)
- [Performance Tuning](./docs/PERFORMANCE.md)

## License

Private - All Rights Reserved