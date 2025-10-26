# GOFUS Hybrid Mode Architecture
## Turn-Based + Real-Time Combat System Design

---

## Executive Summary

This document outlines the comprehensive architecture and implementation plan for adding real-time (non-turn) mode to the existing GOFUS backend while maintaining full compatibility with the turn-based mode. The system will support configurable game modes, allowing fights to be initiated in either turn-based or real-time mode based on game settings, player preferences, or specific game scenarios.

---

## Current State Analysis

### Existing Turn-Based Implementation

The current `CombatService` implements a classic turn-based system with:
- **Turn Order**: Randomized initiative-based turn sequence
- **Action Points (AP)**: 6 per turn for spell casting
- **Movement Points (MP)**: 3 per turn for movement
- **Grid-based Combat**: 14x20 cell grid (Dofus-style)
- **Action Types**: Spell, Move, Pass
- **Turn Management**: Sequential turn processing with AP/MP refresh

### Key Strengths to Preserve
1. Robust state management in PostgreSQL
2. Action logging for replay capability
3. Clear separation of concerns
4. WebSocket infrastructure for real-time updates
5. Well-structured database schema

### Limitations to Address
1. Turn-based only - no real-time option
2. Fixed AP/MP refresh rate
3. No simultaneous action processing
4. Limited action queuing
5. No cooldown system for real-time mode

---

## Hybrid Architecture Design

### Core Principles

1. **Mode Agnostic Core**: Combat logic that works for both modes
2. **Strategy Pattern**: Different combat processors for each mode
3. **Event-Driven Architecture**: Unified event system for both modes
4. **State Synchronization**: Consistent state management across modes
5. **Backwards Compatibility**: Zero breaking changes to existing APIs

### High-Level Architecture

```typescript
┌─────────────────────────────────────────────────────────┐
│                    Combat Manager                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────┐      ┌──────────────────┐       │
│  │  Mode Selector   │      │  State Manager   │       │
│  └──────────────────┘      └──────────────────┘       │
│           │                          │                  │
│    ┌──────┴────────┐         ┌──────┴────────┐        │
│    │  Turn-Based   │         │   Real-Time   │        │
│    │   Processor   │         │   Processor   │        │
│    └───────────────┘         └───────────────┘        │
│           │                          │                  │
│    ┌──────┴──────────────────────────┴────────┐       │
│    │          Unified Action Handler           │       │
│    └───────────────────────────────────────────┘       │
│                         │                               │
│    ┌────────────────────┴────────────────────┐        │
│    │         Database & Event System          │        │
│    └──────────────────────────────────────────┘        │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## Database Schema Modifications

### 1. Update `fights` Table

```sql
ALTER TABLE fights ADD COLUMN IF NOT EXISTS combat_mode VARCHAR(20) DEFAULT 'turn_based';
ALTER TABLE fights ADD COLUMN IF NOT EXISTS config JSONB DEFAULT '{}';
ALTER TABLE fights ADD COLUMN IF NOT EXISTS tick_rate INTEGER DEFAULT 100; -- ms between ticks for real-time
ALTER TABLE fights ADD COLUMN IF NOT EXISTS last_tick_at TIMESTAMP;

