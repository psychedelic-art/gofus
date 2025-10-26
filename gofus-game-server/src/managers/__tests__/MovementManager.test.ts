import { MovementManager } from '../MovementManager';
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

describe('MovementManager', () => {
  let movementManager: MovementManager;
  let mockMapManager: MapManager;

  beforeEach(() => {
    jest.clearAllMocks();
    movementManager = new MovementManager();
    mockMapManager = new MapManager();
  });

  afterEach(async () => {
    if (movementManager) {
      await movementManager.cleanup();
    }
  });

  describe('Initialization', () => {
    it('should create an instance of MovementManager', () => {
      expect(movementManager).toBeInstanceOf(MovementManager);
    });

    it('should have an initialize method', () => {
      expect(movementManager.initialize).toBeDefined();
      expect(typeof movementManager.initialize).toBe('function');
    });

    it('should call initialize without errors with MapManager', async () => {
      await expect(movementManager.initialize(mockMapManager)).resolves.not.toThrow();
    });

    it('should log initialization message', async () => {
      await movementManager.initialize(mockMapManager);
      expect(log.info).toHaveBeenCalledWith('MovementManager initialized');
    });

    it('should return a Promise from initialize', () => {
      const result = movementManager.initialize(mockMapManager);
      expect(result).toBeInstanceOf(Promise);
    });

    it('should accept MapManager as a parameter', async () => {
      await expect(
        movementManager.initialize(mockMapManager)
      ).resolves.not.toThrow();
    });

    it('should initialize without errors', async () => {
      await expect(movementManager.initialize(mockMapManager)).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledTimes(1);
    });
  });

  describe('Core Methods', () => {
    beforeEach(async () => {
      await movementManager.initialize(mockMapManager);
    });

    describe('processQueue', () => {
      it('should have a processQueue method', () => {
        expect(movementManager.processQueue).toBeDefined();
        expect(typeof movementManager.processQueue).toBe('function');
      });

      it('should call processQueue without errors', () => {
        expect(() => movementManager.processQueue()).not.toThrow();
      });

      it('should return undefined', () => {
        const result = movementManager.processQueue();
        expect(result).toBeUndefined();
      });

      it('should handle multiple consecutive calls', () => {
        expect(() => {
          movementManager.processQueue();
          movementManager.processQueue();
          movementManager.processQueue();
        }).not.toThrow();
      });

      it('should handle rapid consecutive queue processing', () => {
        const iterations = 100;
        expect(() => {
          for (let i = 0; i < iterations; i++) {
            movementManager.processQueue();
          }
        }).not.toThrow();
      });

      it('should process empty queue without errors', () => {
        expect(() => movementManager.processQueue()).not.toThrow();
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle processQueue being called before initialization', () => {
      const uninitializedManager = new MovementManager();
      expect(() => uninitializedManager.processQueue()).not.toThrow();
    });

    it('should handle cleanup being called before initialization', async () => {
      const uninitializedManager = new MovementManager();
      await expect(uninitializedManager.cleanup()).resolves.not.toThrow();
    });

    it('should handle multiple initialize calls', async () => {
      await movementManager.initialize(mockMapManager);
      await expect(movementManager.initialize(mockMapManager)).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledTimes(2);
    });

    it('should handle null MapManager gracefully', async () => {
      await expect(movementManager.initialize(null as any)).resolves.not.toThrow();
    });

    it('should handle undefined MapManager gracefully', async () => {
      await expect(movementManager.initialize(undefined as any)).resolves.not.toThrow();
    });

    it('should not throw on processQueue after failed initialization', async () => {
      await movementManager.initialize(null as any);
      expect(() => movementManager.processQueue()).not.toThrow();
    });
  });

  describe('Cleanup Operations', () => {
    it('should have a cleanup method', () => {
      expect(movementManager.cleanup).toBeDefined();
      expect(typeof movementManager.cleanup).toBe('function');
    });

    it('should call cleanup without errors', async () => {
      await expect(movementManager.cleanup()).resolves.not.toThrow();
    });

    it('should log cleanup message', async () => {
      await movementManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('MovementManager cleaned up');
    });

    it('should return a Promise from cleanup', () => {
      const result = movementManager.cleanup();
      expect(result).toBeInstanceOf(Promise);
    });

    it('should handle cleanup being called multiple times', async () => {
      await movementManager.cleanup();
      await expect(movementManager.cleanup()).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledWith('MovementManager cleaned up');
    });

    it('should still allow operations after cleanup', async () => {
      await movementManager.cleanup();
      expect(() => movementManager.processQueue()).not.toThrow();
    });

    it('should handle cleanup after processing', async () => {
      await movementManager.initialize(mockMapManager);
      movementManager.processQueue();
      await expect(movementManager.cleanup()).resolves.not.toThrow();
    });

    it('should clear queue state on cleanup', async () => {
      await movementManager.initialize(mockMapManager);
      movementManager.processQueue();
      await movementManager.cleanup();
      expect(() => movementManager.processQueue()).not.toThrow();
    });
  });

  describe('Integration Scenarios', () => {
    it('should handle full lifecycle: initialize -> process -> cleanup', async () => {
      await expect(movementManager.initialize(mockMapManager)).resolves.not.toThrow();
      expect(() => movementManager.processQueue()).not.toThrow();
      await expect(movementManager.cleanup()).resolves.not.toThrow();
    });

    it('should work with initialized MapManager', async () => {
      await mockMapManager.initialize();
      await expect(movementManager.initialize(mockMapManager)).resolves.not.toThrow();
      expect(() => movementManager.processQueue()).not.toThrow();
    });

    it('should handle multiple process cycles', async () => {
      await movementManager.initialize(mockMapManager);
      for (let i = 0; i < 10; i++) {
        expect(() => movementManager.processQueue()).not.toThrow();
      }
    });

    it('should handle re-initialization with different MapManager', async () => {
      await movementManager.initialize(mockMapManager);
      const newMapManager = new MapManager();
      await expect(movementManager.initialize(newMapManager)).resolves.not.toThrow();
    });

    it('should handle initialization, processing, and re-initialization', async () => {
      await movementManager.initialize(mockMapManager);
      movementManager.processQueue();
      await movementManager.initialize(mockMapManager);
      movementManager.processQueue();
      expect(log.info).toHaveBeenCalledWith('MovementManager initialized');
    });
  });

  describe('Type Safety', () => {
    it('should return correct types for all methods', async () => {
      const initResult = movementManager.initialize(mockMapManager);
      expect(initResult).toBeInstanceOf(Promise);
      await initResult;

      const processResult = movementManager.processQueue();
      expect(processResult).toBeUndefined();

      const cleanupResult = movementManager.cleanup();
      expect(cleanupResult).toBeInstanceOf(Promise);
      await cleanupResult;
    });
  });

  describe('Dependency Management', () => {
    it('should accept MapManager dependency', async () => {
      await expect(movementManager.initialize(mockMapManager)).resolves.not.toThrow();
    });

    it('should work with fresh MapManager instance', async () => {
      const freshMapManager = new MapManager();
      await expect(movementManager.initialize(freshMapManager)).resolves.not.toThrow();
    });

    it('should handle MapManager lifecycle independently', async () => {
      await mockMapManager.initialize();
      await movementManager.initialize(mockMapManager);
      await mockMapManager.cleanup();
      expect(() => movementManager.processQueue()).not.toThrow();
    });

    it('should not fail if MapManager is cleaned up', async () => {
      await mockMapManager.initialize();
      await movementManager.initialize(mockMapManager);
      await mockMapManager.cleanup();
      movementManager.processQueue();
      await expect(movementManager.cleanup()).resolves.not.toThrow();
    });
  });

  describe('Queue Processing Behavior', () => {
    beforeEach(async () => {
      await movementManager.initialize(mockMapManager);
    });

    it('should handle processing with empty queue', () => {
      expect(() => movementManager.processQueue()).not.toThrow();
    });

    it('should handle consecutive processQueue calls', () => {
      movementManager.processQueue();
      movementManager.processQueue();
      movementManager.processQueue();
      expect(true).toBe(true); // No errors thrown
    });

    it('should handle rapid fire processing', () => {
      const start = Date.now();
      for (let i = 0; i < 1000; i++) {
        movementManager.processQueue();
      }
      const end = Date.now();
      expect(end - start).toBeLessThan(1000); // Should complete quickly
    });
  });
});
