/**
 * Player Entity Tests
 * Comprehensive tests for the Player class
 */

import { Player, IPlayerData } from '../Player';

describe('Player', () => {
  const createTestPlayer = (overrides?: Partial<IPlayerData>): Player => {
    const defaultData: IPlayerData = {
      id: 'player-1',
      characterId: 'char-1',
      accountId: 'account-1',
      name: 'Test Player',
      level: 1,
      classId: 1,
      position: {
        mapId: 1,
        cellId: 100,
        direction: 1,
      },
      stats: {
        hp: 100,
        maxHp: 100,
        mp: 50,
        maxMp: 50,
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
      kamas: 1000,
      experience: 0,
      ...overrides,
    };

    return new Player(defaultData);
  };

  describe('constructor', () => {
    it('should create a player with valid data', () => {
      const player = createTestPlayer();

      expect(player.id).toBe('player-1');
      expect(player.characterId).toBe('char-1');
      expect(player.accountId).toBe('account-1');
      expect(player.name).toBe('Test Player');
      expect(player.level).toBe(1);
      expect(player.classId).toBe(1);
      expect(player.kamas).toBe(1000);
      expect(player.experience).toBe(0);
      expect(player.isInCombat).toBe(false);
      expect(player.isDead).toBe(false);
    });

    it('should set isInCombat from data', () => {
      const player = createTestPlayer({ isInCombat: true });

      expect(player.isInCombat).toBe(true);
    });

    it('should set isDead from data', () => {
      const player = createTestPlayer({ isDead: true });

      expect(player.isDead).toBe(true);
    });

    it('should copy stats object', () => {
      const stats = {
        hp: 100,
        maxHp: 100,
        mp: 50,
        maxMp: 50,
        ap: 6,
        maxAp: 6,
        movement: 3,
      };
      const player = createTestPlayer({ stats });

      // Modify original stats
      stats.hp = 999;

      // Player stats should be unchanged
      expect(player.stats.hp).toBe(100);
    });

    it('should copy characteristics object', () => {
      const characteristics = {
        vitality: 10,
        wisdom: 10,
        strength: 10,
        intelligence: 10,
        chance: 10,
        agility: 10,
      };
      const player = createTestPlayer({ characteristics });

      // Modify original
      characteristics.vitality = 999;

      // Player characteristics should be unchanged
      expect(player.characteristics.vitality).toBe(10);
    });

    it('should handle different class IDs', () => {
      const player = createTestPlayer({ classId: 5 });

      expect(player.classId).toBe(5);
    });

    it('should handle high level players', () => {
      const player = createTestPlayer({ level: 200 });

      expect(player.level).toBe(200);
    });

    it('should handle zero kamas', () => {
      const player = createTestPlayer({ kamas: 0 });

      expect(player.kamas).toBe(0);
    });
  });

  describe('takeDamage', () => {
    it('should reduce HP by damage amount', () => {
      const player = createTestPlayer();

      player.takeDamage(30);

      expect(player.stats.hp).toBe(70);
      expect(player.isDead).toBe(false);
    });

    it('should not reduce HP below 0', () => {
      const player = createTestPlayer();

      player.takeDamage(150);

      expect(player.stats.hp).toBe(0);
    });

    it('should set isDead to true when HP reaches 0', () => {
      const player = createTestPlayer();

      player.takeDamage(100);

      expect(player.stats.hp).toBe(0);
      expect(player.isDead).toBe(true);
    });

    it('should set isInCombat to false on death', () => {
      const player = createTestPlayer({ isInCombat: true });

      player.takeDamage(100);

      expect(player.isInCombat).toBe(false);
    });

    it('should not apply damage when already dead', () => {
      const player = createTestPlayer({ isDead: true, stats: { hp: 0, maxHp: 100, mp: 50, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.takeDamage(50);

      expect(player.stats.hp).toBe(0);
    });

    it('should handle exact lethal damage', () => {
      const player = createTestPlayer();

      player.takeDamage(100);

      expect(player.stats.hp).toBe(0);
      expect(player.isDead).toBe(true);
    });

    it('should handle overkill damage', () => {
      const player = createTestPlayer();

      player.takeDamage(200);

      expect(player.stats.hp).toBe(0);
      expect(player.isDead).toBe(true);
    });

    it('should handle zero damage', () => {
      const player = createTestPlayer();

      player.takeDamage(0);

      expect(player.stats.hp).toBe(100);
      expect(player.isDead).toBe(false);
    });

    it('should handle multiple damage instances', () => {
      const player = createTestPlayer();

      player.takeDamage(20);
      player.takeDamage(30);
      player.takeDamage(25);

      expect(player.stats.hp).toBe(25);
      expect(player.isDead).toBe(false);
    });
  });

  describe('heal', () => {
    it('should increase HP by heal amount', () => {
      const player = createTestPlayer({ stats: { hp: 50, maxHp: 100, mp: 50, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.heal(30);

      expect(player.stats.hp).toBe(80);
    });

    it('should not exceed maxHp', () => {
      const player = createTestPlayer({ stats: { hp: 90, maxHp: 100, mp: 50, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.heal(50);

      expect(player.stats.hp).toBe(100);
    });

    it('should not heal when dead', () => {
      const player = createTestPlayer({ isDead: true, stats: { hp: 0, maxHp: 100, mp: 50, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.heal(50);

      expect(player.stats.hp).toBe(0);
    });

    it('should handle zero healing', () => {
      const player = createTestPlayer({ stats: { hp: 50, maxHp: 100, mp: 50, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.heal(0);

      expect(player.stats.hp).toBe(50);
    });

    it('should handle healing to full HP', () => {
      const player = createTestPlayer({ stats: { hp: 1, maxHp: 100, mp: 50, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.heal(99);

      expect(player.stats.hp).toBe(100);
    });

    it('should handle multiple heal instances', () => {
      const player = createTestPlayer({ stats: { hp: 50, maxHp: 100, mp: 50, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.heal(20);
      player.heal(15);
      player.heal(10);

      expect(player.stats.hp).toBe(95);
    });
  });

  describe('addExperience', () => {
    it('should add experience', () => {
      const player = createTestPlayer();

      player.addExperience(500);

      expect(player.experience).toBe(500);
    });

    it('should return false when not leveling up', () => {
      const player = createTestPlayer();

      const leveledUp = player.addExperience(500);

      expect(leveledUp).toBe(false);
      expect(player.level).toBe(1);
    });

    it('should return true when leveling up', () => {
      const player = createTestPlayer({ experience: 1500 });

      const leveledUp = player.addExperience(500);

      expect(leveledUp).toBe(true);
      expect(player.level).toBe(2);
    });

    it('should level up at experience threshold', () => {
      const player = createTestPlayer({ experience: 1500 });

      player.addExperience(500); // Total: 2000, needed for level 2: 2000

      expect(player.level).toBe(2);
    });

    it('should handle multiple level ups', () => {
      const player = createTestPlayer();

      player.addExperience(5000); // Should level to level 5

      expect(player.level).toBeGreaterThan(1);
    });

    it('should accumulate experience across multiple additions', () => {
      const player = createTestPlayer();

      player.addExperience(500);
      player.addExperience(500);
      player.addExperience(1000);

      expect(player.experience).toBe(2000);
      expect(player.level).toBe(2);
    });

    it('should handle zero experience', () => {
      const player = createTestPlayer();

      const leveledUp = player.addExperience(0);

      expect(leveledUp).toBe(false);
      expect(player.experience).toBe(0);
    });
  });

  describe('levelUp', () => {
    it('should increase level', () => {
      const player = createTestPlayer();

      player.addExperience(2000);

      expect(player.level).toBe(2);
    });

    it('should increase maxHp by 10', () => {
      const player = createTestPlayer();
      const originalMaxHp = player.stats.maxHp;

      player.addExperience(2000);

      expect(player.stats.maxHp).toBe(originalMaxHp + 10);
    });

    it('should increase maxMp by 5', () => {
      const player = createTestPlayer();
      const originalMaxMp = player.stats.maxMp;

      player.addExperience(2000);

      expect(player.stats.maxMp).toBe(originalMaxMp + 5);
    });

    it('should restore HP to maxHp on level up', () => {
      const player = createTestPlayer({ stats: { hp: 50, maxHp: 100, mp: 50, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.addExperience(2000);

      expect(player.stats.hp).toBe(player.stats.maxHp);
    });

    it('should restore MP to maxMp on level up', () => {
      const player = createTestPlayer({ stats: { hp: 100, maxHp: 100, mp: 20, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.addExperience(2000);

      expect(player.stats.mp).toBe(player.stats.maxMp);
    });
  });

  describe('respawn', () => {
    it('should set isDead to false', () => {
      const player = createTestPlayer({ isDead: true });

      player.respawn(2, 200);

      expect(player.isDead).toBe(false);
    });

    it('should restore HP to maxHp', () => {
      const player = createTestPlayer({ isDead: true, stats: { hp: 0, maxHp: 100, mp: 50, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.respawn(2, 200);

      expect(player.stats.hp).toBe(100);
    });

    it('should restore MP to maxMp', () => {
      const player = createTestPlayer({ isDead: true, stats: { hp: 0, maxHp: 100, mp: 0, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

      player.respawn(2, 200);

      expect(player.stats.mp).toBe(50);
    });

    it('should update position to respawn location', () => {
      const player = createTestPlayer({ isDead: true });

      player.respawn(5, 500);

      expect(player.position.mapId).toBe(5);
      expect(player.position.cellId).toBe(500);
    });

    it('should handle respawning at different locations', () => {
      const player = createTestPlayer({ isDead: true });

      player.respawn(10, 1000);

      expect(player.position.mapId).toBe(10);
      expect(player.position.cellId).toBe(1000);
      expect(player.isDead).toBe(false);
    });
  });

  describe('combat state', () => {
    it('should enter combat', () => {
      const player = createTestPlayer();

      player.enterCombat();

      expect(player.isInCombat).toBe(true);
    });

    it('should leave combat', () => {
      const player = createTestPlayer({ isInCombat: true });

      player.leaveCombat();

      expect(player.isInCombat).toBe(false);
    });

    it('should handle multiple combat state changes', () => {
      const player = createTestPlayer();

      player.enterCombat();
      expect(player.isInCombat).toBe(true);

      player.leaveCombat();
      expect(player.isInCombat).toBe(false);

      player.enterCombat();
      expect(player.isInCombat).toBe(true);
    });
  });

  describe('resource management', () => {
    describe('restorePoints', () => {
      it('should restore AP to maxAp', () => {
        const player = createTestPlayer({ stats: { hp: 100, maxHp: 100, mp: 50, maxMp: 50, ap: 2, maxAp: 6, movement: 3 } });

        player.restorePoints();

        expect(player.stats.ap).toBe(6);
      });

      it('should restore MP to maxMp', () => {
        const player = createTestPlayer({ stats: { hp: 100, maxHp: 100, mp: 20, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

        player.restorePoints();

        expect(player.stats.mp).toBe(50);
      });

      it('should restore both AP and MP', () => {
        const player = createTestPlayer({ stats: { hp: 100, maxHp: 100, mp: 10, maxMp: 50, ap: 2, maxAp: 6, movement: 3 } });

        player.restorePoints();

        expect(player.stats.ap).toBe(6);
        expect(player.stats.mp).toBe(50);
      });
    });

    describe('useAp', () => {
      it('should use AP when available', () => {
        const player = createTestPlayer();

        const result = player.useAp(3);

        expect(result).toBe(true);
        expect(player.stats.ap).toBe(3);
      });

      it('should not use AP when insufficient', () => {
        const player = createTestPlayer({ stats: { hp: 100, maxHp: 100, mp: 50, maxMp: 50, ap: 2, maxAp: 6, movement: 3 } });

        const result = player.useAp(3);

        expect(result).toBe(false);
        expect(player.stats.ap).toBe(2);
      });

      it('should use all AP', () => {
        const player = createTestPlayer();

        const result = player.useAp(6);

        expect(result).toBe(true);
        expect(player.stats.ap).toBe(0);
      });

      it('should handle using 0 AP', () => {
        const player = createTestPlayer();

        const result = player.useAp(0);

        expect(result).toBe(true);
        expect(player.stats.ap).toBe(6);
      });

      it('should handle multiple AP uses', () => {
        const player = createTestPlayer();

        player.useAp(2);
        player.useAp(2);

        expect(player.stats.ap).toBe(2);
      });
    });

    describe('useMp', () => {
      it('should use MP when available', () => {
        const player = createTestPlayer();

        const result = player.useMp(20);

        expect(result).toBe(true);
        expect(player.stats.mp).toBe(30);
      });

      it('should not use MP when insufficient', () => {
        const player = createTestPlayer({ stats: { hp: 100, maxHp: 100, mp: 10, maxMp: 50, ap: 6, maxAp: 6, movement: 3 } });

        const result = player.useMp(20);

        expect(result).toBe(false);
        expect(player.stats.mp).toBe(10);
      });

      it('should use all MP', () => {
        const player = createTestPlayer();

        const result = player.useMp(50);

        expect(result).toBe(true);
        expect(player.stats.mp).toBe(0);
      });

      it('should handle using 0 MP', () => {
        const player = createTestPlayer();

        const result = player.useMp(0);

        expect(result).toBe(true);
        expect(player.stats.mp).toBe(50);
      });

      it('should handle multiple MP uses', () => {
        const player = createTestPlayer();

        player.useMp(10);
        player.useMp(15);

        expect(player.stats.mp).toBe(25);
      });
    });
  });

  describe('kamas management', () => {
    describe('modifyKamas', () => {
      it('should add kamas', () => {
        const player = createTestPlayer();

        player.modifyKamas(500);

        expect(player.kamas).toBe(1500);
      });

      it('should subtract kamas', () => {
        const player = createTestPlayer();

        player.modifyKamas(-300);

        expect(player.kamas).toBe(700);
      });

      it('should not go below 0', () => {
        const player = createTestPlayer();

        player.modifyKamas(-2000);

        expect(player.kamas).toBe(0);
      });

      it('should handle zero change', () => {
        const player = createTestPlayer();

        player.modifyKamas(0);

        expect(player.kamas).toBe(1000);
      });

      it('should handle multiple transactions', () => {
        const player = createTestPlayer();

        player.modifyKamas(500);
        player.modifyKamas(-200);
        player.modifyKamas(300);

        expect(player.kamas).toBe(1600);
      });

      it('should handle large amounts', () => {
        const player = createTestPlayer();

        player.modifyKamas(1000000);

        expect(player.kamas).toBe(1001000);
      });
    });

    describe('canAfford', () => {
      it('should return true when having enough kamas', () => {
        const player = createTestPlayer();

        expect(player.canAfford(500)).toBe(true);
      });

      it('should return true when having exact kamas', () => {
        const player = createTestPlayer();

        expect(player.canAfford(1000)).toBe(true);
      });

      it('should return false when not having enough kamas', () => {
        const player = createTestPlayer();

        expect(player.canAfford(1500)).toBe(false);
      });

      it('should return true for 0 cost', () => {
        const player = createTestPlayer();

        expect(player.canAfford(0)).toBe(true);
      });

      it('should return true even with 0 kamas for 0 cost', () => {
        const player = createTestPlayer({ kamas: 0 });

        expect(player.canAfford(0)).toBe(true);
      });

      it('should return false when having 0 kamas for any positive cost', () => {
        const player = createTestPlayer({ kamas: 0 });

        expect(player.canAfford(1)).toBe(false);
      });
    });
  });

  describe('toJSON', () => {
    it('should return complete player data', () => {
      const player = createTestPlayer();

      const json = player.toJSON();

      expect(json).toEqual({
        id: 'player-1',
        characterId: 'char-1',
        accountId: 'account-1',
        name: 'Test Player',
        level: 1,
        classId: 1,
        position: {
          mapId: 1,
          cellId: 100,
          direction: 1,
        },
        stats: {
          hp: 100,
          maxHp: 100,
          mp: 50,
          maxMp: 50,
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
        kamas: 1000,
        experience: 0,
        isInCombat: false,
        isDead: false,
      });
    });

    it('should reflect current player state', () => {
      const player = createTestPlayer();

      player.takeDamage(50);
      player.enterCombat();
      player.modifyKamas(500);

      const json = player.toJSON();

      expect(json.stats.hp).toBe(50);
      expect(json.isInCombat).toBe(true);
      expect(json.kamas).toBe(1500);
    });

    it('should include dead state', () => {
      const player = createTestPlayer();

      player.takeDamage(100);

      const json = player.toJSON();

      expect(json.isDead).toBe(true);
      expect(json.stats.hp).toBe(0);
    });

    it('should serialize after level up', () => {
      const player = createTestPlayer();

      player.addExperience(2000);

      const json = player.toJSON();

      expect(json.level).toBe(2);
      expect(json.experience).toBe(2000);
    });
  });

  describe('integration scenarios', () => {
    it('should handle complete combat scenario', () => {
      const player = createTestPlayer();

      // Enter combat
      player.enterCombat();
      expect(player.isInCombat).toBe(true);

      // Use some AP and MP
      player.useAp(3);
      player.useMp(20);
      expect(player.stats.ap).toBe(3);
      expect(player.stats.mp).toBe(30);

      // Take damage
      player.takeDamage(40);
      expect(player.stats.hp).toBe(60);

      // Restore points for new turn
      player.restorePoints();
      expect(player.stats.ap).toBe(6);
      expect(player.stats.mp).toBe(50);

      // Leave combat
      player.leaveCombat();
      expect(player.isInCombat).toBe(false);
    });

    it('should handle death and respawn scenario', () => {
      const player = createTestPlayer({ isInCombat: true });

      // Die in combat
      player.takeDamage(100);
      expect(player.isDead).toBe(true);
      expect(player.isInCombat).toBe(false);
      expect(player.stats.hp).toBe(0);

      // Try to heal while dead (should not work)
      player.heal(50);
      expect(player.stats.hp).toBe(0);

      // Respawn
      player.respawn(2, 200);
      expect(player.isDead).toBe(false);
      expect(player.stats.hp).toBe(player.stats.maxHp);
      expect(player.stats.mp).toBe(player.stats.maxMp);
      expect(player.position.mapId).toBe(2);
    });

    it('should handle progression scenario', () => {
      const player = createTestPlayer();

      // Gain experience
      player.addExperience(1000);
      expect(player.experience).toBe(1000);
      expect(player.level).toBe(1);

      // Level up
      player.addExperience(1000);
      expect(player.level).toBe(2);
      expect(player.stats.maxHp).toBe(110);

      // Earn kamas
      player.modifyKamas(5000);
      expect(player.kamas).toBe(6000);

      // Purchase something
      if (player.canAfford(2000)) {
        player.modifyKamas(-2000);
      }
      expect(player.kamas).toBe(4000);
    });

    it('should handle resource depletion scenario', () => {
      const player = createTestPlayer();

      player.enterCombat();

      // Use all AP
      player.useAp(6);
      expect(player.stats.ap).toBe(0);
      expect(player.useAp(1)).toBe(false);

      // Use all MP
      player.useMp(50);
      expect(player.stats.mp).toBe(0);
      expect(player.useMp(1)).toBe(false);

      // Restore and use again
      player.restorePoints();
      expect(player.useAp(3)).toBe(true);
      expect(player.useMp(20)).toBe(true);
    });
  });
});
