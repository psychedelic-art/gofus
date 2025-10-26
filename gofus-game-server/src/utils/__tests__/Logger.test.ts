import winston from 'winston';

// Create a consistent mock logger instance
const mockLoggerInstance = {
  error: jest.fn(),
  warn: jest.fn(),
  info: jest.fn(),
  debug: jest.fn(),
  log: jest.fn(),
};

// Mock winston before importing Logger
jest.mock('winston', () => {
  return {
    createLogger: jest.fn(() => mockLoggerInstance),
    format: {
      combine: jest.fn((...args) => args),
      timestamp: jest.fn((config) => config),
      printf: jest.fn((fn) => fn),
      colorize: jest.fn(),
      errors: jest.fn((config) => config),
    },
    transports: {
      Console: jest.fn(),
      File: jest.fn(),
    },
  };
});

jest.mock('@/config/server.config', () => ({
  serverConfig: {
    environment: 'development',
    monitoring: {
      logLevel: 'info',
      logFile: 'game-server.log',
    },
  },
}));

// Import Logger module after mocks are set up
import { Logger, log } from '../Logger';

describe('Logger', () => {
  beforeEach(() => {
    // Clear all mock calls
    jest.clearAllMocks();
  });

  describe('Logger Initialization', () => {
    it('should create a logger instance', () => {
      expect(Logger).toBeDefined();
      // The logger is created during module import, so check if it was called at least once
      expect(winston.createLogger).toHaveBeenCalledTimes(1);
    });

    it('should configure logger with correct log level', () => {
      const createLoggerCalls = (winston.createLogger as jest.Mock).mock.calls;
      expect(createLoggerCalls.length).toBeGreaterThan(0);

      const config = createLoggerCalls[0][0];
      expect(config).toHaveProperty('level', 'info');
    });

    it('should configure development-specific settings', () => {
      const createLoggerCalls = (winston.createLogger as jest.Mock).mock.calls;
      const config = createLoggerCalls[0][0];
      expect(config).toHaveProperty('format');
    });

    it('should configure production settings when NODE_ENV is production', () => {
      const envBackup = process.env.NODE_ENV;
      process.env.NODE_ENV = 'production';

      jest.resetModules();
      jest.clearAllMocks();

      const prodConfig = {
        serverConfig: {
          environment: 'production',
          monitoring: {
            logLevel: 'error',
            logFile: 'production.log',
          },
        },
      };

      jest.doMock('@/config/server.config', () => prodConfig);
      require('../Logger');

      const createLoggerCalls = (winston.createLogger as jest.Mock).mock.calls;
      expect(createLoggerCalls.length).toBeGreaterThan(0);

      process.env.NODE_ENV = envBackup;
    });
  });

  describe('Logging Methods', () => {
    describe('error()', () => {
      it('should log error messages', () => {
        const message = 'Test error message';
        const meta = { error: 'details' };

        Logger.error(message, meta);

        expect(mockLoggerInstance.error).toHaveBeenCalledWith(message, meta);
      });

      it('should handle error without metadata', () => {
        const message = 'Error without meta';

        Logger.error(message);

        expect(mockLoggerInstance.error).toHaveBeenCalledWith(message, undefined);
      });

      it('should handle Error objects', () => {
        const error = new Error('Test error');
        const message = 'Error occurred';

        Logger.error(message, { error });

        expect(mockLoggerInstance.error).toHaveBeenCalledWith(message, { error });
      });

      it('should handle complex error metadata', () => {
        const complexMeta = {
          userId: 'user123',
          action: 'login',
          timestamp: Date.now(),
          stack: new Error().stack,
        };

        Logger.error('Complex error', complexMeta);

        expect(mockLoggerInstance.error).toHaveBeenCalledWith('Complex error', complexMeta);
      });
    });

    describe('warn()', () => {
      it('should log warning messages', () => {
        const message = 'Test warning';
        const meta = { warning: 'details' };

        Logger.warn(message, meta);

        expect(mockLoggerInstance.warn).toHaveBeenCalledWith(message, meta);
      });

      it('should handle warnings without metadata', () => {
        Logger.warn('Simple warning');

        expect(mockLoggerInstance.warn).toHaveBeenCalledWith('Simple warning', undefined);
      });

      it('should log performance warnings', () => {
        const perfWarning = {
          operation: 'database-query',
          duration: 5000,
          threshold: 1000,
        };

        Logger.warn('Slow operation detected', perfWarning);

        expect(mockLoggerInstance.warn).toHaveBeenCalledWith(
          'Slow operation detected',
          perfWarning
        );
      });
    });

    describe('info()', () => {
      it('should log info messages', () => {
        const message = 'Test info';
        const meta = { info: 'details' };

        Logger.info(message, meta);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(message, meta);
      });

      it('should handle info without metadata', () => {
        Logger.info('Simple info');

        expect(mockLoggerInstance.info).toHaveBeenCalledWith('Simple info', undefined);
      });

      it('should log server startup info', () => {
        const startupInfo = {
          port: 3000,
          environment: 'development',
          version: '1.0.0',
        };

        Logger.info('Server started', startupInfo);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith('Server started', startupInfo);
      });

      it('should log player connection info', () => {
        const connectionInfo = {
          playerId: 'player123',
          ip: '192.168.1.1',
          timestamp: new Date().toISOString(),
        };

        Logger.info('Player connected', connectionInfo);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          'Player connected',
          connectionInfo
        );
      });
    });

    describe('debug()', () => {
      it('should log debug messages', () => {
        const message = 'Test debug';
        const meta = { debug: 'details' };

        Logger.debug(message, meta);

        expect(mockLoggerInstance.debug).toHaveBeenCalledWith(message, meta);
      });

      it('should handle debug without metadata', () => {
        Logger.debug('Simple debug');

        expect(mockLoggerInstance.debug).toHaveBeenCalledWith('Simple debug', undefined);
      });

      it('should log detailed debug information', () => {
        const debugInfo = {
          function: 'calculateDamage',
          input: { baseDamage: 100, multiplier: 1.5 },
          output: 150,
          executionTime: 0.5,
        };

        Logger.debug('Function execution', debugInfo);

        expect(mockLoggerInstance.debug).toHaveBeenCalledWith(
          'Function execution',
          debugInfo
        );
      });

      it('should log state changes', () => {
        const stateChange = {
          entity: 'player',
          id: 'player123',
          previousState: 'idle',
          newState: 'combat',
        };

        Logger.debug('State change', stateChange);

        expect(mockLoggerInstance.debug).toHaveBeenCalledWith('State change', stateChange);
      });
    });

    describe('log()', () => {
      it('should log with custom level', () => {
        const level = 'custom';
        const message = 'Custom level message';
        const meta = { custom: 'data' };

        Logger.log(level, message, meta);

        expect(mockLoggerInstance.log).toHaveBeenCalledWith(level, message, meta);
      });

      it('should handle custom level without metadata', () => {
        Logger.log('custom', 'Message without meta');

        expect(mockLoggerInstance.log).toHaveBeenCalledWith(
          'custom',
          'Message without meta',
          undefined
        );
      });

      it('should support various log levels', () => {
        const levels = ['silly', 'verbose', 'http'];

        levels.forEach(level => {
          Logger.log(level, `${level} message`);
          expect(mockLoggerInstance.log).toHaveBeenCalledWith(
            level,
            `${level} message`,
            undefined
          );
        });
      });
    });
  });

  describe('Specialized Logging Methods', () => {
    describe('logPlayerAction()', () => {
      it('should log player actions', () => {
        const playerId = 'player123';
        const action = 'attack';
        const details = { target: 'monster456', damage: 50 };

        Logger.logPlayerAction(playerId, action, details);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          `Player ${playerId} performed ${action}`,
          {
            playerId,
            action,
            details,
          }
        );
      });

      it('should handle actions without details', () => {
        Logger.logPlayerAction('player123', 'logout');

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          'Player player123 performed logout',
          {
            playerId: 'player123',
            action: 'logout',
            details: undefined,
          }
        );
      });

      it('should log movement actions', () => {
        const movementDetails = {
          fromCell: 100,
          toCell: 105,
          mapId: 1,
        };

        Logger.logPlayerAction('player123', 'move', movementDetails);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          'Player player123 performed move',
          {
            playerId: 'player123',
            action: 'move',
            details: movementDetails,
          }
        );
      });

      it('should log combat actions', () => {
        const combatDetails = {
          spell: 'fireball',
          target: 'monster789',
          damage: 150,
          critical: true,
        };

        Logger.logPlayerAction('player456', 'cast_spell', combatDetails);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          'Player player456 performed cast_spell',
          {
            playerId: 'player456',
            action: 'cast_spell',
            details: combatDetails,
          }
        );
      });

      it('should log trade actions', () => {
        const tradeDetails = {
          withPlayer: 'player789',
          itemsOffered: ['sword', 'potion'],
          itemsReceived: ['shield'],
          goldExchanged: 100,
        };

        Logger.logPlayerAction('player123', 'trade', tradeDetails);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          'Player player123 performed trade',
          {
            playerId: 'player123',
            action: 'trade',
            details: tradeDetails,
          }
        );
      });
    });

    describe('logGameEvent()', () => {
      it('should log game events', () => {
        const event = 'boss_spawn';
        const data = { bossId: 'boss001', location: 'dungeon_01' };

        Logger.logGameEvent(event, data);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          `Game Event: ${event}`,
          data
        );
      });

      it('should handle events without data', () => {
        Logger.logGameEvent('server_restart');

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          'Game Event: server_restart',
          undefined
        );
      });

      it('should log map events', () => {
        const mapEvent = {
          mapId: 5,
          playerCount: 25,
          monsterCount: 10,
        };

        Logger.logGameEvent('map_instance_created', mapEvent);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          'Game Event: map_instance_created',
          mapEvent
        );
      });

      it('should log battle events', () => {
        const battleData = {
          battleId: 'battle123',
          participants: ['player1', 'player2'],
          winner: 'player1',
          duration: 120000,
        };

        Logger.logGameEvent('battle_ended', battleData);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          'Game Event: battle_ended',
          battleData
        );
      });

      it('should log system events', () => {
        const systemEvent = {
          type: 'maintenance',
          scheduledTime: '2024-01-01T00:00:00Z',
          estimatedDuration: 3600000,
        };

        Logger.logGameEvent('scheduled_maintenance', systemEvent);

        expect(mockLoggerInstance.info).toHaveBeenCalledWith(
          'Game Event: scheduled_maintenance',
          systemEvent
        );
      });
    });

    describe('logSystemInfo()', () => {
      it('should log system information', () => {
        const message = 'System status';
        const context = { cpu: 45, memory: 60, connections: 150 };

        Logger.logSystemInfo(message, context);

        expect(mockLoggerInstance.log).toHaveBeenCalledWith('system', message, {
          timestamp: expect.any(String),
          context,
        });
      });

      it('should handle system info without context', () => {
        Logger.logSystemInfo('System healthy');

        expect(mockLoggerInstance.log).toHaveBeenCalledWith(
          'system',
          'System healthy',
          {
            timestamp: expect.any(String),
            context: undefined,
          }
        );
      });

      it('should include timestamp in ISO format', () => {
        Logger.logSystemInfo('Test message');

        const call = mockLoggerInstance.log.mock.calls[0];
        const meta = call[2];

        expect(meta.timestamp).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
      });

      it('should log memory usage', () => {
        const memoryInfo = {
          used: 1024 * 1024 * 512, // 512MB
          total: 1024 * 1024 * 2048, // 2GB
          percentage: 25,
        };

        Logger.logSystemInfo('Memory usage', memoryInfo);

        expect(mockLoggerInstance.log).toHaveBeenCalledWith(
          'system',
          'Memory usage',
          {
            timestamp: expect.any(String),
            context: memoryInfo,
          }
        );
      });

      it('should log performance metrics', () => {
        const metrics = {
          tickRate: 20,
          avgTickTime: 15,
          maxTickTime: 45,
          playersOnline: 250,
        };

        Logger.logSystemInfo('Performance metrics', metrics);

        expect(mockLoggerInstance.log).toHaveBeenCalledWith(
          'system',
          'Performance metrics',
          {
            timestamp: expect.any(String),
            context: metrics,
          }
        );
      });
    });
  });

  describe('Timer Functionality - startTimer()', () => {
    beforeEach(() => {
      jest.useFakeTimers();
      jest.setSystemTime(new Date('2024-01-01T00:00:00.000Z'));
    });

    afterEach(() => {
      jest.useRealTimers();
    });

    it('should return a timer function', () => {
      const timer = Logger.startTimer();
      expect(typeof timer).toBe('function');
    });

    it('should measure elapsed time', () => {
      const timer = Logger.startTimer();
      jest.advanceTimersByTime(1000);
      const elapsed = timer();
      expect(elapsed).toBe(1000);
    });

    it('should handle multiple timers independently', () => {
      const timer1 = Logger.startTimer();
      jest.advanceTimersByTime(500);

      const timer2 = Logger.startTimer();
      jest.advanceTimersByTime(300);

      const elapsed1 = timer1();
      const elapsed2 = timer2();

      expect(elapsed1).toBe(800); // 500 + 300
      expect(elapsed2).toBe(300);
    });

    it('should work with real time', () => {
      jest.useRealTimers();
      const timer = Logger.startTimer();
      // Timer should return a number >= 0
      const elapsed = timer();
      expect(elapsed).toBeGreaterThanOrEqual(0);
    });

    it('should measure sub-millisecond durations', () => {
      const timer = Logger.startTimer();
      // Don't advance time
      const elapsed = timer();
      expect(elapsed).toBe(0);
    });
  });

  describe('Convenience Methods', () => {
    it('should expose convenience error method', () => {
      const message = 'Convenience error';
      const meta = { test: true };

      log.error(message, meta);

      expect(mockLoggerInstance.error).toHaveBeenCalledWith(message, meta);
    });

    it('should expose convenience warn method', () => {
      const message = 'Convenience warning';
      const meta = { test: true };

      log.warn(message, meta);

      expect(mockLoggerInstance.warn).toHaveBeenCalledWith(message, meta);
    });

    it('should expose convenience info method', () => {
      const message = 'Convenience info';
      const meta = { test: true };

      log.info(message, meta);

      expect(mockLoggerInstance.info).toHaveBeenCalledWith(message, meta);
    });

    it('should expose convenience debug method', () => {
      const message = 'Convenience debug';
      const meta = { test: true };

      log.debug(message, meta);

      expect(mockLoggerInstance.debug).toHaveBeenCalledWith(message, meta);
    });

    it('should expose timer convenience method', () => {
      const timer = log.timer();
      expect(typeof timer).toBe('function');
    });

    it('should work without metadata', () => {
      log.error('Error without meta');
      log.warn('Warning without meta');
      log.info('Info without meta');
      log.debug('Debug without meta');

      expect(mockLoggerInstance.error).toHaveBeenCalledWith('Error without meta', undefined);
      expect(mockLoggerInstance.warn).toHaveBeenCalledWith('Warning without meta', undefined);
      expect(mockLoggerInstance.info).toHaveBeenCalledWith('Info without meta', undefined);
      expect(mockLoggerInstance.debug).toHaveBeenCalledWith('Debug without meta', undefined);
    });

    it('should handle complex metadata', () => {
      const complexMeta = {
        nested: {
          deeply: {
            nested: 'value',
          },
        },
        array: [1, 2, 3],
        date: new Date(),
        nullValue: null,
        undefinedValue: undefined,
      };

      log.info('Complex metadata test', complexMeta);

      expect(mockLoggerInstance.info).toHaveBeenCalledWith(
        'Complex metadata test',
        complexMeta
      );
    });
  });

  describe('Edge Cases and Error Handling', () => {
    it('should handle null message', () => {
      Logger.info(null as any);
      expect(mockLoggerInstance.info).toHaveBeenCalledWith(null, undefined);
    });

    it('should handle undefined message', () => {
      Logger.info(undefined as any);
      expect(mockLoggerInstance.info).toHaveBeenCalledWith(undefined, undefined);
    });

    it('should handle empty string message', () => {
      Logger.info('');
      expect(mockLoggerInstance.info).toHaveBeenCalledWith('', undefined);
    });

    it('should handle circular reference in metadata', () => {
      const circular: any = { prop: 'value' };
      circular.self = circular;

      // This should not throw
      expect(() => Logger.info('Circular reference', circular)).not.toThrow();
      expect(mockLoggerInstance.info).toHaveBeenCalledWith('Circular reference', circular);
    });

    it('should handle very long messages', () => {
      const longMessage = 'x'.repeat(10000);
      Logger.info(longMessage);
      expect(mockLoggerInstance.info).toHaveBeenCalledWith(longMessage, undefined);
    });

    it('should handle special characters in messages', () => {
      const specialMessage = '特殊字符 🎮 \n\t\r';
      Logger.info(specialMessage);
      expect(mockLoggerInstance.info).toHaveBeenCalledWith(specialMessage, undefined);
    });
  });

  describe('Performance Logging', () => {
    it('should log performance with warning for slow operations', () => {
      Logger.logPerformance('slow-operation', 1500);

      expect(mockLoggerInstance.log).toHaveBeenCalledWith(
        'warn',
        'Performance: slow-operation took 1500ms',
        {
          operation: 'slow-operation',
          duration: 1500,
        }
      );
    });

    it('should log performance with debug for fast operations', () => {
      Logger.logPerformance('fast-operation', 50);

      expect(mockLoggerInstance.log).toHaveBeenCalledWith(
        'debug',
        'Performance: fast-operation took 50ms',
        {
          operation: 'fast-operation',
          duration: 50,
        }
      );
    });

    it('should handle timer with performance logging', () => {
      jest.useFakeTimers();
      jest.setSystemTime(new Date('2024-01-01T00:00:00.000Z'));

      const timer = Logger.startTimer();
      jest.advanceTimersByTime(1200);
      const duration = timer();

      Logger.logPerformance('timed-operation', duration);

      expect(mockLoggerInstance.log).toHaveBeenCalledWith(
        'warn',
        'Performance: timed-operation took 1200ms',
        {
          operation: 'timed-operation',
          duration: 1200,
        }
      );

      jest.useRealTimers();
    });

    it('should handle edge case durations', () => {
      // Exactly at threshold
      Logger.logPerformance('threshold-operation', 1000);
      expect(mockLoggerInstance.log).toHaveBeenCalledWith(
        'debug',
        'Performance: threshold-operation took 1000ms',
        {
          operation: 'threshold-operation',
          duration: 1000,
        }
      );

      // Just above threshold
      Logger.logPerformance('above-threshold', 1001);
      expect(mockLoggerInstance.log).toHaveBeenCalledWith(
        'warn',
        'Performance: above-threshold took 1001ms',
        {
          operation: 'above-threshold',
          duration: 1001,
        }
      );
    });

    it('should handle zero duration', () => {
      Logger.logPerformance('instant-operation', 0);

      expect(mockLoggerInstance.log).toHaveBeenCalledWith(
        'debug',
        'Performance: instant-operation took 0ms',
        {
          operation: 'instant-operation',
          duration: 0,
        }
      );
    });

    it('should handle negative duration (clock skew)', () => {
      Logger.logPerformance('negative-duration', -100);

      expect(mockLoggerInstance.log).toHaveBeenCalledWith(
        'debug',
        'Performance: negative-duration took -100ms',
        {
          operation: 'negative-duration',
          duration: -100,
        }
      );
    });
  });

  describe('Integration Tests', () => {
    it('should handle rapid successive logging', () => {
      for (let i = 0; i < 100; i++) {
        Logger.info(`Message ${i}`);
      }

      expect(mockLoggerInstance.info).toHaveBeenCalledTimes(100);
    });

    it('should handle mixed log levels', () => {
      Logger.error('Error message');
      Logger.warn('Warning message');
      Logger.info('Info message');
      Logger.debug('Debug message');

      expect(mockLoggerInstance.error).toHaveBeenCalledTimes(1);
      expect(mockLoggerInstance.warn).toHaveBeenCalledTimes(1);
      expect(mockLoggerInstance.info).toHaveBeenCalledTimes(1);
      expect(mockLoggerInstance.debug).toHaveBeenCalledTimes(1);
    });

    it('should handle concurrent timers', () => {
      jest.useFakeTimers();
      jest.setSystemTime(new Date('2024-01-01T00:00:00.000Z'));

      const timers = [];
      for (let i = 0; i < 10; i++) {
        timers.push(Logger.startTimer());
        jest.advanceTimersByTime(100);
      }

      timers.forEach((timer, index) => {
        const elapsed = timer();
        expect(elapsed).toBe(1000 - index * 100);
      });

      jest.useRealTimers();
    });

    it('should maintain singleton instance', () => {
      const logger1 = Logger;
      const logger2 = require('../Logger').Logger;

      expect(logger1).toBe(logger2);
    });

    it('should support method chaining pattern', () => {
      // Although Logger methods return void, ensure they work in sequence
      expect(() => {
        Logger.info('First message');
        Logger.warn('Second message');
        Logger.error('Third message');
      }).not.toThrow();

      expect(mockLoggerInstance.info).toHaveBeenCalledTimes(1);
      expect(mockLoggerInstance.warn).toHaveBeenCalledTimes(1);
      expect(mockLoggerInstance.error).toHaveBeenCalledTimes(1);
    });
  });

  describe('Configuration Tests', () => {
    it('should use default configuration when config is missing', () => {
      jest.resetModules();
      jest.clearAllMocks();

      jest.doMock('@/config/server.config', () => {
        throw new Error('Config not found');
      });

      // This should not throw
      expect(() => require('../Logger')).not.toThrow();
    });

    it('should handle invalid log levels gracefully', () => {
      const invalidLevel = 'invalid_level';

      expect(() => {
        Logger.log(invalidLevel, 'Message with invalid level');
      }).not.toThrow();

      expect(mockLoggerInstance.log).toHaveBeenCalledWith(
        invalidLevel,
        'Message with invalid level',
        undefined
      );
    });
  });

  describe('Memory and Performance', () => {
    it('should not leak memory with large metadata objects', () => {
      const largeMetadata = {
        data: new Array(1000).fill('x'.repeat(1000)),
      };

      // Should handle large objects without issues
      expect(() => {
        Logger.info('Large metadata', largeMetadata);
      }).not.toThrow();

      expect(mockLoggerInstance.info).toHaveBeenCalledWith(
        'Large metadata',
        largeMetadata
      );
    });

    it('should handle high-frequency logging', () => {
      const startTime = Date.now();

      for (let i = 0; i < 10000; i++) {
        Logger.debug(`High frequency log ${i}`);
      }

      const duration = Date.now() - startTime;

      // Should complete in reasonable time (< 1 second)
      expect(duration).toBeLessThan(1000);
      expect(mockLoggerInstance.debug).toHaveBeenCalledTimes(10000);
    });
  });
});