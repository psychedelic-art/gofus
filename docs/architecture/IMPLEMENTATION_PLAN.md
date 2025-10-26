# GOFUS Hybrid Mode Implementation Plan
## Step-by-Step Guide for Adding Real-Time Combat

---

## Overview

This implementation plan provides a detailed, step-by-step guide to add real-time combat mode to the existing GOFUS backend while maintaining full backward compatibility with the turn-based system.

**Timeline**: 6 weeks
**Team Required**: 1-2 developers
**Risk Level**: Medium
**Backward Compatibility**: 100% maintained

---

## Week 1: Foundation & Database

### Day 1-2: Environment Setup & Planning

#### Tasks:
1. **Create feature branch**
   ```bash
   git checkout -b feature/hybrid-combat-mode
   ```

2. **Set up development environment**
   - Create test database copy
   - Set up monitoring tools
   - Configure logging for debugging

3. **Create project structure**
   ```
   gofus-backend/lib/services/combat/
   ├── interfaces/
   │   ├── ICombatMode.ts
   │   ├── IActionQueue.ts
   │   └── ICombatConfig.ts
   ├── processors/
   │   ├── TurnBasedProcessor.ts
   │   ├── RealTimeProcessor.ts
   │   └── BaseProcessor.ts
   ├── queues/
   │   ├── ActionQueue.ts
   │   └── PriorityQueue.ts
   ├── utils/
   │   ├── CombatCalculator.ts
   │   ├── ResourceManager.ts
   │   └── CooldownManager.ts
   └── CombatServiceV2.ts
   ```

### Day 3-4: Database Schema Updates

#### Migration Files:

**001_add_combat_mode.sql**
```sql
-- Add combat mode support to fights table
ALTER TABLE fights
ADD COLUMN IF NOT EXISTS combat_mode VARCHAR(20) DEFAULT 'turn_based',
ADD COLUMN IF NOT EXISTS config JSONB DEFAULT '{}',
ADD COLUMN IF NOT EXISTS tick_rate INTEGER DEFAULT 100,
ADD COLUMN IF NOT EXISTS last_tick_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS tick_count INTEGER DEFAULT 0;

-- Add index for active real-time fights
CREATE INDEX IF NOT EXISTS idx_fights_realtime_active
ON fights(id)
WHERE combat_mode = 'real_time' AND status = 'active';

-- Add index for last tick optimization
CREATE INDEX IF NOT EXISTS idx_fights_last_tick
ON fights(last_tick_at)
WHERE status = 'active';
```

**002_update_participants.sql**
```sql
-- Add real-time support to fight_participants
ALTER TABLE fight_participants
ADD COLUMN IF NOT EXISTS ap_regen_rate DECIMAL(5,2) DEFAULT 1.0,
ADD COLUMN IF NOT EXISTS mp_regen_rate DECIMAL(5,2) DEFAULT 0.5,
ADD COLUMN IF NOT EXISTS last_ap_regen TIMESTAMP DEFAULT NOW(),
ADD COLUMN IF NOT EXISTS last_mp_regen TIMESTAMP DEFAULT NOW(),
ADD COLUMN IF NOT EXISTS action_queue JSONB DEFAULT '[]',
ADD COLUMN IF NOT EXISTS cooldowns JSONB DEFAULT '{}',
ADD COLUMN IF NOT EXISTS fractional_ap DECIMAL(5,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS fractional_mp DECIMAL(5,2) DEFAULT 0;

-- Add index for participant lookups
CREATE INDEX IF NOT EXISTS idx_participants_fight_alive
ON fight_participants(fight_id, is_alive)
WHERE is_alive = true;
```

**003_combat_configurations.sql**
```sql
-- Create configuration table
CREATE TABLE IF NOT EXISTS combat_configurations (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    mode VARCHAR(20) NOT NULL,
    config JSONB NOT NULL,
    is_default BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Insert default configurations
INSERT INTO combat_configurations (name, mode, config, is_default) VALUES
('classic_turn_based', 'turn_based', '{
    "turn_time_limit": 120,
    "ap_per_turn": 6,
    "mp_per_turn": 3,
    "allow_flee": true,
    "auto_end_turn": true
}', true),
('fast_real_time', 'real_time', '{
    "tick_rate": 100,
    "ap_regen_per_second": 2,
    "mp_regen_per_second": 1,
    "global_cooldown": 500,
    "spell_queue_size": 3,
    "allow_flee": false,
    "max_fight_duration": 600
}', false),
('tactical_real_time', 'real_time', '{
    "tick_rate": 200,
    "ap_regen_per_second": 1,
    "mp_regen_per_second": 0.5,
    "global_cooldown": 1000,
    "spell_queue_size": 2,
    "allow_flee": true,
    "max_fight_duration": 900
}', false);

-- Add audit fields
CREATE TABLE IF NOT EXISTS combat_configuration_history (
    id SERIAL PRIMARY KEY,
    configuration_id INTEGER REFERENCES combat_configurations(id),
    changed_by INTEGER,
    changed_at TIMESTAMP DEFAULT NOW(),
    old_config JSONB,
    new_config JSONB
);
```

### Day 5: Core Interfaces

