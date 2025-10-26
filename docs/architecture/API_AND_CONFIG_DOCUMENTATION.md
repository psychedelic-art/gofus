# GOFUS Hybrid Combat - API & Configuration Documentation
## Complete Reference Guide for Turn-Based and Real-Time Modes

---

## Table of Contents

1. [Configuration System](#configuration-system)
2. [API Endpoints](#api-endpoints)
3. [WebSocket Events](#websocket-events)
4. [Configuration Examples](#configuration-examples)
5. [Migration Guide](#migration-guide)
6. [Client Integration](#client-integration)

---

## Configuration System

### Environment Variables

Add these new environment variables to `.env.local`:

```bash
# Combat Configuration
COMBAT_DEFAULT_MODE=turn_based              # Default combat mode
COMBAT_MAX_CONCURRENT_REALTIME=100         # Max concurrent real-time fights
COMBAT_TICK_RATE_MIN=50                    # Minimum tick rate (ms)
COMBAT_TICK_RATE_MAX=500                   # Maximum tick rate (ms)
COMBAT_ACTION_QUEUE_SIZE=5                 # Default action queue size
COMBAT_ENABLE_MODE_SWITCHING=true          # Allow mode switching mid-fight

# Performance Settings
COMBAT_STATE_CACHE_TTL=5                   # State cache TTL in seconds
COMBAT_BROADCAST_INTERVAL=100              # State broadcast interval (ms)
COMBAT_MAX_PARTICIPANTS=10                 # Max participants per fight

# Redis Configuration for Combat
REDIS_COMBAT_PREFIX=combat:                # Redis key prefix for combat data
REDIS_COMBAT_TTL=3600                      # Combat data TTL (seconds)
```

### Database Configuration Schema

```typescript
interface CombatConfiguration {
    id: number;
    name: string;                          // Unique configuration name
    mode: 'turn_based' | 'real_time';      // Combat mode
    config: TurnBasedConfig | RealTimeConfig;
    is_default: boolean;                   // Default for this mode
    is_active: boolean;                    // Available for use
    created_at: Date;
    updated_at: Date;
}

interface TurnBasedConfig {
    turn_time_limit: number;               // Seconds per turn (0 = unlimited)
    ap_per_turn: number;                   // Action points per turn
    mp_per_turn: number;                   // Movement points per turn
    allow_flee: boolean;                   // Can flee from fight
    auto_end_turn: boolean;                // Auto-end when no actions available
    auto_pass_timeout: boolean;            // Auto-pass on timeout
    show_turn_order: boolean;              // Display turn order to players
    penalty_for_timeout: {                 // Penalties for turn timeout
        ap_reduction: number;
        mp_reduction: number;
        skip_next_turn: boolean;
    };
}

interface RealTimeConfig {
    tick_rate: number;                     // Milliseconds between game ticks
    ap_regen_per_second: number;           // AP regeneration rate
    mp_regen_per_second: number;           // MP regeneration rate
    global_cooldown: number;               // Global cooldown between actions (ms)
    spell_queue_size: number;              // Max queued actions per player
    allow_flee: boolean;                   // Can flee from fight
    max_fight_duration: number;            // Maximum fight duration (seconds)
    action_prediction: boolean;            // Enable client-side prediction
    lag_compensation: boolean;             // Enable lag compensation
    resource_regen_tick: number;           // Ticks between resource updates
    cooldown_reduction_rate: number;       // Cooldown reduction per second
}
```

---

## API Endpoints

### Fight Management

#### 1. Start Fight
```http
POST /api/fights
Content-Type: application/json
Authorization: Bearer {token}

{
    "team0CharacterIds": [1, 2, 3],
    "team1Ids": [4, 5, 6],
    "fightType": "pvp",                   // "pvp" | "pve"
    "combatMode": "real_time",            // "turn_based" | "real_time"
    "mapId": 7411,
    "configName": "fast_real_time"        // Optional, uses default if not specified
}

Response:
{
    "success": true,
    "fight": {
        "id": 123,
        "type": "pvp",
        "combat_mode": "real_time",
        "status": "active",
        "config": { ... },
        "participants": [ ... ]
    },
    "mode": "real_time",
    "websocket_room": "fight:123"
}
```

#### 2. Get Fight State
```http
GET /api/fights/{id}/state

Response:
{
    "fightId": 123,
    "mode": "real_time",
    "status": "active",
    "tick_count": 245,
    "participants": [
        {
            "id": 1,
            "team": 0,
            "currentHp": 85,
            "maxHp": 100,
            "currentAp": 4.5,
            "maxAp": 6,
            "currentMp": 2.3,
            "maxMp": 3,
            "position": 125,
            "cooldowns": {
                "spell_1": 1234567890
            },
            "actionQueue": [
                {
                    "id": "abc-123",
                    "type": "spell",
                    "spellId": 1,
                    "targetId": 2,
                    "queuedAt": "2024-01-01T12:00:00Z"
                }
            ]
        }
    ],
    "turnOrder": null,                    // null for real-time
    "currentTurn": null,                  // null for real-time
    "lastUpdate": "2024-01-01T12:00:00Z"
}
```

#### 3. Perform Action
```http
POST /api/fights/{id}/action
Content-Type: application/json
Authorization: Bearer {token}

{
    "actorId": 1,
    "action": {
        "type": "spell",                  // "spell" | "move" | "pass"
        "spellId": 1,                     // For spell actions
        "targetId": 2,                     // For targeted actions
        "fromCell": 125,                  // For movement
        "toCell": 139                     // For movement
    }
}

Response (Turn-Based):
{
    "success": true,
    "damage": 25,
    "healing": 0,
    "resourcesUsed": {
        "ap": 3,
        "mp": 0
    },
    "effects": [ ... ]
}

Response (Real-Time):
{
    "success": true,
    "queued": true,
    "queueId": "action-xyz-789",
    "queuePosition": 2,
    "estimatedExecutionTime": 250
}
```

#### 4. Convert Combat Mode
```http
POST /api/fights/{id}/convert
Content-Type: application/json
Authorization: Bearer {token}

{
    "targetMode": "real_time",
    "configName": "tactical_real_time"    // Optional
}

Response:
{
    "success": true,
    "previousMode": "turn_based",
    "newMode": "real_time",
    "message": "Fight successfully converted to real-time mode"
}
```

#### 5. Cancel Queued Action (Real-Time Only)
```http
DELETE /api/fights/{id}/action/{queueId}
Authorization: Bearer {token}

Response:
{
    "success": true,
    "cancelledActionId": "action-xyz-789"
}
```

### Configuration Management

#### 1. List Configurations
```http
GET /api/fights/configurations

Response:
{
    "configurations": [
        {
            "id": 1,
            "name": "classic_turn_based",
            "mode": "turn_based",
            "is_default": true,
            "is_active": true,
            "config": { ... }
        },
        {
            "id": 2,
            "name": "fast_real_time",
            "mode": "real_time",
            "is_default": false,
            "is_active": true,
            "config": { ... }
        }
    ],
    "modes": ["turn_based", "real_time"]
}
```

#### 2. Get Configuration
```http
GET /api/fights/configurations/{name}

Response:
{
    "id": 2,
    "name": "fast_real_time",
    "mode": "real_time",
    "config": {
        "tick_rate": 100,
        "ap_regen_per_second": 2,
        "mp_regen_per_second": 1,
        ...
    }
}
```

#### 3. Create Configuration
```http
POST /api/fights/configurations
Content-Type: application/json
Authorization: Bearer {admin_token}

{
    "name": "custom_config",
    "mode": "real_time",
    "config": {
        "tick_rate": 75,
        "ap_regen_per_second": 2.5,
        ...
    }
}

Response:
{
    "id": 5,
    "name": "custom_config",
    "mode": "real_time",
    "is_default": false,
    "is_active": true,
    "config": { ... },
    "created_at": "2024-01-01T12:00:00Z"
}
```

#### 4. Update Configuration
```http
PATCH /api/fights/configurations/{id}
Content-Type: application/json
Authorization: Bearer {admin_token}

{
    "config": {
        "tick_rate": 80
    },
    "is_active": true
}

Response:
{
    "success": true,
    "configuration": { ... },
    "updated_at": "2024-01-01T12:00:00Z"
}
```

### Fight History & Statistics

#### 1. Get Fight History
```http
GET /api/fights/history?mode={mode}&limit=10&offset=0

Response:
{
    "fights": [
        {
            "id": 123,
            "mode": "real_time",
            "type": "pvp",
            "duration": 245,
            "winner": 0,
            "participants": [ ... ],
            "started_at": "2024-01-01T12:00:00Z",
            "ended_at": "2024-01-01T12:04:05Z"
        }
    ],
    "total": 150,
    "limit": 10,
    "offset": 0
}
```

#### 2. Get Combat Statistics
```http
GET /api/fights/statistics

Response:
{
    "total_fights": 1500,
    "by_mode": {
        "turn_based": 900,
        "real_time": 600
    },
    "by_type": {
        "pvp": 800,
        "pve": 700
    },
    "average_duration": {
        "turn_based": 420,
        "real_time": 180
    },
    "active_fights": 25,
    "peak_concurrent": 50
}
```

---

## WebSocket Events

### Connection & Authentication

```javascript
// Client -> Server
socket.emit('authenticate', {
    token: 'jwt_token',
    characterId: 1
});

// Server -> Client
socket.on('authenticated', (data) => {
    console.log('Authenticated:', data);
});
```

### Fight Events

#### Join Fight
```javascript
// Client -> Server
socket.emit('fight:join', {
    fightId: 123
});

// Server -> Client
socket.on('fight:joined', (data) => {
    console.log('Joined fight:', data.fightId);
    console.log('Current state:', data.state);
});
```

#### Turn-Based Events
```javascript
// Server -> Client
socket.on('turn:start', (data) => {
    console.log('Turn started for:', data.actorId);
    console.log('Turn number:', data.turnNumber);
    console.log('Time limit:', data.timeLimit);
});

socket.on('turn:end', (data) => {
    console.log('Turn ended for:', data.actorId);
    console.log('Next actor:', data.nextActorId);
});

socket.on('action:executed', (data) => {
    console.log('Action executed:', data.action);
    console.log('Result:', data.result);
});
```

#### Real-Time Events
```javascript
// Server -> Client (High frequency)
socket.on('state:update', (data) => {
    console.log('Fight ID:', data.fightId);
    console.log('State:', data.state);
    console.log('Timestamp:', data.timestamp);
});

// Server -> Client
socket.on('action:queued', (data) => {
    console.log('Action queued:', data.queueId);
    console.log('Position:', data.queuePosition);
});

socket.on('action:processing', (data) => {
    console.log('Processing action:', data.actionId);
});

socket.on('action:completed', (data) => {
    console.log('Action completed:', data.actionId);
    console.log('Result:', data.result);
});

socket.on('resource:update', (data) => {
    console.log('Resources updated for:', data.actorId);
    console.log('AP:', data.ap, 'MP:', data.mp);
});

socket.on('cooldown:update', (data) => {
    console.log('Cooldowns for:', data.actorId);
    console.log('Cooldowns:', data.cooldowns);
});
```

#### Common Events
```javascript
// Server -> Client
socket.on('fight:ended', (data) => {
    console.log('Fight ended');
    console.log('Winner:', data.winner);
    console.log('Rewards:', data.rewards);
});

socket.on('participant:disconnected', (data) => {
    console.log('Player disconnected:', data.participantId);
});

socket.on('participant:reconnected', (data) => {
    console.log('Player reconnected:', data.participantId);
});

socket.on('error', (error) => {
    console.error('Combat error:', error.message);
});
```

---

## Configuration Examples

### 1. Classic Turn-Based
```json
{
    "name": "classic_dofus",
    "mode": "turn_based",
    "config": {
        "turn_time_limit": 90,
        "ap_per_turn": 6,
        "mp_per_turn": 3,
        "allow_flee": true,
        "auto_end_turn": false,
        "auto_pass_timeout": true,
        "show_turn_order": true,
        "penalty_for_timeout": {
            "ap_reduction": 0,
            "mp_reduction": 0,
            "skip_next_turn": false
        }
    }
}
```

### 2. Speed Chess Mode (Fast Turn-Based)
```json
{
    "name": "speed_chess",
    "mode": "turn_based",
    "config": {
        "turn_time_limit": 15,
        "ap_per_turn": 4,
        "mp_per_turn": 2,
        "allow_flee": false,
        "auto_end_turn": true,
        "auto_pass_timeout": true,
        "show_turn_order": true,
        "penalty_for_timeout": {
            "ap_reduction": 2,
            "mp_reduction": 1,
            "skip_next_turn": false
        }
    }
}
```

### 3. Action Real-Time
```json
{
    "name": "action_combat",
    "mode": "real_time",
    "config": {
        "tick_rate": 50,
        "ap_regen_per_second": 3,
        "mp_regen_per_second": 1.5,
        "global_cooldown": 250,
        "spell_queue_size": 5,
        "allow_flee": false,
        "max_fight_duration": 300,
        "action_prediction": true,
        "lag_compensation": true,
        "resource_regen_tick": 5,
        "cooldown_reduction_rate": 0
    }
}
```

### 4. Tactical Real-Time
```json
{
    "name": "tactical_combat",
    "mode": "real_time",
    "config": {
        "tick_rate": 200,
        "ap_regen_per_second": 1,
        "mp_regen_per_second": 0.5,
        "global_cooldown": 1000,
        "spell_queue_size": 2,
        "allow_flee": true,
        "max_fight_duration": 600,
        "action_prediction": false,
        "lag_compensation": true,
        "resource_regen_tick": 10,
        "cooldown_reduction_rate": 0.1
    }
}
```

### 5. PvE Boss Fight
```json
{
    "name": "boss_fight",
    "mode": "real_time",
    "config": {
        "tick_rate": 100,
        "ap_regen_per_second": 2,
        "mp_regen_per_second": 1,
        "global_cooldown": 500,
        "spell_queue_size": 3,
        "allow_flee": false,
        "max_fight_duration": 900,
        "action_prediction": true,
        "lag_compensation": false,
        "resource_regen_tick": 5,
        "cooldown_reduction_rate": 0
    }
}
```

---

## Migration Guide

### For Existing Clients

#### 1. Update API Calls

**Old API Call:**
```javascript
// Old way - no mode specification
const response = await fetch('/api/fights', {
    method: 'POST',
    body: JSON.stringify({
        team0CharacterIds: [1, 2],
        team1Ids: [3, 4],
        fightType: 'pvp',
        mapId: 100
    })
});
```

**New API Call:**
```javascript
// New way - with mode specification
const response = await fetch('/api/fights', {
    method: 'POST',
    body: JSON.stringify({
        team0CharacterIds: [1, 2],
        team1Ids: [3, 4],
        fightType: 'pvp',
        combatMode: 'turn_based',  // Explicitly specify mode
        mapId: 100,
        configName: 'classic_turn_based'  // Optional
    })
});
```

#### 2. Handle New Response Format

```javascript
const result = await response.json();

if (result.mode === 'real_time') {
    // Setup real-time event listeners
    setupRealTimeListeners(result.fight.id);
} else {
    // Use existing turn-based logic
    setupTurnBasedListeners(result.fight.id);
}
```

#### 3. Update WebSocket Handlers

```javascript
// Add new event handlers for real-time mode
function setupRealTimeListeners(fightId) {
    socket.on('state:update', handleStateUpdate);
    socket.on('action:queued', handleActionQueued);
    socket.on('resource:update', handleResourceUpdate);
    socket.on('cooldown:update', handleCooldownUpdate);
}

function handleStateUpdate(data) {
    // Update UI with latest state
    updateCombatUI(data.state);

    // Update resource bars smoothly
    updateResourceBars(data.state.participants);
}

function handleActionQueued(data) {
    // Show queued action in UI
    showQueuedAction(data.queueId, data.queuePosition);
}
```

### Database Migration Script

```sql
-- Migration script for existing database
BEGIN;

-- Add new columns to fights table
ALTER TABLE fights
ADD COLUMN IF NOT EXISTS combat_mode VARCHAR(20) DEFAULT 'turn_based',
ADD COLUMN IF NOT EXISTS config JSONB DEFAULT '{}',
ADD COLUMN IF NOT EXISTS tick_rate INTEGER DEFAULT 100,
ADD COLUMN IF NOT EXISTS last_tick_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS tick_count INTEGER DEFAULT 0;

-- Update existing fights to have turn_based mode
UPDATE fights
SET combat_mode = 'turn_based',
    config = '{
        "turn_time_limit": 120,
        "ap_per_turn": 6,
        "mp_per_turn": 3,
        "allow_flee": true,
        "auto_end_turn": true
    }'::jsonb
WHERE combat_mode IS NULL;

-- Add new columns to fight_participants
ALTER TABLE fight_participants
ADD COLUMN IF NOT EXISTS ap_regen_rate DECIMAL(5,2) DEFAULT 1.0,
ADD COLUMN IF NOT EXISTS mp_regen_rate DECIMAL(5,2) DEFAULT 0.5,
ADD COLUMN IF NOT EXISTS last_ap_regen TIMESTAMP DEFAULT NOW(),
ADD COLUMN IF NOT EXISTS last_mp_regen TIMESTAMP DEFAULT NOW(),
ADD COLUMN IF NOT EXISTS action_queue JSONB DEFAULT '[]',
ADD COLUMN IF NOT EXISTS cooldowns JSONB DEFAULT '{}',
ADD COLUMN IF NOT EXISTS fractional_ap DECIMAL(5,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS fractional_mp DECIMAL(5,2) DEFAULT 0;

-- Create configurations table
CREATE TABLE IF NOT EXISTS combat_configurations (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    mode VARCHAR(20) NOT NULL,
    config JSONB NOT NULL,
    is_default BOOLEAN DEFAULT false,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Insert default configurations
INSERT INTO combat_configurations (name, mode, config, is_default)
VALUES
    ('classic_turn_based', 'turn_based', '{"turn_time_limit": 120, "ap_per_turn": 6, "mp_per_turn": 3}', true),
    ('fast_real_time', 'real_time', '{"tick_rate": 100, "ap_regen_per_second": 2, "mp_regen_per_second": 1}', false)
ON CONFLICT (name) DO NOTHING;

-- Add indexes
CREATE INDEX IF NOT EXISTS idx_fights_combat_mode ON fights(combat_mode);
CREATE INDEX IF NOT EXISTS idx_fights_realtime_active ON fights(id) WHERE combat_mode = 'real_time' AND status = 'active';

COMMIT;
```

---

## Client Integration

### Unity Client Example

```csharp
// CombatManager.cs
public class CombatManager : MonoBehaviour
{
    private string combatMode;
    private int fightId;
    private SocketIOClient socket;

    public async Task StartFight(int[] team0, int[] team1, string mode = "turn_based")
    {
        var request = new StartFightRequest
        {
            Team0CharacterIds = team0,
            Team1Ids = team1,
            FightType = "pvp",
            CombatMode = mode,
            MapId = GameManager.Instance.CurrentMapId
        };

        var response = await ApiClient.PostAsync<StartFightResponse>("/api/fights", request);

        this.combatMode = response.Mode;
        this.fightId = response.Fight.Id;

        // Join WebSocket room
        socket.Emit("fight:join", new { fightId = this.fightId });

        // Setup appropriate handlers
        if (combatMode == "real_time")
        {
            SetupRealTimeHandlers();
        }
        else
        {
            SetupTurnBasedHandlers();
        }
    }

    private void SetupRealTimeHandlers()
    {
        socket.On("state:update", (data) =>
        {
            var state = JsonConvert.DeserializeObject<FightState>(data);
            UpdateCombatState(state);
        });

        socket.On("action:queued", (data) =>
        {
            var queueData = JsonConvert.DeserializeObject<QueuedActionData>(data);
            ShowActionQueued(queueData);
        });

        // Start interpolation for smooth updates
        StartCoroutine(InterpolatePositions());
    }

    public async Task PerformAction(CombatAction action)
    {
        var response = await ApiClient.PostAsync<ActionResponse>(
            $"/api/fights/{fightId}/action",
            new { actorId = LocalPlayer.Id, action }
        );

        if (combatMode == "real_time" && response.Queued)
        {
            // Show action in queue UI
            UI.ShowQueuedAction(response.QueueId, response.QueuePosition);
        }
        else
        {
            // Show immediate result for turn-based
            UI.ShowActionResult(response);
        }
    }

    private IEnumerator InterpolatePositions()
    {
        while (combatMode == "real_time" && IsInCombat())
        {
            // Smooth position updates between server ticks
            foreach (var participant in participants)
            {
                participant.InterpolatePosition(Time.deltaTime);
            }
            yield return null;
        }
    }
}
```

### Web Client Example

```typescript
// combatManager.ts
class CombatManager {
    private mode: 'turn_based' | 'real_time';
    private fightId: number;
    private socket: Socket;
    private stateUpdateInterval: number;

    async startFight(
        team0: number[],
        team1: number[],
        mode: 'turn_based' | 'real_time' = 'turn_based'
    ) {
        const response = await fetch('/api/fights', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                team0CharacterIds: team0,
                team1Ids: team1,
                fightType: 'pvp',
                combatMode: mode,
                mapId: this.currentMapId
            })
        });

        const data = await response.json();
        this.mode = data.mode;
        this.fightId = data.fight.id;

        // Join fight room
        this.socket.emit('fight:join', { fightId: this.fightId });

        // Setup handlers based on mode
        if (this.mode === 'real_time') {
            this.setupRealTimeMode();
        } else {
            this.setupTurnBasedMode();
        }
    }

    private setupRealTimeMode() {
        // High-frequency state updates
        this.socket.on('state:update', (data) => {
            this.handleStateUpdate(data);
        });

        // Action queue events
        this.socket.on('action:queued', (data) => {
            this.ui.showQueuedAction(data);
        });

        // Resource updates
        this.socket.on('resource:update', (data) => {
            this.ui.updateResourceBars(data);
        });

        // Start client-side prediction
        this.stateUpdateInterval = setInterval(() => {
            this.predictNextState();
        }, 16); // 60 FPS
    }

    async performAction(action: CombatAction) {
        const response = await fetch(`/api/fights/${this.fightId}/action`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                actorId: this.localPlayer.id,
                action
            })
        });

        const result = await response.json();

        if (this.mode === 'real_time' && result.queued) {
            // Add to local queue for prediction
            this.localActionQueue.push({
                id: result.queueId,
                action,
                estimatedTime: result.estimatedExecutionTime
            });
        }
    }

    private predictNextState() {
        if (this.mode !== 'real_time') return;

        // Client-side prediction for smoother gameplay
        const deltaTime = Date.now() - this.lastStateUpdate;

        // Predict resource regeneration
        this.participants.forEach(p => {
            if (p.id === this.localPlayer.id) {
                p.ap = Math.min(p.maxAp, p.ap + (this.apRegenRate * deltaTime / 1000));
                p.mp = Math.min(p.maxMp, p.mp + (this.mpRegenRate * deltaTime / 1000));
            }
        });

        // Update UI
        this.ui.render(this.participants);
    }

    cleanup() {
        if (this.stateUpdateInterval) {
            clearInterval(this.stateUpdateInterval);
        }
        this.socket.off('state:update');
        this.socket.off('action:queued');
        this.socket.off('resource:update');
    }
}
```

---

## Performance Considerations

### API Rate Limits

```typescript
// Rate limiting configuration
export const RATE_LIMITS = {
    startFight: {
        windowMs: 60000,      // 1 minute
        max: 5                // 5 fights per minute
    },
    performAction: {
        turn_based: {
            windowMs: 1000,   // 1 second
            max: 10           // 10 actions per second
        },
        real_time: {
            windowMs: 1000,   // 1 second
            max: 30           // 30 actions per second
        }
    },
    getState: {
        windowMs: 1000,       // 1 second
        max: 100              // 100 requests per second
    }
};
```

### Caching Strategy

```typescript
// Cache configuration for different endpoints
export const CACHE_CONFIG = {
    configurations: {
        ttl: 300,             // 5 minutes
        key: 'combat:configs'
    },
    fightState: {
        ttl: 5,               // 5 seconds for real-time
        key: (id: number) => `combat:state:${id}`
    },
    statistics: {
        ttl: 60,              // 1 minute
        key: 'combat:stats'
    }
};
```

---

## Error Codes

### Combat-Specific Error Codes

| Code | Message | Description |
|------|---------|-------------|
| COMBAT_001 | Invalid combat mode | Specified mode is not supported |
| COMBAT_002 | Configuration not found | Specified configuration name doesn't exist |
| COMBAT_003 | Not your turn | Action attempted outside of turn (turn-based) |
| COMBAT_004 | Insufficient resources | Not enough AP/MP for action |
| COMBAT_005 | Action on cooldown | Action still on cooldown (real-time) |
| COMBAT_006 | Queue full | Action queue is at capacity (real-time) |
| COMBAT_007 | Fight not active | Fight has ended or not started |
| COMBAT_008 | Invalid target | Target is invalid or out of range |
| COMBAT_009 | Mode conversion failed | Cannot convert mode for this fight |
| COMBAT_010 | Participant disconnected | Participant has disconnected |

---

## Testing

### API Testing Examples

```bash
# Start a turn-based fight
curl -X POST http://localhost:3000/api/fights \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "team0CharacterIds": [1, 2],
    "team1Ids": [3, 4],
    "fightType": "pvp",
    "combatMode": "turn_based"
  }'

# Start a real-time fight
curl -X POST http://localhost:3000/api/fights \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "team0CharacterIds": [1],
    "team1Ids": [2],
    "fightType": "pvp",
    "combatMode": "real_time",
    "configName": "fast_real_time"
  }'

# Get fight state
curl -X GET http://localhost:3000/api/fights/123/state \
  -H "Authorization: Bearer $TOKEN"

# Perform action
curl -X POST http://localhost:3000/api/fights/123/action \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "actorId": 1,
    "action": {
      "type": "spell",
      "spellId": 1,
      "targetId": 2
    }
  }'
```

### WebSocket Testing

```javascript
// test-websocket.js
const io = require('socket.io-client');

const socket = io('http://localhost:3000');

socket.on('connect', () => {
    console.log('Connected');

    // Authenticate
    socket.emit('authenticate', {
        token: 'your-jwt-token',
        characterId: 1
    });
});

socket.on('authenticated', () => {
    // Join a fight
    socket.emit('fight:join', { fightId: 123 });
});

socket.on('state:update', (data) => {
    console.log('State update:', data);
});

socket.on('error', (error) => {
    console.error('Error:', error);
});
```

---

## Conclusion

This documentation provides a comprehensive guide for implementing and using the hybrid combat system in GOFUS. The system maintains full backward compatibility while adding powerful real-time capabilities. Follow the migration guide carefully to ensure a smooth transition for existing clients.