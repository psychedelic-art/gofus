# GOFUS Backend Migration Guide
## Java to Node.js Serverless Architecture

---

## Executive Summary

This document provides a comprehensive migration path from the current Java-based MMO server to a modern Node.js serverless architecture using **Next.js**, **Cloudflare Workers/Vercel**, **Supabase**, and **Redis**.

---

## Current Architecture Analysis

### Java Server Stack
- **Core Technology:** Java 15 with multi-threaded TCP socket server
- **Database:** MySQL/MariaDB with HikariCP connection pooling
- **Connections:** Raw TCP sockets on port 7780
- **State Management:** In-memory with periodic saves
- **Multi-Server:** Custom synchronization protocol on port 3435

### Key Components
1. **ServidorServer.java** - Main server listener (accepts connections)
2. **ServidorSocket.java** - Per-client connection handler
3. **Mundo.java** - Global world state (singleton pattern)
4. **GestorSQL.java** - Database connection management
5. **Pelea.java** - Combat system (190KB, turn-based)
6. **Personaje.java** - Character management (193KB)

---

## Target Architecture

### Hybrid Approach (Recommended)
Due to the real-time nature of MMO gameplay, a pure serverless approach has limitations. We recommend:

```
┌─────────────────────────────────────────────────┐
│            SERVERLESS LAYER                     │
│         (Next.js on Vercel/CF)                  │
├─────────────────────────────────────────────────┤
│ • REST API endpoints                            │
│ • Authentication & Authorization                │
│ • Marketplace & Trading                         │
│ • Guild Management                              │
│ • Leaderboards & Rankings                       │
│ • Admin Dashboard                               │
│ • Email/Push Notifications                      │
└─────────────────────────────────────────────────┘
                    ↕ API Calls
┌─────────────────────────────────────────────────┐
│          STATEFUL GAME SERVER                   │
│      (Node.js + Socket.IO on VPS)               │
├─────────────────────────────────────────────────┤
│ • Real-time gameplay                            │
│ • WebSocket connections                         │
│ • Combat engine                                 │
│ • Movement & Pathfinding                        │
│ • Map state management                          │
│ • Mob AI & NPCs                                 │
│ • Chat system                                   │
└─────────────────────────────────────────────────┘
                    ↕
┌─────────────────────────────────────────────────┐
│              DATA LAYER                         │
├─────────────────────────────────────────────────┤
│ Supabase (PostgreSQL) | Redis | Cloudflare R2  │
└─────────────────────────────────────────────────┘
```

---

## Migration Phases

### Phase 1: Infrastructure Setup (Week 1-2)

#### 1.1 Initialize Next.js Project
```bash
npx create-next-app@latest gofus-backend --typescript --app
cd gofus-backend

# Install core dependencies
npm install @supabase/supabase-js @supabase/auth-helpers-nextjs
npm install ioredis bullmq
npm install socket.io socket.io-client
npm install zod dotenv
```

#### 1.2 Project Structure
```
gofus-backend/
├── app/
│   ├── api/
│   │   ├── auth/
│   │   │   ├── login/route.ts
│   │   │   ├── register/route.ts
│   │   │   └── logout/route.ts
│   │   ├── character/
│   │   │   ├── [id]/route.ts
│   │   │   ├── create/route.ts
│   │   │   └── list/route.ts
│   │   ├── marketplace/
│   │   ├── guild/
│   │   └── admin/
│   └── (dashboard)/
├── lib/
│   ├── supabase/
│   │   ├── client.ts
│   │   └── admin.ts
│   ├── redis/
│   │   └── client.ts
│   ├── services/
│   ├── utils/
│   └── validators/
├── game-server/
│   ├── src/
│   ├── package.json
│   └── tsconfig.json
└── migrations/
```