**ICombatMode.ts**
```typescript
// lib/services/combat/interfaces/ICombatMode.ts
import { Fight, Participant, Action, ActionResult, FightState } from '../types';

export interface ICombatMode {
    readonly mode: 'turn_based' | 'real_time';

    // Lifecycle methods
    initializeFight(fight: Fight, participants: Participant[]): Promise<void>;
    startFight(fightId: number): Promise<void>;
    pauseFight(fightId: number): Promise<void>;
    resumeFight(fightId: number): Promise<void>;
    endFight(fightId: number, winnerId: number): Promise<void>;

    // Action handling
    validateAction(fight: Fight, actor: Participant, action: Action): Promise<ValidationResult>;
    queueAction(fight: Fight, actor: Participant, action: Action): Promise<QueueResult>;
    processAction(fight: Fight, actor: Participant, action: Action): Promise<ActionResult>;
    cancelAction(fight: Fight, actorId: number, actionId: string): Promise<boolean>;

    // State management
    updateState(fightId: number): Promise<void>;
    getState(fightId: number): Promise<FightState>;
    syncState(fightId: number): Promise<void>;

    // Resource management
    handleResourceRegeneration(fightId: number): Promise<void>;

    // Event handling
    onParticipantDisconnect(fightId: number, participantId: number): Promise<void>;
    onParticipantReconnect(fightId: number, participantId: number): Promise<void>;
}

export interface ValidationResult {
    isValid: boolean;
    reason?: string;
    resourcesRequired?: {
        ap?: number;
        mp?: number;
    };
}

export interface QueueResult {
    success: boolean;
    queueId?: string;
    position?: number;
    estimatedExecutionTime?: number;
}
```

---

## Week 2: Base Implementation

### Day 6-7: Base Processor & Shared Logic

**BaseProcessor.ts**
```typescript
// lib/services/combat/processors/BaseProcessor.ts
import { db } from '@/lib/db';
import { ICombatMode } from '../interfaces/ICombatMode';
import { Logger } from '@/lib/utils/logger';

export abstract class BaseProcessor implements ICombatMode {
    protected logger: Logger;
    protected redis: Redis;

    constructor() {
        this.logger = new Logger(`CombatProcessor:${this.mode}`);
        this.redis = new Redis(process.env.REDIS_URL!);
    }

    abstract get mode(): 'turn_based' | 'real_time';

    // Shared helper methods
    protected async getFight(fightId: number): Promise<Fight> {
        const [fight] = await db
            .select()
            .from(fights)
            .where(eq(fights.id, fightId));

        if (!fight) {
            throw new Error(`Fight ${fightId} not found`);
        }

        return fight;
    }

    protected async getParticipants(fightId: number): Promise<Participant[]> {
        return await db
            .select()
            .from(fightParticipants)
            .where(eq(fightParticipants.fightId, fightId));
    }

    protected async getParticipant(fightId: number, participantId: number): Promise<Participant> {
        const [participant] = await db
            .select()
            .from(fightParticipants)
            .where(and(
                eq(fightParticipants.fightId, fightId),
                or(
                    eq(fightParticipants.characterId, participantId),
                    eq(fightParticipants.monsterId, participantId)
                )
            ));

        if (!participant) {
            throw new Error(`Participant ${participantId} not found in fight ${fightId}`);
        }

        return participant;
    }

    protected calculateDistance(cell1: number, cell2: number): number {
        const width = 14; // Grid width
        const col1 = cell1 % width;
        const row1 = Math.floor(cell1 / width);
        const col2 = cell2 % width;
        const row2 = Math.floor(cell2 / width);

        return Math.abs(col1 - col2) + Math.abs(row1 - row2);
    }

    protected async logAction(
        fightId: number,
        action: Action,
        result: ActionResult
    ): Promise<void> {
        await db.insert(fightActions).values({
            fightId,
            turn: action.turn || 0,
            actorId: action.actorId,
            actionType: action.type,
            targetId: action.targetId,
            spellId: action.spellId,
            fromCell: action.fromCell,
            toCell: action.toCell,
            damage: result.damage,
            healing: result.healing,
            success: result.success,
            timestamp: new Date(),
            executionTime: result.executionTime
        });
    }

    // Abstract methods that must be implemented
    abstract initializeFight(fight: Fight, participants: Participant[]): Promise<void>;
    abstract startFight(fightId: number): Promise<void>;
    abstract validateAction(fight: Fight, actor: Participant, action: Action): Promise<ValidationResult>;
    abstract processAction(fight: Fight, actor: Participant, action: Action): Promise<ActionResult>;
}
```

### Day 8-9: Turn-Based Processor Refactor

