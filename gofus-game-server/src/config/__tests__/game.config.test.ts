import {
  GAME_CONSTANTS,
  ExperienceFormulas,
  CombatFormulas,
  MovementFormulas,
  ItemFormulas,
} from '../game.config';

describe('game.config', () => {
  describe('GAME_CONSTANTS', () => {
    describe('Character Constants', () => {
      it('should have correct character limits', () => {
        expect(GAME_CONSTANTS.MAX_CHARACTERS_PER_ACCOUNT).toBe(5);
        expect(GAME_CONSTANTS.MAX_LEVEL).toBe(200);
        expect(GAME_CONSTANTS.DEFAULT_START_MAP).toBe(7411);
        expect(GAME_CONSTANTS.DEFAULT_START_CELL).toBe(311);
      });
    });

    describe('Stats Constants', () => {
      it('should have correct stats per level', () => {
        expect(GAME_CONSTANTS.STATS_PER_LEVEL).toBe(5);
        expect(GAME_CONSTANTS.SPELL_POINTS_PER_LEVEL).toBe(1);
      });
    });

    describe('Energy Constants', () => {
      it('should have correct energy values', () => {
        expect(GAME_CONSTANTS.MAX_ENERGY).toBe(10000);
        expect(GAME_CONSTANTS.ENERGY_RECOVERY_RATE).toBe(1);
      });
    });

    describe('Movement Constants', () => {
      it('should have correct movement values', () => {
        expect(GAME_CONSTANTS.DEFAULT_MOVEMENT_POINTS).toBe(3);
        expect(GAME_CONSTANTS.DIAGONAL_MOVEMENT_COST).toBe(1);
        expect(GAME_CONSTANTS.LINEAR_MOVEMENT_COST).toBe(1);
      });
    });

    describe('Combat Constants', () => {
      it('should have correct combat values', () => {
        expect(GAME_CONSTANTS.DEFAULT_ACTION_POINTS).toBe(6);
        expect(GAME_CONSTANTS.DEFAULT_INITIATIVE).toBe(100);
        expect(GAME_CONSTANTS.MAX_SUMMONS_PER_PLAYER).toBe(1);
        expect(GAME_CONSTANTS.CRITICAL_HIT_BASE_CHANCE).toBe(5);
        expect(GAME_CONSTANTS.CRITICAL_HIT_MULTIPLIER).toBe(1.5);
      });
    });

    describe('Chat Constants', () => {
      it('should have correct chat values', () => {
        expect(GAME_CONSTANTS.CHAT_MESSAGE_MAX_LENGTH).toBe(255);
        expect(GAME_CONSTANTS.CHAT_RATE_LIMIT).toBe(10);
        expect(GAME_CONSTANTS.PRIVATE_MESSAGE_MIN_LEVEL).toBe(10);
      });
    });

    describe('Trade Constants', () => {
      it('should have correct trade values', () => {
        expect(GAME_CONSTANTS.TRADE_TAX_RATE).toBe(0.02);
        expect(GAME_CONSTANTS.MIN_TRADE_LEVEL).toBe(20);
        expect(GAME_CONSTANTS.TRADE_TIMEOUT).toBe(60000);
      });
    });

    describe('Guild Constants', () => {
      it('should have correct guild values', () => {
        expect(GAME_CONSTANTS.GUILD_NAME_MIN_LENGTH).toBe(3);
        expect(GAME_CONSTANTS.GUILD_NAME_MAX_LENGTH).toBe(20);
        expect(GAME_CONSTANTS.MIN_LEVEL_CREATE_GUILD).toBe(10);
        expect(GAME_CONSTANTS.GUILD_CREATION_COST).toBe(10000);
      });
    });

    describe('Inventory Constants', () => {
      it('should have correct inventory values', () => {
        expect(GAME_CONSTANTS.MAX_INVENTORY_SLOTS).toBe(100);
        expect(GAME_CONSTANTS.MAX_STACK_SIZE).toBe(100);
      });

      it('should have all equipment slots defined', () => {
        const slots = GAME_CONSTANTS.EQUIPMENT_SLOTS;

        expect(slots.HAT).toBe(0);
        expect(slots.CLOAK).toBe(1);
        expect(slots.AMULET).toBe(2);
        expect(slots.LEFT_RING).toBe(3);
        expect(slots.RIGHT_RING).toBe(4);
        expect(slots.BELT).toBe(5);
        expect(slots.BOOTS).toBe(6);
        expect(slots.WEAPON).toBe(7);
        expect(slots.SHIELD).toBe(8);
        expect(slots.PET).toBe(9);
        expect(slots.MOUNT).toBe(10);
      });
    });

    describe('Respawn Constants', () => {
      it('should have correct respawn values', () => {
        expect(GAME_CONSTANTS.RESPAWN_HP_PERCENT).toBe(10);
        expect(GAME_CONSTANTS.RESPAWN_ENERGY_COST).toBe(100);
        expect(GAME_CONSTANTS.DEATH_XP_PENALTY).toBe(0.1);
      });
    });

    describe('Map Constants', () => {
      it('should have correct map dimensions', () => {
        expect(GAME_CONSTANTS.MAP_WIDTH).toBe(14);
        expect(GAME_CONSTANTS.MAP_HEIGHT).toBe(20);
        expect(GAME_CONSTANTS.CELLS_PER_MAP).toBe(560);
        expect(GAME_CONSTANTS.VIEW_DISTANCE).toBe(20);
      });
    });

    describe('AI Constants', () => {
      it('should have correct AI values', () => {
        expect(GAME_CONSTANTS.MOB_AGGRO_RANGE).toBe(5);
        expect(GAME_CONSTANTS.MOB_RESPAWN_TIME).toBe(300000);
        expect(GAME_CONSTANTS.MOB_MAX_PURSUIT_DISTANCE).toBe(15);
      });
    });

    describe('Packet Types', () => {
      it('should have all authentication packet types', () => {
        const types = GAME_CONSTANTS.PACKET_TYPES;

        expect(types.AUTH_REQUEST).toBe('AUTH_REQUEST');
        expect(types.AUTH_SUCCESS).toBe('AUTH_SUCCESS');
        expect(types.AUTH_FAILED).toBe('AUTH_FAILED');
      });

      it('should have all movement packet types', () => {
        const types = GAME_CONSTANTS.PACKET_TYPES;

        expect(types.MOVEMENT_REQUEST).toBe('MOVEMENT_REQUEST');
        expect(types.MOVEMENT_UPDATE).toBe('MOVEMENT_UPDATE');
        expect(types.MOVEMENT_INVALID).toBe('MOVEMENT_INVALID');
      });

      it('should have all combat packet types', () => {
        const types = GAME_CONSTANTS.PACKET_TYPES;

        expect(types.COMBAT_START).toBe('COMBAT_START');
        expect(types.COMBAT_TURN).toBe('COMBAT_TURN');
        expect(types.COMBAT_ACTION).toBe('COMBAT_ACTION');
        expect(types.COMBAT_END).toBe('COMBAT_END');
      });

      it('should have all chat packet types', () => {
        const types = GAME_CONSTANTS.PACKET_TYPES;

        expect(types.CHAT_MESSAGE).toBe('CHAT_MESSAGE');
        expect(types.CHAT_PRIVATE).toBe('CHAT_PRIVATE');
        expect(types.CHAT_GUILD).toBe('CHAT_GUILD');
        expect(types.CHAT_ERROR).toBe('CHAT_ERROR');
      });

      it('should have all map packet types', () => {
        const types = GAME_CONSTANTS.PACKET_TYPES;

        expect(types.MAP_ENTER).toBe('MAP_ENTER');
        expect(types.MAP_LEAVE).toBe('MAP_LEAVE');
        expect(types.MAP_STATE).toBe('MAP_STATE');
        expect(types.MAP_UPDATE).toBe('MAP_UPDATE');
      });

      it('should have all entity packet types', () => {
        const types = GAME_CONSTANTS.PACKET_TYPES;

        expect(types.ENTITY_SPAWN).toBe('ENTITY_SPAWN');
        expect(types.ENTITY_DESPAWN).toBe('ENTITY_DESPAWN');
        expect(types.ENTITY_UPDATE).toBe('ENTITY_UPDATE');
      });

      it('should have all system packet types', () => {
        const types = GAME_CONSTANTS.PACKET_TYPES;

        expect(types.PING).toBe('PING');
        expect(types.PONG).toBe('PONG');
        expect(types.SERVER_MESSAGE).toBe('SERVER_MESSAGE');
        expect(types.ERROR).toBe('ERROR');
      });
    });
  });

  describe('ExperienceFormulas', () => {
    describe('getExperienceForLevel()', () => {
      it('should return 0 for level 0', () => {
        expect(ExperienceFormulas.getExperienceForLevel(0)).toBe(0);
      });

      it('should return 0 for level 1', () => {
        expect(ExperienceFormulas.getExperienceForLevel(1)).toBe(0);
      });

      it('should calculate correct XP for level 2', () => {
        expect(ExperienceFormulas.getExperienceForLevel(2)).toBe(400);
      });

      it('should calculate correct XP for level 10', () => {
        expect(ExperienceFormulas.getExperienceForLevel(10)).toBe(10000);
      });

      it('should calculate correct XP for level 50', () => {
        expect(ExperienceFormulas.getExperienceForLevel(50)).toBe(250000);
      });

      it('should calculate correct XP for max level 200', () => {
        expect(ExperienceFormulas.getExperienceForLevel(200)).toBe(4000000);
      });

      it('should use quadratic formula (level * level * 100)', () => {
        const level = 15;
        const expected = level * level * 100;
        expect(ExperienceFormulas.getExperienceForLevel(level)).toBe(expected);
      });
    });

    describe('getLevelFromExperience()', () => {
      it('should return 0 for 0 XP', () => {
        expect(ExperienceFormulas.getLevelFromExperience(0)).toBe(0);
      });

      it('should return 1 for 100 XP', () => {
        expect(ExperienceFormulas.getLevelFromExperience(100)).toBe(1);
      });

      it('should return 10 for 10000 XP', () => {
        expect(ExperienceFormulas.getLevelFromExperience(10000)).toBe(10);
      });

      it('should return 50 for 250000 XP', () => {
        expect(ExperienceFormulas.getLevelFromExperience(250000)).toBe(50);
      });

      it('should return 100 for 1000000 XP', () => {
        expect(ExperienceFormulas.getLevelFromExperience(1000000)).toBe(100);
      });

      it('should handle XP values between levels', () => {
        // Between level 10 (10000) and level 11 (12100)
        expect(ExperienceFormulas.getLevelFromExperience(11000)).toBe(10);
      });
    });

    describe('calculateXPReward()', () => {
      it('should return full XP for same level', () => {
        expect(ExperienceFormulas.calculateXPReward(10, 10, 100)).toBe(100);
      });

      it('should return full XP for small level difference', () => {
        expect(ExperienceFormulas.calculateXPReward(10, 15, 100)).toBe(100);
      });

      it('should reduce XP for large level difference (higher monster)', () => {
        const result = ExperienceFormulas.calculateXPReward(10, 25, 100);
        expect(result).toBeLessThan(100);
      });

      it('should reduce XP for large level difference (lower monster)', () => {
        const result = ExperienceFormulas.calculateXPReward(25, 10, 100);
        expect(result).toBeLessThan(100);
      });

      it('should have minimum multiplier of 0.1', () => {
        const result = ExperienceFormulas.calculateXPReward(10, 100, 100);
        expect(result).toBeGreaterThanOrEqual(10); // 0.1 * 100
      });

      it('should return floored integer values', () => {
        const result = ExperienceFormulas.calculateXPReward(10, 15, 155);
        expect(Number.isInteger(result)).toBe(true);
      });

      it('should handle level difference exactly at 10', () => {
        const result = ExperienceFormulas.calculateXPReward(10, 20, 100);
        expect(result).toBe(100);
      });

      it('should reduce for level difference of 11', () => {
        const result = ExperienceFormulas.calculateXPReward(10, 21, 100);
        expect(result).toBe(90);
      });
    });
  });

  describe('CombatFormulas', () => {
    describe('calculateDamage()', () => {
      it('should calculate fire damage with intelligence', () => {
        const attackerStats = { intelligence: 50 };
        const defenderStats = { fireResistance: 10 };

        const damage = CombatFormulas.calculateDamage(100, attackerStats, defenderStats, 'fire');

        expect(damage).toBe(140); // 100 + 50 - 10
      });

      it('should calculate water damage with chance', () => {
        const attackerStats = { chance: 40 };
        const defenderStats = { waterResistance: 5 };

        const damage = CombatFormulas.calculateDamage(100, attackerStats, defenderStats, 'water');

        expect(damage).toBe(135); // 100 + 40 - 5
      });

      it('should calculate earth damage with strength', () => {
        const attackerStats = { strength: 60 };
        const defenderStats = { earthResistance: 15 };

        const damage = CombatFormulas.calculateDamage(100, attackerStats, defenderStats, 'earth');

        expect(damage).toBe(145); // 100 + 60 - 15
      });

      it('should calculate air damage with agility', () => {
        const attackerStats = { agility: 70 };
        const defenderStats = { airResistance: 20 };

        const damage = CombatFormulas.calculateDamage(100, attackerStats, defenderStats, 'air');

        expect(damage).toBe(150); // 100 + 70 - 20
      });

      it('should calculate neutral damage with half strength', () => {
        const attackerStats = { strength: 100 };
        const defenderStats = { neutralResistance: 10 };

        const damage = CombatFormulas.calculateDamage(100, attackerStats, defenderStats, 'neutral');

        expect(damage).toBe(140); // 100 + 50 - 10
      });

      it('should have minimum damage of 1', () => {
        const attackerStats = {};
        const defenderStats = { fireResistance: 200 };

        const damage = CombatFormulas.calculateDamage(100, attackerStats, defenderStats, 'fire');

        expect(damage).toBe(1);
      });

      it('should handle missing stats gracefully', () => {
        const attackerStats = {};
        const defenderStats = {};

        const damage = CombatFormulas.calculateDamage(100, attackerStats, defenderStats, 'fire');

        expect(damage).toBe(100);
      });

      it('should floor damage values', () => {
        const attackerStats = { intelligence: 55 };
        const defenderStats = { fireResistance: 10 };

        const damage = CombatFormulas.calculateDamage(100, attackerStats, defenderStats, 'fire');

        expect(Number.isInteger(damage)).toBe(true);
      });
    });

    describe('calculateInitiative()', () => {
      it('should calculate initiative with agility and intelligence', () => {
        const stats = { agility: 50, intelligence: 30 };

        const initiative = CombatFormulas.calculateInitiative(stats);

        expect(initiative).toBe(230); // 100 + (50 * 2) + 30
      });

      it('should use default base initiative', () => {
        const stats = { agility: 0, intelligence: 0 };

        const initiative = CombatFormulas.calculateInitiative(stats);

        expect(initiative).toBe(100);
      });

      it('should double agility bonus', () => {
        const stats = { agility: 100, intelligence: 0 };

        const initiative = CombatFormulas.calculateInitiative(stats);

        expect(initiative).toBe(300); // 100 + (100 * 2) + 0
      });

      it('should add intelligence bonus once', () => {
        const stats = { agility: 0, intelligence: 100 };

        const initiative = CombatFormulas.calculateInitiative(stats);

        expect(initiative).toBe(200); // 100 + 0 + 100
      });

      it('should handle missing stats', () => {
        const stats = {};

        const initiative = CombatFormulas.calculateInitiative(stats);

        expect(initiative).toBe(100);
      });
    });

    describe('calculateCriticalChance()', () => {
      it('should calculate critical chance with agility', () => {
        const stats = { agility: 100 };

        const critChance = CombatFormulas.calculateCriticalChance(stats);

        expect(critChance).toBe(15); // 5 + 10 (100 / 10)
      });

      it('should add weapon critical chance', () => {
        const stats = { agility: 50 };

        const critChance = CombatFormulas.calculateCriticalChance(stats, 10);

        expect(critChance).toBe(20); // 5 + 5 (50 / 10) + 10
      });

      it('should cap at 100%', () => {
        const stats = { agility: 1000 };

        const critChance = CombatFormulas.calculateCriticalChance(stats, 50);

        expect(critChance).toBe(100);
      });

      it('should use base chance of 5', () => {
        const stats = { agility: 0 };

        const critChance = CombatFormulas.calculateCriticalChance(stats);

        expect(critChance).toBe(5);
      });

      it('should floor agility bonus', () => {
        const stats = { agility: 95 };

        const critChance = CombatFormulas.calculateCriticalChance(stats);

        expect(critChance).toBe(14); // 5 + floor(95 / 10) = 5 + 9
      });
    });

    describe('calculateDodgeChance()', () => {
      it('should calculate dodge chance from agility', () => {
        const stats = { agility: 100 };

        const dodgeChance = CombatFormulas.calculateDodgeChance(stats);

        expect(dodgeChance).toBe(25); // floor(100 / 4)
      });

      it('should cap at 50%', () => {
        const stats = { agility: 1000 };

        const dodgeChance = CombatFormulas.calculateDodgeChance(stats);

        expect(dodgeChance).toBe(50);
      });

      it('should return 0 for no agility', () => {
        const stats = { agility: 0 };

        const dodgeChance = CombatFormulas.calculateDodgeChance(stats);

        expect(dodgeChance).toBe(0);
      });

      it('should floor the result', () => {
        const stats = { agility: 99 };

        const dodgeChance = CombatFormulas.calculateDodgeChance(stats);

        expect(dodgeChance).toBe(24); // floor(99 / 4)
      });

      it('should handle missing stats', () => {
        const stats = {};

        const dodgeChance = CombatFormulas.calculateDodgeChance(stats);

        expect(dodgeChance).toBe(0);
      });
    });

    describe('calculateHeal()', () => {
      it('should calculate heal with intelligence bonus', () => {
        const casterStats = { intelligence: 100 };

        const heal = CombatFormulas.calculateHeal(50, casterStats);

        expect(heal).toBe(100); // 50 + (100 * 0.5)
      });

      it('should use base heal with no intelligence', () => {
        const casterStats = { intelligence: 0 };

        const heal = CombatFormulas.calculateHeal(50, casterStats);

        expect(heal).toBe(50);
      });

      it('should handle missing intelligence stat', () => {
        const casterStats = {};

        const heal = CombatFormulas.calculateHeal(50, casterStats);

        expect(heal).toBe(50);
      });

      it('should floor heal values', () => {
        const casterStats = { intelligence: 55 };

        const heal = CombatFormulas.calculateHeal(50, casterStats);

        expect(heal).toBe(77); // floor(50 + 27.5)
        expect(Number.isInteger(heal)).toBe(true);
      });

      it('should handle large intelligence values', () => {
        const casterStats = { intelligence: 500 };

        const heal = CombatFormulas.calculateHeal(100, casterStats);

        expect(heal).toBe(350); // 100 + (500 * 0.5)
      });
    });
  });

  describe('MovementFormulas', () => {
    describe('calculateMovementPoints()', () => {
      it('should return base MP with no agility', () => {
        const stats = { agility: 0 };

        const mp = MovementFormulas.calculateMovementPoints(stats);

        expect(mp).toBe(3);
      });

      it('should add bonus MP from agility', () => {
        const stats = { agility: 100 };

        const mp = MovementFormulas.calculateMovementPoints(stats);

        expect(mp).toBe(4); // 3 + floor(100 / 100)
      });

      it('should floor agility bonus', () => {
        const stats = { agility: 99 };

        const mp = MovementFormulas.calculateMovementPoints(stats);

        expect(mp).toBe(3); // 3 + floor(99 / 100)
      });

      it('should handle high agility', () => {
        const stats = { agility: 500 };

        const mp = MovementFormulas.calculateMovementPoints(stats);

        expect(mp).toBe(8); // 3 + floor(500 / 100)
      });

      it('should handle missing agility stat', () => {
        const stats = {};

        const mp = MovementFormulas.calculateMovementPoints(stats);

        expect(mp).toBe(3);
      });
    });

    describe('calculateDistance()', () => {
      it('should calculate Manhattan distance between cells', () => {
        const distance = MovementFormulas.calculateDistance(0, 15); // 0,0 to 1,1 (14 width)

        expect(distance).toBe(2); // |1-0| + |1-0|
      });

      it('should return 0 for same cell', () => {
        const distance = MovementFormulas.calculateDistance(100, 100);

        expect(distance).toBe(0);
      });

      it('should calculate horizontal distance', () => {
        const distance = MovementFormulas.calculateDistance(0, 5); // Same row

        expect(distance).toBe(5);
      });

      it('should calculate vertical distance', () => {
        const distance = MovementFormulas.calculateDistance(0, 56); // 4 rows down (14 * 4)

        expect(distance).toBe(4); // |0-0| + |4-0|
      });

      it('should work with diagonal movement', () => {
        const distance = MovementFormulas.calculateDistance(0, 29); // 15 = 14 + 1

        expect(distance).toBe(3); // |1-0| + |2-0|
      });

      it('should be symmetric', () => {
        const dist1 = MovementFormulas.calculateDistance(10, 50);
        const dist2 = MovementFormulas.calculateDistance(50, 10);

        expect(dist1).toBe(dist2);
      });
    });

    describe('hasLineOfSight()', () => {
      it('should return true for adjacent cells with no obstacles', () => {
        const obstacles = new Set<number>();

        const result = MovementFormulas.hasLineOfSight(0, 1, obstacles);

        expect(result).toBe(true);
      });

      it('should return true for same cell', () => {
        const obstacles = new Set<number>();

        const result = MovementFormulas.hasLineOfSight(5, 5, obstacles);

        expect(result).toBe(true);
      });

      it('should return false if obstacle in path', () => {
        const obstacles = new Set([15]); // Obstacle at cell 15

        const result = MovementFormulas.hasLineOfSight(0, 30, obstacles);

        expect(result).toBe(false);
      });

      it('should return true for clear line of sight', () => {
        const obstacles = new Set([10, 20]); // Obstacles not in path

        const result = MovementFormulas.hasLineOfSight(0, 5, obstacles);

        expect(result).toBe(true);
      });

      it('should handle horizontal line of sight', () => {
        const obstacles = new Set<number>();

        const result = MovementFormulas.hasLineOfSight(0, 10, obstacles);

        expect(result).toBe(true);
      });

      it('should handle vertical line of sight', () => {
        const obstacles = new Set<number>();

        const result = MovementFormulas.hasLineOfSight(0, 140, obstacles); // 10 rows down

        expect(result).toBe(true);
      });

      it('should detect obstacle blocking vertical path', () => {
        const obstacles = new Set([28]); // 2 rows down

        const result = MovementFormulas.hasLineOfSight(0, 56, obstacles); // 4 rows down

        expect(result).toBe(false);
      });

      it('should handle empty obstacle set', () => {
        const obstacles = new Set<number>();

        const result = MovementFormulas.hasLineOfSight(0, 100, obstacles);

        expect(result).toBe(true);
      });
    });
  });

  describe('ItemFormulas', () => {
    describe('calculateItemPower()', () => {
      it('should calculate power from item level', () => {
        const item = { level: 50 };

        const power = ItemFormulas.calculateItemPower(item);

        expect(power).toBe(50);
      });

      it('should add stat bonuses to power', () => {
        const item = {
          level: 50,
          stats: { strength: 20, agility: 30, intelligence: 10 },
        };

        const power = ItemFormulas.calculateItemPower(item);

        expect(power).toBe(110); // 50 + 20 + 30 + 10
      });

      it('should handle negative stats', () => {
        const item = {
          level: 50,
          stats: { strength: -10, agility: 20 },
        };

        const power = ItemFormulas.calculateItemPower(item);

        expect(power).toBe(80); // 50 + |-10| + 20
      });

      it('should return 0 for item with no level', () => {
        const item = {};

        const power = ItemFormulas.calculateItemPower(item);

        expect(power).toBe(0);
      });

      it('should handle item with only stats', () => {
        const item = {
          stats: { vitality: 50, wisdom: 25 },
        };

        const power = ItemFormulas.calculateItemPower(item);

        expect(power).toBe(75);
      });

      it('should handle empty stats object', () => {
        const item = { level: 25, stats: {} };

        const power = ItemFormulas.calculateItemPower(item);

        expect(power).toBe(25);
      });
    });

    describe('calculateItemPrice()', () => {
      it('should calculate price with full condition', () => {
        const item = { price: 1000, level: 50, stats: {} };

        const price = ItemFormulas.calculateItemPrice(item, 100);

        expect(price).toBe(1500); // 1000 * 1.0 * 1.5
      });

      it('should reduce price for poor condition', () => {
        const item = { price: 1000, level: 50, stats: {} };

        const price = ItemFormulas.calculateItemPrice(item, 50);

        expect(price).toBe(750); // 1000 * 0.5 * 1.5
      });

      it('should use default condition of 100', () => {
        const item = { price: 1000, level: 50, stats: {} };

        const price = ItemFormulas.calculateItemPrice(item);

        expect(price).toBe(1500); // Same as 100 condition
      });

      it('should increase price for powerful items', () => {
        const item = {
          price: 1000,
          level: 100,
          stats: { strength: 50, agility: 50 },
        };

        const price = ItemFormulas.calculateItemPrice(item, 100);

        expect(price).toBe(3000); // 1000 * 1.0 * 3.0 (1 + 200/100)
      });

      it('should floor price values', () => {
        const item = { price: 999, level: 25, stats: {} };

        const price = ItemFormulas.calculateItemPrice(item, 50);

        expect(Number.isInteger(price)).toBe(true);
        expect(price).toBe(624); // floor(999 * 0.5 * 1.25)
      });

      it('should return 0 for item with no price', () => {
        const item = { level: 50, stats: {} };

        const price = ItemFormulas.calculateItemPrice(item);

        expect(price).toBe(0);
      });

      it('should handle zero condition', () => {
        const item = { price: 1000, level: 50, stats: {} };

        const price = ItemFormulas.calculateItemPrice(item, 0);

        expect(price).toBe(0);
      });
    });
  });

  describe('Default Export', () => {
    it('should export all formulas and constants', () => {
      const defaultExport = require('../game.config').default;

      expect(defaultExport).toHaveProperty('CONSTANTS');
      expect(defaultExport).toHaveProperty('Experience');
      expect(defaultExport).toHaveProperty('Combat');
      expect(defaultExport).toHaveProperty('Movement');
      expect(defaultExport).toHaveProperty('Items');
    });

    it('should have all formula methods in default export', () => {
      const defaultExport = require('../game.config').default;

      expect(typeof defaultExport.Experience.getExperienceForLevel).toBe('function');
      expect(typeof defaultExport.Combat.calculateDamage).toBe('function');
      expect(typeof defaultExport.Movement.calculateDistance).toBe('function');
      expect(typeof defaultExport.Items.calculateItemPower).toBe('function');
    });
  });

  describe('Integration Tests', () => {
    it('should calculate complete player progression', () => {
      const level = 50;
      const xp = ExperienceFormulas.getExperienceForLevel(level);
      const calculatedLevel = ExperienceFormulas.getLevelFromExperience(xp);

      expect(calculatedLevel).toBe(level);
    });

    it('should calculate complete combat scenario', () => {
      const attackerStats = { strength: 100, agility: 80, intelligence: 60 };
      const defenderStats = { earthResistance: 20, agility: 50 };

      const initiative = CombatFormulas.calculateInitiative(attackerStats);
      const damage = CombatFormulas.calculateDamage(100, attackerStats, defenderStats, 'earth');
      const critChance = CombatFormulas.calculateCriticalChance(attackerStats);
      const dodgeChance = CombatFormulas.calculateDodgeChance(defenderStats);

      expect(initiative).toBeGreaterThan(0);
      expect(damage).toBeGreaterThan(0);
      expect(critChance).toBeGreaterThan(0);
      expect(dodgeChance).toBeGreaterThan(0);
    });

    it('should calculate complete movement scenario', () => {
      const stats = { agility: 150 };
      const obstacles = new Set([15, 30]);

      const mp = MovementFormulas.calculateMovementPoints(stats);
      const distance = MovementFormulas.calculateDistance(0, 50);
      const los = MovementFormulas.hasLineOfSight(0, 50, obstacles);

      expect(mp).toBeGreaterThan(0);
      expect(distance).toBeGreaterThan(0);
      expect(typeof los).toBe('boolean');
    });

    it('should calculate complete item valuation', () => {
      const item = {
        level: 75,
        price: 5000,
        stats: { strength: 50, vitality: 30, wisdom: 20 },
      };

      const power = ItemFormulas.calculateItemPower(item);
      const fullPrice = ItemFormulas.calculateItemPrice(item, 100);
      const halfPrice = ItemFormulas.calculateItemPrice(item, 50);

      expect(power).toBe(175); // 75 + 50 + 30 + 20
      expect(fullPrice).toBeGreaterThan(halfPrice);
    });
  });

  describe('Edge Cases', () => {
    it('should handle extremely high levels', () => {
      const xp = ExperienceFormulas.getExperienceForLevel(1000);

      expect(xp).toBe(100000000);
      expect(Number.isFinite(xp)).toBe(true);
    });

    it('should handle negative level input', () => {
      const xp = ExperienceFormulas.getExperienceForLevel(-5);

      expect(xp).toBe(0);
    });

    it('should handle fractional stats', () => {
      const stats = { agility: 99.9 };
      const mp = MovementFormulas.calculateMovementPoints(stats);

      expect(Number.isInteger(mp)).toBe(true);
    });

    it('should handle all zero stats', () => {
      const stats = { strength: 0, agility: 0, intelligence: 0, chance: 0 };

      const initiative = CombatFormulas.calculateInitiative(stats);
      const critChance = CombatFormulas.calculateCriticalChance(stats);

      expect(initiative).toBe(100); // Base initiative
      expect(critChance).toBe(5); // Base crit chance
    });

    it('should handle very large obstacle sets', () => {
      const largeObstacles = new Set<number>();
      for (let i = 100; i < 500; i++) {
        largeObstacles.add(i);
      }

      const los = MovementFormulas.hasLineOfSight(0, 10, largeObstacles);

      expect(typeof los).toBe('boolean');
    });

    it('should handle items with no properties', () => {
      const emptyItem = {};

      const power = ItemFormulas.calculateItemPower(emptyItem);
      const price = ItemFormulas.calculateItemPrice(emptyItem);

      expect(power).toBe(0);
      expect(price).toBe(0);
    });
  });
});