#### 1.3 Database Setup (Supabase)
```sql
-- Create main tables
CREATE TABLE accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    login VARCHAR(100) UNIQUE NOT NULL,
    email VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW(),
    last_login TIMESTAMP,
    ban_status BOOLEAN DEFAULT FALSE,
    vip_status BOOLEAN DEFAULT FALSE
);

CREATE TABLE characters (
    id SERIAL PRIMARY KEY,
    account_id UUID REFERENCES accounts(id),
    name VARCHAR(50) UNIQUE NOT NULL,
    level INTEGER DEFAULT 1,
    class_id INTEGER NOT NULL,
    experience BIGINT DEFAULT 0,
    kamas INTEGER DEFAULT 0,
    map_id INTEGER DEFAULT 7411,
    cell_id INTEGER DEFAULT 311,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE items (
    id SERIAL PRIMARY KEY,
    character_id INTEGER REFERENCES characters(id),
    model_id INTEGER NOT NULL,
    quantity INTEGER DEFAULT 1,
    position INTEGER DEFAULT -1,
    stats JSONB
);

-- Enable Row Level Security
ALTER TABLE characters ENABLE ROW LEVEL SECURITY;
ALTER TABLE items ENABLE ROW LEVEL SECURITY;

-- Create RLS policies
CREATE POLICY "Users can only see their own characters"
    ON characters FOR SELECT
    USING (account_id = auth.uid());
```

---

### Phase 2: Core Services Migration (Week 3-6)

#### 2.1 Authentication Service
```typescript
// lib/services/auth-service.ts
import { createClient } from '@supabase/supabase-js'
import { Redis } from 'ioredis'
import bcrypt from 'bcryptjs'
import jwt from 'jsonwebtoken'

export class AuthService {
    private supabase = createClient(
        process.env.SUPABASE_URL!,
        process.env.SUPABASE_ANON_KEY!
    )
    private redis = new Redis(process.env.REDIS_URL!)

    async login(login: string, password: string) {
        // Migrate from Java authentication logic
        const { data: account } = await this.supabase
            .from('accounts')
            .select('*')
            .eq('login', login)
            .single()

        if (!account) {
            throw new Error('Invalid credentials')
        }

        // Verify password (migrate from Java encryption)
        const valid = await this.verifyPassword(password, account.password_hash)
        if (!valid) {
            throw new Error('Invalid credentials')
        }

        // Check if already logged in (anti-multiaccount)
        const existingSession = await this.redis.get(`session:${account.id}`)
        if (existingSession) {
            await this.kickPlayer(account.id)
        }

        // Create session
        const token = jwt.sign(
            { accountId: account.id, login: account.login },
            process.env.JWT_SECRET!,
            { expiresIn: '24h' }
        )

        // Store session in Redis
        await this.redis.setex(`session:${account.id}`, 86400, token)

        return { token, account }
    }

    async verifyPassword(password: string, hash: string): Promise<boolean> {
        // Port from Java Encriptador.java logic
        return bcrypt.compare(password, hash)
    }

    async kickPlayer(accountId: string) {
        // Notify game server to disconnect player
        await this.redis.publish('player:kick', accountId)
    }
}
```