**TurnBasedProcessor.ts**
```typescript
// lib/services/combat/processors/TurnBasedProcessor.ts
import { BaseProcessor } from './BaseProcessor';

export class TurnBasedProcessor extends BaseProcessor {
    get mode() {
        return 'turn_based' as const;
    }

    async initializeFight(fight: Fight, participants: Participant[]): Promise<void> {
        this.logger.info(`Initializing turn-based fight ${fight.id}`);

        // Calculate turn order based on initiative
        const turnOrder = await this.calculateTurnOrder(participants);

        // Save turn order to fight
        await db.update(fights)
            .set({
                turnOrder,
                currentTurn: 0
            })
            .where(eq(fights.id, fight.id));

        // Set initial resources
        for (const participant of participants) {
            const config = fight.config as TurnBasedConfig;
            await db.update(fightParticipants)
                .set({
                    currentAp: config.ap_per_turn || 6,
                    currentMp: config.mp_per_turn || 3
                })
                .where(eq(fightParticipants.id, participant.id));
        }
    }

    async startFight(fightId: number): Promise<void> {
        // Turn-based fights start immediately
        await db.update(fights)
            .set({ status: 'active' })
            .where(eq(fights.id, fightId));

        // Set turn timer if configured
        const fight = await this.getFight(fightId);
        const config = fight.config as TurnBasedConfig;

        if (config.turn_time_limit) {
            await this.setTurnTimer(fightId, config.turn_time_limit);
        }
    }

    async validateAction(
        fight: Fight,
        actor: Participant,
        action: Action
    ): Promise<ValidationResult> {
        // Check if it's the actor's turn
        const turnOrder = fight.turnOrder as number[];
        const currentActorId = turnOrder[fight.currentTurn % turnOrder.length];

        if (currentActorId !== actor.characterId && currentActorId !== actor.monsterId) {
            return {
                isValid: false,
                reason: 'Not your turn'
            };
        }

        // Validate resources
        return this.validateResources(actor, action);
    }

    async processAction(
        fight: Fight,
        actor: Participant,
        action: Action
    ): Promise<ActionResult> {
        const startTime = Date.now();

        // Execute action immediately
        const result = await this.executeAction(fight, actor, action);

        // Log action
        await this.logAction(fight.id, action, {
            ...result,
            executionTime: Date.now() - startTime
        });

        // Check if turn should end
        if (action.type === 'pass' || this.shouldAutoEndTurn(actor)) {
            await this.endTurn(fight.id);
        }

        return result;
    }

    private async endTurn(fightId: number): Promise<void> {
        const fight = await this.getFight(fightId);
        const turnOrder = fight.turnOrder as number[];
        const nextTurn = (fight.currentTurn + 1) % turnOrder.length;

        // Update turn
        await db.update(fights)
            .set({ currentTurn: nextTurn })
            .where(eq(fights.id, fightId));

        // Refresh resources for next player
        const nextActorId = turnOrder[nextTurn];
        const config = fight.config as TurnBasedConfig;

        await db.update(fightParticipants)
            .set({
                currentAp: config.ap_per_turn || 6,
                currentMp: config.mp_per_turn || 3
            })
            .where(and(
                eq(fightParticipants.fightId, fightId),
                or(
                    eq(fightParticipants.characterId, nextActorId),
                    eq(fightParticipants.monsterId, nextActorId)
                )
            ));

        // Emit turn change event
        this.emitEvent(fightId, {
            type: 'turn_change',
            currentTurn: nextTurn,
            actorId: nextActorId
        });
    }
}
```

### Day 10: Action Queue System

**ActionQueue.ts**
```typescript
// lib/services/combat/queues/ActionQueue.ts
export class ActionQueue {
    private queues: Map<number, QueuedAction[]> = new Map();
    private maxQueueSize: number;

    constructor(maxQueueSize: number = 3) {
        this.maxQueueSize = maxQueueSize;
    }

    enqueue(actorId: number, action: QueuedAction): boolean {
        if (!this.queues.has(actorId)) {
            this.queues.set(actorId, []);
        }

        const queue = this.queues.get(actorId)!;

        if (queue.length >= this.maxQueueSize) {
            return false;
        }

        queue.push(action);
        queue.sort((a, b) => a.priority - b.priority);

        return true;
    }

    dequeue(actorId: number): QueuedAction | null {
        const queue = this.queues.get(actorId);

        if (!queue || queue.length === 0) {
            return null;
        }

        return queue.shift() || null;
    }

    peek(actorId: number): QueuedAction | null {
        const queue = this.queues.get(actorId);
        return queue && queue.length > 0 ? queue[0] : null;
    }

    getQueueLength(actorId: number): number {
        return this.queues.get(actorId)?.length || 0;
    }

    clear(actorId?: number): void {
        if (actorId) {
            this.queues.delete(actorId);
        } else {
            this.queues.clear();
        }
    }

    getAllQueued(): Map<number, QueuedAction[]> {
        return new Map(this.queues);
    }
}

interface QueuedAction extends Action {
    id: string;
    queuedAt: Date;
    priority: number;
    estimatedExecutionTime?: number;
}
```

---

## Week 3: Real-Time Implementation

### Day 11-12: Real-Time Processor

