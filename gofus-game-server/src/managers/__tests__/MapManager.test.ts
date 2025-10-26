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

describe('MapManager', () => {
  let mapManager: MapManager;

  beforeEach(() => {
    jest.clearAllMocks();
    mapManager = new MapManager();
  });

  afterEach(async () => {
    if (mapManager) {
      await mapManager.cleanup();
    }
  });

  describe('Initialization', () => {
    it('should create an instance of MapManager', () => {
      expect(mapManager).toBeInstanceOf(MapManager);
    });

    it('should initialize with an empty map instances collection', () => {
      expect(mapManager.getActiveInstanceCount()).toBe(0);
    });

    it('should have an initialize method', () => {
      expect(mapManager.initialize).toBeDefined();
      expect(typeof mapManager.initialize).toBe('function');
    });

    it('should call initialize without errors', async () => {
      await expect(mapManager.initialize()).resolves.not.toThrow();
    });

    it('should log initialization message', async () => {
      await mapManager.initialize();
      expect(log.info).toHaveBeenCalledWith('MapManager initialized');
    });

    it('should return a Promise from initialize', () => {
      const result = mapManager.initialize();
      expect(result).toBeInstanceOf(Promise);
    });
  });

  describe('Core Methods', () => {
    beforeEach(async () => {
      await mapManager.initialize();
    });

    describe('updateAll', () => {
      it('should have an updateAll method', () => {
        expect(mapManager.updateAll).toBeDefined();
        expect(typeof mapManager.updateAll).toBe('function');
      });

      it('should call updateAll without errors', () => {
        expect(() => mapManager.updateAll()).not.toThrow();
      });

      it('should return undefined', () => {
        const result = mapManager.updateAll();
        expect(result).toBeUndefined();
      });

      it('should handle multiple consecutive calls', () => {
        expect(() => {
          mapManager.updateAll();
          mapManager.updateAll();
          mapManager.updateAll();
        }).not.toThrow();
      });
    });

    describe('saveAll', () => {
      it('should have a saveAll method', () => {
        expect(mapManager.saveAll).toBeDefined();
        expect(typeof mapManager.saveAll).toBe('function');
      });

      it('should call saveAll without errors', async () => {
        await expect(mapManager.saveAll()).resolves.not.toThrow();
      });

      it('should return a Promise', () => {
        const result = mapManager.saveAll();
        expect(result).toBeInstanceOf(Promise);
      });

      it('should handle multiple consecutive save calls', async () => {
        await expect(
          Promise.all([
            mapManager.saveAll(),
            mapManager.saveAll(),
            mapManager.saveAll(),
          ])
        ).resolves.not.toThrow();
      });
    });

    describe('getActiveInstanceCount', () => {
      it('should have a getActiveInstanceCount method', () => {
        expect(mapManager.getActiveInstanceCount).toBeDefined();
        expect(typeof mapManager.getActiveInstanceCount).toBe('function');
      });

      it('should return a number', () => {
        const count = mapManager.getActiveInstanceCount();
        expect(typeof count).toBe('number');
      });

      it('should return 0 initially', () => {
        expect(mapManager.getActiveInstanceCount()).toBe(0);
      });

      it('should return a non-negative number', () => {
        const count = mapManager.getActiveInstanceCount();
        expect(count).toBeGreaterThanOrEqual(0);
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle updateAll being called before initialization', () => {
      const uninitializedManager = new MapManager();
      expect(() => uninitializedManager.updateAll()).not.toThrow();
    });

    it('should handle saveAll being called before initialization', async () => {
      const uninitializedManager = new MapManager();
      await expect(uninitializedManager.saveAll()).resolves.not.toThrow();
    });

    it('should handle getActiveInstanceCount being called before initialization', () => {
      const uninitializedManager = new MapManager();
      expect(() => uninitializedManager.getActiveInstanceCount()).not.toThrow();
      expect(uninitializedManager.getActiveInstanceCount()).toBe(0);
    });

    it('should handle cleanup being called before initialization', async () => {
      const uninitializedManager = new MapManager();
      await expect(uninitializedManager.cleanup()).resolves.not.toThrow();
    });

    it('should handle multiple initialize calls', async () => {
      await mapManager.initialize();
      await expect(mapManager.initialize()).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledTimes(2);
    });
  });

  describe('Cleanup Operations', () => {
    it('should have a cleanup method', () => {
      expect(mapManager.cleanup).toBeDefined();
      expect(typeof mapManager.cleanup).toBe('function');
    });

    it('should call cleanup without errors', async () => {
      await expect(mapManager.cleanup()).resolves.not.toThrow();
    });

    it('should log cleanup message', async () => {
      await mapManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('MapManager cleaned up');
    });

    it('should return a Promise from cleanup', () => {
      const result = mapManager.cleanup();
      expect(result).toBeInstanceOf(Promise);
    });

    it('should handle cleanup being called multiple times', async () => {
      await mapManager.cleanup();
      await expect(mapManager.cleanup()).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledWith('MapManager cleaned up');
    });

    it('should still allow operations after cleanup', async () => {
      await mapManager.cleanup();
      expect(() => mapManager.updateAll()).not.toThrow();
      expect(mapManager.getActiveInstanceCount()).toBe(0);
    });

    it('should handle cleanup after saveAll', async () => {
      await mapManager.saveAll();
      await expect(mapManager.cleanup()).resolves.not.toThrow();
    });
  });

  describe('Integration Scenarios', () => {
    it('should handle full lifecycle: initialize -> update -> save -> cleanup', async () => {
      await expect(mapManager.initialize()).resolves.not.toThrow();
      expect(() => mapManager.updateAll()).not.toThrow();
      await expect(mapManager.saveAll()).resolves.not.toThrow();
      await expect(mapManager.cleanup()).resolves.not.toThrow();
    });

    it('should maintain state consistency across operations', async () => {
      await mapManager.initialize();
      const countBefore = mapManager.getActiveInstanceCount();
      mapManager.updateAll();
      await mapManager.saveAll();
      const countAfter = mapManager.getActiveInstanceCount();
      expect(countBefore).toBe(countAfter);
    });

    it('should handle rapid consecutive updates', () => {
      const iterations = 100;
      expect(() => {
        for (let i = 0; i < iterations; i++) {
          mapManager.updateAll();
        }
      }).not.toThrow();
    });

    it('should handle concurrent save operations', async () => {
      const promises = Array.from({ length: 10 }, () => mapManager.saveAll());
      await expect(Promise.all(promises)).resolves.not.toThrow();
    });
  });

  describe('Type Safety', () => {
    it('should return correct types for all methods', async () => {
      const initResult = mapManager.initialize();
      expect(initResult).toBeInstanceOf(Promise);
      await initResult;

      const updateResult = mapManager.updateAll();
      expect(updateResult).toBeUndefined();

      const saveResult = mapManager.saveAll();
      expect(saveResult).toBeInstanceOf(Promise);
      await saveResult;

      const countResult = mapManager.getActiveInstanceCount();
      expect(typeof countResult).toBe('number');

      const cleanupResult = mapManager.cleanup();
      expect(cleanupResult).toBeInstanceOf(Promise);
      await cleanupResult;
    });
  });
});