#### 2.2 Character Service (Migrating Personaje.java)
```typescript
// lib/services/character-service.ts
import { createClient } from '@supabase/supabase-js'
import { Redis } from 'ioredis'
import { CharacterFormulas } from '../utils/formulas'

export class CharacterService {
    private supabase = createClient(
        process.env.SUPABASE_URL!,
        process.env.SUPABASE_SERVICE_KEY!
    )
    private redis = new Redis(process.env.REDIS_URL!)

    async getCharacter(characterId: number) {
        // Check cache first
        const cached = await this.redis.get(`character:${characterId}`)
        if (cached) return JSON.parse(cached)

        // Fetch from database with related data
        const { data: character, error } = await this.supabase
            .from('characters')
            .select(`
                *,
                items!items_character_id_fkey(*),
                spells:character_spells(*),
                stats:character_stats(*),
                guild:guild_members(guild:guilds(*))
            `)
            .eq('id', characterId)
            .single()

        if (error) throw error

        // Calculate total stats (port from Personaje.java)
        character.totalStats = this.calculateTotalStats(character)

        // Cache for 5 minutes
        await this.redis.setex(
            `character:${characterId}`,
            300,
            JSON.stringify(character)
        )

        return character
    }

    private calculateTotalStats(character: any) {
        // Port logic from Stats.java and TotalStats.java
        const baseStats = character.stats
        const itemStats = this.calculateItemStats(character.items)

        return {
            vitality: baseStats.vitality + itemStats.vitality,
            wisdom: baseStats.wisdom + itemStats.wisdom,
            strength: baseStats.strength + itemStats.strength,
            intelligence: baseStats.intelligence + itemStats.intelligence,
            chance: baseStats.chance + itemStats.chance,
            agility: baseStats.agility + itemStats.agility,
            // Calculate derived stats
            health: CharacterFormulas.calculateHealth(character.level, baseStats.vitality),
            ap: CharacterFormulas.calculateAP(character),
            mp: CharacterFormulas.calculateMP(character),
            initiative: CharacterFormulas.calculateInitiative(character)
        }
    }

    async levelUp(characterId: number) {
        const character = await this.getCharacter(characterId)

        // Calculate exp needed (from Formulas.java)
        const expNeeded = CharacterFormulas.getExpForLevel(character.level + 1)

        if (character.experience < expNeeded) {
            throw new Error('Not enough experience')
        }

        // Update level and stats
        const newLevel = character.level + 1
        const statPoints = CharacterFormulas.getStatPointsForLevel(newLevel)
        const spellPoints = CharacterFormulas.getSpellPointsForLevel(newLevel)

        await this.supabase
            .from('characters')
            .update({
                level: newLevel,
                stat_points: character.stat_points + statPoints,
                spell_points: character.spell_points + spellPoints
            })
            .eq('id', characterId)

        // Invalidate cache
        await this.redis.del(`character:${characterId}`)

        return this.getCharacter(characterId)
    }
}
```

#### 2.3 Marketplace Service (Migrating Mercadillo.java)
```typescript
// lib/services/marketplace-service.ts
export class MarketplaceService {
    async createListing(sellerId: number, itemId: number, price: number, quantity: number) {
        // Verify item ownership
        const { data: item } = await this.supabase
            .from('items')
            .select('*')
            .eq('id', itemId)
            .eq('character_id', sellerId)
            .single()

        if (!item || item.quantity < quantity) {
            throw new Error('Invalid item or quantity')
        }

        // Create listing
        const { data: listing } = await this.supabase
            .from('marketplace_listings')
            .insert({
                seller_id: sellerId,
                item_id: itemId,
                price: price,
                quantity: quantity,
                status: 'active',
                created_at: new Date()
            })
            .select()
            .single()

        // Remove item from inventory or reduce quantity
        if (item.quantity === quantity) {
            await this.supabase
                .from('items')
                .delete()
                .eq('id', itemId)
        } else {
            await this.supabase
                .from('items')
                .update({ quantity: item.quantity - quantity })
                .eq('id', itemId)
        }

        return listing
    }

    async purchaseListing(buyerId: number, listingId: number) {
        // Start transaction
        const { data: listing } = await this.supabase
            .from('marketplace_listings')
            .select('*, item:items(*), seller:characters(*)')
            .eq('id', listingId)
            .eq('status', 'active')
            .single()

        if (!listing) {
            throw new Error('Listing not found or already sold')
        }

        const { data: buyer } = await this.supabase
            .from('characters')
            .select('kamas')
            .eq('id', buyerId)
            .single()

        if (buyer.kamas < listing.price) {
            throw new Error('Insufficient kamas')
        }

        // Transfer kamas
        await this.supabase.rpc('transfer_kamas', {
            from_id: buyerId,
            to_id: listing.seller_id,
            amount: listing.price
        })

        // Transfer item
        await this.supabase
            .from('items')
            .insert({
                character_id: buyerId,
                model_id: listing.item.model_id,
                quantity: listing.quantity,
                stats: listing.item.stats
            })

        // Mark listing as sold
        await this.supabase
            .from('marketplace_listings')
            .update({ status: 'sold', sold_at: new Date() })
            .eq('id', listingId)

        return { success: true }
    }
}
```

---

### Phase 3: Game Server (Stateful) (Week 7-10)