**RealTimeProcessor.ts**
```typescript
// lib/services/combat/processors/RealTimeProcessor.ts
import { BaseProcessor } from './BaseProcessor';
import { ActionQueue } from '../queues/ActionQueue';
import { CooldownManager } from '../utils/CooldownManager';
import { ResourceManager } from '../utils/ResourceManager';

export class RealTimeProcessor extends BaseProcessor {
    private gameLoops: Map<number, NodeJS.Timeout> = new Map();
    private actionQueues: Map<number, ActionQueue> = new Map();
    private cooldownManagers: Map<number, CooldownManager> = new Map();
    private resourceManagers: Map<number, ResourceManager> = new Map();

    get mode() {
        return 'real_time' as const;
    }

    async initializeFight(fight: Fight, participants: Participant[]): Promise<void> {
        this.logger.info(`Initializing real-time fight ${fight.id}`);

        // Initialize managers
        this.actionQueues.set(fight.id, new ActionQueue(fight.config?.spell_queue_size || 3));
        this.cooldownManagers.set(fight.id, new CooldownManager());
        this.resourceManagers.set(fight.id, new ResourceManager(fight.config as RealTimeConfig));

        // Set initial state for participants
        for (const participant of participants) {
            await db.update(fightParticipants)
                .set({
                    last_ap_regen: new Date(),
                    last_mp_regen: new Date(),
                    fractional_ap: 0,
                    fractional_mp: 0
                })
                .where(eq(fightParticipants.id, participant.id));
        }

        // No turn order needed
        await db.update(fights)
            .set({
                turnOrder: null,
                currentTurn: null,
                last_tick_at: new Date()
            })
            .where(eq(fights.id, fight.id));
    }

    async startFight(fightId: number): Promise<void> {
        const fight = await this.getFight(fightId);
        const config = fight.config as RealTimeConfig;
        const tickRate = config.tick_rate || 100;

        this.logger.info(`Starting real-time fight ${fightId} with tick rate ${tickRate}ms`);

        // Start game loop
        const gameLoop = setInterval(async () => {
            try {
                await this.gameTick(fightId);
            } catch (error) {
                this.logger.error(`Game tick error for fight ${fightId}:`, error);
            }
        }, tickRate);

        this.gameLoops.set(fightId, gameLoop);

        // Update fight status
        await db.update(fights)
            .set({ status: 'active' })
            .where(eq(fights.id, fightId));
    }

    private async gameTick(fightId: number): Promise<void> {
        const tickStart = Date.now();

        // Update tick count
        await db.update(fights)
            .set({
                tick_count: sql`tick_count + 1`,
                last_tick_at: new Date()
            })
            .where(eq(fights.id, fightId));

        // 1. Process resource regeneration
        await this.processResourceRegeneration(fightId);

        // 2. Process action queues
        await this.processActionQueues(fightId);

        // 3. Update cooldowns
        await this.updateCooldowns(fightId);

        // 4. Check victory conditions
        const victoryResult = await this.checkVictoryConditions(fightId);
        if (victoryResult.isEnded) {
            await this.endFight(fightId, victoryResult.winnerId!);
            return;
        }

        // 5. Broadcast state update
        await this.broadcastStateUpdate(fightId);

        // Log tick performance
        const tickDuration = Date.now() - tickStart;
        if (tickDuration > 50) {
            this.logger.warn(`Slow tick for fight ${fightId}: ${tickDuration}ms`);
        }
    }

    private async processResourceRegeneration(fightId: number): Promise<void> {
        const resourceManager = this.resourceManagers.get(fightId);
        if (!resourceManager) return;

        const participants = await this.getParticipants(fightId);
        const fight = await this.getFight(fightId);

        for (const participant of participants) {
            if (!participant.isAlive) continue;

            const updates = await resourceManager.calculateRegeneration(participant);

            if (updates.apGained > 0 || updates.mpGained > 0) {
                await db.update(fightParticipants)
                    .set({
                        currentAp: updates.newAp,
                        currentMp: updates.newMp,
                        fractional_ap: updates.fractionalAp,
                        fractional_mp: updates.fractionalMp,
                        last_ap_regen: new Date(),
                        last_mp_regen: new Date()
                    })
                    .where(eq(fightParticipants.id, participant.id));
            }
        }
    }

    private async processActionQueues(fightId: number): Promise<void> {
        const queue = this.actionQueues.get(fightId);
        if (!queue) return;

        const allQueued = queue.getAllQueued();

        for (const [actorId, actions] of allQueued) {
            if (actions.length === 0) continue;

            const actor = await this.getParticipant(fightId, actorId);
            if (!actor.isAlive) {
                queue.clear(actorId);
                continue;
            }

            // Process first action in queue
            const action = queue.peek(actorId);
            if (!action) continue;

            // Check if action can be executed
            const validation = await this.validateAction(
                await this.getFight(fightId),
                actor,
                action
            );

            if (validation.isValid) {
                // Remove from queue and execute
                queue.dequeue(actorId);
                await this.executeAction(
                    await this.getFight(fightId),
                    actor,
                    action
                );

                // Apply cooldown
                const cooldownManager = this.cooldownManagers.get(fightId);
                if (cooldownManager) {
                    await cooldownManager.applyCooldown(actor.id, action);
                }
            }
        }
    }

    async validateAction(
        fight: Fight,
        actor: Participant,
        action: Action
    ): Promise<ValidationResult> {
        // Check if actor is alive
        if (!actor.isAlive) {
            return { isValid: false, reason: 'Actor is not alive' };
        }

        // Check cooldown
        const cooldownManager = this.cooldownManagers.get(fight.id);
        if (cooldownManager && cooldownManager.isOnCooldown(actor.id, action)) {
            return { isValid: false, reason: 'Action is on cooldown' };
        }

        // Validate resources
        return this.validateResources(actor, action);
    }

    async queueAction(
        fight: Fight,
        actor: Participant,
        action: Action
    ): Promise<QueueResult> {
        const queue = this.actionQueues.get(fight.id);
        if (!queue) {
            return { success: false };
        }

        const queuedAction = {
            ...action,
            id: crypto.randomUUID(),
            queuedAt: new Date(),
            priority: this.calculateActionPriority(action)
        };

        const success = queue.enqueue(actor.id, queuedAction);

        if (success) {
            return {
                success: true,
                queueId: queuedAction.id,
                position: queue.getQueueLength(actor.id),
                estimatedExecutionTime: this.estimateExecutionTime(fight, action)
            };
        }

        return { success: false };
    }

    async endFight(fightId: number, winnerId: number): Promise<void> {
        this.logger.info(`Ending real-time fight ${fightId}, winner: ${winnerId}`);

        // Stop game loop
        const gameLoop = this.gameLoops.get(fightId);
        if (gameLoop) {
            clearInterval(gameLoop);
            this.gameLoops.delete(fightId);
        }

        // Clean up managers
        this.actionQueues.delete(fightId);
        this.cooldownManagers.delete(fightId);
        this.resourceManagers.delete(fightId);

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

### Day 13-14: Support Utilities

**ResourceManager.ts**
```typescript
// lib/services/combat/utils/ResourceManager.ts
export class ResourceManager {
    private config: RealTimeConfig;

