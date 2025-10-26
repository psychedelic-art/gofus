/**
 * Monster Entity Tests
 * Comprehensive tests for the Monster class
 */

import { Monster, IMonsterData, IMonsterDrop } from '../Monster';
import { IPosition } from '../Entity';

describe('Monster', () => {
  const createTestMonster = (overrides?: Partial<IMonsterData>): Monster => {
    const defaultData: IMonsterData = {
      id: 'monster-1',
      name: 'Test Monster',
      level: 5,
      position: {
        mapId: 1,
        cellId: 100,
        direction: 1,
      },
      stats: {
        hp: 200,
        maxHp: 200,
        damage: 30,
        defense: 10,
        ap: 6,
        mp: 3,
      },
      aggroRange: 5,
      respawnTime: 30,
      drops: [],
      experience: 100,
      ...overrides,
    };

    return new Monster(defaultData);
  };

  describe('constructor', () => {
    it('should create a monster with valid data', () => {
      const monster = createTestMonster();

      expect(monster.id).toBe('monster-1');
      expect(monster.name).toBe('Test Monster');
      expect(monster.level).toBe(5);
      expect(monster.stats.hp).toBe(200);
      expect(monster.stats.maxHp).toBe(200);
      expect(monster.aggroRange).toBe(5);
      expect(monster.respawnTime).toBe(30);
      expect(monster.experience).toBe(100);
      expect(monster.isDead).toBe(false);
      expect(monster.isInCombat).toBe(false);
    });

    it('should set isBoss from data', () => {
      const monster = createTestMonster({ isBoss: true });

      expect(monster.isBoss).toBe(true);
    });

    it('should default isBoss to false', () => {
      const monster = createTestMonster();

      expect(monster.isBoss).toBe(false);
    });

    it('should set optional sprite', () => {
      const monster = createTestMonster({ sprite: 'monster_sprite_1' });

      expect(monster.sprite).toBe('monster_sprite_1');
    });

    it('should set optional family', () => {
      const monster = createTestMonster({ family: 'Goblin' });

      expect(monster.family).toBe('Goblin');
    });

    it('should handle abilities array', () => {
      const monster = createTestMonster({ abilities: ['fireball', 'ice_shield'] });

      expect(monster.abilities).toEqual(['fireball', 'ice_shield']);
    });

    it('should default to empty abilities array', () => {
      const monster = createTestMonster();

      expect(monster.abilities).toEqual([]);
    });

    it('should handle drops array', () => {
      const drops: IMonsterDrop[] = [
        { itemId: 'item-1', itemName: 'Gold Coin', chance: 50, minQuantity: 1, maxQuantity: 5 },
      ];
      const monster = createTestMonster({ drops });

      expect(monster.drops).toEqual(drops);
    });

    it('should copy stats object', () => {
      const stats = {
        hp: 200,
        maxHp: 200,
        damage: 30,
        defense: 10,
      };
      const monster = createTestMonster({ stats });

      // Modify original
      stats.hp = 999;

      expect(monster.stats.hp).toBe(200);
    });

    it('should handle resistance stats', () => {
      const monster = createTestMonster({
        stats: {
          hp: 200,
          maxHp: 200,
          damage: 30,
          defense: 10,
          resistance: {
            fire: 20,
            water: 15,
            earth: 10,
            air: 5,
            neutral: 8,
          },
        },
      });

      expect(monster.stats.resistance?.fire).toBe(20);
      expect(monster.stats.resistance?.water).toBe(15);
    });
  });

  describe('takeDamage', () => {
    it('should reduce HP by damage amount', () => {
      const monster = createTestMonster();

      const finalDamage = monster.takeDamage(50);

      expect(finalDamage).toBe(40); // 50 - 10 defense
      expect(monster.stats.hp).toBe(160);
    });

    it('should apply defense to reduce damage', () => {
      const monster = createTestMonster();

      const finalDamage = monster.takeDamage(20);

      expect(finalDamage).toBe(10); // 20 - 10 defense
      expect(monster.stats.hp).toBe(190);
    });

    it('should guarantee minimum 1 damage after defense', () => {
      const monster = createTestMonster();

      const finalDamage = monster.takeDamage(5);

      expect(finalDamage).toBe(1);
      expect(monster.stats.hp).toBe(199);
    });

    it('should apply resistance for specific damage types', () => {
      const monster = createTestMonster({
        stats: {
          hp: 200,
          maxHp: 200,
          damage: 30,
          defense: 10,
          resistance: { fire: 15 },
        },
      });

      const finalDamage = monster.takeDamage(50, 'fire');

      expect(finalDamage).toBe(25); // 50 - 15 fire resistance - 10 defense
      expect(monster.stats.hp).toBe(175);
    });

    it('should handle damage type without resistance', () => {
      const monster = createTestMonster({
        stats: {
          hp: 200,
          maxHp: 200,
          damage: 30,
          defense: 10,
          resistance: { fire: 15 },
        },
      });

      const finalDamage = monster.takeDamage(50, 'water');

      expect(finalDamage).toBe(40); // 50 - 10 defense (no water resistance)
      expect(monster.stats.hp).toBe(160);
    });

    it('should set isDead when HP reaches 0', () => {
      const monster = createTestMonster();

      monster.takeDamage(300);

      expect(monster.stats.hp).toBe(0);
      expect(monster.isDead).toBe(true);
    });

    it('should set isInCombat to false on death', () => {
      const monster = createTestMonster();
      monster.enterCombat('player-1');

      monster.takeDamage(300);

      expect(monster.isInCombat).toBe(false);
    });

    it('should clear currentTarget on death', () => {
      const monster = createTestMonster();
      monster.enterCombat('player-1');

      monster.takeDamage(300);

      expect(monster.currentTarget).toBeUndefined();
    });

    it('should return 0 when already dead', () => {
      const monster = createTestMonster();
      monster.takeDamage(300);

      const finalDamage = monster.takeDamage(50);

      expect(finalDamage).toBe(0);
    });

    it('should not reduce HP below 0', () => {
      const monster = createTestMonster();

      monster.takeDamage(1000);

      expect(monster.stats.hp).toBe(0);
    });

    it('should handle multiple damage instances', () => {
      const monster = createTestMonster();

      monster.takeDamage(30);
      monster.takeDamage(40);
      monster.takeDamage(50);

      expect(monster.stats.hp).toBeGreaterThan(0);
      expect(monster.stats.hp).toBeLessThan(200);
    });
  });

  describe('attack', () => {
    it('should return damage when in combat', () => {
      const monster = createTestMonster();
      monster.enterCombat('player-1');

      const damage = monster.attack('player-1');

      expect(damage).toBe(30);
    });

    it('should set currentTarget', () => {
      const monster = createTestMonster();
      monster.enterCombat();

      monster.attack('player-2');

      expect(monster.currentTarget).toBe('player-2');
    });

    it('should return 0 when not in combat', () => {
      const monster = createTestMonster();

      const damage = monster.attack('player-1');

      expect(damage).toBe(0);
    });

    it('should return 0 when dead', () => {
      const monster = createTestMonster();
      monster.enterCombat('player-1');
      monster.takeDamage(300);

      const damage = monster.attack('player-1');

      expect(damage).toBe(0);
    });

    it('should allow attacking different targets', () => {
      const monster = createTestMonster();
      monster.enterCombat();

      monster.attack('player-1');
      expect(monster.currentTarget).toBe('player-1');

      monster.attack('player-2');
      expect(monster.currentTarget).toBe('player-2');
    });
  });

  describe('shouldAggro', () => {
    it('should aggro when player is in range on same map', () => {
      const monster = createTestMonster();
      const playerPosition: IPosition = {
        mapId: 1,
        cellId: 103, // Within range
      };

      const shouldAggro = monster.shouldAggro(playerPosition);

      expect(shouldAggro).toBe(true);
    });

    it('should not aggro when player is on different map', () => {
      const monster = createTestMonster();
      const playerPosition: IPosition = {
        mapId: 2,
        cellId: 100,
      };

      const shouldAggro = monster.shouldAggro(playerPosition);

      expect(shouldAggro).toBe(false);
    });

    it('should not aggro when player is out of range', () => {
      const monster = createTestMonster({ aggroRange: 2 });
      const playerPosition: IPosition = {
        mapId: 1,
        cellId: 1000, // Far away - guaranteed to be out of range
      };

      const shouldAggro = monster.shouldAggro(playerPosition);

      expect(shouldAggro).toBe(false);
    });

    it('should not aggro when already in combat', () => {
      const monster = createTestMonster();
      monster.enterCombat('player-1');

      const playerPosition: IPosition = {
        mapId: 1,
        cellId: 103,
      };

      const shouldAggro = monster.shouldAggro(playerPosition);

      expect(shouldAggro).toBe(false);
    });

    it('should not aggro when dead', () => {
      const monster = createTestMonster();
      monster.takeDamage(300);

      const playerPosition: IPosition = {
        mapId: 1,
        cellId: 103,
      };

      const shouldAggro = monster.shouldAggro(playerPosition);

      expect(shouldAggro).toBe(false);
    });

    it('should use aggro range correctly', () => {
      const monster = createTestMonster({ aggroRange: 2, position: { mapId: 1, cellId: 100 } });
      const closePosition: IPosition = {
        mapId: 1,
        cellId: 101, // Very close
      };
      const farPosition: IPosition = {
        mapId: 1,
        cellId: 2000, // Very far away
      };

      expect(monster.shouldAggro(closePosition)).toBe(true);
      expect(monster.shouldAggro(farPosition)).toBe(false);
    });
  });

  describe('drop', () => {
    it('should return empty array when no drops configured', () => {
      const monster = createTestMonster();

      const drops = monster.drop();

      expect(drops).toEqual([]);
    });

    it('should have chance to drop items', () => {
      const monster = createTestMonster({
        drops: [
          { itemId: 'item-1', itemName: 'Gold Coin', chance: 100, minQuantity: 1, maxQuantity: 5 },
        ],
      });

      const drops = monster.drop();

      expect(drops.length).toBeGreaterThanOrEqual(0);
      expect(drops.length).toBeLessThanOrEqual(1);
    });

    it('should drop item with 100% chance', () => {
      const monster = createTestMonster({
        drops: [
          { itemId: 'item-1', itemName: 'Guaranteed Drop', chance: 100, minQuantity: 1, maxQuantity: 1 },
        ],
      });

      const drops = monster.drop();

      expect(drops.length).toBe(1);
      expect(drops[0].itemId).toBe('item-1');
    });

    it('should never drop item with 0% chance', () => {
      const monster = createTestMonster({
        drops: [
          { itemId: 'item-1', itemName: 'No Drop', chance: 0, minQuantity: 1, maxQuantity: 1 },
        ],
      });

      const drops = monster.drop();

      expect(drops.length).toBe(0);
    });

    it('should handle multiple drop items', () => {
      const monster = createTestMonster({
        drops: [
          { itemId: 'item-1', itemName: 'Item 1', chance: 100, minQuantity: 1, maxQuantity: 1 },
          { itemId: 'item-2', itemName: 'Item 2', chance: 100, minQuantity: 1, maxQuantity: 1 },
        ],
      });

      const drops = monster.drop();

      expect(drops.length).toBeGreaterThanOrEqual(0);
      expect(drops.length).toBeLessThanOrEqual(2);
    });

    it('should randomize quantity within range', () => {
      const monster = createTestMonster({
        drops: [
          { itemId: 'item-1', itemName: 'Gold Coin', chance: 100, minQuantity: 1, maxQuantity: 10 },
        ],
      });

      const drops = monster.drop();

      if (drops.length > 0) {
        expect(drops[0].minQuantity).toBeGreaterThanOrEqual(1);
        expect(drops[0].maxQuantity).toBeLessThanOrEqual(10);
        expect(drops[0].minQuantity).toBe(drops[0].maxQuantity); // Quantity is set to same value
      }
    });

    it('should handle single quantity items', () => {
      const monster = createTestMonster({
        drops: [
          { itemId: 'item-1', itemName: 'Unique Item', chance: 100, minQuantity: 1, maxQuantity: 1 },
        ],
      });

      const drops = monster.drop();

      expect(drops.length).toBe(1);
      expect(drops[0].minQuantity).toBe(1);
      expect(drops[0].maxQuantity).toBe(1);
    });
  });

  describe('respawn system', () => {
    beforeEach(() => {
      jest.useFakeTimers();
    });

    afterEach(() => {
      jest.useRealTimers();
    });

    it('should respawn after death', () => {
      const monster = createTestMonster({ respawnTime: 5 });

      monster.takeDamage(300);
      expect(monster.isDead).toBe(true);

      jest.advanceTimersByTime(5000);

      expect(monster.isDead).toBe(false);
      expect(monster.stats.hp).toBe(monster.stats.maxHp);
    });

    it('should restore HP on respawn', () => {
      const monster = createTestMonster();

      monster.takeDamage(300);
      monster.respawn();

      expect(monster.stats.hp).toBe(200);
    });

    it('should clear combat state on respawn', () => {
      const monster = createTestMonster();
      monster.enterCombat('player-1');

      monster.takeDamage(300);
      monster.respawn();

      expect(monster.isInCombat).toBe(false);
      expect(monster.currentTarget).toBeUndefined();
    });

    it('should set isDead to false on respawn', () => {
      const monster = createTestMonster();

      monster.takeDamage(300);
      monster.respawn();

      expect(monster.isDead).toBe(false);
    });

    it('should handle manual respawn', () => {
      const monster = createTestMonster();

      monster.takeDamage(300);
      monster.respawn();

      expect(monster.isDead).toBe(false);
      expect(monster.stats.hp).toBe(monster.stats.maxHp);
    });
  });

  describe('combat state', () => {
    it('should enter combat', () => {
      const monster = createTestMonster();

      monster.enterCombat();

      expect(monster.isInCombat).toBe(true);
    });

    it('should enter combat with target', () => {
      const monster = createTestMonster();

      monster.enterCombat('player-1');

      expect(monster.isInCombat).toBe(true);
      expect(monster.currentTarget).toBe('player-1');
    });

    it('should leave combat', () => {
      const monster = createTestMonster();
      monster.enterCombat('player-1');

      monster.leaveCombat();

      expect(monster.isInCombat).toBe(false);
      expect(monster.currentTarget).toBeUndefined();
    });

    it('should clear target when leaving combat', () => {
      const monster = createTestMonster();
      monster.enterCombat('player-1');

      monster.leaveCombat();

      expect(monster.currentTarget).toBeUndefined();
    });
  });

  describe('abilities and resource usage', () => {
    describe('hasAbility', () => {
      it('should return true for possessed ability', () => {
        const monster = createTestMonster({ abilities: ['fireball', 'ice_shield'] });

        expect(monster.hasAbility('fireball')).toBe(true);
      });

      it('should return false for non-possessed ability', () => {
        const monster = createTestMonster({ abilities: ['fireball'] });

        expect(monster.hasAbility('ice_shield')).toBe(false);
      });

      it('should return false when no abilities', () => {
        const monster = createTestMonster();

        expect(monster.hasAbility('fireball')).toBe(false);
      });
    });

    describe('useAp', () => {
      it('should use AP when available', () => {
        const monster = createTestMonster();

        const result = monster.useAp(3);

        expect(result).toBe(true);
        expect(monster.stats.ap).toBe(3);
      });

      it('should not use AP when insufficient', () => {
        const monster = createTestMonster({ stats: { hp: 200, maxHp: 200, damage: 30, defense: 10, ap: 2 } });

        const result = monster.useAp(3);

        expect(result).toBe(false);
        expect(monster.stats.ap).toBe(2);
      });

      it('should return false when AP is undefined', () => {
        const monster = createTestMonster({ stats: { hp: 200, maxHp: 200, damage: 30, defense: 10 } });

        const result = monster.useAp(3);

        expect(result).toBe(false);
      });

      it('should handle using all AP', () => {
        const monster = createTestMonster();

        const result = monster.useAp(6);

        expect(result).toBe(true);
        expect(monster.stats.ap).toBe(0);
      });
    });

    describe('useMp', () => {
      it('should use MP when available', () => {
        const monster = createTestMonster();

        const result = monster.useMp(2);

        expect(result).toBe(true);
        expect(monster.stats.mp).toBe(1);
      });

      it('should not use MP when insufficient', () => {
        const monster = createTestMonster({ stats: { hp: 200, maxHp: 200, damage: 30, defense: 10, mp: 1 } });

        const result = monster.useMp(2);

        expect(result).toBe(false);
        expect(monster.stats.mp).toBe(1);
      });

      it('should return false when MP is undefined', () => {
        const monster = createTestMonster({ stats: { hp: 200, maxHp: 200, damage: 30, defense: 10 } });

        const result = monster.useMp(2);

        expect(result).toBe(false);
      });

      it('should handle using all MP', () => {
        const monster = createTestMonster();

        const result = monster.useMp(3);

        expect(result).toBe(true);
        expect(monster.stats.mp).toBe(0);
      });
    });

    describe('heal', () => {
      it('should heal the monster', () => {
        const monster = createTestMonster({ stats: { hp: 100, maxHp: 200, damage: 30, defense: 10 } });

        monster.heal(50);

        expect(monster.stats.hp).toBe(150);
      });

      it('should not exceed maxHp', () => {
        const monster = createTestMonster({ stats: { hp: 180, maxHp: 200, damage: 30, defense: 10 } });

        monster.heal(50);

        expect(monster.stats.hp).toBe(200);
      });

      it('should not heal when dead', () => {
        const monster = createTestMonster();
        monster.takeDamage(300);

        monster.heal(50);

        expect(monster.stats.hp).toBe(0);
      });
    });
  });

  describe('time tracking', () => {
    it('should track time since death', () => {
      const monster = createTestMonster();

      expect(monster.getTimeSinceDeath()).toBeNull();

      monster.takeDamage(300);

      const timeSinceDeath = monster.getTimeSinceDeath();
      expect(timeSinceDeath).toBeGreaterThanOrEqual(0);
    });

    it('should return null when not dead', () => {
      const monster = createTestMonster();

      expect(monster.getTimeSinceDeath()).toBeNull();
    });

    it('should calculate remaining respawn time', () => {
      const monster = createTestMonster({ respawnTime: 30 });

      expect(monster.getRemainingRespawnTime()).toBe(0);

      monster.takeDamage(300);

      const remaining = monster.getRemainingRespawnTime();
      expect(remaining).toBeGreaterThan(0);
      expect(remaining).toBeLessThanOrEqual(30);
    });

    it('should return 0 remaining time when not dead', () => {
      const monster = createTestMonster();

      expect(monster.getRemainingRespawnTime()).toBe(0);
    });
  });

  describe('cleanup', () => {
    beforeEach(() => {
      jest.useFakeTimers();
    });

    afterEach(() => {
      jest.useRealTimers();
    });

    it('should clear respawn timer', () => {
      const monster = createTestMonster();

      monster.takeDamage(300);
      monster.cleanup();

      jest.advanceTimersByTime(100000);

      expect(monster.isDead).toBe(true);
    });

    it('should be safe to call multiple times', () => {
      const monster = createTestMonster();

      monster.cleanup();
      monster.cleanup();

      expect(monster).toBeDefined();
    });
  });

  describe('toJSON', () => {
    it('should return complete monster data', () => {
      const monster = createTestMonster();

      const json = monster.toJSON();

      expect(json).toMatchObject({
        id: 'monster-1',
        name: 'Test Monster',
        level: 5,
        position: {
          mapId: 1,
          cellId: 100,
          direction: 1,
        },
        stats: {
          hp: 200,
          maxHp: 200,
          damage: 30,
          defense: 10,
          ap: 6,
          mp: 3,
        },
        aggroRange: 5,
        respawnTime: 30,
        experience: 100,
        isDead: false,
        isInCombat: false,
      });
    });

    it('should include boss status', () => {
      const monster = createTestMonster({ isBoss: true });

      const json = monster.toJSON();

      expect(json.isBoss).toBe(true);
    });

    it('should include current target when in combat', () => {
      const monster = createTestMonster();
      monster.enterCombat('player-1');

      const json = monster.toJSON();

      expect(json.currentTarget).toBe('player-1');
    });

    it('should include remaining respawn time', () => {
      const monster = createTestMonster();
      monster.takeDamage(300);

      const json = monster.toJSON();

      expect(json.remainingRespawnTime).toBeGreaterThan(0);
    });
  });

  describe('integration scenarios', () => {
    it('should handle complete combat scenario', () => {
      const monster = createTestMonster();

      // Aggro on player
      const playerPos: IPosition = { mapId: 1, cellId: 103 };
      expect(monster.shouldAggro(playerPos)).toBe(true);

      // Enter combat
      monster.enterCombat('player-1');
      expect(monster.isInCombat).toBe(true);

      // Attack
      const damage = monster.attack('player-1');
      expect(damage).toBeGreaterThan(0);

      // Take damage
      monster.takeDamage(50);
      expect(monster.stats.hp).toBeLessThan(200);

      // Use abilities
      if (monster.stats.ap) {
        monster.useAp(3);
      }

      // Leave combat
      monster.leaveCombat();
      expect(monster.isInCombat).toBe(false);
    });

    it('should handle death and loot scenario', () => {
      const monster = createTestMonster({
        drops: [
          { itemId: 'gold', itemName: 'Gold', chance: 100, minQuantity: 10, maxQuantity: 20 },
        ],
      });

      // Kill monster
      monster.takeDamage(300);
      expect(monster.isDead).toBe(true);

      // Get drops
      const drops = monster.drop();
      expect(drops.length).toBeGreaterThanOrEqual(0);

      // Respawn
      monster.respawn();
      expect(monster.isDead).toBe(false);
      expect(monster.stats.hp).toBe(monster.stats.maxHp);
    });

    it('should handle boss monster scenario', () => {
      const boss = createTestMonster({
        isBoss: true,
        level: 50,
        stats: {
          hp: 10000,
          maxHp: 10000,
          damage: 200,
          defense: 50,
          resistance: {
            fire: 30,
            water: 30,
            earth: 30,
            air: 30,
          },
        },
        abilities: ['mega_fireball', 'summon_minions', 'heal'],
      });

      expect(boss.isBoss).toBe(true);
      expect(boss.hasAbility('mega_fireball')).toBe(true);
      expect(boss.stats.hp).toBe(10000);

      // Boss takes reduced damage due to high defense and resistance
      const damage = boss.takeDamage(100, 'fire');
      expect(damage).toBeLessThan(100);
    });
  });
});
