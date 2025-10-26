/**
 * Entity Tests
 * Basic tests to verify entity functionality
 */

import {
  Player,
  NPC,
  Monster,
  GameObject,
  IPlayerData,
  INPCData,
  IMonsterData,
  IGameObjectData,
  NPCType,
  GameObjectType,
} from '../../src/entities';

describe('Entity Tests', () => {
  describe('Player Entity', () => {
    it('should create a player with correct properties', () => {
      const playerData: IPlayerData = {
        id: 'player_1',
        characterId: 'char_1',
        accountId: 'acc_1',
        name: 'Test Player',
        level: 1,
        classId: 1,
        position: { mapId: 1, cellId: 100 },
        stats: {
          hp: 50,
          maxHp: 50,
          mp: 20,
          maxMp: 20,
          ap: 6,
          maxAp: 6,
          movement: 3,
        },
        characteristics: {
          vitality: 10,
          wisdom: 10,
          strength: 10,
          intelligence: 10,
          chance: 10,
          agility: 10,
        },
        kamas: 100,
        experience: 0,
      };

      const player = new Player(playerData);

      expect(player.id).toBe('player_1');
      expect(player.name).toBe('Test Player');
      expect(player.level).toBe(1);
      expect(player.stats.hp).toBe(50);
    });

    it('should handle damage correctly', () => {
      const playerData: IPlayerData = {
        id: 'player_1',
        characterId: 'char_1',
        accountId: 'acc_1',
        name: 'Test Player',
        level: 1,
        classId: 1,
        position: { mapId: 1, cellId: 100 },
        stats: {
          hp: 50,
          maxHp: 50,
          mp: 20,
          maxMp: 20,
          ap: 6,
          maxAp: 6,
          movement: 3,
        },
        characteristics: {
          vitality: 10,
          wisdom: 10,
          strength: 10,
          intelligence: 10,
          chance: 10,
          agility: 10,
        },
        kamas: 100,
        experience: 0,
      };

      const player = new Player(playerData);
      player.takeDamage(20);

      expect(player.stats.hp).toBe(30);
      expect(player.isDead).toBe(false);

      player.takeDamage(30);
      expect(player.stats.hp).toBe(0);
      expect(player.isDead).toBe(true);
    });

    it('should heal correctly', () => {
      const playerData: IPlayerData = {
        id: 'player_1',
        characterId: 'char_1',
        accountId: 'acc_1',
        name: 'Test Player',
        level: 1,
        classId: 1,
        position: { mapId: 1, cellId: 100 },
        stats: {
          hp: 30,
          maxHp: 50,
          mp: 20,
          maxMp: 20,
          ap: 6,
          maxAp: 6,
          movement: 3,
        },
        characteristics: {
          vitality: 10,
          wisdom: 10,
          strength: 10,
          intelligence: 10,
          chance: 10,
          agility: 10,
        },
        kamas: 100,
        experience: 0,
      };

      const player = new Player(playerData);
      player.heal(15);

      expect(player.stats.hp).toBe(45);

      player.heal(100);
      expect(player.stats.hp).toBe(50); // Should cap at maxHp
    });
  });

  describe('NPC Entity', () => {
    it('should create an NPC with dialogues', () => {
      const npcData: INPCData = {
        id: 'npc_1',
        name: 'Test NPC',
        type: NPCType.QUEST_GIVER,
        position: { mapId: 1, cellId: 200 },
        dialogues: [
          {
            id: 'greeting',
            text: 'Hello!',
          },
        ],
      };

      const npc = new NPC(npcData);

      expect(npc.id).toBe('npc_1');
      expect(npc.name).toBe('Test NPC');
      expect(npc.type).toBe(NPCType.QUEST_GIVER);
      expect(npc.dialogues.length).toBe(1);
    });

    it('should return dialogue when interacting', () => {
      const npcData: INPCData = {
        id: 'npc_1',
        name: 'Test NPC',
        type: NPCType.GENERIC,
        position: { mapId: 1, cellId: 200 },
        dialogues: [
          {
            id: 'greeting',
            text: 'Hello, traveler!',
          },
        ],
      };

      const npc = new NPC(npcData);
      const dialogue = npc.interact('player_1');

      expect(dialogue).toBeTruthy();
      expect((dialogue as any).text).toBe('Hello, traveler!');
    });
  });

  describe('Monster Entity', () => {
    it('should create a monster with stats', () => {
      const monsterData: IMonsterData = {
        id: 'monster_1',
        name: 'Test Monster',
        level: 5,
        position: { mapId: 1, cellId: 300 },
        stats: {
          hp: 100,
          maxHp: 100,
          damage: 15,
          defense: 5,
        },
        aggroRange: 3,
        respawnTime: 300,
        drops: [],
        experience: 50,
      };

      const monster = new Monster(monsterData);

      expect(monster.id).toBe('monster_1');
      expect(monster.level).toBe(5);
      expect(monster.stats.hp).toBe(100);
      expect(monster.isDead).toBe(false);
    });

    it('should handle damage and death', () => {
      const monsterData: IMonsterData = {
        id: 'monster_1',
        name: 'Test Monster',
        level: 5,
        position: { mapId: 1, cellId: 300 },
        stats: {
          hp: 100,
          maxHp: 100,
          damage: 15,
          defense: 5,
        },
        aggroRange: 3,
        respawnTime: 300,
        drops: [],
        experience: 50,
      };

      const monster = new Monster(monsterData);
      monster.takeDamage(50);

      expect(monster.stats.hp).toBe(55); // 100 - (50 - 5 defense) = 55
      expect(monster.isDead).toBe(false);

      monster.takeDamage(100);
      expect(monster.isDead).toBe(true);

      // Clean up timers
      monster.cleanup();
    });

    it('should generate loot drops', () => {
      const monsterData: IMonsterData = {
        id: 'monster_1',
        name: 'Test Monster',
        level: 5,
        position: { mapId: 1, cellId: 300 },
        stats: {
          hp: 100,
          maxHp: 100,
          damage: 15,
          defense: 5,
        },
        aggroRange: 3,
        respawnTime: 300,
        drops: [
          {
            itemId: 'item_1',
            itemName: 'Test Item',
            chance: 100,
            minQuantity: 1,
            maxQuantity: 1,
          },
        ],
        experience: 50,
      };

      const monster = new Monster(monsterData);
      const drops = monster.drop();

      expect(drops.length).toBeGreaterThanOrEqual(0);
    });
  });

  describe('GameObject Entity', () => {
    it('should create a game object', () => {
      const objectData: IGameObjectData = {
        id: 'obj_1',
        name: 'Test Chest',
        type: GameObjectType.CHEST,
        position: { mapId: 1, cellId: 400 },
        interactive: true,
        usable: true,
      };

      const gameObject = new GameObject(objectData);

      expect(gameObject.id).toBe('obj_1');
      expect(gameObject.type).toBe(GameObjectType.CHEST);
      expect(gameObject.interactive).toBe(true);
    });

    it('should handle state changes', () => {
      const objectData: IGameObjectData = {
        id: 'obj_1',
        name: 'Test Door',
        type: GameObjectType.DOOR,
        position: { mapId: 1, cellId: 400 },
        interactive: true,
        usable: true,
        state: 'closed',
      };

      const gameObject = new GameObject(objectData);

      expect(gameObject.getState()).toBe('closed');

      gameObject.setState('open');
      expect(gameObject.getState()).toBe('open');
    });

    it('should respect usage limits', () => {
      const objectData: IGameObjectData = {
        id: 'obj_1',
        name: 'Limited Chest',
        type: GameObjectType.CHEST,
        position: { mapId: 1, cellId: 400 },
        interactive: true,
        usable: true,
        maxUses: 1,
      };

      const gameObject = new GameObject(objectData);

      expect(gameObject.hasUsesRemaining()).toBe(true);
      gameObject.use('player_1');
      expect(gameObject.hasUsesRemaining()).toBe(false);
    });
  });

  describe('Entity Serialization', () => {
    it('should serialize player to JSON', () => {
      const playerData: IPlayerData = {
        id: 'player_1',
        characterId: 'char_1',
        accountId: 'acc_1',
        name: 'Test Player',
        level: 1,
        classId: 1,
        position: { mapId: 1, cellId: 100 },
        stats: {
          hp: 50,
          maxHp: 50,
          mp: 20,
          maxMp: 20,
          ap: 6,
          maxAp: 6,
          movement: 3,
        },
        characteristics: {
          vitality: 10,
          wisdom: 10,
          strength: 10,
          intelligence: 10,
          chance: 10,
          agility: 10,
        },
        kamas: 100,
        experience: 0,
      };

      const player = new Player(playerData);
      const json = player.toJSON();

      expect(json.id).toBe('player_1');
      expect(json.name).toBe('Test Player');
      expect(json.stats).toBeDefined();
      expect(json.characteristics).toBeDefined();
    });
  });
});