    constructor(config: RealTimeConfig) {
        this.config = config;
    }

    async calculateRegeneration(participant: Participant): Promise<ResourceUpdate> {
        const now = Date.now();
        const apTimeDiff = now - participant.last_ap_regen.getTime();
        const mpTimeDiff = now - participant.last_mp_regen.getTime();

        // Calculate regeneration based on time passed
        const apRegenRate = this.config.ap_regen_per_second || 1;
        const mpRegenRate = this.config.mp_regen_per_second || 0.5;

        const apGained = (apTimeDiff / 1000) * apRegenRate;
        const mpGained = (mpTimeDiff / 1000) * mpRegenRate;

        // Add to fractional values
        let fractionalAp = participant.fractional_ap + apGained;
        let fractionalMp = participant.fractional_mp + mpGained;

        // Extract whole numbers
        const wholeAp = Math.floor(fractionalAp);
        const wholeMp = Math.floor(fractionalMp);

        fractionalAp -= wholeAp;
        fractionalMp -= wholeMp;

        // Calculate new totals
        const newAp = Math.min(participant.maxAp, participant.currentAp + wholeAp);
        const newMp = Math.min(participant.maxMp, participant.currentMp + wholeMp);

        return {
            newAp,
            newMp,
            fractionalAp,
            fractionalMp,
            apGained: wholeAp,
            mpGained: wholeMp
        };
    }
}
```

**CooldownManager.ts**
```typescript
// lib/services/combat/utils/CooldownManager.ts
export class CooldownManager {
    private cooldowns: Map<number, Map<string, number>> = new Map();

    isOnCooldown(actorId: number, action: Action): boolean {
        const actorCooldowns = this.cooldowns.get(actorId);
        if (!actorCooldowns) return false;

        const cooldownKey = this.getCooldownKey(action);
        const cooldownEnd = actorCooldowns.get(cooldownKey);

        if (!cooldownEnd) return false;

        return Date.now() < cooldownEnd;
    }

    async applyCooldown(actorId: number, action: Action): Promise<void> {
        if (!this.cooldowns.has(actorId)) {
            this.cooldowns.set(actorId, new Map());
        }

        const actorCooldowns = this.cooldowns.get(actorId)!;
        const cooldownKey = this.getCooldownKey(action);
        const cooldownDuration = this.getCooldownDuration(action);

        actorCooldowns.set(cooldownKey, Date.now() + cooldownDuration);

        // Also update database
        await db.update(fightParticipants)
            .set({
                cooldowns: Object.fromEntries(actorCooldowns)
            })
            .where(eq(fightParticipants.id, actorId));
    }

    private getCooldownKey(action: Action): string {
        if (action.type === 'spell') {
            return `spell_${action.spellId}`;
        }
        return `action_${action.type}`;
    }

    private getCooldownDuration(action: Action): number {
        // Get cooldown from spell data or use global cooldown
        if (action.type === 'spell' && action.spellCooldown) {
            return action.spellCooldown;
        }
        return 500; // Default global cooldown
    }
}
```

---

## Week 4: Integration & WebSocket

### Day 15-16: Unified Combat Service

**CombatServiceV2.ts**
```typescript
// lib/services/combat/CombatServiceV2.ts
import { TurnBasedProcessor } from './processors/TurnBasedProcessor';
import { RealTimeProcessor } from './processors/RealTimeProcessor';
import { ICombatMode } from './interfaces/ICombatMode';
import { EventEmitter } from 'events';

export class CombatServiceV2 extends EventEmitter {
    private processors: Map<string, ICombatMode> = new Map();
    private activeFights: Map<number, ICombatMode> = new Map();

