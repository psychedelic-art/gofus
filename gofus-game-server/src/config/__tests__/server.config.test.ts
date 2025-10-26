import { z } from 'zod';

describe('server.config', () => {
  // Store original env
  const originalEnv = process.env;

  beforeEach(() => {
    // Clear module cache to get fresh config
    jest.resetModules();
    // Reset environment
    process.env = { ...originalEnv };
  });

  afterEach(() => {
    // Restore original env
    process.env = originalEnv;
  });

  describe('Environment Variable Parsing', () => {
    it('should parse environment variables with values from .env or defaults', () => {
      // Since .env file exists, values come from there or defaults
      const { config } = require('../server.config');

      // Test that all expected properties exist and have correct types
      expect(config.NODE_ENV).toBeDefined();
      expect(['development', 'production', 'test']).toContain(config.NODE_ENV);
      expect(typeof config.PORT).toBe('number');
      expect(typeof config.GAME_SERVER_ID).toBe('string');
      expect(typeof config.REGION).toBe('string');
      expect(typeof config.DATABASE_URL).toBe('string');
      expect(typeof config.REDIS_HOST).toBe('string');
      expect(typeof config.REDIS_PORT).toBe('number');
      expect(typeof config.JWT_SECRET).toBe('string');
      expect(typeof config.API_URL).toBe('string');
      expect(typeof config.MAX_PLAYERS_PER_MAP).toBe('number');
      expect(typeof config.TICK_RATE).toBe('number');
      expect(typeof config.SAVE_INTERVAL).toBe('number');
      expect(typeof config.COMBAT_TIMEOUT).toBe('number');
      expect(typeof config.MAP_INSTANCE_TIMEOUT).toBe('number');
      expect(typeof config.AI_UPDATE_INTERVAL).toBe('number');
      expect(typeof config.MAX_CONNECTIONS).toBe('number');
      expect(typeof config.WORKER_THREADS).toBe('number');
      expect(typeof config.USE_CLUSTERING).toBe('boolean');
      expect(typeof config.METRICS_PORT).toBe('number');
      expect(['error', 'warn', 'info', 'debug']).toContain(config.LOG_LEVEL);
      expect(typeof config.LOG_FILE).toBe('string');
      expect(typeof config.RATE_LIMIT_WINDOW).toBe('number');
      expect(typeof config.RATE_LIMIT_MAX_REQUESTS).toBe('number');
      expect(typeof config.MAX_PACKET_SIZE).toBe('number');
      expect(typeof config.DEBUG).toBe('boolean');
      expect(typeof config.ENABLE_HOT_RELOAD).toBe('boolean');
    });

    it('should parse custom environment variables', () => {
      process.env.NODE_ENV = 'production';
      process.env.PORT = '8080';
      process.env.GAME_SERVER_ID = 'gs-prod-001';
      process.env.REGION = 'eu-west-1';
      process.env.DATABASE_URL = 'postgresql://prod-db/game';
      process.env.REDIS_HOST = 'redis.prod.com';
      process.env.REDIS_PORT = '6380';
      process.env.REDIS_PASSWORD = 'secret-password';
      process.env.JWT_SECRET = 'prod-secret';
      process.env.MAX_PLAYERS_PER_MAP = '100';
      process.env.TICK_RATE = '30';
      process.env.USE_CLUSTERING = 'true';
      process.env.DEBUG = 'true';

      const { config } = require('../server.config');

      expect(config.NODE_ENV).toBe('production');
      expect(config.PORT).toBe(8080);
      expect(config.GAME_SERVER_ID).toBe('gs-prod-001');
      expect(config.REGION).toBe('eu-west-1');
      expect(config.DATABASE_URL).toBe('postgresql://prod-db/game');
      expect(config.REDIS_HOST).toBe('redis.prod.com');
      expect(config.REDIS_PORT).toBe(6380);
      expect(config.REDIS_PASSWORD).toBe('secret-password');
      expect(config.JWT_SECRET).toBe('prod-secret');
      expect(config.MAX_PLAYERS_PER_MAP).toBe(100);
      expect(config.TICK_RATE).toBe(30);
      expect(config.USE_CLUSTERING).toBe(true);
      expect(config.DEBUG).toBe(true);
    });

    it('should transform string numbers to numbers', () => {
      process.env.PORT = '5000';
      process.env.REDIS_PORT = '7000';
      process.env.MAX_CONNECTIONS = '10000';
      process.env.WORKER_THREADS = '8';

      const { config } = require('../server.config');

      expect(typeof config.PORT).toBe('number');
      expect(typeof config.REDIS_PORT).toBe('number');
      expect(typeof config.MAX_CONNECTIONS).toBe('number');
      expect(typeof config.WORKER_THREADS).toBe('number');
      expect(config.PORT).toBe(5000);
      expect(config.REDIS_PORT).toBe(7000);
    });

    it('should transform boolean strings to booleans', () => {
      process.env.USE_CLUSTERING = 'true';
      process.env.DEBUG = 'false';
      process.env.ENABLE_HOT_RELOAD = 'false';

      const { config } = require('../server.config');

      expect(typeof config.USE_CLUSTERING).toBe('boolean');
      expect(typeof config.DEBUG).toBe('boolean');
      expect(typeof config.ENABLE_HOT_RELOAD).toBe('boolean');
      expect(config.USE_CLUSTERING).toBe(true);
      expect(config.DEBUG).toBe(false);
      expect(config.ENABLE_HOT_RELOAD).toBe(false);
    });

    it('should handle optional environment variables', () => {
      process.env.REDIS_PASSWORD = 'optional-password';
      process.env.DIRECT_URL = 'postgresql://direct-connection';

      const { config } = require('../server.config');

      expect(config.REDIS_PASSWORD).toBe('optional-password');
      expect(config.DIRECT_URL).toBe('postgresql://direct-connection');
    });

    it('should handle optional variables', () => {
      process.env = {};

      const { config } = require('../server.config');

      // Optional variables may be undefined or have default values from .env
      // Just check they exist in config when provided
      if (config.REDIS_PASSWORD !== undefined) {
        expect(typeof config.REDIS_PASSWORD).toBe('string');
      }
      // DIRECT_URL is truly optional
      expect(config.DIRECT_URL === undefined || typeof config.DIRECT_URL === 'string').toBe(true);
    });

    it('should validate NODE_ENV enum values', () => {
      const exitSpy = jest.spyOn(process, 'exit').mockImplementation(((code: number) => {
        throw new Error('Process exited');
      }) as any);
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      process.env.NODE_ENV = 'invalid-env';

      // This will cause the config to exit the process
      expect(() => require('../server.config')).toThrow('Process exited');

      expect(consoleErrorSpy).toHaveBeenCalledWith('❌ Invalid environment variables:');
      expect(exitSpy).toHaveBeenCalledWith(1);

      exitSpy.mockRestore();
      consoleErrorSpy.mockRestore();
    });

    it('should validate LOG_LEVEL enum values', () => {
      const exitSpy = jest.spyOn(process, 'exit').mockImplementation(((code: number) => {
        throw new Error('Process exited');
      }) as any);
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

      process.env.LOG_LEVEL = 'invalid-level';

      expect(() => require('../server.config')).toThrow('Process exited');

      expect(consoleErrorSpy).toHaveBeenCalledWith('❌ Invalid environment variables:');
      expect(exitSpy).toHaveBeenCalledWith(1);

      exitSpy.mockRestore();
      consoleErrorSpy.mockRestore();
    });
  });

  describe('Server Configuration Object', () => {
    beforeEach(() => {
      process.env = { NODE_ENV: 'development' };
    });

    it('should export serverConfig with correct structure', () => {
      const { serverConfig } = require('../server.config');

      expect(serverConfig).toHaveProperty('port');
      expect(serverConfig).toHaveProperty('serverId');
      expect(serverConfig).toHaveProperty('region');
      expect(serverConfig).toHaveProperty('environment');
      expect(serverConfig).toHaveProperty('websocket');
      expect(serverConfig).toHaveProperty('performance');
      expect(serverConfig).toHaveProperty('security');
      expect(serverConfig).toHaveProperty('monitoring');
    });

    it('should configure WebSocket correctly for development', () => {
      process.env.NODE_ENV = 'development';
      process.env.API_URL = 'http://localhost:3000';

      const { serverConfig } = require('../server.config');

      expect(serverConfig.websocket.cors.origin).toBe('*');
      expect(serverConfig.websocket.cors.credentials).toBe(true);
      expect(serverConfig.websocket.pingTimeout).toBe(60000);
      expect(serverConfig.websocket.pingInterval).toBe(25000);
      expect(serverConfig.websocket.transports).toEqual(['websocket', 'polling']);
    });

    it('should configure WebSocket correctly for production', () => {
      process.env.NODE_ENV = 'production';
      process.env.API_URL = 'https://api.example.com';

      const { serverConfig } = require('../server.config');

      expect(serverConfig.websocket.cors.origin).toBe('https://api.example.com');
      expect(serverConfig.websocket.cors.credentials).toBe(true);
    });

    it('should calculate maxHttpBufferSize from MAX_PACKET_SIZE', () => {
      process.env.MAX_PACKET_SIZE = '2048';

      const { serverConfig } = require('../server.config');

      expect(serverConfig.websocket.maxHttpBufferSize).toBe(2048 * 1024);
    });

    it('should configure performance settings', () => {
      process.env.MAX_CONNECTIONS = '10000';
      process.env.WORKER_THREADS = '8';
      process.env.USE_CLUSTERING = 'true';
      process.env.TICK_RATE = '30';

      const { serverConfig } = require('../server.config');

      expect(serverConfig.performance.maxConnections).toBe(10000);
      expect(serverConfig.performance.workerThreads).toBe(8);
      expect(serverConfig.performance.useClustering).toBe(true);
      expect(serverConfig.performance.tickRate).toBe(30);
    });

    it('should configure security settings', () => {
      process.env.RATE_LIMIT_WINDOW = '120000';
      process.env.RATE_LIMIT_MAX_REQUESTS = '200';
      process.env.JWT_SECRET = 'test-secret';

      const { serverConfig } = require('../server.config');

      expect(serverConfig.security.rateLimitWindow).toBe(120000);
      expect(serverConfig.security.rateLimitMaxRequests).toBe(200);
      expect(serverConfig.security.jwtSecret).toBe('test-secret');
    });

    it('should configure monitoring settings', () => {
      process.env.METRICS_PORT = '9999';
      process.env.LOG_LEVEL = 'debug';
      process.env.LOG_FILE = 'custom.log';

      const { serverConfig } = require('../server.config');

      expect(serverConfig.monitoring.metricsPort).toBe(9999);
      expect(serverConfig.monitoring.logLevel).toBe('debug');
      expect(serverConfig.monitoring.logFile).toBe('custom.log');
    });
  });

  describe('Game Configuration Object', () => {
    it('should export gameConfig with correct structure', () => {
      const { gameConfig } = require('../server.config');

      expect(gameConfig).toHaveProperty('maps');
      expect(gameConfig).toHaveProperty('combat');
      expect(gameConfig).toHaveProperty('ai');
      expect(gameConfig).toHaveProperty('persistence');
    });

    it('should configure maps settings', () => {
      process.env.MAX_PLAYERS_PER_MAP = '75';
      process.env.MAP_INSTANCE_TIMEOUT = '900000';

      const { gameConfig } = require('../server.config');

      expect(gameConfig.maps.maxPlayersPerMap).toBe(75);
      expect(gameConfig.maps.instanceTimeout).toBe(900000);
    });

    it('should configure combat settings with defaults', () => {
      const { gameConfig } = require('../server.config');

      expect(gameConfig.combat.timeout).toBe(120000);
      expect(gameConfig.combat.maxTurnsPerBattle).toBe(200);
      expect(gameConfig.combat.turnDuration).toBe(30000);
    });

    it('should configure AI settings', () => {
      process.env.AI_UPDATE_INTERVAL = '150';

      const { gameConfig } = require('../server.config');

      expect(gameConfig.ai.updateInterval).toBe(150);
      expect(gameConfig.ai.maxAIPerTick).toBe(100);
      expect(gameConfig.ai.difficultyLevels).toEqual({
        easy: 0.6,
        normal: 0.8,
        hard: 1.0,
        expert: 1.2,
        nightmare: 1.5,
      });
    });

    it('should configure persistence settings', () => {
      process.env.SAVE_INTERVAL = '600000';

      const { gameConfig } = require('../server.config');

      expect(gameConfig.persistence.saveInterval).toBe(600000);
      expect(gameConfig.persistence.batchSize).toBe(100);
    });
  });

  describe('Database Configuration Object', () => {
    it('should export databaseConfig with correct structure', () => {
      const { databaseConfig } = require('../server.config');

      expect(databaseConfig).toHaveProperty('postgresql');
      expect(databaseConfig).toHaveProperty('redis');
    });

    it('should configure PostgreSQL settings', () => {
      process.env.DATABASE_URL = 'postgresql://user:pass@host:5432/db';
      process.env.DIRECT_URL = 'postgresql://direct-url';

      const { databaseConfig } = require('../server.config');

      expect(databaseConfig.postgresql.connectionString).toBe('postgresql://user:pass@host:5432/db');
      expect(databaseConfig.postgresql.directUrl).toBe('postgresql://direct-url');
      expect(databaseConfig.postgresql.poolSize).toBe(20);
      expect(databaseConfig.postgresql.idleTimeoutMillis).toBe(30000);
    });

    it('should configure Redis settings', () => {
      process.env.REDIS_HOST = 'redis.example.com';
      process.env.REDIS_PORT = '7000';
      process.env.REDIS_PASSWORD = 'redis-pass';

      const { databaseConfig } = require('../server.config');

      expect(databaseConfig.redis.host).toBe('redis.example.com');
      expect(databaseConfig.redis.port).toBe(7000);
      expect(databaseConfig.redis.password).toBe('redis-pass');
      expect(typeof databaseConfig.redis.retryStrategy).toBe('function');
    });

    it('should have working retry strategy', () => {
      const { databaseConfig } = require('../server.config');

      const retryStrategy = databaseConfig.redis.retryStrategy;

      expect(retryStrategy(1)).toBe(50);
      expect(retryStrategy(10)).toBe(500);
      expect(retryStrategy(50)).toBe(2000); // Should cap at 2000
      expect(retryStrategy(100)).toBe(2000); // Should cap at 2000
    });
  });

  describe('Default Export', () => {
    it('should export default object with all configs', () => {
      const defaultExport = require('../server.config').default;

      expect(defaultExport).toHaveProperty('server');
      expect(defaultExport).toHaveProperty('game');
      expect(defaultExport).toHaveProperty('database');
    });

    it('should have all nested properties in default export', () => {
      const defaultExport = require('../server.config').default;

      expect(defaultExport.server).toHaveProperty('port');
      expect(defaultExport.server).toHaveProperty('websocket');
      expect(defaultExport.game).toHaveProperty('maps');
      expect(defaultExport.game).toHaveProperty('combat');
      expect(defaultExport.database).toHaveProperty('postgresql');
      expect(defaultExport.database).toHaveProperty('redis');
    });
  });

  describe('Edge Cases', () => {
    it('should handle empty string for boolean values as false', () => {
      process.env.USE_CLUSTERING = '';
      process.env.DEBUG = '';

      const { config } = require('../server.config');

      expect(config.USE_CLUSTERING).toBe(false);
      expect(config.DEBUG).toBe(false);
    });

    it('should handle "false" string as false boolean', () => {
      process.env.USE_CLUSTERING = 'false';
      process.env.DEBUG = 'false';

      const { config } = require('../server.config');

      expect(config.USE_CLUSTERING).toBe(false);
      expect(config.DEBUG).toBe(false);
    });

    it('should handle very large numbers', () => {
      process.env.MAX_CONNECTIONS = '999999';
      process.env.SAVE_INTERVAL = '999999999';

      const { config } = require('../server.config');

      expect(config.MAX_CONNECTIONS).toBe(999999);
      expect(config.SAVE_INTERVAL).toBe(999999999);
    });

    it('should handle minimum valid values', () => {
      process.env.PORT = '1';
      process.env.REDIS_PORT = '1';
      process.env.WORKER_THREADS = '1';

      const { config } = require('../server.config');

      expect(config.PORT).toBe(1);
      expect(config.REDIS_PORT).toBe(1);
      expect(config.WORKER_THREADS).toBe(1);
    });
  });

  describe('Integration Tests', () => {
    it('should work with realistic production configuration', () => {
      process.env.NODE_ENV = 'production';
      process.env.PORT = '3001';
      process.env.GAME_SERVER_ID = 'gs-prod-us-001';
      process.env.REGION = 'us-east-1';
      process.env.DATABASE_URL = 'postgresql://prod-user:prod-pass@db.example.com:5432/gofus';
      process.env.REDIS_HOST = 'redis.example.com';
      process.env.REDIS_PORT = '6379';
      process.env.REDIS_PASSWORD = 'prod-redis-pass';
      process.env.JWT_SECRET = 'production-jwt-secret-key';
      process.env.API_URL = 'https://api.gofus.com';
      process.env.USE_CLUSTERING = 'true';
      process.env.WORKER_THREADS = '8';
      process.env.MAX_CONNECTIONS = '10000';
      process.env.LOG_LEVEL = 'warn';

      const { config, serverConfig, gameConfig, databaseConfig } = require('../server.config');

      expect(config.NODE_ENV).toBe('production');
      expect(serverConfig.environment).toBe('production');
      expect(serverConfig.websocket.cors.origin).toBe('https://api.gofus.com');
      expect(serverConfig.performance.useClustering).toBe(true);
      expect(databaseConfig.redis.password).toBe('prod-redis-pass');
    });

    it('should work with minimal development configuration', () => {
      process.env = {};

      const { config, serverConfig } = require('../server.config');

      expect(config.NODE_ENV).toBe('development');
      expect(serverConfig.environment).toBe('development');
      expect(serverConfig.websocket.cors.origin).toBe('*');
    });
  });
});