#### 3.1 Main Game Server
```typescript
// game-server/src/index.ts
import express from 'express'
import { createServer } from 'http'
import { Server } from 'socket.io'
import { Redis } from 'ioredis'
import { MapManager } from './managers/MapManager'
import { CombatManager } from './managers/CombatManager'
import { MovementManager } from './managers/MovementManager'
import { ChatManager } from './managers/ChatManager'

const app = express()
const httpServer = createServer(app)
const io = new Server(httpServer, {
    cors: {
        origin: process.env.CLIENT_URL,
        credentials: true
    }
})

const redis = new Redis(process.env.REDIS_URL!)
const redisSub = new Redis(process.env.REDIS_URL!)

// Initialize managers
const mapManager = new MapManager(io, redis)
const combatManager = new CombatManager(io, redis)
const movementManager = new MovementManager(io, redis, mapManager)
const chatManager = new ChatManager(io, redis)

// Redis subscriptions for multi-server sync
redisSub.subscribe('player:kick', 'server:sync')
redisSub.on('message', (channel, message) => {
    switch (channel) {
        case 'player:kick':
            const socketId = connectedPlayers.get(message)
            if (socketId) {
                io.to(socketId).disconnect()
            }
            break
    }
})

// Socket.IO connection handling
io.on('connection', (socket) => {
    console.log('Client connected:', socket.id)

    // Authentication
    socket.on('auth', async (token: string) => {
        try {
            const payload = jwt.verify(token, process.env.JWT_SECRET!)
            socket.data.accountId = payload.accountId

            // Load character data
            const character = await characterService.getCharacter(payload.characterId)
            socket.data.character = character

            // Join map room
            socket.join(`map:${character.map_id}`)

            // Notify others in map
            socket.to(`map:${character.map_id}`).emit('player:joined', {
                id: character.id,
                name: character.name,
                level: character.level,
                cellId: character.cell_id
            })

            socket.emit('auth:success', character)
        } catch (error) {
            socket.emit('auth:failed', { error: error.message })
            socket.disconnect()
        }
    })

    // Movement
    socket.on('move', async (data: { path: number[] }) => {
        await movementManager.handleMovement(socket, data.path)
    })

    // Combat
    socket.on('combat:action', async (action: any) => {
        await combatManager.handleAction(socket, action)
    })

    // Chat
    socket.on('chat:message', async (message: any) => {
        await chatManager.handleMessage(socket, message)
    })

    // Disconnect
    socket.on('disconnect', () => {
        if (socket.data.character) {
            mapManager.removePlayer(socket.data.character.id)
            socket.to(`map:${socket.data.character.map_id}`).emit('player:left', {
                id: socket.data.character.id
            })
        }
    })
})

// Start server
const PORT = process.env.GAME_SERVER_PORT || 3001
httpServer.listen(PORT, () => {
    console.log(`Game server running on port ${PORT}`)
})
```