    constructor() {
        super();
        this.processors.set('turn_based', new TurnBasedProcessor());
        this.processors.set('real_time', new RealTimeProcessor());
    }

    async startFight(
        team0CharacterIds: number[],
        team1Ids: number[],
        fightType: 'pvp' | 'pve',
        combatMode: 'turn_based' | 'real_time' = 'turn_based',
        mapId: number,
        configName?: string
    ): Promise<FightResponse> {
        try {
            // Begin transaction
            await db.transaction(async (tx) => {
                // Load configuration
                const config = await this.loadConfiguration(configName || `default_${combatMode}`);

                // Create fight record
                const [fight] = await tx
                    .insert(fights)
                    .values({
                        type: fightType,
                        combat_mode: combatMode,
                        config,
                        mapId,
                        status: 'initializing',
                        tick_rate: config.tick_rate || 100
                    })
                    .returning();

                // Create participants
                const participants = await this.createParticipants(tx, fight.id, team0CharacterIds, team1Ids, fightType);

                // Get processor
                const processor = this.processors.get(combatMode);
                if (!processor) {
                    throw new Error(`Unknown combat mode: ${combatMode}`);
                }

                // Initialize fight
                await processor.initializeFight(fight, participants);

                // Start fight
                await processor.startFight(fight.id);

                // Track active fight
                this.activeFights.set(fight.id, processor);

                // Emit event
                this.emit('fight:started', {
                    fightId: fight.id,
                    mode: combatMode,
                    participants: participants.map(p => ({
                        id: p.characterId || p.monsterId,
                        team: p.team,
                        name: p.name
                    }))
                });

                return {
                    success: true,
                    fight,
                    mode: combatMode
                };
            });
        } catch (error) {
            this.emit('fight:error', { error: error.message });
            throw error;
        }
    }

    async performAction(
        fightId: number,
        actorId: number,
        action: Action
    ): Promise<ActionResponse> {
        const processor = this.activeFights.get(fightId);
        if (!processor) {
            throw new Error(`No active processor for fight ${fightId}`);
        }

        const fight = await this.getFight(fightId);
        const actor = await this.getParticipant(fightId, actorId);

        // For real-time, queue the action
        if (fight.combat_mode === 'real_time') {
            const queueResult = await (processor as RealTimeProcessor).queueAction(fight, actor, action);
            return {
                success: queueResult.success,
                queued: true,
                queueId: queueResult.queueId,
                queuePosition: queueResult.position
            };
        }

        // For turn-based, process immediately
        const validation = await processor.validateAction(fight, actor, action);
        if (!validation.isValid) {
            throw new Error(validation.reason || 'Invalid action');
        }

        return await processor.processAction(fight, actor, action);
    }

    async getFightState(fightId: number): Promise<FightState> {
        const processor = this.activeFights.get(fightId);
        if (!processor) {
            // Load from database for inactive fights
            return this.loadFightStateFromDB(fightId);
        }

        return await processor.getState(fightId);
    }

    async endFight(fightId: number): Promise<void> {
        const processor = this.activeFights.get(fightId);
        if (processor) {
            const result = await this.checkVictoryConditions(fightId);
            await processor.endFight(fightId, result.winnerId || -1);
            this.activeFights.delete(fightId);
        }
    }

    // Cleanup on shutdown
    async shutdown(): Promise<void> {
        for (const [fightId, processor] of this.activeFights) {
            await processor.pauseFight(fightId);
        }
        this.activeFights.clear();
    }
}
```

### Day 17-18: WebSocket Integration

**WebSocket Handler Updates**
```typescript
// lib/websocket/combat-handler.ts
import { Server, Socket } from 'socket.io';
import { CombatServiceV2 } from '@/lib/services/combat/CombatServiceV2';

export class CombatWebSocketHandler {
    private io: Server;
    private combatService: CombatServiceV2;
    private stateIntervals: Map<number, NodeJS.Timeout> = new Map();

    constructor(io: Server) {
        this.io = io;
        this.combatService = new CombatServiceV2();
        this.setupEventListeners();
    }

    private setupEventListeners(): void {
        // Listen to combat service events
        this.combatService.on('fight:started', (data) => {
            this.io.to(`fight:${data.fightId}`).emit('fight:started', data);

            // Start state broadcasting for real-time fights
            if (data.mode === 'real_time') {
                this.startStateBroadcast(data.fightId);
            }
        });

        this.combatService.on('fight:ended', (data) => {
            this.io.to(`fight:${data.fightId}`).emit('fight:ended', data);
            this.stopStateBroadcast(data.fightId);
        });

        this.combatService.on('action:executed', (data) => {
            this.io.to(`fight:${data.fightId}`).emit('action:executed', data);
        });
    }

    handleConnection(socket: Socket): void {
        // Join fight room
        socket.on('fight:join', async (fightId: number) => {
            socket.join(`fight:${fightId}`);

            // Send current state
            const state = await this.combatService.getFightState(fightId);
            socket.emit('fight:state', state);
        });

        // Handle action submission
        socket.on('fight:action', async (data) => {
            try {
                const result = await this.combatService.performAction(
                    data.fightId,
                    data.actorId,
                    data.action
                );

                socket.emit('action:result', result);
            } catch (error) {
                socket.emit('action:error', { error: error.message });
            }
        });

        // Handle disconnection
        socket.on('disconnect', () => {
            // Handle reconnection logic
        });
    }

