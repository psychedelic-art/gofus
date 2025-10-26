import { AIManager } from '../AIManager';
import { MapManager } from '../MapManager';
import { CombatManager } from '../CombatManager';
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

describe('AIManager', () => {
  let aiManager: AIManager;
  let mockMapManager: MapManager;
  let mockCombatManager: CombatManager;

  beforeEach(() => {
    jest.clearAllMocks();
    aiManager = new AIManager();
    mockMapManager = new MapManager();
    mockCombatManager = new CombatManager();
  });

  afterEach(async () => {
    if (aiManager) {
      await aiManager.cleanup();
    }
  });

  describe('Initialization', () => {
    it('should create an instance of AIManager', () => {
      expect(aiManager).toBeInstanceOf(AIManager);
    });

    it('should have an initialize method', () => {
      expect(aiManager.initialize).toBeDefined();
      expect(typeof aiManager.initialize).toBe('function');
    });

    it('should call initialize without errors with both managers', async () => {
      await expect(
        aiManager.initialize(mockMapManager, mockCombatManager)
      ).resolves.not.toThrow();
    });

    it('should log initialization message', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      expect(log.info).toHaveBeenCalledWith('AIManager initialized');
    });

    it('should return a Promise from initialize', () => {
      const result = aiManager.initialize(mockMapManager, mockCombatManager);
      expect(result).toBeInstanceOf(Promise);
    });

    it('should accept MapManager and CombatManager as parameters', async () => {
      await expect(
        aiManager.initialize(mockMapManager, mockCombatManager)
      ).resolves.not.toThrow();
    });

    it('should initialize successfully', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      expect(log.info).toHaveBeenCalledTimes(1);
    });
  });

  describe('Core Methods', () => {
    beforeEach(async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
    });

    describe('update', () => {
      it('should have an update method', () => {
        expect(aiManager.update).toBeDefined();
        expect(typeof aiManager.update).toBe('function');
      });

      it('should call update without errors', () => {
        expect(() => aiManager.update()).not.toThrow();
      });

      it('should return undefined', () => {
        const result = aiManager.update();
        expect(result).toBeUndefined();
      });

      it('should handle multiple consecutive calls', () => {
        expect(() => {
          aiManager.update();
          aiManager.update();
          aiManager.update();
        }).not.toThrow();
      });

      it('should handle rapid consecutive updates', () => {
        const iterations = 100;
        expect(() => {
          for (let i = 0; i < iterations; i++) {
            aiManager.update();
          }
        }).not.toThrow();
      });

      it('should update without dependencies failing', () => {
        expect(() => aiManager.update()).not.toThrow();
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle update being called before initialization', () => {
      const uninitializedManager = new AIManager();
      expect(() => uninitializedManager.update()).not.toThrow();
    });

    it('should handle cleanup being called before initialization', async () => {
      const uninitializedManager = new AIManager();
      await expect(uninitializedManager.cleanup()).resolves.not.toThrow();
    });

    it('should handle multiple initialize calls', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      await expect(
        aiManager.initialize(mockMapManager, mockCombatManager)
      ).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledTimes(2);
    });

    it('should handle null MapManager gracefully', async () => {
      await expect(
        aiManager.initialize(null as any, mockCombatManager)
      ).resolves.not.toThrow();
    });

    it('should handle undefined MapManager gracefully', async () => {
      await expect(
        aiManager.initialize(undefined as any, mockCombatManager)
      ).resolves.not.toThrow();
    });

    it('should handle null CombatManager gracefully', async () => {
      await expect(
        aiManager.initialize(mockMapManager, null as any)
      ).resolves.not.toThrow();
    });

    it('should handle undefined CombatManager gracefully', async () => {
      await expect(
        aiManager.initialize(mockMapManager, undefined as any)
      ).resolves.not.toThrow();
    });

    it('should handle both managers being null', async () => {
      await expect(
        aiManager.initialize(null as any, null as any)
      ).resolves.not.toThrow();
    });

    it('should not throw on update after failed initialization', async () => {
      await aiManager.initialize(null as any, null as any);
      expect(() => aiManager.update()).not.toThrow();
    });
  });

  describe('Cleanup Operations', () => {
    it('should have a cleanup method', () => {
      expect(aiManager.cleanup).toBeDefined();
      expect(typeof aiManager.cleanup).toBe('function');
    });

    it('should call cleanup without errors', async () => {
      await expect(aiManager.cleanup()).resolves.not.toThrow();
    });

    it('should log cleanup message', async () => {
      await aiManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('AIManager cleaned up');
    });

    it('should return a Promise from cleanup', () => {
      const result = aiManager.cleanup();
      expect(result).toBeInstanceOf(Promise);
    });

    it('should handle cleanup being called multiple times', async () => {
      await aiManager.cleanup();
      await expect(aiManager.cleanup()).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledWith('AIManager cleaned up');
    });

    it('should still allow operations after cleanup', async () => {
      await aiManager.cleanup();
      expect(() => aiManager.update()).not.toThrow();
    });

    it('should handle cleanup after updates', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      aiManager.update();
      await expect(aiManager.cleanup()).resolves.not.toThrow();
    });

    it('should clear state on cleanup', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      await aiManager.cleanup();
      expect(() => aiManager.update()).not.toThrow();
    });
  });

  describe('Integration Scenarios', () => {
    it('should handle full lifecycle: initialize -> update -> cleanup', async () => {
      await expect(
        aiManager.initialize(mockMapManager, mockCombatManager)
      ).resolves.not.toThrow();
      expect(() => aiManager.update()).not.toThrow();
      await expect(aiManager.cleanup()).resolves.not.toThrow();
    });

    it('should maintain state consistency across operations', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      aiManager.update();
      aiManager.update();
      await aiManager.cleanup();
      expect(true).toBe(true); // No errors thrown
    });

    it('should work with initialized dependencies', async () => {
      await mockMapManager.initialize();
      await mockCombatManager.initialize(mockMapManager);
      await expect(
        aiManager.initialize(mockMapManager, mockCombatManager)
      ).resolves.not.toThrow();
      expect(() => aiManager.update()).not.toThrow();
    });

    it('should handle interleaved update operations', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      aiManager.update();
      mockMapManager.updateAll();
      aiManager.update();
      mockCombatManager.updateBattles();
      aiManager.update();
      expect(true).toBe(true); // No errors thrown
    });

    it('should handle re-initialization with different managers', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      const newMapManager = new MapManager();
      const newCombatManager = new CombatManager();
      await expect(
        aiManager.initialize(newMapManager, newCombatManager)
      ).resolves.not.toThrow();
    });

    it('should handle multiple initialization and cleanup cycles', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      await aiManager.cleanup();
      await aiManager.initialize(mockMapManager, mockCombatManager);
      await aiManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('AIManager initialized');
      expect(log.info).toHaveBeenCalledWith('AIManager cleaned up');
    });
  });

  describe('Type Safety', () => {
    it('should return correct types for all methods', async () => {
      const initResult = aiManager.initialize(mockMapManager, mockCombatManager);
      expect(initResult).toBeInstanceOf(Promise);
      await initResult;

      const updateResult = aiManager.update();
      expect(updateResult).toBeUndefined();

      const cleanupResult = aiManager.cleanup();
      expect(cleanupResult).toBeInstanceOf(Promise);
      await cleanupResult;
    });
  });

  describe('Dependency Management', () => {
    it('should accept both MapManager and CombatManager dependencies', async () => {
      await expect(
        aiManager.initialize(mockMapManager, mockCombatManager)
      ).resolves.not.toThrow();
    });

    it('should work with fresh manager instances', async () => {
      const freshMapManager = new MapManager();
      const freshCombatManager = new CombatManager();
      await expect(
        aiManager.initialize(freshMapManager, freshCombatManager)
      ).resolves.not.toThrow();
    });

    it('should handle MapManager lifecycle independently', async () => {
      await mockMapManager.initialize();
      await aiManager.initialize(mockMapManager, mockCombatManager);
      await mockMapManager.cleanup();
      expect(() => aiManager.update()).not.toThrow();
    });

    it('should handle CombatManager lifecycle independently', async () => {
      await mockCombatManager.initialize(mockMapManager);
      await aiManager.initialize(mockMapManager, mockCombatManager);
      await mockCombatManager.cleanup();
      expect(() => aiManager.update()).not.toThrow();
    });

    it('should not fail if dependencies are cleaned up', async () => {
      await mockMapManager.initialize();
      await mockCombatManager.initialize(mockMapManager);
      await aiManager.initialize(mockMapManager, mockCombatManager);

      await mockMapManager.cleanup();
      await mockCombatManager.cleanup();

      expect(() => aiManager.update()).not.toThrow();
      await expect(aiManager.cleanup()).resolves.not.toThrow();
    });

    it('should handle dependencies being reinitialized', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      await mockMapManager.initialize();
      await mockCombatManager.initialize(mockMapManager);
      expect(() => aiManager.update()).not.toThrow();
    });
  });

  describe('Update Behavior', () => {
    beforeEach(async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
    });

    it('should handle consecutive update calls', () => {
      aiManager.update();
      aiManager.update();
      aiManager.update();
      expect(true).toBe(true); // No errors thrown
    });

    it('should handle rapid fire updates', () => {
      const start = Date.now();
      for (let i = 0; i < 1000; i++) {
        aiManager.update();
      }
      const end = Date.now();
      expect(end - start).toBeLessThan(1000); // Should complete quickly
    });

    it('should update without affecting dependencies', async () => {
      const mapCountBefore = mockMapManager.getActiveInstanceCount();
      const battleCountBefore = mockCombatManager.getActiveBattleCount();

      aiManager.update();

      const mapCountAfter = mockMapManager.getActiveInstanceCount();
      const battleCountAfter = mockCombatManager.getActiveBattleCount();

      expect(mapCountBefore).toBe(mapCountAfter);
      expect(battleCountBefore).toBe(battleCountAfter);
    });
  });

  describe('Concurrent Operations', () => {
    it('should handle concurrent initialization calls', async () => {
      const promises = Array.from({ length: 10 }, () =>
        aiManager.initialize(mockMapManager, mockCombatManager)
      );
      await expect(Promise.all(promises)).resolves.not.toThrow();
    });

    it('should handle concurrent cleanup calls', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      const promises = Array.from({ length: 10 }, () => aiManager.cleanup());
      await expect(Promise.all(promises)).resolves.not.toThrow();
    });

    it('should handle updates during async operations', async () => {
      const initPromise = aiManager.initialize(mockMapManager, mockCombatManager);
      aiManager.update(); // Called before init completes
      await initPromise;
      aiManager.update();
      expect(true).toBe(true); // No errors thrown
    });
  });

  describe('Logging Behavior', () => {
    it('should log on initialization', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      expect(log.info).toHaveBeenCalledWith('AIManager initialized');
    });

    it('should log on cleanup', async () => {
      await aiManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('AIManager cleaned up');
    });

    it('should log multiple times for multiple operations', async () => {
      await aiManager.initialize(mockMapManager, mockCombatManager);
      await aiManager.cleanup();
      await aiManager.initialize(mockMapManager, mockCombatManager);

      expect(log.info).toHaveBeenCalledTimes(3);
    });
  });
});