#### 3.2 Combat Manager (Migrating Pelea.java)
```typescript
// game-server/src/managers/CombatManager.ts
import { Server, Socket } from 'socket.io'
import { Redis } from 'ioredis'
import { Battle } from '../models/Battle'
import { CombatFormulas } from '../utils/CombatFormulas'

export class CombatManager {
    private battles: Map<string, Battle> = new Map()

    constructor(
        private io: Server,
        private redis: Redis
    ) {}

    async startBattle(attackerId: number, defenderId: number, mapId: number) {
        const battleId = `battle:${Date.now()}`

        // Create battle instance
        const battle = new Battle(battleId)
        battle.addTeam(1, [attackerId])
        battle.addTeam(2, [defenderId])

        // Initialize turn order (from Pelea.java logic)
        battle.initializeTurnOrder()

        this.battles.set(battleId, battle)

        // Notify players
        this.io.to(`map:${mapId}`).emit('battle:started', {
            battleId,
            fighters: battle.getFighters(),
            currentTurn: battle.getCurrentTurn()
        })

        // Start turn timer (120 seconds from config)
        this.startTurnTimer(battleId)

        return battleId
    }

    async handleAction(socket: Socket, action: any) {
        const battle = this.battles.get(action.battleId)
        if (!battle) {
            socket.emit('error', { message: 'Battle not found' })
            return
        }

        const fighter = battle.getFighter(socket.data.character.id)
        if (!fighter || !battle.isCurrentTurn(fighter.id)) {
            socket.emit('error', { message: 'Not your turn' })
            return
        }

        switch (action.type) {
            case 'move':
                await this.handleMovement(battle, fighter, action.path)
                break
            case 'spell':
                await this.handleSpellCast(battle, fighter, action)
                break
            case 'pass':
                await this.handlePassTurn(battle, fighter)
                break
        }

        // Check victory conditions
        if (battle.checkVictory()) {
            await this.endBattle(battle)
        } else {
            // Next turn
            battle.nextTurn()
            this.notifyTurnChange(battle)
        }
    }

    private async handleSpellCast(battle: Battle, caster: any, action: any) {
        const spell = await this.getSpell(action.spellId)

        // Validate AP cost
        if (caster.ap < spell.apCost) {
            throw new Error('Not enough AP')
        }

        // Validate range
        const distance = this.calculateDistance(caster.cellId, action.targetCellId)
        if (distance < spell.minRange || distance > spell.maxRange) {
            throw new Error('Target out of range')
        }

        // Calculate damage (port from Formulas.java)
        const damage = CombatFormulas.calculateDamage(
            caster,
            spell,
            battle.getFighterAt(action.targetCellId)
        )

        // Apply effects
        const effects = {
            damage: damage,
            targetId: action.targetCellId,
            spellId: action.spellId,
            critical: Math.random() < spell.criticalChance
        }

        // Broadcast to all battle participants
        this.io.to(`battle:${battle.id}`).emit('battle:spell', effects)

        // Update AP
        caster.ap -= spell.apCost
    }

    private startTurnTimer(battleId: string) {
        setTimeout(() => {
            const battle = this.battles.get(battleId)
            if (battle && !battle.isEnded) {
                this.handlePassTurn(battle, battle.getCurrentFighter())
            }
        }, 120000) // 120 seconds from config
    }
}
```

---

### Phase 4: Background Jobs (Week 11-12)

#### 4.1 Job Queue Setup
```typescript
// lib/queues/game-jobs.ts
import { Queue, Worker } from 'bullmq'
import { Redis } from 'ioredis'

const connection = new Redis(process.env.REDIS_URL!)

// Define queues
export const saveQueue = new Queue('save-state', { connection })
export const mobQueue = new Queue('mob-movement', { connection })
export const maintenanceQueue = new Queue('maintenance', { connection })

// Save worker (replaces contador() from ServidorServer.java)
const saveWorker = new Worker('save-state', async (job) => {
    console.log('Executing periodic save...')

    // Save all online players
    const players = await redis.smembers('players:online')
    for (const playerId of players) {
        await characterService.saveCharacter(playerId)
    }

    // Save world state
    await worldService.saveState()

    return { saved: players.length }
}, { connection })

// Mob movement worker
const mobWorker = new Worker('mob-movement', async (job) => {
    const { mapId } = job.data

    // Get all mobs on map
    const mobs = await mobService.getMobsOnMap(mapId)

    for (const mob of mobs) {
        if (mob.shouldMove()) {
            const newPosition = await pathfinding.getRandomCell(mob.position)
            await mobService.moveMob(mob.id, newPosition)

            // Notify players on map
            io.to(`map:${mapId}`).emit('mob:moved', {
                mobId: mob.id,
                position: newPosition
            })
        }
    }
}, { connection })

// Schedule recurring jobs
export async function scheduleJobs() {
    // Save every 4000 seconds (from config)
    await saveQueue.add('periodic-save', {}, {
        repeat: { every: 4000000 }
    })

    // Mob movement every 40 seconds
    await mobQueue.add('move-mobs', {}, {
        repeat: { every: 40000 }
    })

    // Daily maintenance
    await maintenanceQueue.add('daily-maintenance', {}, {
        repeat: { pattern: '0 4 * * *' } // 4 AM daily
    })
}
```

---

## Migration Strategy

### Step-by-Step Migration

#### Week 1-2: Foundation
- [ ] Set up Next.js project structure
- [ ] Configure Supabase database and auth
- [ ] Set up Redis for caching and queues
- [ ] Create base API endpoints

#### Week 3-4: Authentication & Characters
- [ ] Migrate authentication system
- [ ] Port character management
- [ ] Implement session management
- [ ] Create character API endpoints