    private startStateBroadcast(fightId: number): void {
        // Broadcast state every 100ms for real-time fights
        const interval = setInterval(async () => {
            try {
                const state = await this.combatService.getFightState(fightId);
                this.io.to(`fight:${fightId}`).emit('state:update', {
                    fightId,
                    state,
                    timestamp: Date.now()
                });
            } catch (error) {
                console.error(`State broadcast error for fight ${fightId}:`, error);
            }
        }, 100);

        this.stateIntervals.set(fightId, interval);
    }

    private stopStateBroadcast(fightId: number): void {
        const interval = this.stateIntervals.get(fightId);
        if (interval) {
            clearInterval(interval);
            this.stateIntervals.delete(fightId);
        }
    }
}
```

---

## Week 5: API & Testing

### Day 19-20: API Endpoints

**Fight Routes**
```typescript
// app/api/fights/route.ts
import { NextRequest, NextResponse } from 'next/server';
import { CombatServiceV2 } from '@/lib/services/combat/CombatServiceV2';
import { z } from 'zod';

const startFightSchema = z.object({
    team0CharacterIds: z.array(z.number()),
    team1Ids: z.array(z.number()),
    fightType: z.enum(['pvp', 'pve']),
    combatMode: z.enum(['turn_based', 'real_time']).optional(),
    mapId: z.number(),
    configName: z.string().optional()
});

export async function POST(request: NextRequest) {
    try {
        const body = await request.json();
        const validated = startFightSchema.parse(body);

        const combatService = new CombatServiceV2();
        const result = await combatService.startFight(
            validated.team0CharacterIds,
            validated.team1Ids,
            validated.fightType,
            validated.combatMode,
            validated.mapId,
            validated.configName
        );

        return NextResponse.json(result);
    } catch (error) {
        if (error instanceof z.ZodError) {
            return NextResponse.json(
                { error: 'Invalid request', details: error.errors },
                { status: 400 }
            );
        }

        return NextResponse.json(
            { error: error.message },
            { status: 500 }
        );
    }
}
```

**Configuration Routes**
```typescript
// app/api/fights/configurations/route.ts
export async function GET() {
    const configs = await db
        .select()
        .from(combat_configurations)
        .orderBy(combat_configurations.name);

    return NextResponse.json({
        configurations: configs,
        modes: ['turn_based', 'real_time']
    });
}

export async function POST(request: NextRequest) {
    const body = await request.json();

    const [config] = await db
        .insert(combat_configurations)
        .values({
            name: body.name,
            mode: body.mode,
            config: body.config
        })
        .returning();

    return NextResponse.json(config);
}
```

### Day 21-22: Testing Setup

**Combat Service Tests**
```typescript
// __tests__/combat/combat-service-v2.test.ts
import { CombatServiceV2 } from '@/lib/services/combat/CombatServiceV2';
import { db } from '@/lib/db';

describe('CombatServiceV2', () => {
    let service: CombatServiceV2;

    beforeEach(() => {
        service = new CombatServiceV2();
    });

    afterEach(async () => {
        await service.shutdown();
    });

    describe('Turn-Based Mode', () => {
        it('should create a turn-based fight', async () => {
            const result = await service.startFight(
                [1, 2],
                [3, 4],
                'pvp',
                'turn_based',
                100
            );

            expect(result.success).toBe(true);
            expect(result.mode).toBe('turn_based');
            expect(result.fight.combat_mode).toBe('turn_based');
        });

        it('should enforce turn order', async () => {
            const fight = await service.startFight([1], [2], 'pvp', 'turn_based', 100);

            // Try to act out of turn
            await expect(service.performAction(fight.fight.id, 2, {
                type: 'pass',
                actorId: 2
            })).rejects.toThrow('Not your turn');
        });

        it('should refresh resources per turn', async () => {
            // Test AP/MP refresh logic
        });
    });

    describe('Real-Time Mode', () => {
        it('should create a real-time fight', async () => {
            const result = await service.startFight(
                [1, 2],
                [3, 4],
                'pvp',
                'real_time',
                100
            );

            expect(result.success).toBe(true);
            expect(result.mode).toBe('real_time');
        });

        it('should queue actions', async () => {
            const fight = await service.startFight([1], [2], 'pvp', 'real_time', 100);

            const result = await service.performAction(fight.fight.id, 1, {
                type: 'spell',
                actorId: 1,
                targetId: 2,
                spellId: 1
            });

            expect(result.queued).toBe(true);
            expect(result.queueId).toBeDefined();
        });

        it('should regenerate resources over time', async () => {
            // Test resource regeneration
        });

        it('should apply cooldowns', async () => {
            // Test cooldown system
        });
    });

    describe('Mode Conversion', () => {
        it('should convert from turn-based to real-time', async () => {
            // Test conversion logic
        });
    });
});
```

**Integration Tests**
```typescript
// __tests__/integration/combat-flow.test.ts
describe('Combat Flow Integration', () => {
    it('should complete a full turn-based fight', async () => {
        // Create fight
        // Perform actions
        // Check winner
        // Verify rewards
    });

    it('should complete a full real-time fight', async () => {
        // Create fight
        // Queue actions
        // Wait for processing
        // Check winner
    });

    it('should handle disconnections gracefully', async () => {
        // Test reconnection logic
    });
});
```

---

## Week 6: Optimization & Deployment

### Day 23-24: Performance Optimization

**Optimizations to Implement:**

1. **Database Query Optimization**
```sql
-- Add composite indexes
CREATE INDEX idx_participants_fight_character
ON fight_participants(fight_id, character_id)
WHERE character_id IS NOT NULL;

