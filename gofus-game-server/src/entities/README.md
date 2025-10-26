# Game Entities

This directory contains all core entity classes for the GOFUS game server.

## Overview

All entities extend the base `Entity` class which provides common properties and methods:
- `id`: Unique identifier
- `name`: Entity name
- `position`: Map location (mapId, cellId, direction)

## Entity Classes

### Entity (Base Class)
The abstract base class for all game entities.

```typescript
import { Entity, IPosition } from '@entities';
```

### Player
Represents a player character with full stats, characteristics, and combat capabilities.

**Key Features:**
- Character stats (HP, MP, AP, movement)
- Six characteristics (vitality, wisdom, strength, intelligence, chance, agility)
- Experience and leveling system
- Combat state management
- Kamas (currency) management

**Example Usage:**
```typescript
import { Player, IPlayerData } from '@entities';

const playerData: IPlayerData = {
  id: 'player_123',
  characterId: 'char_456',
  accountId: 'acc_789',
  name: 'Heroic Warrior',
  level: 10,
  classId: 1,
  position: { mapId: 1001, cellId: 250, direction: 1 },
  stats: {
    hp: 150,
    maxHp: 150,
    mp: 50,
    maxMp: 50,
    ap: 6,
    maxAp: 6,
    movement: 3
  },
  characteristics: {
    vitality: 20,
    wisdom: 15,
    strength: 25,
    intelligence: 10,
    chance: 12,
    agility: 18
  },
  kamas: 1000,
  experience: 5000
};

const player = new Player(playerData);

// Take damage
player.takeDamage(30);

// Heal
player.heal(20);

// Add experience (returns true if leveled up)
const leveledUp = player.addExperience(500);

// Use action points
if (player.useAp(4)) {
  console.log('Used 4 AP for spell');
}

// Respawn
player.respawn(1001, 250);
```

### NPC
Non-player characters with dialogue trees and interactive actions.

**Key Features:**
- Multiple NPC types (quest giver, merchant, banker, trainer, etc.)
- Dialogue system with branching conversations
- Action system (quests, shops, services)
- Per-player dialogue state tracking

**Example Usage:**
```typescript
import { NPC, NPCType, INPCData } from '@entities';

const npcData: INPCData = {
  id: 'npc_001',
  name: 'Village Elder',
  type: NPCType.QUEST_GIVER,
  position: { mapId: 1001, cellId: 300 },
  dialogues: [
    {
      id: 'greeting',
      text: 'Greetings, traveler! How may I help you?',
      responses: [
        {
          id: 'quest',
          text: 'Do you have any quests?',
          nextDialogueId: 'quest_info',
          action: 'start_quest',
          actionParams: { questId: 'quest_001' }
        },
        {
          id: 'goodbye',
          text: 'Nothing, thanks.',
          nextDialogueId: undefined
        }
      ]
    },
    {
      id: 'quest_info',
      text: 'I need help defeating the monsters in the forest!',
      responses: [
        {
          id: 'accept',
          text: 'I will help you.',
          action: 'accept_quest'
        }
      ]
    }
  ],
  questId: 'quest_001'
};

const npc = new NPC(npcData);

// Interact with NPC
const dialogue = npc.interact('player_123');

// Handle player response
const nextDialogue = npc.handleResponse('player_123', 'quest');

// Reset dialogue for player
npc.resetDialogue('player_123');
```

### Monster
Hostile creatures with combat AI and loot drops.

**Key Features:**
- Combat stats (HP, damage, defense)
- Aggro system with configurable range
- Automatic respawn system
- Loot drop system with probability
- Damage resistance by element type

**Example Usage:**
```typescript
import { Monster, IMonsterData } from '@entities';

const monsterData: IMonsterData = {
  id: 'monster_001',
  name: 'Forest Wolf',
  level: 5,
  position: { mapId: 1001, cellId: 400 },
  stats: {
    hp: 100,
    maxHp: 100,
    damage: 15,
    defense: 5,
    resistance: {
      fire: 0,
      water: 5,
      earth: 10,
      air: 0,
      neutral: 5
    }
  },
  aggroRange: 3,
  respawnTime: 300, // 5 minutes
  drops: [
    {
      itemId: 'wolf_pelt',
      itemName: 'Wolf Pelt',
      chance: 50,
      minQuantity: 1,
      maxQuantity: 2
    },
    {
      itemId: 'wolf_fang',
      itemName: 'Wolf Fang',
      chance: 20,
      minQuantity: 1,
      maxQuantity: 1
    }
  ],
  experience: 150
};

const monster = new Monster(monsterData);

// Check if should aggro on player
const playerPos = { mapId: 1001, cellId: 402 };
if (monster.shouldAggro(playerPos)) {
  monster.enterCombat('player_123');
}

// Take damage
const damageDealt = monster.takeDamage(25, 'fire');

// Attack
const damage = monster.attack('player_123');

// Generate loot
const drops = monster.drop();
```

### GameObject
Interactive objects in the game world.

**Key Features:**
- Multiple object types (doors, chests, levers, teleporters, etc.)
- Requirement system (level, items, quests, keys)
- Reward system
- Cooldown and usage limits
- State management

**Example Usage:**
```typescript
import { GameObject, GameObjectType, IGameObjectData } from '@entities';

const chestData: IGameObjectData = {
  id: 'chest_001',
  name: 'Treasure Chest',
  type: GameObjectType.CHEST,
  position: { mapId: 1001, cellId: 500 },
  interactive: true,
  usable: true,
  requirements: [
    {
      type: 'level',
      value: 10,
      message: 'You must be level 10 to open this chest.'
    },
    {
      type: 'key',
      value: 'golden_key',
      message: 'This chest requires a Golden Key.'
    }
  ],
  rewards: [
    {
      type: 'item',
      value: 'rare_sword',
      quantity: 1
    },
    {
      type: 'kamas',
      value: 500
    }
  ],
  maxUses: 1
};

const chest = new GameObject(chestData);

// Check if player can interact
const canOpen = chest.canInteract('player_123', {
  level: 12,
  hasKey: (keyId: string) => keyId === 'golden_key'
});

// Use the object
const rewards = chest.use('player_123');

// Check remaining cooldown
const cooldown = chest.getRemainingCooldown('player_123');
```

## Common Patterns

### Entity Serialization
All entities implement a `toJSON()` method for serialization:

```typescript
const player = new Player(playerData);
const serialized = player.toJSON();
// Send to client or save to database
```

### Position Management
All entities inherit position management:

```typescript
entity.updatePosition(newMapId, newCellId, newDirection);
const position = entity.getPosition();
```

## Type Safety

All entities are fully typed with TypeScript interfaces. Import the interfaces you need:

```typescript
import {
  IPlayerData,
  IPlayerStats,
  INPCData,
  IMonsterData,
  IGameObjectData
} from '@entities';
```

## Best Practices

1. Always validate player data before creating entities
2. Use the provided interfaces for type safety
3. Handle entity cleanup (especially Monster timers) when removing from game
4. Serialize entities using `toJSON()` for network transmission
5. Check requirements before allowing interactions with GameObjects
6. Track combat state properly for both Players and Monsters
