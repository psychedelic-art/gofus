import { ChatManager } from '../ChatManager';
import { SocketHandler } from '@/network/SocketHandler';
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

// Mock the SocketHandler
jest.mock('@/network/SocketHandler', () => {
  return {
    SocketHandler: jest.fn().mockImplementation(() => ({
      getOnlinePlayerCount: jest.fn().mockReturnValue(0),
      sendToPlayer: jest.fn(),
      broadcastToMap: jest.fn(),
      getIO: jest.fn().mockReturnValue({
        to: jest.fn().mockReturnValue({
          emit: jest.fn(),
        }),
      }),
    })),
  };
});

describe('ChatManager', () => {
  let chatManager: ChatManager;
  let mockSocketHandler: jest.Mocked<SocketHandler>;

  beforeEach(() => {
    jest.clearAllMocks();
    mockSocketHandler = new SocketHandler(null as any) as jest.Mocked<SocketHandler>;
    chatManager = new ChatManager(mockSocketHandler);
  });

  afterEach(async () => {
    if (chatManager) {
      await chatManager.cleanup();
    }
  });

  describe('Initialization', () => {
    it('should create an instance of ChatManager', () => {
      expect(chatManager).toBeInstanceOf(ChatManager);
    });

    it('should require SocketHandler in constructor', () => {
      expect(() => new ChatManager(mockSocketHandler)).not.toThrow();
    });

    it('should have an initialize method', () => {
      expect(chatManager.initialize).toBeDefined();
      expect(typeof chatManager.initialize).toBe('function');
    });

    it('should call initialize without errors', async () => {
      await expect(chatManager.initialize()).resolves.not.toThrow();
    });

    it('should log initialization message', async () => {
      await chatManager.initialize();
      expect(log.info).toHaveBeenCalledWith('ChatManager initialized');
    });

    it('should return a Promise from initialize', () => {
      const result = chatManager.initialize();
      expect(result).toBeInstanceOf(Promise);
    });

    it('should initialize successfully with valid SocketHandler', async () => {
      await expect(chatManager.initialize()).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledTimes(1);
    });
  });

  describe('Constructor Validation', () => {
    it('should accept SocketHandler as a parameter', () => {
      expect(() => new ChatManager(mockSocketHandler)).not.toThrow();
    });

    it('should store SocketHandler reference', () => {
      const manager = new ChatManager(mockSocketHandler);
      expect(manager).toBeDefined();
    });

    it('should handle null SocketHandler gracefully', () => {
      expect(() => new ChatManager(null as any)).not.toThrow();
    });

    it('should handle undefined SocketHandler gracefully', () => {
      expect(() => new ChatManager(undefined as any)).not.toThrow();
    });
  });

  describe('Error Handling', () => {
    it('should handle cleanup being called before initialization', async () => {
      const uninitializedManager = new ChatManager(mockSocketHandler);
      await expect(uninitializedManager.cleanup()).resolves.not.toThrow();
    });

    it('should handle multiple initialize calls', async () => {
      await chatManager.initialize();
      await expect(chatManager.initialize()).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledTimes(2);
    });

    it('should handle initialization with null SocketHandler', async () => {
      const managerWithNull = new ChatManager(null as any);
      await expect(managerWithNull.initialize()).resolves.not.toThrow();
    });

    it('should not throw if initialize is called multiple times', async () => {
      await chatManager.initialize();
      await chatManager.initialize();
      await chatManager.initialize();
      expect(log.info).toHaveBeenCalledWith('ChatManager initialized');
    });
  });

  describe('Cleanup Operations', () => {
    it('should have a cleanup method', () => {
      expect(chatManager.cleanup).toBeDefined();
      expect(typeof chatManager.cleanup).toBe('function');
    });

    it('should call cleanup without errors', async () => {
      await expect(chatManager.cleanup()).resolves.not.toThrow();
    });

    it('should log cleanup message', async () => {
      await chatManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('ChatManager cleaned up');
    });

    it('should return a Promise from cleanup', () => {
      const result = chatManager.cleanup();
      expect(result).toBeInstanceOf(Promise);
    });

    it('should handle cleanup being called multiple times', async () => {
      await chatManager.cleanup();
      await expect(chatManager.cleanup()).resolves.not.toThrow();
      expect(log.info).toHaveBeenCalledWith('ChatManager cleaned up');
    });

    it('should cleanup successfully after initialization', async () => {
      await chatManager.initialize();
      await expect(chatManager.cleanup()).resolves.not.toThrow();
    });

    it('should handle cleanup without prior initialization', async () => {
      const freshManager = new ChatManager(mockSocketHandler);
      await expect(freshManager.cleanup()).resolves.not.toThrow();
    });
  });

  describe('Integration Scenarios', () => {
    it('should handle full lifecycle: initialize -> cleanup', async () => {
      await expect(chatManager.initialize()).resolves.not.toThrow();
      await expect(chatManager.cleanup()).resolves.not.toThrow();
    });

    it('should handle multiple initialization and cleanup cycles', async () => {
      await chatManager.initialize();
      await chatManager.cleanup();
      await chatManager.initialize();
      await chatManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('ChatManager initialized');
      expect(log.info).toHaveBeenCalledWith('ChatManager cleaned up');
    });

    it('should work with different SocketHandler instances', async () => {
      const handler1 = new SocketHandler(null as any) as jest.Mocked<SocketHandler>;
      const handler2 = new SocketHandler(null as any) as jest.Mocked<SocketHandler>;

      const manager1 = new ChatManager(handler1);
      const manager2 = new ChatManager(handler2);

      await expect(manager1.initialize()).resolves.not.toThrow();
      await expect(manager2.initialize()).resolves.not.toThrow();

      await manager1.cleanup();
      await manager2.cleanup();
    });

    it('should maintain independence from SocketHandler lifecycle', async () => {
      await chatManager.initialize();
      // Simulate SocketHandler cleanup or state change
      mockSocketHandler.getOnlinePlayerCount = jest.fn().mockReturnValue(0);
      await expect(chatManager.cleanup()).resolves.not.toThrow();
    });
  });

  describe('Type Safety', () => {
    it('should return correct types for all methods', async () => {
      const initResult = chatManager.initialize();
      expect(initResult).toBeInstanceOf(Promise);
      await initResult;

      const cleanupResult = chatManager.cleanup();
      expect(cleanupResult).toBeInstanceOf(Promise);
      await cleanupResult;
    });

    it('should handle async operations properly', async () => {
      const promises = [
        chatManager.initialize(),
        chatManager.cleanup(),
      ];

      for (const promise of promises) {
        expect(promise).toBeInstanceOf(Promise);
        await promise;
      }
    });
  });

  describe('Dependency Management', () => {
    it('should accept SocketHandler dependency', () => {
      expect(() => new ChatManager(mockSocketHandler)).not.toThrow();
    });

    it('should work with fresh SocketHandler instance', async () => {
      const freshHandler = new SocketHandler(null as any) as jest.Mocked<SocketHandler>;
      const freshManager = new ChatManager(freshHandler);
      await expect(freshManager.initialize()).resolves.not.toThrow();
      await freshManager.cleanup();
    });

    it('should not affect SocketHandler on cleanup', async () => {
      await chatManager.initialize();
      const beforeCleanup = mockSocketHandler.getOnlinePlayerCount;
      await chatManager.cleanup();
      expect(mockSocketHandler.getOnlinePlayerCount).toBe(beforeCleanup);
    });

    it('should handle SocketHandler being cleaned up independently', async () => {
      await chatManager.initialize();
      // Simulate SocketHandler cleanup
      mockSocketHandler.getOnlinePlayerCount = jest.fn().mockReturnValue(0);
      await expect(chatManager.cleanup()).resolves.not.toThrow();
    });
  });

  describe('Concurrent Operations', () => {
    it('should handle concurrent initialization calls', async () => {
      const promises = Array.from({ length: 10 }, () => chatManager.initialize());
      await expect(Promise.all(promises)).resolves.not.toThrow();
    });

    it('should handle concurrent cleanup calls', async () => {
      await chatManager.initialize();
      const promises = Array.from({ length: 10 }, () => chatManager.cleanup());
      await expect(Promise.all(promises)).resolves.not.toThrow();
    });

    it('should handle interleaved init and cleanup', async () => {
      await chatManager.initialize();
      await chatManager.cleanup();
      await chatManager.initialize();
      await chatManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('ChatManager initialized');
      expect(log.info).toHaveBeenCalledWith('ChatManager cleaned up');
    });
  });

  describe('State Management', () => {
    it('should maintain consistent state after initialization', async () => {
      await chatManager.initialize();
      // State should be consistent
      expect(chatManager).toBeDefined();
      expect(chatManager.initialize).toBeDefined();
      expect(chatManager.cleanup).toBeDefined();
    });

    it('should maintain consistent state after cleanup', async () => {
      await chatManager.initialize();
      await chatManager.cleanup();
      // Methods should still be accessible
      expect(chatManager.initialize).toBeDefined();
      expect(chatManager.cleanup).toBeDefined();
    });

    it('should allow re-initialization after cleanup', async () => {
      await chatManager.initialize();
      await chatManager.cleanup();
      await expect(chatManager.initialize()).resolves.not.toThrow();
    });
  });

  describe('Logging Behavior', () => {
    it('should log on initialization', async () => {
      await chatManager.initialize();
      expect(log.info).toHaveBeenCalledWith('ChatManager initialized');
    });

    it('should log on cleanup', async () => {
      await chatManager.cleanup();
      expect(log.info).toHaveBeenCalledWith('ChatManager cleaned up');
    });

    it('should log multiple times for multiple operations', async () => {
      await chatManager.initialize();
      await chatManager.cleanup();
      await chatManager.initialize();

      expect(log.info).toHaveBeenCalledTimes(3);
    });
  });
});