CREATE INDEX idx_actions_fight_turn
ON fight_actions(fight_id, turn);

-- Optimize common queries
CREATE OR REPLACE VIEW active_fights AS
SELECT f.*, COUNT(p.id) as participant_count
FROM fights f
JOIN fight_participants p ON p.fight_id = f.id
WHERE f.status = 'active'
GROUP BY f.id;
```

2. **Caching Strategy**
```typescript
// lib/services/combat/cache/CombatCache.ts
export class CombatCache {
    private redis: Redis;

    async getFightState(fightId: number): Promise<FightState | null> {
        const cached = await this.redis.get(`fight:${fightId}:state`);
        return cached ? JSON.parse(cached) : null;
    }

    async setFightState(fightId: number, state: FightState): Promise<void> {
        await this.redis.setex(
            `fight:${fightId}:state`,
            5, // 5 second TTL
            JSON.stringify(state)
        );
    }
}
```

3. **Connection Pooling**
```typescript
// lib/db/connection-pool.ts
export const combatDbPool = new Pool({
    max: 20, // Maximum connections
    idleTimeoutMillis: 30000,
    connectionTimeoutMillis: 2000,
});
```

### Day 25-26: Monitoring & Logging

**Monitoring Setup:**
```typescript
// lib/monitoring/combat-metrics.ts
import { Counter, Histogram, Gauge } from 'prom-client';

export const combatMetrics = {
    fightsCreated: new Counter({
        name: 'combat_fights_created_total',
        help: 'Total number of fights created',
        labelNames: ['mode', 'type']
    }),

    actionProcessingTime: new Histogram({
        name: 'combat_action_processing_duration_seconds',
        help: 'Time taken to process combat actions',
        labelNames: ['mode', 'action_type']
    }),

    activeFights: new Gauge({
        name: 'combat_active_fights',
        help: 'Number of currently active fights',
        labelNames: ['mode']
    }),

    tickDuration: new Histogram({
        name: 'combat_tick_duration_milliseconds',
        help: 'Duration of game ticks in real-time mode',
        buckets: [10, 25, 50, 100, 250, 500, 1000]
    })
};
```

### Day 27-28: Documentation & Deployment

**API Documentation:**
```yaml
# openapi.yaml
openapi: 3.0.0
info:
  title: GOFUS Combat API
  version: 2.0.0
  description: Hybrid turn-based and real-time combat system

paths:
  /api/fights:
    post:
      summary: Start a new fight
      parameters:
        - name: combatMode
          in: body
          schema:
            type: string
            enum: [turn_based, real_time]
            default: turn_based
      responses:
        200:
          description: Fight created successfully
```

**Deployment Checklist:**
```markdown
## Pre-Deployment
- [ ] All tests passing
- [ ] Performance benchmarks met
- [ ] Security audit completed
- [ ] Documentation updated
- [ ] Migration scripts tested

## Deployment Steps
1. [ ] Backup production database
2. [ ] Run database migrations
3. [ ] Deploy to staging
4. [ ] Run smoke tests
5. [ ] Deploy to production (gradual rollout)
6. [ ] Monitor metrics and logs
7. [ ] Verify no degradation

## Post-Deployment
- [ ] Monitor error rates
- [ ] Check performance metrics
- [ ] Gather user feedback
- [ ] Document lessons learned
```

---

## Success Criteria

### Performance Targets
- Turn-based action processing: < 50ms
- Real-time tick processing: < 20ms
- WebSocket latency: < 100ms
- Database queries: < 10ms
- Memory usage: < 100MB per fight

### Quality Metrics
- Test coverage: > 80%
- No breaking changes to existing APIs
- Zero downtime deployment
- Error rate: < 0.1%

### User Experience
- Smooth transition between modes
- No perceived lag in real-time mode
- Clear visual feedback for actions
- Consistent behavior across modes

---

## Risk Mitigation

### Technical Risks
1. **Performance degradation**
   - Mitigation: Extensive load testing, gradual rollout
2. **State synchronization issues**
   - Mitigation: Comprehensive integration tests, state validation
3. **Memory leaks**
   - Mitigation: Memory profiling, cleanup handlers

### Rollback Plan
1. Feature flag to disable real-time mode
2. Database migration rollback scripts ready
3. Previous version deployable within 5 minutes
4. Monitoring alerts configured

---

## Conclusion

This implementation plan provides a structured approach to adding real-time combat mode to GOFUS while maintaining the existing turn-based system. The modular architecture ensures clean separation of concerns and allows for future enhancements. With proper testing and monitoring, this hybrid system will provide players with flexible combat options while maintaining game balance and performance.