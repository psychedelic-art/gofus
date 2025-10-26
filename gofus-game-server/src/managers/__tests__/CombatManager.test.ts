import { CombatManager } from '../CombatManager';
import { MapManager } from '../MapManager';
import { log } from '@/utils/Logger';

// Mock the Logger module
jest.mock('@/utils/Logger', () => ({
  log: {
    info: jest.fn(),
    error: jest.fn(),
    warn: jest.fn(),
    debug: jest.fn(),
  },
}));

describe('CombatManager', () => {
  let combatManager: CombatManager;
  let mockMapManager: MapManager;

  beforeEach(() => {
    jest.clearAllMocks();
    combatManager = new CombatManager();
    mockMapManager = new MapManager();
  });

  afterEach(async () => {
    if (combatManager) {
      await combatManager.cleanup();
    }
  });

  describe('Initialization', () => {
    it('should create an instance of CombatManager', () => {
      expect(combatManager).toBeInstanceOf(CombatManager);
    });

    it('should initialize with an empty battles collection', () => {
      expect(combatManager.getActiveBattleCount()).toBe(0);
    });

    it('should have an initialize method', () => {
      expect(combatManager.initialize).toBeDefined();
      expect(typeof combatManager.initialize).toBe('function');
    });

    it('should call initialize without errors with MapManager', async () => {
      await expect(combatManager.initialize(mockMapManager)).resolves.not.toThrow();
    });

    it('should log initialization message', async () => {
      await combatManager.initialize(mockMapManager);
      expect(log.info).toHaveBeenCalledWith('CombatManager initialized');
    });

    it('should return a Promise from initialize', () => {
      const result = combatManager.initialize(mockMapManager);
      expect(result).toBeInstanceOf(Promise);
    });

    it('should accept MapManager as a parameter', async () => {
      await expect(
        combatManager.initialize(mockMapManager)
      ).resolves.not.toThrow();
    });
  });

  describe('Core Methods', () => {
    beforeEach(async () => {
      await combatManager.initialize(mockMapManager);
    });

    describe('updateBattles', () => {
      it('should have an updateBattles method', () => {
        expect(combatManager.updateBattles).toBeDefined();
        expect(typeof combatManager.updateBattles).toBe('function');
      });

      it('should call updateBattles without errors', () => {
        expect(() => combatManager.updateBattles()).not.toThrow();
      });

      it('should return undefined', () => {
        const result = combatManager.updateBattles();
        expect(result).toBeUndefined();
      });

      it('should handle multiple consecutive calls', () => {
        expect(() => {
          combatManager.updateBattles();
          combatManager.updateBattles();
          combatManager.updateBattles();
        }).not.toThrow();
      });

      it('should handle rapid consecutive updates', () => {
        const iterations = 100;
        expect(() => {
          for (let i = 0; i < iterations; i++) {
            combatManager.updateBattles();
          }
        }).not.toThrow();
      });
    });

    describe('saveAll', () => {
      it('should have a saveAll method', () => {
        expect(combatManager.saveAll).toBeDefined();
        expect(typeof combatManager.saveAll).toBe('function');
      });

      it('should call saveAll without errors', async () => {
        await expect(combatManager.saveAll()).resolves.not.toThrow();
      });

      it('should return a Promise', () => {
        const result = combatManager.saveAll();
        expect(result).toBeInstanceOf(Promise);
      });

      it('should handle multiple consecutive save calls', async () => {
        await expect(
          Promise.all([
            combatManager.saveAll(),
            combatManager.saveAll(),
            combatManager.saveAll(),
          ])
        ).resolves.not.toThrow();
      });

      it('should handle concurrent save operations', async () => {
        const promises = Array.from({ length: 10 }, () => combatManager.saveAll());
        await expect(Promise.all(promises)).resolves.not.toThrow();
      });
    });

    describe('getActiveBattleCount', () => {
      it('should have a getActiveBattleCount method', () => {
        expect(combatManager.getActiveBattleCount).toBeDefined();
        expect(typeof combatManager.getActiveBattleCount).toBe('function');
      });

      it('should return a number', () => {
        const count = combatManager.getActiveBattleCount();
        expect(typeof count).toBe('number');
      });

      it('should return 0 initially', () => {
        expect(combatManager.getActiveBattleCount()).toBe(0);
      });

      it('should return a non-negative number', () => {
        const count = combatManager.getActiveBattleCount();
        expect(count).toBeGreaterThanOrEqual(0);
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle updateBattles being called before initialization', () => {
      const uninitializedManager = new CombatManager();
      expect(() => uninitializedManager.updateBattles()).not.toThrow();
    });

    it('should handle saveAll being called before initialization', async () => {
      const uninitializedManager = new CombatManager();
      await expect(uninitializedManager.saveAll()).resolves.not.toThrow();
    });

    it('should handle getActiveBattleCount being called before initialization', () => {
      const uninitializedManager = new CombatManager();
      expect(() => uninitializedManager.getActiveBattleCount()).not.toThrow();
      expect(uninitializedManager.getActiveBattleCount()).toBe(0);
    });

    it('should handle cleanup being called before initialization', async () => {
      const uninitializedManager = new CombatManager();
      await expect(uninitializedManager.cleanup()).resolves.not.toThrow();
    });

    it('should handle multiple initialize calls', async () => {
      await combatManager.initialize(mockMapManager);
      await expect(combatManager.initialize(mockMapManager)).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledTimes(2);
    });

    it('should handle null MapManager gracefully', async () => {
      await expect(combatManager.initialize(null as any)).resolves.not.toThrow();
    });

    it('should handle undefined MapManager gracefully', async () => {
      await expect(combatManager.initialize(undefined as any)).resolves.not.toThrow();
    });
  });

  describe('Cleanup Operations', () => {
    it('should have a cleanup method', () => {
      expect(combatManager.cleanup).toBeDefined();
      expect(typeof combatManager.cleanup).toBe('function');
    });

    it('should call cleanup without errors', async () => {
      await expect(combatManager.cleanup()).resolves.not.toThrow();
    });

    it('should log cleanup message', async () => {
      await combatManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('CombatManager cleaned up');
    });

    it('should return a Promise from cleanup', () => {
      const result = combatManager.cleanup();
      expect(result).toBeInstanceOf(Promise);
    });

    it('should handle cleanup being called multiple times', async () => {
      await combatManager.cleanup();
      await expect(combatManager.cleanup()).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledWith('CombatManager cleaned up');
    });

    it('should still allow operations after cleanup', async () => {
      await combatManager.cleanup();
      expect(() => combatManager.updateBattles()).not.toThrow();
      expect(combatManager.getActiveBattleCount()).toBe(0);
    });

    it('should handle cleanup after saveAll', async () => {
      await combatManager.saveAll();
      await expect(combatManager.cleanup()).resolves.not.toThrow();
    });

    it('should clear state on cleanup', async () => {
      await combatManager.initialize(mockMapManager);
      await combatManager.cleanup();
      expect(combatManager.getActiveBattleCount()).toBe(0);
    });
  });

  describe('Integration Scenarios', () => {
    it('should handle full lifecycle: initialize -> update -> save -> cleanup', async () => {
      await expect(combatManager.initialize(mockMapManager)).resolves.not.toThrow();
      expect(() => combatManager.updateBattles()).not.toThrow();
      await expect(combatManager.saveAll()).resolves.not.toThrow();
      await expect(combatManager.cleanup()).resolves.not.toThrow();
    });

    it('should maintain state consistency across operations', async () => {
      await combatManager.initialize(mockMapManager);
      const countBefore = combatManager.getActiveBattleCount();
      combatManager.updateBattles();
      await combatManager.saveAll();
      const countAfter = combatManager.getActiveBattleCount();
      expect(countBefore).toBe(countAfter);
    });

    it('should work with initialized MapManager', async () => {
      await mockMapManager.initialize();
      await expect(combatManager.initialize(mockMapManager)).resolves.not.toThrow();
      expect(() => combatManager.updateBattles()).not.toThrow();
    });

    it('should handle interleaved update and save operations', async () => {
      await combatManager.initialize(mockMapManager);
      combatManager.updateBattles();
      await combatManager.saveAll();
      combatManager.updateBattles();
      await combatManager.saveAll();
      expect(combatManager.getActiveBattleCount()).toBe(0);
    });

    it('should handle re-initialization with different MapManager', async () => {
      await combatManager.initialize(mockMapManager);
      const newMapManager = new MapManager();
      await expect(combatManager.initialize(newMapManager)).resolves.not.toThrow();
    });
  });

  describe('Type Safety', () => {
    it('should return correct types for all methods', async () => {
      const initResult = combatManager.initialize(mockMapManager);
      expect(initResult).toBeInstanceOf(Promise);
      await initResult;

      const updateResult = combatManager.updateBattles();
      expect(updateResult).toBeUndefined();

      const saveResult = combatManager.saveAll();
      expect(saveResult).toBeInstanceOf(Promise);
      await saveResult;

      const countResult = combatManager.getActiveBattleCount();
      expect(typeof countResult).toBe('number');

      const cleanupResult = combatManager.cleanup();
      expect(cleanupResult).toBeInstanceOf(Promise);
      await cleanupResult;
    });
  });

  describe('Dependency Management', () => {
    it('should accept MapManager dependency', async () => {
      await expect(combatManager.initialize(mockMapManager)).resolves.not.toThrow();
    });

    it('should work with fresh MapManager instance', async () => {
      const freshMapManager = new MapManager();
      await expect(combatManager.initialize(freshMapManager)).resolves.not.toThrow();
    });

    it('should handle MapManager lifecycle independently', async () => {
      await mockMapManager.initialize();
      await combatManager.initialize(mockMapManager);
      await mockMapManager.cleanup();
      expect(() => combatManager.updateBattles()).not.toThrow();
    });
  });
});