-- Add index for active real-time fights
CREATE INDEX idx_fights_realtime_active ON fights(id)
WHERE combat_mode = 'real_time' AND status = 'active';
```

### 2. Update `fight_participants` Table

```sql
ALTER TABLE fight_participants ADD COLUMN IF NOT EXISTS ap_regen_rate FLOAT DEFAULT 1.0;
ALTER TABLE fight_participants ADD COLUMN IF NOT EXISTS mp_regen_rate FLOAT DEFAULT 0.5;
ALTER TABLE fight_participants ADD COLUMN IF NOT EXISTS last_ap_regen TIMESTAMP;
ALTER TABLE fight_participants ADD COLUMN IF NOT EXISTS last_mp_regen TIMESTAMP;
ALTER TABLE fight_participants ADD COLUMN IF NOT EXISTS action_queue JSONB DEFAULT '[]';
ALTER TABLE fight_participants ADD COLUMN IF NOT EXISTS cooldowns JSONB DEFAULT '{}';
```

### 3. Update `fight_actions` Table

```sql
ALTER TABLE fight_actions ADD COLUMN IF NOT EXISTS timestamp TIMESTAMP DEFAULT NOW();
ALTER TABLE fight_actions ADD COLUMN IF NOT EXISTS execution_time INTEGER; -- ms taken to execute
ALTER TABLE fight_actions ADD COLUMN IF NOT EXISTS queued_at TIMESTAMP;
ALTER TABLE fight_actions ADD COLUMN IF NOT EXISTS processed_at TIMESTAMP;
```

### 4. New `combat_configurations` Table

```sql
CREATE TABLE IF NOT EXISTS combat_configurations (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    mode VARCHAR(20) NOT NULL, -- 'turn_based' or 'real_time'
    config JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Default configurations
INSERT INTO combat_configurations (name, mode, config) VALUES
('classic_turn_based', 'turn_based', '{
    "turn_time_limit": 120,
    "ap_per_turn": 6,
    "mp_per_turn": 3,
    "allow_flee": true
}'),
('fast_real_time', 'real_time', '{
    "tick_rate": 100,
    "ap_regen_per_second": 2,
    "mp_regen_per_second": 1,
    "global_cooldown": 500,
    "spell_queue_size": 3,
    "allow_flee": false
}'),
('tactical_real_time', 'real_time', '{
    "tick_rate": 200,
    "ap_regen_per_second": 1,
    "mp_regen_per_second": 0.5,
    "global_cooldown": 1000,
    "spell_queue_size": 2,
    "allow_flee": true
}');
```

---

## Implementation Components

### 1. Combat Mode Interface

```typescript
// lib/services/combat/interfaces/ICombatMode.ts
export interface ICombatMode {
    mode: 'turn_based' | 'real_time';

    // Lifecycle
    initializeFight(fight: Fight, participants: Participant[]): Promise<void>;
    startFight(fightId: number): Promise<void>;
    endFight(fightId: number, winnerId: number): Promise<void>;

    // Action handling
    validateAction(fight: Fight, actor: Participant, action: Action): Promise<boolean>;
    processAction(fight: Fight, actor: Participant, action: Action): Promise<ActionResult>;

    // State management
    updateState(fightId: number): Promise<void>;
    getState(fightId: number): Promise<FightState>;

    // Resource management
    handleResourceRegeneration(fightId: number): Promise<void>;
}
```

### 2. Turn-Based Processor (Existing Enhanced)

```typescript
// lib/services/combat/processors/TurnBasedProcessor.ts
import { ICombatMode } from '../interfaces/ICombatMode';

export class TurnBasedProcessor implements ICombatMode {
    mode = 'turn_based' as const;

    async initializeFight(fight: Fight, participants: Participant[]): Promise<void> {
        // Existing turn order logic
        const turnOrder = this.calculateTurnOrder(participants);
        await this.saveTurnOrder(fight.id, turnOrder);
    }

    async validateAction(fight: Fight, actor: Participant, action: Action): Promise<boolean> {
        // Check if it's actor's turn
        if (!this.isActorsTurn(fight, actor)) {
            return false;
        }

        // Existing validation logic
        return this.validateResources(actor, action);
    }

    async processAction(fight: Fight, actor: Participant, action: Action): Promise<ActionResult> {
        // Process action immediately (existing logic)
        const result = await this.executeAction(fight, actor, action);

        // Check for turn end
        if (this.shouldEndTurn(actor)) {
            await this.endTurn(fight.id, actor.id);
        }

        return result;
    }

    async handleResourceRegeneration(fightId: number): Promise<void> {
        // Refresh AP/MP at turn start (existing logic)
        const currentActor = await this.getCurrentActor(fightId);
        await this.refreshResources(currentActor);
    }
}
```

### 3. Real-Time Processor (New)

```typescript
// lib/services/combat/processors/RealTimeProcessor.ts
import { ICombatMode } from '../interfaces/ICombatMode';
import { Queue } from 'bullmq';

export class RealTimeProcessor implements ICombatMode {
    mode = 'real_time' as const;
    private tickQueues: Map<number, NodeJS.Timeout> = new Map();
    private actionQueues: Map<number, ActionQueue> = new Map();

    async initializeFight(fight: Fight, participants: Participant[]): Promise<void> {
        // Initialize real-time specific state
        await this.setupActionQueues(fight.id, participants);
        await this.initializeResourceTimers(fight.id, participants);

        // No turn order needed
        await db.update(fights)
            .set({
                turnOrder: null,
                currentTurn: null
            })
            .where(eq(fights.id, fight.id));
    }

    async startFight(fightId: number): Promise<void> {
        const fight = await this.getFight(fightId);
        const tickRate = fight.config?.tick_rate || 100;

        // Start game loop
        const ticker = setInterval(async () => {
            await this.gameTick(fightId);
        }, tickRate);

        this.tickQueues.set(fightId, ticker);
    }

    private async gameTick(fightId: number): Promise<void> {
        try {
            // 1. Process resource regeneration
            await this.processResourceRegeneration(fightId);

            // 2. Process queued actions
            await this.processActionQueue(fightId);

            // 3. Update cooldowns
            await this.updateCooldowns(fightId);

            // 4. Check victory conditions
            const result = await this.checkVictoryConditions(fightId);
            if (result.isEnded) {
                await this.endFight(fightId, result.winnerId);
            }

            // 5. Broadcast state update
            await this.broadcastStateUpdate(fightId);

        } catch (error) {
            console.error(`Game tick error for fight ${fightId}:`, error);
        }
    }

    async validateAction(fight: Fight, actor: Participant, action: Action): Promise<boolean> {
        // Check resources
        if (!this.hasRequiredResources(actor, action)) {
            return false;
        }

        // Check cooldowns
        if (this.isOnCooldown(actor, action)) {
            return false;
        }

        // Check action queue capacity
        const queue = this.actionQueues.get(fight.id);
        if (queue && queue.isFull(actor.id)) {
            return false;
        }

        return true;
    }

    async processAction(fight: Fight, actor: Participant, action: Action): Promise<ActionResult> {
        // Queue action for processing
        const queue = this.actionQueues.get(fight.id);
        if (!queue) {
            throw new Error('Action queue not initialized');
        }

        // Add to queue with timestamp
        const queuedAction = {
            ...action,
            actorId: actor.id,
            queuedAt: new Date(),
            priority: this.calculatePriority(action)
        };

        queue.enqueue(queuedAction);

        // Return queued status immediately
        return {
            success: true,
            queued: true,
            queuePosition: queue.getPosition(queuedAction)
        };
    }

    private async processActionQueue(fightId: number): Promise<void> {
        const queue = this.actionQueues.get(fightId);
        if (!queue || queue.isEmpty()) {
            return;
        }

        const participants = await this.getParticipants(fightId);

        // Process actions in priority order
        while (!queue.isEmpty()) {
            const queuedAction = queue.dequeue();
            const actor = participants.find(p => p.id === queuedAction.actorId);

            if (!actor || !actor.isAlive) {
                continue;
            }

            // Validate action is still valid
            if (!await this.validateAction(await this.getFight(fightId), actor, queuedAction)) {
                continue;
            }

            // Execute action
            await this.executeAction(fightId, actor, queuedAction);

            // Apply cooldown
            await this.applyCooldown(actor, queuedAction);
        }
    }

    private async processResourceRegeneration(fightId: number): Promise<void> {
        const participants = await this.getParticipants(fightId);
        const fight = await this.getFight(fightId);
        const config = fight.config as RealTimeConfig;

        const now = new Date();

        for (const participant of participants) {
            if (!participant.isAlive) continue;

            // Calculate time since last regen
            const apTimeDiff = now.getTime() - (participant.last_ap_regen?.getTime() || now.getTime());
            const mpTimeDiff = now.getTime() - (participant.last_mp_regen?.getTime() || now.getTime());

            // Calculate regen amount
            const apRegen = (apTimeDiff / 1000) * (config.ap_regen_per_second || 1);
            const mpRegen = (mpTimeDiff / 1000) * (config.mp_regen_per_second || 0.5);

            // Apply regeneration
            const newAp = Math.min(participant.maxAp, participant.currentAp + apRegen);
            const newMp = Math.min(participant.maxMp, participant.currentMp + mpRegen);

            if (newAp !== participant.currentAp || newMp !== participant.currentMp) {
                await db.update(fightParticipants)
                    .set({
                        currentAp: newAp,
                        currentMp: newMp,
                        last_ap_regen: now,
                        last_mp_regen: now
                    })
                    .where(eq(fightParticipants.id, participant.id));
            }
        }
    }

    private async updateCooldowns(fightId: number): Promise<void> {
        const participants = await this.getParticipants(fightId);
        const now = Date.now();

        for (const participant of participants) {
            const cooldowns = participant.cooldowns as Record<string, number>;
            const updatedCooldowns: Record<string, number> = {};

            for (const [key, endTime] of Object.entries(cooldowns)) {
                if (endTime > now) {
                    updatedCooldowns[key] = endTime;
                }
            }

            if (Object.keys(updatedCooldowns).length !== Object.keys(cooldowns).length) {
                await db.update(fightParticipants)
                    .set({ cooldowns: updatedCooldowns })
                    .where(eq(fightParticipants.id, participant.id));
            }
        }
    }

    async endFight(fightId: number, winnerId: number): Promise<void> {
        // Stop game loop
        const ticker = this.tickQueues.get(fightId);
        if (ticker) {
            clearInterval(ticker);
            this.tickQueues.delete(fightId);
        }

        // Clear action queue
        this.actionQueues.delete(fightId);

        // Update fight status
        await db.update(fights)
            .set({
                status: 'completed',
                winnerId,
                endedAt: new Date()
            })
            .where(eq(fights.id, fightId));
    }
}
```

### 4. Unified Combat Service

```typescript
// lib/services/combat/CombatServiceV2.ts
import { TurnBasedProcessor } from './processors/TurnBasedProcessor';
import { RealTimeProcessor } from './processors/RealTimeProcessor';
import { ICombatMode } from './interfaces/ICombatMode';

export class CombatServiceV2 {
    private processors: Map<string, ICombatMode> = new Map();

    constructor() {
        this.processors.set('turn_based', new TurnBasedProcessor());
        this.processors.set('real_time', new RealTimeProcessor());
    }

    /**
     * Start a fight with specified mode
     */
    async startFight(
        team0CharacterIds: number[],
        team1Ids: number[],
        fightType: 'pvp' | 'pve',
        combatMode: 'turn_based' | 'real_time',
        mapId: number,
        configName?: string
    ): Promise<FightResponse> {
        // Load configuration
        const config = configName
            ? await this.loadConfiguration(configName)
            : this.getDefaultConfig(combatMode);

        // Create fight record
        const fight = await this.createFightRecord({
            type: fightType,
            combat_mode: combatMode,
            config,
            mapId,
            status: 'initializing'
        });

        // Create participants
        const participants = await this.createParticipants(
            fight.id,
            team0CharacterIds,
            team1Ids,
            fightType
        );

        // Get appropriate processor
        const processor = this.processors.get(combatMode);
        if (!processor) {
            throw new Error(`Unknown combat mode: ${combatMode}`);
        }

        // Initialize fight with mode-specific logic
        await processor.initializeFight(fight, participants);

        // Start the fight
        await processor.startFight(fight.id);

        // Update status to active
        await db.update(fights)
            .set({ status: 'active' })
            .where(eq(fights.id, fight.id));

        return {
            success: true,
            fight,
            mode: combatMode
        };
    }

    /**
     * Perform an action (works for both modes)
     */
    async performAction(
        fightId: number,
        actorId: number,
        action: Action
    ): Promise<ActionResponse> {
        const fight = await this.getFight(fightId);
        const processor = this.processors.get(fight.combat_mode);

        if (!processor) {
            throw new Error(`Unknown combat mode: ${fight.combat_mode}`);
        }

        const actor = await this.getParticipant(fightId, actorId);

        // Validate action
        const isValid = await processor.validateAction(fight, actor, action);
        if (!isValid) {
            throw new Error('Invalid action');
        }

        // Process action
        return await processor.processAction(fight, actor, action);
    }

    /**
     * Get current fight state (unified for both modes)
     */
    async getFightState(fightId: number): Promise<FightState> {
        const fight = await this.getFight(fightId);
        const processor = this.processors.get(fight.combat_mode);

        if (!processor) {
            throw new Error(`Unknown combat mode: ${fight.combat_mode}`);
        }

        return await processor.getState(fightId);
    }

    /**
     * Convert a turn-based fight to real-time (special feature)
     */
    async convertToRealTime(fightId: number): Promise<void> {
        const fight = await this.getFight(fightId);

        if (fight.combat_mode === 'real_time') {
            throw new Error('Fight is already in real-time mode');
        }

        // Stop turn-based processing
        const turnBasedProcessor = this.processors.get('turn_based');
        await turnBasedProcessor?.endFight(fightId, -1);

        // Update mode
        await db.update(fights)
            .set({
                combat_mode: 'real_time',
                config: this.getDefaultConfig('real_time')
            })
            .where(eq(fights.id, fightId));

        // Initialize real-time processing
        const realTimeProcessor = this.processors.get('real_time');
        const participants = await this.getParticipants(fightId);
        await realTimeProcessor?.initializeFight(fight, participants);
        await realTimeProcessor?.startFight(fightId);
    }
}
```

### 5. WebSocket Event System

```typescript
// lib/websocket/combat-events.ts
export class CombatEventEmitter {
    private io: Server;

    constructor(io: Server) {
        this.io = io;
    }

    /**
     * Emit events based on combat mode
     */
    emitCombatEvent(fightId: number, event: CombatEvent): void {
        const room = `fight:${fightId}`;

        switch (event.type) {
            case 'turn_start':
                this.io.to(room).emit('combat:turn:start', {
                    fightId,
                    actorId: event.actorId,
                    turnNumber: event.turnNumber
                });
                break;

            case 'action_queued':
                this.io.to(room).emit('combat:action:queued', {
                    fightId,
                    actorId: event.actorId,
                    action: event.action,
                    queuePosition: event.queuePosition
                });
                break;

            case 'action_executed':
                this.io.to(room).emit('combat:action:executed', {
                    fightId,
                    actorId: event.actorId,
                    action: event.action,
                    result: event.result,
                    timestamp: event.timestamp
                });
                break;

            case 'resource_update':
                this.io.to(room).emit('combat:resource:update', {
                    fightId,
                    updates: event.updates // [{actorId, ap, mp}]
                });
                break;

            case 'state_sync':
                this.io.to(room).emit('combat:state:sync', {
                    fightId,
                    state: event.state,
                    mode: event.mode,
                    timestamp: event.timestamp
                });
                break;
        }
    }

    /**
     * Broadcast state updates at different rates based on mode
     */
    setupStateBroadcast(fightId: number, mode: 'turn_based' | 'real_time'): void {
        if (mode === 'real_time') {
            // Broadcast state every 100ms for real-time
            const interval = setInterval(async () => {
                const state = await this.combatService.getFightState(fightId);
                this.emitCombatEvent(fightId, {
                    type: 'state_sync',
                    state,
                    mode,
                    timestamp: Date.now()
                });
            }, 100);

            this.intervals.set(fightId, interval);
        }
        // Turn-based only broadcasts on action completion
    }
}
```

---

## API Endpoints

### 1. Create Fight with Mode

```typescript
// app/api/fights/route.ts
export async function POST(request: Request) {
    const body = await request.json();
    const {
        team0CharacterIds,
        team1Ids,
        fightType,
        combatMode = 'turn_based', // Default to turn-based
        configName,
        mapId
    } = body;

    const combatService = new CombatServiceV2();

    const result = await combatService.startFight(
        team0CharacterIds,
        team1Ids,
        fightType,
        combatMode,
        mapId,
        configName
    );

    return NextResponse.json(result);
}
```

### 2. Get Available Combat Configurations

```typescript
// app/api/fights/configurations/route.ts
export async function GET() {
    const configs = await db
        .select()
        .from(combat_configurations);

    return NextResponse.json({
        configurations: configs,
        modes: ['turn_based', 'real_time']
    });
}
```

### 3. Convert Fight Mode

```typescript
// app/api/fights/[id]/convert/route.ts
export async function POST(
    request: Request,
    { params }: { params: { id: string } }
) {
    const fightId = parseInt(params.id);
    const { targetMode } = await request.json();

    const combatService = new CombatServiceV2();

    if (targetMode === 'real_time') {
        await combatService.convertToRealTime(fightId);
    } else {
        throw new Error('Only conversion to real-time is supported');
    }

    return NextResponse.json({ success: true });
}
```

---

## Configuration Examples

### Turn-Based Configuration
```json
{
    "name": "classic_turn_based",
    "mode": "turn_based",
    "config": {
        "turn_time_limit": 120,
        "ap_per_turn": 6,
        "mp_per_turn": 3,
        "allow_flee": true,
        "auto_end_turn": true,
        "show_turn_order": true
    }
}
```

### Real-Time Configuration
```json
{
    "name": "action_real_time",
    "mode": "real_time",
    "config": {
        "tick_rate": 50,
        "ap_regen_per_second": 3,
        "mp_regen_per_second": 1.5,
        "global_cooldown": 250,
        "spell_queue_size": 5,
        "allow_flee": false,
        "max_fight_duration": 600,
        "action_prediction": true,
        "lag_compensation": true
    }
}
```

### Hybrid Configuration (Turn-Based with Timer)
```json
{
    "name": "speed_chess_mode",
    "mode": "turn_based",
    "config": {
        "turn_time_limit": 15,
        "ap_per_turn": 4,
        "mp_per_turn": 2,
        "auto_pass_on_timeout": true,
        "penalty_for_timeout": {
            "ap_reduction": 1,
            "mp_reduction": 1
        }
    }
}
```

---

## Migration Strategy

### Phase 1: Database Updates (Week 1)
1. Create migration scripts for schema changes
2. Add new tables and columns
3. Create default configurations
4. Test database integrity

### Phase 2: Core Implementation (Week 2-3)
1. Implement ICombatMode interface
2. Create RealTimeProcessor
3. Refactor existing combat service to TurnBasedProcessor
4. Implement CombatServiceV2

### Phase 3: WebSocket Integration (Week 4)
1. Update WebSocket event handlers
2. Implement real-time state broadcasting
3. Add action queue visualization
4. Test latency and synchronization

### Phase 4: Testing & Optimization (Week 5-6)
1. Unit tests for both processors
2. Integration tests for mode switching
3. Load testing for real-time mode
4. Performance optimization

---

## Performance Considerations

### Real-Time Mode Optimization

1. **Action Queue Management**
   - Use priority queue for action processing
   - Implement action prediction on client
   - Buffer actions to reduce network calls

2. **State Synchronization**
   - Delta compression for state updates
   - Adaptive sync rates based on activity
   - Client-side interpolation

3. **Resource Management**
   - Connection pooling for database
   - Redis caching for active fights
   - Memory-efficient data structures

### Scalability Targets

- **Turn-Based**: 1000+ concurrent fights
- **Real-Time**: 100+ concurrent fights per server
- **Tick Rate**: 50-200ms configurable
- **Latency**: < 100ms for action acknowledgment
- **State Sync**: 10-20 updates per second

---

## Testing Strategy

### Unit Tests
```typescript
describe('CombatServiceV2', () => {
    describe('Turn-Based Mode', () => {
        it('should maintain turn order');
        it('should refresh AP/MP per turn');
        it('should enforce turn-based validation');
    });

    describe('Real-Time Mode', () => {
        it('should process action queues');
        it('should regenerate resources over time');
        it('should handle simultaneous actions');
        it('should apply cooldowns correctly');
    });

    describe('Mode Conversion', () => {
        it('should convert from turn-based to real-time');
        it('should preserve fight state during conversion');
        it('should handle active actions during conversion');
    });
});
```

### Integration Tests
- Multi-player real-time combat
- Mode switching mid-fight
- Network failure recovery
- State consistency verification

### Load Tests
- 100 concurrent real-time fights
- 1000 actions per second
- State synchronization under load
- Database performance monitoring

---

## Monitoring & Analytics

### Key Metrics

1. **Performance Metrics**
   - Action processing latency
   - State sync frequency
   - Database query time
   - WebSocket message throughput

2. **Game Metrics**
   - Mode preference distribution
   - Average fight duration by mode
   - Action queue depths
   - Resource regeneration rates

3. **Error Metrics**
   - Failed action validations
   - Desync occurrences
   - Timeout frequencies
   - Connection drops

### Monitoring Implementation
```typescript
// lib/monitoring/combat-metrics.ts
export class CombatMetrics {
    private metrics: Map<string, any> = new Map();

    recordActionLatency(fightId: number, mode: string, latency: number): void {
        // Record to Prometheus/DataDog
    }

    recordQueueDepth(fightId: number, depth: number): void {
        // Track queue performance
    }

    recordModeUsage(mode: string): void {
        // Track mode preferences
    }
}
```

---

## Security Considerations

### Anti-Cheat Measures

1. **Server Authoritative**
   - All actions validated server-side
   - No client-side state modification
   - Cryptographic action signatures

2. **Rate Limiting**
   - Action frequency limits
   - Resource regeneration caps
   - Queue size restrictions

3. **Replay Protection**
   - Action timestamps validation
   - Sequence number tracking
   - Duplicate action detection

---

## Future Enhancements

### Phase 2 Features
1. **Hybrid Modes**
   - Pause and resume functionality
   - Dynamic mode switching based on player count
   - Time-limited turn mode

2. **Advanced Real-Time Features**
   - Combo system
   - Reaction-based counters
   - Area denial mechanics

3. **AI Improvements**
   - Adaptive AI for real-time mode
   - Predictive action queuing
   - Learning from player patterns

---

## Conclusion

This hybrid architecture provides a robust foundation for supporting both turn-based and real-time combat modes in GOFUS. The design maintains backward compatibility while enabling new gameplay experiences. The modular approach allows for easy extension and modification of combat mechanics without affecting the core system.

The implementation leverages the existing strengths of the GOFUS backend while introducing modern real-time capabilities that meet current gaming standards. With proper testing and optimization, this system can scale to support thousands of concurrent players across both combat modes.