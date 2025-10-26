import { GameServer } from '../GameServer';
import { SocketHandler } from '@/network/SocketHandler';
import { PlayerManager } from '@/core/PlayerManager';
import { WorldState } from '@/core/WorldState';
import { MapManager } from '@/managers/MapManager';
import { CombatManager } from '@/managers/CombatManager';
import { MovementManager } from '@/managers/MovementManager';
import { ChatManager } from '@/managers/ChatManager';
import { AIManager } from '@/managers/AIManager';
import { initializeDatabases, closeDatabases } from '@/config/database.config';
import { log } from '@/utils/Logger';

// Mock all dependencies
jest.mock('@/network/SocketHandler');
jest.mock('@/core/PlayerManager');
jest.mock('@/core/WorldState');
jest.mock('@/managers/MapManager');
jest.mock('@/managers/CombatManager');
jest.mock('@/managers/MovementManager');
jest.mock('@/managers/ChatManager');
jest.mock('@/managers/AIManager');
jest.mock('@/config/database.config');
jest.mock('@/utils/Logger');
jest.mock('http', () => ({
  createServer: jest.fn(() => ({
    listen: jest.fn((port, callback) => callback()),
    on: jest.fn(),
    close: jest.fn((callback) => callback()),
  })),
}));

describe('GameServer', () => {
  let gameServer: GameServer;
  let mockSocketHandler: jest.Mocked<SocketHandler>;
  let mockPlayerManager: jest.Mocked<PlayerManager>;
  let mockWorldState: jest.Mocked<WorldState>;
  let mockMapManager: jest.Mocked<MapManager>;
  let mockCombatManager: jest.Mocked<CombatManager>;
  let mockMovementManager: jest.Mocked<MovementManager>;
  let mockChatManager: jest.Mocked<ChatManager>;
  let mockAIManager: jest.Mocked<AIManager>;

  beforeEach(() => {
    // Clear all mocks before each test
    jest.clearAllMocks();

    // Reset timers - use modern fake timers
    jest.useFakeTimers({ legacyFakeTimers: false });

    // Reset HTTP mock to default implementation
    const http = require('http');
    http.createServer.mockImplementation(() => ({
      listen: jest.fn((port, callback) => callback()),
      on: jest.fn(),
      close: jest.fn((callback) => callback()),
    }));

    // Setup mock implementations
    mockSocketHandler = {
      getOnlinePlayerCount: jest.fn().mockReturnValue(0),
    } as any;

    mockPlayerManager = {
      initialize: jest.fn().mockResolvedValue(undefined),
      saveAll: jest.fn().mockResolvedValue(undefined),
      disconnectAll: jest.fn().mockResolvedValue(undefined),
      cleanup: jest.fn().mockResolvedValue(undefined),
    } as any;

    mockWorldState = {
      initialize: jest.fn().mockResolvedValue(undefined),
      update: jest.fn(),
      cleanup: jest.fn().mockResolvedValue(undefined),
    } as any;

    mockMapManager = {
      initialize: jest.fn().mockResolvedValue(undefined),
      updateAll: jest.fn(),
      saveAll: jest.fn().mockResolvedValue(undefined),
      cleanup: jest.fn().mockResolvedValue(undefined),
      getActiveInstanceCount: jest.fn().mockReturnValue(0),
    } as any;

    mockCombatManager = {
      initialize: jest.fn().mockResolvedValue(undefined),
      updateBattles: jest.fn(),
      saveAll: jest.fn().mockResolvedValue(undefined),
      cleanup: jest.fn().mockResolvedValue(undefined),
      getActiveBattleCount: jest.fn().mockReturnValue(0),
    } as any;

    mockMovementManager = {
      initialize: jest.fn().mockResolvedValue(undefined),
      processQueue: jest.fn(),
      cleanup: jest.fn().mockResolvedValue(undefined),
    } as any;

    mockChatManager = {
      // Chat manager might not have initialize method
    } as any;

    mockAIManager = {
      initialize: jest.fn().mockResolvedValue(undefined),
      update: jest.fn(),
      cleanup: jest.fn().mockResolvedValue(undefined),
    } as any;

    // Mock the constructors to return our mocks
    (SocketHandler as jest.Mock).mockImplementation(() => mockSocketHandler);
    (PlayerManager as jest.Mock).mockImplementation(() => mockPlayerManager);
    (WorldState as jest.Mock).mockImplementation(() => mockWorldState);
    (MapManager as jest.Mock).mockImplementation(() => mockMapManager);
    (CombatManager as jest.Mock).mockImplementation(() => mockCombatManager);
    (MovementManager as jest.Mock).mockImplementation(() => mockMovementManager);
    (ChatManager as jest.Mock).mockImplementation(() => mockChatManager);
    (AIManager as jest.Mock).mockImplementation(() => mockAIManager);

    // Mock database functions
    (initializeDatabases as jest.Mock).mockResolvedValue(undefined);
    (closeDatabases as jest.Mock).mockResolvedValue(undefined);

    // Mock logger
    (log.info as jest.Mock) = jest.fn();
    (log.exception as jest.Mock) = jest.fn();
    (log.performance as jest.Mock) = jest.fn();
    (log.error as jest.Mock) = jest.fn();
    (log.timer as jest.Mock) = jest.fn(() => jest.fn().mockReturnValue(100));

    // Create a fresh instance for each test
    gameServer = new GameServer();
  });

  afterEach(async () => {
    // Stop the server if it's running to clean up intervals
    try {
      await gameServer.stop();
    } catch (error) {
      // Ignore errors during cleanup
    }

    // Clean up timers
    jest.clearAllTimers();
    jest.useRealTimers();

    // Remove all process event listeners
    process.removeAllListeners('SIGTERM');
    process.removeAllListeners('SIGINT');
    process.removeAllListeners('uncaughtException');
    process.removeAllListeners('unhandledRejection');

    // Restore all mocks
    jest.restoreAllMocks();
  });

  describe('Constructor', () => {
    it('should create a new GameServer instance', () => {
      expect(gameServer).toBeInstanceOf(GameServer);
    });

    it('should initialize all managers', () => {
      expect(SocketHandler).toHaveBeenCalledTimes(1);
      expect(PlayerManager).toHaveBeenCalledTimes(1);
      expect(WorldState).toHaveBeenCalledTimes(1);
      expect(MapManager).toHaveBeenCalledTimes(1);
      expect(CombatManager).toHaveBeenCalledTimes(1);
      expect(MovementManager).toHaveBeenCalledTimes(1);
      expect(AIManager).toHaveBeenCalledTimes(1);
      expect(ChatManager).toHaveBeenCalledTimes(1);
    });

    it('should pass socketHandler to ChatManager constructor', () => {
      expect(ChatManager).toHaveBeenCalledWith(mockSocketHandler);
    });
  });

  describe('start()', () => {
    it('should successfully start the game server', async () => {
      await gameServer.start();

      expect(initializeDatabases).toHaveBeenCalledTimes(1);
      expect(log.info).toHaveBeenCalledWith('Starting GOFUS Game Server...');
      expect(log.info).toHaveBeenCalledWith(expect.stringContaining('Game Server started on port'));
    });

    it('should initialize managers in the correct order', async () => {
      const callOrder: string[] = [];

      mockWorldState.initialize.mockImplementation(async () => {
        callOrder.push('worldState');
      });
      mockMapManager.initialize.mockImplementation(async () => {
        callOrder.push('mapManager');
      });
      mockPlayerManager.initialize.mockImplementation(async () => {
        callOrder.push('playerManager');
      });
      mockCombatManager.initialize.mockImplementation(async () => {
        callOrder.push('combatManager');
      });
      mockMovementManager.initialize.mockImplementation(async () => {
        callOrder.push('movementManager');
      });
      mockAIManager.initialize.mockImplementation(async () => {
        callOrder.push('aiManager');
      });

      await gameServer.start();

      expect(callOrder).toEqual([
        'worldState',
        'mapManager',
        'playerManager',
        'combatManager',
        'movementManager',
        'aiManager',
      ]);
    });

    it('should pass mapManager to combatManager.initialize', async () => {
      await gameServer.start();

      expect(mockCombatManager.initialize).toHaveBeenCalledWith(mockMapManager);
    });

    it('should pass mapManager to movementManager.initialize', async () => {
      await gameServer.start();

      expect(mockMovementManager.initialize).toHaveBeenCalledWith(mockMapManager);
    });

    it('should pass mapManager and combatManager to aiManager.initialize', async () => {
      await gameServer.start();

      expect(mockAIManager.initialize).toHaveBeenCalledWith(
        mockMapManager,
        mockCombatManager
      );
    });

    it('should start the game loop', async () => {
      await gameServer.start();

      // Verify the game loop is running by checking if ticks happen
      await jest.advanceTimersByTimeAsync(50);
      expect(mockWorldState.update).toHaveBeenCalled();
    });

    it('should start periodic save', async () => {
      await gameServer.start();

      // Verify periodic save runs
      await jest.advanceTimersByTimeAsync(300000);
      expect(mockPlayerManager.saveAll).toHaveBeenCalled();
    });

    it('should setup graceful shutdown handlers', async () => {
      const processSpy = jest.spyOn(process, 'on');

      await gameServer.start();

      expect(processSpy).toHaveBeenCalledWith('SIGTERM', expect.any(Function));
      expect(processSpy).toHaveBeenCalledWith('SIGINT', expect.any(Function));
      expect(processSpy).toHaveBeenCalledWith('uncaughtException', expect.any(Function));
      expect(processSpy).toHaveBeenCalledWith('unhandledRejection', expect.any(Function));
    });

    it('should throw error if database initialization fails', async () => {
      const dbError = new Error('Database connection failed');
      (initializeDatabases as jest.Mock).mockRejectedValue(dbError);

      await expect(gameServer.start()).rejects.toThrow('Database connection failed');
      expect(log.exception).toHaveBeenCalledWith(dbError, 'GameServer.start');
    });

    it('should throw error if manager initialization fails', async () => {
      const managerError = new Error('Manager initialization failed');
      mockWorldState.initialize.mockRejectedValue(managerError);

      await expect(gameServer.start()).rejects.toThrow('Manager initialization failed');
      expect(log.exception).toHaveBeenCalledWith(managerError, 'GameServer.start');
    });
  });

  describe('stop()', () => {
    beforeEach(async () => {
      // Start the server before each stop test
      await gameServer.start();
    });

    it('should successfully stop the game server', async () => {
      await gameServer.stop();

      expect(log.info).toHaveBeenCalledWith('Stopping game server...');
      expect(log.info).toHaveBeenCalledWith(expect.stringContaining('Game server stopped'));
    });

    it('should clear game loop interval', async () => {
      await gameServer.stop();

      // Verify intervals were cleared by checking that ticks don't happen anymore
      await jest.advanceTimersByTimeAsync(50);
      const updateCallsAfterStop = mockWorldState.update.mock.calls.length;

      await jest.advanceTimersByTimeAsync(50);
      expect(mockWorldState.update.mock.calls.length).toBe(updateCallsAfterStop);
    });

    it('should clear periodic save interval', async () => {
      const saveCallsBeforeStop = mockPlayerManager.saveAll.mock.calls.length;

      await gameServer.stop();

      // Verify periodic save doesn't run anymore
      await jest.advanceTimersByTimeAsync(300000);
      expect(mockPlayerManager.saveAll.mock.calls.length).toBe(saveCallsBeforeStop + 1); // +1 for final save in stop()
    });

    it('should save final world state before stopping', async () => {
      await gameServer.stop();

      expect(mockPlayerManager.saveAll).toHaveBeenCalled();
      expect(mockMapManager.saveAll).toHaveBeenCalled();
      expect(mockCombatManager.saveAll).toHaveBeenCalled();
    });

    it('should disconnect all players', async () => {
      await gameServer.stop();

      expect(mockPlayerManager.disconnectAll).toHaveBeenCalled();
    });

    it('should cleanup all managers', async () => {
      await gameServer.stop();

      expect(mockAIManager.cleanup).toHaveBeenCalled();
      expect(mockCombatManager.cleanup).toHaveBeenCalled();
      expect(mockMovementManager.cleanup).toHaveBeenCalled();
      expect(mockMapManager.cleanup).toHaveBeenCalled();
      expect(mockPlayerManager.cleanup).toHaveBeenCalled();
      expect(mockWorldState.cleanup).toHaveBeenCalled();
    });

    it('should close database connections', async () => {
      await gameServer.stop();

      expect(closeDatabases).toHaveBeenCalled();
    });

    it('should do nothing if server is not running', async () => {
      const newServer = new GameServer();

      await newServer.stop();

      // Should not call any cleanup methods
      expect(mockPlayerManager.disconnectAll).not.toHaveBeenCalled();
      expect(closeDatabases).not.toHaveBeenCalled();
    });

    it('should handle errors during save gracefully', async () => {
      const saveError = new Error('Save failed');
      mockPlayerManager.saveAll.mockRejectedValue(saveError);

      await gameServer.stop();

      expect(log.exception).toHaveBeenCalledWith(saveError, 'GameServer.saveWorldState');
      // Should still continue with shutdown
      expect(closeDatabases).toHaveBeenCalled();
    });
  });

  describe('Game Loop (tick system)', () => {
    beforeEach(async () => {
      await gameServer.start();
    });

    it('should call update on worldState during tick', async () => {
      await jest.advanceTimersByTimeAsync(50); // Advance by one tick interval

      expect(mockWorldState.update).toHaveBeenCalled();
    });

    it('should call updateAll on mapManager during tick', async () => {
      await jest.advanceTimersByTimeAsync(50);

      expect(mockMapManager.updateAll).toHaveBeenCalled();
    });

    it('should call updateBattles on combatManager during tick', async () => {
      await jest.advanceTimersByTimeAsync(50);

      expect(mockCombatManager.updateBattles).toHaveBeenCalled();
    });

    it('should call update on aiManager during tick', async () => {
      await jest.advanceTimersByTimeAsync(50);

      expect(mockAIManager.update).toHaveBeenCalled();
    });

    it('should call processQueue on movementManager during tick', async () => {
      await jest.advanceTimersByTimeAsync(50);

      expect(mockMovementManager.processQueue).toHaveBeenCalled();
    });

    it('should execute multiple ticks over time', async () => {
      await jest.advanceTimersByTimeAsync(50);
      expect(mockWorldState.update).toHaveBeenCalledTimes(1);

      await jest.advanceTimersByTimeAsync(50);
      expect(mockWorldState.update).toHaveBeenCalledTimes(2);

      await jest.advanceTimersByTimeAsync(50);
      expect(mockWorldState.update).toHaveBeenCalledTimes(3);
    });

    it('should log performance warning if tick takes too long', async () => {
      // Mock Date.now to simulate slow tick
      const originalDateNow = Date.now;
      let callCount = 0;
      jest.spyOn(global.Date, 'now').mockImplementation(() => {
        callCount++;
        // First call: start time, second call: end time (51ms later)
        return callCount === 1 ? 1000 : 1051;
      });

      await jest.advanceTimersByTimeAsync(50);

      expect(log.performance).toHaveBeenCalledWith(
        'Game tick',
        51,
        expect.objectContaining({ tickCount: expect.any(Number) })
      );

      (global.Date.now as jest.Mock).mockRestore();
    });

    it('should handle errors during tick gracefully', async () => {
      const tickError = new Error('Tick error');
      mockWorldState.update.mockImplementation(() => {
        throw tickError;
      });

      await jest.advanceTimersByTimeAsync(50);

      expect(log.exception).toHaveBeenCalledWith(tickError, 'GameServer.gameTick');

      // Server should continue running
      await jest.advanceTimersByTimeAsync(50);
      expect(mockWorldState.update).toHaveBeenCalledTimes(2);
    });
  });

  describe('Periodic Save', () => {
    beforeEach(async () => {
      await gameServer.start();
    });

    it('should save world state periodically', async () => {
      // Advance time to trigger periodic save (default 300000ms)
      await jest.advanceTimersByTimeAsync(300000);

      expect(mockPlayerManager.saveAll).toHaveBeenCalled();
      expect(mockMapManager.saveAll).toHaveBeenCalled();
      expect(mockCombatManager.saveAll).toHaveBeenCalled();
    });

    it('should execute multiple saves over time', async () => {
      await jest.advanceTimersByTimeAsync(300000);
      expect(mockPlayerManager.saveAll).toHaveBeenCalledTimes(1);

      await jest.advanceTimersByTimeAsync(300000);
      expect(mockPlayerManager.saveAll).toHaveBeenCalledTimes(2);
    });

    it('should log save duration', async () => {
      await jest.advanceTimersByTimeAsync(300000);

      expect(log.info).toHaveBeenCalledWith('Saving world state...');
      expect(log.info).toHaveBeenCalledWith(expect.stringContaining('World state saved in'));
    });

    it('should handle save errors gracefully', async () => {
      const saveError = new Error('Save error');
      mockPlayerManager.saveAll.mockRejectedValue(saveError);

      await jest.advanceTimersByTimeAsync(300000);

      expect(log.exception).toHaveBeenCalledWith(saveError, 'GameServer.saveWorldState');
    });
  });

  describe('Graceful Shutdown', () => {
    it('should handle SIGTERM signal', async () => {
      const stopSpy = jest.spyOn(gameServer, 'stop');
      const exitSpy = jest.spyOn(process, 'exit').mockImplementation(() => {
        throw new Error('Process exit called');
      });

      await gameServer.start();

      // Trigger SIGTERM
      const sigtermHandler = process.listeners('SIGTERM')[0] as Function;

      await expect(async () => {
        await sigtermHandler();
      }).rejects.toThrow('Process exit called');

      expect(log.info).toHaveBeenCalledWith(expect.stringContaining('SIGTERM'));
      expect(stopSpy).toHaveBeenCalled();
      expect(exitSpy).toHaveBeenCalledWith(0);

      exitSpy.mockRestore();
    });

    it('should handle SIGINT signal', async () => {
      const stopSpy = jest.spyOn(gameServer, 'stop');
      const exitSpy = jest.spyOn(process, 'exit').mockImplementation(() => {
        throw new Error('Process exit called');
      });

      await gameServer.start();

      // Trigger SIGINT
      const sigintHandler = process.listeners('SIGINT')[0] as Function;

      await expect(async () => {
        await sigintHandler();
      }).rejects.toThrow('Process exit called');

      expect(log.info).toHaveBeenCalledWith(expect.stringContaining('SIGINT'));
      expect(stopSpy).toHaveBeenCalled();
      expect(exitSpy).toHaveBeenCalledWith(0);

      exitSpy.mockRestore();
    });

    it('should handle uncaught exceptions', async () => {
      const exitSpy = jest.spyOn(process, 'exit').mockImplementation((() => {}) as any);
      const stopSpy = jest.spyOn(gameServer, 'stop');

      await gameServer.start();

      const uncaughtHandler = process.listeners('uncaughtException')[0] as Function;
      const testError = new Error('Uncaught error');

      // Call the handler - it's async but returns a promise
      const shutdownPromise = uncaughtHandler(testError);

      // Advance timers to allow async operations to complete
      await jest.runAllTimersAsync();
      await shutdownPromise;

      expect(log.exception).toHaveBeenCalledWith(testError, 'Uncaught Exception');
      expect(stopSpy).toHaveBeenCalled();
      expect(exitSpy).toHaveBeenCalledWith(0);

      exitSpy.mockRestore();
    });

    it('should handle unhandled rejections', async () => {
      const exitSpy = jest.spyOn(process, 'exit').mockImplementation((() => {}) as any);
      const stopSpy = jest.spyOn(gameServer, 'stop');

      await gameServer.start();

      const unhandledHandler = process.listeners('unhandledRejection')[0] as Function;
      const testReason = 'Unhandled promise rejection';
      const testPromise = Promise.reject(testReason).catch(() => {}); // Prevent actual unhandled rejection

      // Call the handler - it's async but returns a promise
      const shutdownPromise = unhandledHandler(testReason, testPromise);

      // Advance timers to allow async operations to complete
      await jest.runAllTimersAsync();
      await shutdownPromise;

      expect(log.error).toHaveBeenCalledWith('Unhandled Rejection', {
        reason: testReason,
        promise: testPromise,
      });
      expect(stopSpy).toHaveBeenCalled();
      expect(exitSpy).toHaveBeenCalledWith(0);

      exitSpy.mockRestore();
    });
  });

  describe('Manager Getters', () => {
    it('should return SocketHandler instance', () => {
      expect(gameServer.getSocketHandler()).toBe(mockSocketHandler);
    });

    it('should return PlayerManager instance', () => {
      expect(gameServer.getPlayerManager()).toBe(mockPlayerManager);
    });

    it('should return WorldState instance', () => {
      expect(gameServer.getWorldState()).toBe(mockWorldState);
    });

    it('should return MapManager instance', () => {
      expect(gameServer.getMapManager()).toBe(mockMapManager);
    });

    it('should return CombatManager instance', () => {
      expect(gameServer.getCombatManager()).toBe(mockCombatManager);
    });

    it('should return MovementManager instance', () => {
      expect(gameServer.getMovementManager()).toBe(mockMovementManager);
    });

    it('should return ChatManager instance', () => {
      expect(gameServer.getChatManager()).toBe(mockChatManager);
    });

    it('should return AIManager instance', () => {
      expect(gameServer.getAIManager()).toBe(mockAIManager);
    });
  });

  describe('Metrics Collection', () => {
    beforeEach(async () => {
      await gameServer.start();
    });

    it('should return server metrics', () => {
      const metrics = gameServer.getMetrics();

      expect(metrics).toHaveProperty('serverId');
      expect(metrics).toHaveProperty('uptime');
      expect(metrics).toHaveProperty('tickCount');
      expect(metrics).toHaveProperty('lastTickDuration');
      expect(metrics).toHaveProperty('onlinePlayers');
      expect(metrics).toHaveProperty('activeMapInstances');
      expect(metrics).toHaveProperty('activeBattles');
      expect(metrics).toHaveProperty('memoryUsage');
    });

    it('should include correct tick count', () => {
      // Execute a few ticks
      jest.advanceTimersByTime(50);
      jest.advanceTimersByTime(50);
      jest.advanceTimersByTime(50);

      const metrics = gameServer.getMetrics();
      expect(metrics.tickCount).toBe(3);
    });

    it('should query managers for active counts', () => {
      mockSocketHandler.getOnlinePlayerCount.mockReturnValue(42);
      mockMapManager.getActiveInstanceCount.mockReturnValue(15);
      mockCombatManager.getActiveBattleCount.mockReturnValue(7);

      const metrics = gameServer.getMetrics();

      expect(metrics.onlinePlayers).toBe(42);
      expect(metrics.activeMapInstances).toBe(15);
      expect(metrics.activeBattles).toBe(7);
      expect(mockSocketHandler.getOnlinePlayerCount).toHaveBeenCalled();
      expect(mockMapManager.getActiveInstanceCount).toHaveBeenCalled();
      expect(mockCombatManager.getActiveBattleCount).toHaveBeenCalled();
    });

    it('should include memory usage', () => {
      const metrics = gameServer.getMetrics();

      expect(metrics.memoryUsage).toBeDefined();
      expect(typeof metrics.memoryUsage).toBe('object');
    });

    it('should include process uptime', () => {
      const metrics = gameServer.getMetrics();

      expect(metrics.uptime).toBeDefined();
      expect(typeof metrics.uptime).toBe('number');
    });

    it('should track last tick duration', async () => {
      const originalDateNow = Date.now;
      let callCount = 0;
      jest.spyOn(global.Date, 'now').mockImplementation(() => {
        callCount++;
        return callCount === 1 ? 1000 : 1025;
      });

      await jest.advanceTimersByTimeAsync(50);

      const metrics = gameServer.getMetrics();
      expect(metrics.lastTickDuration).toBe(25);

      (global.Date.now as jest.Mock).mockRestore();
    });
  });

  describe('Error Handling', () => {
    it('should handle HTTP server listen errors', async () => {
      const http = require('http');
      const listenError = new Error('Port already in use');

      http.createServer.mockImplementation(() => ({
        listen: jest.fn((port, callback) => {
          // Don't call callback, simulate error immediately
        }),
        on: jest.fn((event, handler) => {
          if (event === 'error') {
            // Synchronously trigger error
            handler(listenError);
          }
        }),
      }));

      // Create new server with error mock
      const errorServer = new GameServer();

      await expect(errorServer.start()).rejects.toThrow(listenError);
    }, 10000);

    it('should continue operation even if a single tick fails', async () => {
      await gameServer.start();

      // Make first tick fail
      mockWorldState.update.mockImplementationOnce(() => {
        throw new Error('First tick error');
      });

      await jest.advanceTimersByTimeAsync(50);
      expect(log.exception).toHaveBeenCalled();

      // Clear the mock call history
      jest.clearAllMocks();

      // Next tick should work fine
      await jest.advanceTimersByTimeAsync(50);
      expect(mockWorldState.update).toHaveBeenCalled();
      expect(log.exception).not.toHaveBeenCalled();
    }, 10000);

    it('should handle manager initialization errors individually', async () => {
      mockMapManager.initialize.mockRejectedValue(new Error('MapManager init failed'));

      await expect(gameServer.start()).rejects.toThrow('MapManager init failed');
      expect(log.exception).toHaveBeenCalled();
    });
  });

  describe('Integration Tests', () => {
    it('should start, run ticks, save, and stop successfully', async () => {
      await gameServer.start();

      // Run some ticks
      await jest.advanceTimersByTimeAsync(150);
      expect(mockWorldState.update).toHaveBeenCalledTimes(3);

      // Trigger periodic save
      await jest.advanceTimersByTimeAsync(300000);

      // Stop server
      await gameServer.stop();

      expect(closeDatabases).toHaveBeenCalled();
      expect(mockPlayerManager.cleanup).toHaveBeenCalled();
    }, 10000);

    it('should maintain state consistency across start-stop cycles', async () => {
      // First start
      await gameServer.start();
      await jest.advanceTimersByTimeAsync(100);
      await gameServer.stop();

      // Reset mocks
      jest.clearAllMocks();

      // Second start should work
      await gameServer.start();
      expect(initializeDatabases).toHaveBeenCalled();
      expect(mockWorldState.initialize).toHaveBeenCalled();
    }, 10000);

    it('should handle rapid start-stop cycles', async () => {
      await gameServer.start();
      await gameServer.stop();
      await gameServer.start();
      await gameServer.stop();

      expect(initializeDatabases).toHaveBeenCalledTimes(2);
      expect(closeDatabases).toHaveBeenCalledTimes(2);
    }, 10000);
  });

  describe('Performance and Load', () => {
    beforeEach(async () => {
      await gameServer.start();
    });

    it('should track tick count accurately over many ticks', async () => {
      // Run 100 ticks
      await jest.advanceTimersByTimeAsync(5000); // 100 ticks at 50ms each

      const metrics = gameServer.getMetrics();
      expect(metrics.tickCount).toBe(100);
    }, 10000);

    it('should not skip ticks under normal operation', async () => {
      await jest.advanceTimersByTimeAsync(500); // 10 ticks at 50ms each

      // Should have called update exactly 10 times
      expect(mockWorldState.update).toHaveBeenCalledTimes(10);
    }, 10000);

    it('should handle managers returning quickly', async () => {
      mockWorldState.update.mockImplementation(() => {
        // Instant return
      });

      await jest.advanceTimersByTimeAsync(50);

      expect(mockWorldState.update).toHaveBeenCalled();
      // Should not log performance warning
      expect(log.performance).not.toHaveBeenCalled();
    }, 10000);
  });
});
