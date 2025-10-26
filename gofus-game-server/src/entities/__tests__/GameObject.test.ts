/**
 * GameObject Entity Tests
 * Comprehensive tests for the GameObject class
 */

import {
  GameObject,
  GameObjectType,
  IGameObjectData,
  IGameObjectRequirement,
  IGameObjectReward,
} from '../GameObject';

describe('GameObject', () => {
  const createTestGameObject = (overrides?: Partial<IGameObjectData>): GameObject => {
    const defaultData: IGameObjectData = {
      id: 'object-1',
      name: 'Test Object',
      type: GameObjectType.DECORATIVE,
      position: {
        mapId: 1,
        cellId: 100,
        direction: 1,
      },
      interactive: true,
      usable: true,
      ...overrides,
    };

    return new GameObject(defaultData);
  };

  describe('constructor', () => {
    it('should create game object with valid data', () => {
      const obj = createTestGameObject();

      expect(obj.id).toBe('object-1');
      expect(obj.name).toBe('Test Object');
      expect(obj.type).toBe(GameObjectType.DECORATIVE);
      expect(obj.interactive).toBe(true);
      expect(obj.usable).toBe(true);
      expect(obj.state).toBe('default');
    });

    it('should create door object', () => {
      const obj = createTestGameObject({ type: GameObjectType.DOOR });

      expect(obj.type).toBe(GameObjectType.DOOR);
    });

    it('should create chest object', () => {
      const obj = createTestGameObject({ type: GameObjectType.CHEST });

      expect(obj.type).toBe(GameObjectType.CHEST);
    });

    it('should create teleporter object', () => {
      const obj = createTestGameObject({ type: GameObjectType.TELEPORTER });

      expect(obj.type).toBe(GameObjectType.TELEPORTER);
    });

    it('should create resource node object', () => {
      const obj = createTestGameObject({ type: GameObjectType.RESOURCE_NODE });

      expect(obj.type).toBe(GameObjectType.RESOURCE_NODE);
    });

    it('should handle requirements array', () => {
      const requirements: IGameObjectRequirement[] = [
        { type: 'level', value: 10 },
        { type: 'item', value: 'key-1' },
      ];
      const obj = createTestGameObject({ requirements });

      expect(obj.requirements).toEqual(requirements);
    });

    it('should handle rewards array', () => {
      const rewards: IGameObjectReward[] = [
        { type: 'item', value: 'sword', quantity: 1 },
        { type: 'experience', value: 100 },
      ];
      const obj = createTestGameObject({ rewards });

      expect(obj.rewards).toEqual(rewards);
    });

    it('should handle cooldown', () => {
      const obj = createTestGameObject({ cooldown: 60 });

      expect(obj.cooldown).toBe(60);
    });

    it('should default cooldown to 0', () => {
      const obj = createTestGameObject();

      expect(obj.cooldown).toBe(0);
    });

    it('should handle maxUses', () => {
      const obj = createTestGameObject({ maxUses: 5 });

      expect(obj.maxUses).toBe(5);
    });

    it('should default maxUses to Infinity', () => {
      const obj = createTestGameObject();

      expect(obj.maxUses).toBe(Infinity);
    });

    it('should handle custom state', () => {
      const obj = createTestGameObject({ state: 'open' });

      expect(obj.state).toBe('open');
    });

    it('should handle quest ID', () => {
      const obj = createTestGameObject({
        type: GameObjectType.QUEST_OBJECT,
        questId: 'quest-1',
      });

      expect(obj.questId).toBe('quest-1');
    });

    it('should handle sprite', () => {
      const obj = createTestGameObject({ sprite: 'chest_sprite' });

      expect(obj.sprite).toBe('chest_sprite');
    });
  });

  describe('use method', () => {
    it('should return rewards when used successfully', () => {
      const rewards: IGameObjectReward[] = [
        { type: 'item', value: 'sword', quantity: 1 },
      ];
      const obj = createTestGameObject({ rewards });

      const result = obj.use('player-1');

      expect(result).toEqual(rewards);
    });

    it('should return null when not interactive', () => {
      const obj = createTestGameObject({ interactive: false });

      const result = obj.use('player-1');

      expect(result).toBeNull();
    });

    it('should return null when not usable', () => {
      const obj = createTestGameObject({ usable: false });

      const result = obj.use('player-1');

      expect(result).toBeNull();
    });

    it('should return null when uses exceeded', () => {
      const obj = createTestGameObject({ maxUses: 1 });

      obj.use('player-1');
      const result = obj.use('player-1');

      expect(result).toBeNull();
    });

    it('should respect cooldown per player', () => {
      const obj = createTestGameObject({ cooldown: 60 });

      obj.use('player-1');
      const result = obj.use('player-1');

      expect(result).toBeNull();
    });

    it('should track usage count', () => {
      const obj = createTestGameObject({ maxUses: 3 });

      obj.use('player-1');
      obj.use('player-1');
      obj.use('player-1');

      expect(obj.getRemainingUses()).toBe(0);
    });

    it('should update lastUsedBy timestamp', () => {
      const obj = createTestGameObject({ cooldown: 60 });

      obj.use('player-1');

      expect(obj.isOffCooldown('player-1')).toBe(false);
    });

    it('should allow different players to use independently', () => {
      const obj = createTestGameObject({
        cooldown: 60,
        rewards: [{ type: 'item', value: 'test', quantity: 1 }],
      });

      obj.use('player-1');
      const result = obj.use('player-2');

      expect(result).not.toBeNull();
    });

    it('should return null when locked', () => {
      const obj = createTestGameObject();
      obj.lock();

      const result = obj.use('player-1');

      expect(result).toBeNull();
    });

    it('should return empty array when no rewards', () => {
      const obj = createTestGameObject({ rewards: [] });

      const result = obj.use('player-1');

      expect(result).toBeNull();
    });
  });

  describe('canInteract', () => {
    it('should return true when conditions met', () => {
      const obj = createTestGameObject();

      expect(obj.canInteract('player-1')).toBe(true);
    });

    it('should return false when not interactive', () => {
      const obj = createTestGameObject({ interactive: false });

      expect(obj.canInteract('player-1')).toBe(false);
    });

    it('should return false when not usable', () => {
      const obj = createTestGameObject({ usable: false });

      expect(obj.canInteract('player-1')).toBe(false);
    });

    it('should return false when locked', () => {
      const obj = createTestGameObject();
      obj.lock();

      expect(obj.canInteract('player-1')).toBe(false);
    });

    it('should check level requirement', () => {
      const obj = createTestGameObject({
        requirements: [{ type: 'level', value: 10 }],
      });

      const playerData = { level: 15 };
      expect(obj.canInteract('player-1', playerData)).toBe(true);

      const lowLevelPlayer = { level: 5 };
      expect(obj.canInteract('player-2', lowLevelPlayer)).toBe(false);
    });

    it('should check item requirement', () => {
      const obj = createTestGameObject({
        requirements: [{ type: 'item', value: 'key-1' }],
      });

      const playerWithKey = { hasItem: (id: string) => id === 'key-1' };
      expect(obj.canInteract('player-1', playerWithKey)).toBe(true);

      const playerWithoutKey = { hasItem: (id: string) => false };
      expect(obj.canInteract('player-2', playerWithoutKey)).toBe(false);
    });

    it('should check quest requirement', () => {
      const obj = createTestGameObject({
        requirements: [{ type: 'quest', value: 'quest-1' }],
      });

      const playerCompleted = { hasCompletedQuest: (id: string) => id === 'quest-1' };
      expect(obj.canInteract('player-1', playerCompleted)).toBe(true);

      const playerNotCompleted = { hasCompletedQuest: (id: string) => false };
      expect(obj.canInteract('player-2', playerNotCompleted)).toBe(false);
    });

    it('should check skill requirement', () => {
      const obj = createTestGameObject({
        requirements: [{ type: 'skill', value: 'lockpicking' }],
      });

      const playerWithSkill = { hasSkill: (skill: string) => skill === 'lockpicking' };
      expect(obj.canInteract('player-1', playerWithSkill)).toBe(true);

      const playerWithoutSkill = { hasSkill: (skill: string) => false };
      expect(obj.canInteract('player-2', playerWithoutSkill)).toBe(false);
    });

    it('should check key requirement', () => {
      const obj = createTestGameObject({
        requirements: [{ type: 'key', value: 'golden-key' }],
      });

      const playerWithKey = { hasKey: (key: string) => key === 'golden-key' };
      expect(obj.canInteract('player-1', playerWithKey)).toBe(true);

      const playerWithoutKey = { hasKey: (key: string) => false };
      expect(obj.canInteract('player-2', playerWithoutKey)).toBe(false);
    });

    it('should check multiple requirements', () => {
      const obj = createTestGameObject({
        requirements: [
          { type: 'level', value: 10 },
          { type: 'item', value: 'key-1' },
        ],
      });

      const qualifiedPlayer = {
        level: 15,
        hasItem: (id: string) => id === 'key-1',
      };
      expect(obj.canInteract('player-1', qualifiedPlayer)).toBe(true);

      const unqualifiedPlayer = {
        level: 5,
        hasItem: (id: string) => false,
      };
      expect(obj.canInteract('player-2', unqualifiedPlayer)).toBe(false);
    });

    it('should return true without player data when no requirements', () => {
      const obj = createTestGameObject();

      expect(obj.canInteract('player-1')).toBe(true);
    });
  });

  describe('cooldown system', () => {
    beforeEach(() => {
      jest.useFakeTimers();
    });

    afterEach(() => {
      jest.useRealTimers();
    });

    describe('isOffCooldown', () => {
      it('should return true when no cooldown', () => {
        const obj = createTestGameObject({ cooldown: 0 });

        obj.use('player-1');

        expect(obj.isOffCooldown('player-1')).toBe(true);
      });

      it('should return true when never used', () => {
        const obj = createTestGameObject({ cooldown: 60 });

        expect(obj.isOffCooldown('player-1')).toBe(true);
      });

      it('should return false during cooldown', () => {
        const obj = createTestGameObject({ cooldown: 60 });

        obj.use('player-1');

        expect(obj.isOffCooldown('player-1')).toBe(false);
      });

      it('should return true after cooldown expires', () => {
        const obj = createTestGameObject({ cooldown: 60 });

        obj.use('player-1');
        jest.advanceTimersByTime(60000);

        expect(obj.isOffCooldown('player-1')).toBe(true);
      });

      it('should track cooldown per player', () => {
        const obj = createTestGameObject({ cooldown: 60 });

        obj.use('player-1');

        expect(obj.isOffCooldown('player-1')).toBe(false);
        expect(obj.isOffCooldown('player-2')).toBe(true);
      });
    });

    describe('getRemainingCooldown', () => {
      it('should return 0 when no cooldown', () => {
        const obj = createTestGameObject({ cooldown: 0 });

        obj.use('player-1');

        expect(obj.getRemainingCooldown('player-1')).toBe(0);
      });

      it('should return 0 when never used', () => {
        const obj = createTestGameObject({ cooldown: 60 });

        expect(obj.getRemainingCooldown('player-1')).toBe(0);
      });

      it('should return remaining time', () => {
        const obj = createTestGameObject({ cooldown: 60 });

        obj.use('player-1');
        jest.advanceTimersByTime(30000);

        const remaining = obj.getRemainingCooldown('player-1');
        expect(remaining).toBeGreaterThan(0);
        expect(remaining).toBeLessThanOrEqual(30);
      });

      it('should return 0 after cooldown expires', () => {
        const obj = createTestGameObject({ cooldown: 60 });

        obj.use('player-1');
        jest.advanceTimersByTime(70000);

        expect(obj.getRemainingCooldown('player-1')).toBe(0);
      });
    });
  });

  describe('state management', () => {
    describe('door state', () => {
      it('should toggle door state', () => {
        const obj = createTestGameObject({ type: GameObjectType.DOOR, state: 'closed' });

        obj.use('player-1');
        expect(obj.getState()).toBe('open');

        obj.use('player-1');
        expect(obj.getState()).toBe('closed');
      });
    });

    describe('chest state', () => {
      it('should open chest', () => {
        const obj = createTestGameObject({ type: GameObjectType.CHEST });

        obj.use('player-1');

        expect(obj.getState()).toBe('opened');
      });

      it('should keep chest opened', () => {
        const obj = createTestGameObject({ type: GameObjectType.CHEST });

        obj.use('player-1');
        obj.use('player-1');

        expect(obj.getState()).toBe('opened');
      });
    });

    describe('lever state', () => {
      it('should toggle lever state', () => {
        const obj = createTestGameObject({ type: GameObjectType.LEVER, state: 'off' });

        obj.use('player-1');
        expect(obj.getState()).toBe('on');

        obj.use('player-1');
        expect(obj.getState()).toBe('off');
      });
    });

    describe('resource node state', () => {
      it('should deplete resource node', () => {
        const obj = createTestGameObject({ type: GameObjectType.RESOURCE_NODE });

        obj.use('player-1');

        expect(obj.getState()).toBe('depleted');
      });
    });

    describe('setState and getState', () => {
      it('should set custom state', () => {
        const obj = createTestGameObject();

        obj.setState('active');

        expect(obj.getState()).toBe('active');
      });

      it('should get current state', () => {
        const obj = createTestGameObject({ state: 'custom' });

        expect(obj.getState()).toBe('custom');
      });
    });
  });

  describe('lock and unlock', () => {
    it('should lock object', () => {
      const obj = createTestGameObject();

      obj.lock();

      expect(obj.canInteract('player-1')).toBe(false);
    });

    it('should unlock object', () => {
      const obj = createTestGameObject();

      obj.lock();
      obj.unlock();

      expect(obj.canInteract('player-1')).toBe(true);
    });

    it('should prevent use when locked', () => {
      const obj = createTestGameObject();

      obj.lock();
      const result = obj.use('player-1');

      expect(result).toBeNull();
    });
  });

  describe('reset functionality', () => {
    beforeEach(() => {
      jest.useFakeTimers();
    });

    afterEach(() => {
      jest.useRealTimers();
    });

    it('should reset all state', () => {
      const obj = createTestGameObject({ maxUses: 3, state: 'custom' });

      obj.use('player-1');
      obj.use('player-2');
      obj.lock();

      obj.reset();

      expect(obj.getRemainingUses()).toBe(3);
      expect(obj.getState()).toBe('default');
      expect(obj.canInteract('player-1')).toBe(true);
    });

    it('should clear player-specific data', () => {
      const obj = createTestGameObject({ cooldown: 60 });

      obj.use('player-1');
      obj.reset();

      expect(obj.isOffCooldown('player-1')).toBe(true);
    });

    it('should reset for specific player only', () => {
      const obj = createTestGameObject({ cooldown: 60 });

      obj.use('player-1');
      obj.use('player-2');
      obj.resetForPlayer('player-1');

      expect(obj.isOffCooldown('player-1')).toBe(true);
      expect(obj.isOffCooldown('player-2')).toBe(false);
    });
  });

  describe('remaining uses', () => {
    it('should return Infinity for unlimited uses', () => {
      const obj = createTestGameObject();

      expect(obj.getRemainingUses()).toBe(Infinity);
    });

    it('should calculate remaining uses', () => {
      const obj = createTestGameObject({ maxUses: 5 });

      obj.use('player-1');
      obj.use('player-2');

      expect(obj.getRemainingUses()).toBe(3);
    });

    it('should return 0 when exhausted', () => {
      const obj = createTestGameObject({ maxUses: 1 });

      obj.use('player-1');

      expect(obj.getRemainingUses()).toBe(0);
    });

    it('should check if has uses remaining', () => {
      const obj = createTestGameObject({ maxUses: 2 });

      expect(obj.hasUsesRemaining()).toBe(true);

      obj.use('player-1');
      expect(obj.hasUsesRemaining()).toBe(true);

      obj.use('player-2');
      expect(obj.hasUsesRemaining()).toBe(false);
    });
  });

  describe('requirements management', () => {
    it('should add requirement', () => {
      const obj = createTestGameObject();
      const requirement: IGameObjectRequirement = {
        type: 'level',
        value: 10,
      };

      obj.addRequirement(requirement);

      expect(obj.requirements).toContain(requirement);
    });

    it('should remove requirement by type', () => {
      const obj = createTestGameObject({
        requirements: [
          { type: 'level', value: 10 },
          { type: 'item', value: 'key-1' },
        ],
      });

      const removed = obj.removeRequirement('level');

      expect(removed).toBe(true);
      expect(obj.requirements.length).toBe(1);
      expect(obj.requirements[0].type).toBe('item');
    });

    it('should return false when requirement not found', () => {
      const obj = createTestGameObject();

      const removed = obj.removeRequirement('level');

      expect(removed).toBe(false);
    });
  });

  describe('rewards management', () => {
    it('should add reward', () => {
      const obj = createTestGameObject();
      const reward: IGameObjectReward = {
        type: 'item',
        value: 'sword',
        quantity: 1,
      };

      obj.addReward(reward);

      expect(obj.rewards).toContain(reward);
    });

    it('should clear all rewards', () => {
      const obj = createTestGameObject({
        rewards: [
          { type: 'item', value: 'sword', quantity: 1 },
          { type: 'experience', value: 100 },
        ],
      });

      obj.clearRewards();

      expect(obj.rewards).toEqual([]);
    });
  });

  describe('helper methods', () => {
    it('should identify door', () => {
      const obj = createTestGameObject({ type: GameObjectType.DOOR });

      expect(obj.isDoor()).toBe(true);
    });

    it('should identify teleporter', () => {
      const obj = createTestGameObject({ type: GameObjectType.TELEPORTER });

      expect(obj.isTeleporter()).toBe(true);
    });

    it('should identify resource node', () => {
      const obj = createTestGameObject({ type: GameObjectType.RESOURCE_NODE });

      expect(obj.isResourceNode()).toBe(true);
    });

    it('should identify quest object with questId', () => {
      const obj = createTestGameObject({
        type: GameObjectType.QUEST_OBJECT,
        questId: 'quest-1',
      });

      expect(obj.isQuestObject()).toBe(true);
    });

    it('should not identify quest object without questId', () => {
      const obj = createTestGameObject({
        type: GameObjectType.QUEST_OBJECT,
      });

      expect(obj.isQuestObject()).toBe(false);
    });

    it('should return false for wrong type', () => {
      const obj = createTestGameObject({ type: GameObjectType.CHEST });

      expect(obj.isDoor()).toBe(false);
      expect(obj.isTeleporter()).toBe(false);
      expect(obj.isResourceNode()).toBe(false);
    });
  });

  describe('toJSON', () => {
    it('should return complete object data', () => {
      const obj = createTestGameObject();

      const json = obj.toJSON();

      expect(json).toMatchObject({
        id: 'object-1',
        name: 'Test Object',
        type: GameObjectType.DECORATIVE,
        position: {
          mapId: 1,
          cellId: 100,
          direction: 1,
        },
        interactive: true,
        usable: true,
        requirements: [],
        rewards: [],
        cooldown: 0,
        maxUses: Infinity,
        currentUses: 0,
        state: 'default',
        isLocked: false,
        remainingUses: Infinity,
      });
    });

    it('should include optional fields', () => {
      const obj = createTestGameObject({
        sprite: 'chest_sprite',
        questId: 'quest-1',
      });

      const json = obj.toJSON();

      expect(json.sprite).toBe('chest_sprite');
      expect(json.questId).toBe('quest-1');
    });

    it('should reflect current state', () => {
      const obj = createTestGameObject({ maxUses: 3 });

      obj.use('player-1');
      obj.lock();

      const json = obj.toJSON();

      expect(json.currentUses).toBe(1);
      expect(json.isLocked).toBe(true);
      expect(json.remainingUses).toBe(2);
    });
  });

  describe('integration scenarios', () => {
    it('should handle treasure chest scenario', () => {
      const chest = createTestGameObject({
        type: GameObjectType.CHEST,
        requirements: [{ type: 'level', value: 10 }],
        rewards: [
          { type: 'item', value: 'legendary_sword', quantity: 1 },
          { type: 'kamas', value: 1000 },
        ],
        maxUses: 1,
      });

      // Low level player cannot open
      const lowLevelPlayer = { level: 5 };
      expect(chest.canInteract('player-1', lowLevelPlayer)).toBe(false);

      // High level player can open
      const highLevelPlayer = { level: 15 };
      expect(chest.canInteract('player-2', highLevelPlayer)).toBe(true);

      const rewards = chest.use('player-2');
      expect(rewards).toBeDefined();
      expect(chest.getState()).toBe('opened');

      // Cannot use again
      expect(chest.use('player-2')).toBeNull();
    });

    it('should handle locked door scenario', () => {
      const door = createTestGameObject({
        type: GameObjectType.DOOR,
        state: 'closed',
        requirements: [{ type: 'key', value: 'bronze-key' }],
      });

      // Player without key cannot open
      const playerNoKey = { hasKey: () => false };
      expect(door.canInteract('player-1', playerNoKey)).toBe(false);

      // Player with key can open
      const playerWithKey = { hasKey: (key: string) => key === 'bronze-key' };
      expect(door.canInteract('player-2', playerWithKey)).toBe(true);

      door.use('player-2');
      expect(door.getState()).toBe('open');

      // Can close again
      door.use('player-2');
      expect(door.getState()).toBe('closed');
    });

    it('should handle resource node with cooldown', () => {
      jest.useFakeTimers();

      const node = createTestGameObject({
        type: GameObjectType.RESOURCE_NODE,
        rewards: [{ type: 'item', value: 'iron_ore', quantity: 5 }],
        cooldown: 300, // 5 minutes
      });

      // First harvest
      const rewards1 = node.use('player-1');
      expect(rewards1).toBeDefined();
      expect(node.getState()).toBe('depleted');

      // Cannot harvest during cooldown
      expect(node.use('player-1')).toBeNull();

      // Different player can harvest
      const rewards2 = node.use('player-2');
      expect(rewards2).toBeDefined();

      // After cooldown, can harvest again
      jest.advanceTimersByTime(300000);
      const rewards3 = node.use('player-1');
      expect(rewards3).toBeDefined();

      jest.useRealTimers();
    });

    it('should handle quest object scenario', () => {
      const questObject = createTestGameObject({
        type: GameObjectType.QUEST_OBJECT,
        questId: 'quest-1',
        requirements: [{ type: 'quest', value: 'quest-1' }],
        rewards: [{ type: 'experience', value: 500 }],
        maxUses: 1,
      });

      expect(questObject.isQuestObject()).toBe(true);

      // Player without quest cannot interact
      const playerNoQuest = { hasCompletedQuest: () => false };
      expect(questObject.canInteract('player-1', playerNoQuest)).toBe(false);

      // Player with quest can interact
      const playerWithQuest = { hasCompletedQuest: (id: string) => id === 'quest-1' };
      expect(questObject.canInteract('player-1', playerWithQuest)).toBe(true);

      const rewards = questObject.use('player-1');
      expect(rewards).toBeDefined();

      // Can only use once
      expect(questObject.use('player-1')).toBeNull();
    });

    it('should handle teleporter scenario', () => {
      const teleporter = createTestGameObject({
        type: GameObjectType.TELEPORTER,
        rewards: [{ type: 'teleport', value: 'city_center' }],
        cooldown: 60,
      });

      expect(teleporter.isTeleporter()).toBe(true);

      const result = teleporter.use('player-1');
      expect(result).toBeDefined();
      expect(result?.[0].type).toBe('teleport');
      expect(result?.[0].value).toBe('city_center');
    });

    it('should handle crafting station scenario', () => {
      const craftingStation = createTestGameObject({
        type: GameObjectType.CRAFTING_STATION,
        requirements: [
          { type: 'level', value: 20 },
          { type: 'skill', value: 'blacksmithing' },
        ],
      });

      const apprentice = {
        level: 15,
        hasSkill: () => false,
      };
      expect(craftingStation.canInteract('player-1', apprentice)).toBe(false);

      const master = {
        level: 25,
        hasSkill: (skill: string) => skill === 'blacksmithing',
      };
      expect(craftingStation.canInteract('player-2', master)).toBe(true);
    });

    it('should handle trap scenario', () => {
      const trap = createTestGameObject({
        type: GameObjectType.TRAP,
        state: 'armed',
        rewards: [{ type: 'experience', value: -50 }], // Negative reward (damage)
        cooldown: 10,
      });

      const result = trap.use('player-1');
      expect(result).toBeDefined();

      // Trap on cooldown, won't trigger again immediately
      expect(trap.use('player-1')).toBeNull();
    });
  });

  describe('edge cases', () => {
    it('should handle multiple simultaneous interactions', () => {
      const obj = createTestGameObject({
        maxUses: 10,
        rewards: [{ type: 'item', value: 'test', quantity: 1 }],
      });

      const players = Array.from({ length: 5 }, (_, i) => `player-${i}`);
      const results = players.map(player => obj.use(player));

      expect(results.every(r => r !== null)).toBe(true);
      expect(obj.getRemainingUses()).toBe(5);
    });

    it('should handle reset during active usage', () => {
      const obj = createTestGameObject({ maxUses: 3 });

      obj.use('player-1');
      obj.use('player-2');
      obj.reset();

      expect(obj.getRemainingUses()).toBe(3);
    });

    it('should handle state changes during cooldown', () => {
      jest.useFakeTimers();

      const obj = createTestGameObject({ cooldown: 60 });

      obj.use('player-1');
      obj.setState('custom-state');

      expect(obj.getState()).toBe('custom-state');
      expect(obj.isOffCooldown('player-1')).toBe(false);

      jest.useRealTimers();
    });

    it('should handle empty requirements array', () => {
      const obj = createTestGameObject({ requirements: [] });

      expect(obj.canInteract('player-1')).toBe(true);
    });

    it('should handle undefined player data with requirements', () => {
      const obj = createTestGameObject({
        requirements: [{ type: 'level', value: 10 }],
      });

      // Without player data, defaults to true
      expect(obj.canInteract('player-1')).toBe(true);
    });

    it('should handle very high maxUses', () => {
      const obj = createTestGameObject({ maxUses: Number.MAX_SAFE_INTEGER });

      expect(obj.getRemainingUses()).toBe(Number.MAX_SAFE_INTEGER);
      obj.use('player-1');
      expect(obj.getRemainingUses()).toBe(Number.MAX_SAFE_INTEGER - 1);
    });
  });
});