#### Week 5-6: Core Game Systems
- [ ] Port inventory system
- [ ] Migrate guild system
- [ ] Implement marketplace
- [ ] Create admin dashboard

#### Week 7-8: Game Server
- [ ] Set up Socket.IO server
- [ ] Implement movement system
- [ ] Port map management
- [ ] Create chat system

#### Week 9-10: Combat System
- [ ] Port combat engine
- [ ] Implement spell system
- [ ] Create AI for mobs
- [ ] Test combat mechanics

#### Week 11-12: Background & Polish
- [ ] Set up job queues
- [ ] Implement periodic saves
- [ ] Create monitoring
- [ ] Performance optimization

---

## Database Migration

### MySQL to PostgreSQL
```bash
# Export MySQL data
mysqldump -h localhost -u root bustar_accounts > accounts.sql
mysqldump -h localhost -u root bustar_dinamicos > dynamic.sql
mysqldump -h localhost -u root bustar_estaticos > static.sql

# Convert to PostgreSQL
pgloader mysql://user:pass@localhost/bustar_accounts \
         postgresql://user:pass@db.supabase.co/postgres

# Or use custom migration script
npm run migrate:data
```

### Migration Script Example
```typescript
// scripts/migrate-data.ts
import mysql from 'mysql2/promise'
import { createClient } from '@supabase/supabase-js'

async function migrateCharacters() {
    const mysqlConn = await mysql.createConnection({
        host: 'localhost',
        user: 'root',
        password: 'password',
        database: 'bustar_dinamicos'
    })

    const supabase = createClient(
        process.env.SUPABASE_URL!,
        process.env.SUPABASE_SERVICE_KEY!
    )

    const [rows] = await mysqlConn.execute('SELECT * FROM personnages')

    for (const row of rows) {
        await supabase.from('characters').insert({
            id: row.id,
            account_id: row.account,
            name: row.name,
            level: row.level,
            class_id: row.class,
            experience: row.xp,
            kamas: row.kamas,
            map_id: row.mapID,
            cell_id: row.cell
        })
    }
}
```

---

## Performance Considerations

### Caching Strategy
- **Redis:** Session data, character cache, battle state
- **Cloudflare KV:** Static data (items, spells, maps)
- **Browser Cache:** Assets, UI components

### Scaling Plan
- **Horizontal Scaling:** Multiple game server instances
- **Load Balancing:** Cloudflare or custom LB
- **Database Sharding:** Partition by server/region
- **CDN:** Static assets via Cloudflare

---

## Monitoring & Observability

### Recommended Tools
1. **Sentry:** Error tracking
2. **Datadog/New Relic:** APM
3. **Grafana + Prometheus:** Metrics
4. **ELK Stack:** Logs

### Key Metrics
- Player count per server
- API response times
- WebSocket connection count
- Database query performance
- Redis memory usage
- Job queue processing times

---

## Security Considerations

### Authentication
- JWT tokens with refresh mechanism
- Rate limiting on auth endpoints
- IP-based anti-bruteforce
- 2FA support (optional)

### Game Security
- Server-authoritative gameplay
- Input validation on all endpoints
- Anti-cheat measures
- Encrypted WebSocket messages

### Data Protection
- Supabase RLS policies
- API key rotation
- Environment variable management
- GDPR compliance

---

## Cost Analysis

### Monthly Estimates

#### Development/Testing (Free Tier)
- Supabase: $0 (500MB DB)
- Redis: $0 (Upstash free tier)
- Vercel: $0
- **Total: $0/month**

#### Production (100-500 players)
- Supabase Pro: $25
- Redis Cloud: $10
- Game Server (Railway): $20
- Vercel Pro: $20
- **Total: $75/month**

#### Scale (1000+ players)
- Supabase Team: $599
- Redis Cluster: $50
- Multiple Game Servers: $100
- Cloudflare Workers: $25
- **Total: $774/month**

---

## Conclusion

This migration provides:
- ✅ Modern, scalable architecture
- ✅ Reduced operational complexity
- ✅ Better developer experience
- ✅ Cost-effective hosting
- ✅ Improved performance
- ✅ Easier maintenance

The hybrid approach balances the benefits of serverless with the requirements of real-time gameplay, ensuring a smooth migration path from Java to Node.js.