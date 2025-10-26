// Mock server.config first
jest.mock('../server.config', () => ({
  databaseConfig: {
    postgresql: {
      connectionString: 'postgresql://test:test@localhost:5432/testdb',
      directUrl: 'postgresql://direct-test',
      poolSize: 20,
      idleTimeoutMillis: 30000,
    },
    redis: {
      host: 'localhost',
      port: 6379,
      password: 'test-password',
      retryStrategy: (times: number) => Math.min(times * 50, 2000),
    },
  },
}));

// Setup mocks before requiring modules
const mockPostgresInstance: any = jest.fn().mockResolvedValue({ rows: [{ result: 1 }] });
mockPostgresInstance.end = jest.fn().mockResolvedValue(undefined);

const mockRedisInstance = {
  connect: jest.fn().mockResolvedValue(undefined),
  quit: jest.fn().mockResolvedValue(undefined),
  get: jest.fn(),
  set: jest.fn(),
  setex: jest.fn(),
  del: jest.fn(),
  incr: jest.fn(),
  expire: jest.fn(),
  multi: jest.fn(() => ({
    incr: jest.fn().mockReturnThis(),
    expire: jest.fn().mockReturnThis(),
    exec: jest.fn().mockResolvedValue([[null, 5], [null, 'OK']]),
  })),
  status: 'ready',
};

const mockDrizzleDb = { query: jest.fn() };

// Mock dependencies
let mockPostgres = jest.fn(() => mockPostgresInstance);
jest.mock('postgres', () => {
  const mock = jest.fn(() => mockPostgresInstance);
  // Store reference for testing
  (global as any).__mockPostgres = mock;
  return mock;
});

let mockDrizzle = jest.fn(() => mockDrizzleDb);
jest.mock('drizzle-orm/postgres-js', () => {
  const mock = { drizzle: jest.fn(() => mockDrizzleDb) };
  // Store reference for testing
  (global as any).__mockDrizzle = mock.drizzle;
  return mock;
});

let MockRedis = jest.fn(() => mockRedisInstance);
jest.mock('ioredis', () => {
  const mock = jest.fn(() => mockRedisInstance);
  // Store reference for testing
  (global as any).__mockRedis = mock;
  return mock;
});

describe('database.config', () => {
  let dbConfig: any;

  beforeAll(() => {
    // Clear module cache and import fresh
    jest.resetModules();
    // Import the module which will trigger all module-level code
    dbConfig = require('../database.config');

    // Get the stored mock references
    mockPostgres = (global as any).__mockPostgres;
    mockDrizzle = (global as any).__mockDrizzle;
    MockRedis = (global as any).__mockRedis;
  });

  beforeEach(() => {
    // Clear mock call history but not the implementations
    jest.clearAllMocks();

    // Reset mock function states
    mockRedisInstance.connect.mockClear();
    mockRedisInstance.quit.mockClear();
    mockRedisInstance.get.mockReset();
    mockRedisInstance.set.mockReset();
    mockRedisInstance.setex.mockReset();
    mockRedisInstance.del.mockReset();
    mockPostgresInstance.mockClear();
    mockPostgresInstance.end.mockClear();

    // Reset resolved values
    mockRedisInstance.connect.mockResolvedValue(undefined);
    mockRedisInstance.quit.mockResolvedValue(undefined);
    mockPostgresInstance.mockResolvedValue({ rows: [{ result: 1 }] });
    mockPostgresInstance.end.mockResolvedValue(undefined);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  describe('PostgreSQL Connection', () => {
    it('should create PostgreSQL connection with correct configuration', () => {
      // The postgres mock should have been called when the module was imported
      expect(mockPostgres).toHaveBeenCalledWith(
        'postgresql://test:test@localhost:5432/testdb',
        {
          max: 20,
          idle_timeout: 30000,
          connect_timeout: 10,
        }
      );
    });

    it('should create Drizzle ORM instance with postgres connection', () => {
      // The drizzle mock should have been called with the postgres instance
      expect(mockDrizzle).toHaveBeenCalledWith(mockPostgresInstance);
    });

    it('should export db instance', () => {
      expect(dbConfig.db).toBeDefined();
      expect(dbConfig.db).toBe(mockDrizzleDb);
      expect(dbConfig.default.db).toBe(mockDrizzleDb);
    });
  });

  describe('Redis Connections', () => {
    it('should create main Redis connection with correct configuration', () => {
      // Check that Redis was called with correct config for main instance
      expect(MockRedis).toHaveBeenCalledWith(
        expect.objectContaining({
          host: 'localhost',
          port: 6379,
          password: 'test-password',
          lazyConnect: true,
        })
      );
    });

    it('should create three Redis instances (main, pub, sub)', () => {
      // Three instances should be created
      expect(MockRedis).toHaveBeenCalledTimes(3);
    });

    it('should export redis, redisPub, and redisSub instances', () => {
      expect(dbConfig.redis).toBeDefined();
      expect(dbConfig.redisPub).toBeDefined();
      expect(dbConfig.redisSub).toBeDefined();
    });

    it('should configure Redis with retry strategy', () => {
      // Get the first call to Redis constructor
      const calls = (MockRedis as any).mock.calls;
      expect(calls.length).toBeGreaterThan(0);
      const callArgs = calls[0][0];
      expect(callArgs.retryStrategy).toBeDefined();
      expect(typeof callArgs.retryStrategy).toBe('function');

      // Test the retry strategy function
      const delay = callArgs.retryStrategy(3);
      expect(delay).toBe(150); // 3 * 50 = 150
    });

    it('should use lazyConnect for all Redis instances', () => {
      const calls = (MockRedis as any).mock.calls;
      calls.forEach((call: any) => {
        expect(call[0].lazyConnect).toBe(true);
      });
    });
  });

  describe('REDIS_KEYS Constants', () => {
    it('should export REDIS_KEYS with all prefixes', () => {
      expect(dbConfig.REDIS_KEYS).toBeDefined();
      expect(dbConfig.REDIS_KEYS.SESSION).toBe('session:');
      expect(dbConfig.REDIS_KEYS.PLAYER).toBe('player:');
      expect(dbConfig.REDIS_KEYS.MAP_INSTANCE).toBe('map:');
      expect(dbConfig.REDIS_KEYS.BATTLE).toBe('battle:');
      expect(dbConfig.REDIS_KEYS.CHAT_HISTORY).toBe('chat:');
      expect(dbConfig.REDIS_KEYS.RATE_LIMIT).toBe('ratelimit:');
      expect(dbConfig.REDIS_KEYS.CACHE).toBe('cache:');
      expect(dbConfig.REDIS_KEYS.LOCK).toBe('lock:');
      expect(dbConfig.REDIS_KEYS.QUEUE).toBe('queue:');
    });

    it('should have string values for all keys', () => {
      Object.values(dbConfig.REDIS_KEYS).forEach((value) => {
        expect(typeof value).toBe('string');
        expect((value as string).endsWith(':')).toBe(true);
      });
    });
  });

  describe('initializeDatabases()', () => {
    const consoleLogSpy = jest.spyOn(console, 'log').mockImplementation();
    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation();
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();

    beforeEach(() => {
      consoleLogSpy.mockClear();
      consoleWarnSpy.mockClear();
      consoleErrorSpy.mockClear();
    });

    it('should connect to all Redis instances successfully', async () => {
      const { initializeDatabases } = dbConfig;

      await initializeDatabases();

      expect(mockRedisInstance.connect).toHaveBeenCalledTimes(3);
      expect(consoleLogSpy).toHaveBeenCalledWith('✅ Connected to Redis');
    });

    it('should test PostgreSQL connection successfully', async () => {
      const { initializeDatabases } = dbConfig;

      await initializeDatabases();

      // The SQL template literal creates an array like ['SELECT 1']
      expect(mockPostgresInstance).toHaveBeenCalled();
      const callArgs = mockPostgresInstance.mock.calls[0];
      expect(callArgs[0][0]).toBe('SELECT 1');
      expect(consoleLogSpy).toHaveBeenCalledWith('✅ Connected to PostgreSQL');
    });

    it('should handle Redis connection failure gracefully', async () => {
      mockRedisInstance.connect.mockRejectedValueOnce(new Error('Redis connection failed'));

      const { initializeDatabases } = dbConfig;
      await expect(initializeDatabases()).resolves.not.toThrow();

      expect(consoleWarnSpy).toHaveBeenCalledWith(
        '⚠️ Redis connection failed, running without Redis:',
        expect.any(Error)
      );
    });

    it('should handle PostgreSQL connection failure gracefully', async () => {
      mockPostgresInstance.mockRejectedValueOnce(new Error('PostgreSQL connection failed'));

      const { initializeDatabases } = dbConfig;
      await expect(initializeDatabases()).resolves.not.toThrow();

      expect(consoleWarnSpy).toHaveBeenCalledWith(
        '⚠️ PostgreSQL connection failed, running without database:',
        expect.any(Error)
      );
    });

    it('should continue if Redis fails but PostgreSQL succeeds', async () => {
      mockRedisInstance.connect.mockRejectedValueOnce(new Error('Redis failed'));
      mockPostgresInstance.mockResolvedValueOnce({ rows: [{ result: 1 }] });

      const { initializeDatabases } = dbConfig;
      await expect(initializeDatabases()).resolves.not.toThrow();

      expect(consoleWarnSpy).toHaveBeenCalledWith(
        '⚠️ Redis connection failed, running without Redis:',
        expect.any(Error)
      );
      expect(consoleLogSpy).toHaveBeenCalledWith('✅ Connected to PostgreSQL');
    });

    it('should continue if PostgreSQL fails but Redis succeeds', async () => {
      mockPostgresInstance.mockRejectedValueOnce(new Error('PostgreSQL failed'));

      const { initializeDatabases } = dbConfig;
      await expect(initializeDatabases()).resolves.not.toThrow();

      expect(consoleLogSpy).toHaveBeenCalledWith('✅ Connected to Redis');
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        '⚠️ PostgreSQL connection failed, running without database:',
        expect.any(Error)
      );
    });

    it('should handle critical database errors gracefully', async () => {
      // This test was checking for a general catch block that doesn't exist
      // The function has specific try-catch blocks for Redis and PostgreSQL
      // Let's test that both failing doesn't crash
      mockRedisInstance.connect.mockRejectedValueOnce(new Error('Redis failed'));
      mockPostgresInstance.mockRejectedValueOnce(new Error('PostgreSQL failed'));

      const { initializeDatabases } = dbConfig;
      await expect(initializeDatabases()).resolves.not.toThrow();

      expect(consoleWarnSpy).toHaveBeenCalledTimes(2);
    });
  });

  describe('closeDatabases()', () => {
    const consoleLogSpy = jest.spyOn(console, 'log').mockImplementation();
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();

    beforeEach(() => {
      consoleLogSpy.mockClear();
      consoleErrorSpy.mockClear();
    });

    it('should close all Redis connections and PostgreSQL', async () => {
      const { closeDatabases } = dbConfig;

      await closeDatabases();

      expect(mockRedisInstance.quit).toHaveBeenCalledTimes(3);
      expect(mockPostgresInstance.end).toHaveBeenCalled();
      expect(consoleLogSpy).toHaveBeenCalledWith('✅ Database connections closed');
    });

    it('should handle Redis quit errors gracefully', async () => {
      mockRedisInstance.quit.mockRejectedValueOnce(new Error('Redis quit failed'));

      const { closeDatabases } = dbConfig;
      await closeDatabases();

      expect(consoleErrorSpy).toHaveBeenCalledWith(
        '❌ Error closing database connections:',
        expect.any(Error)
      );
    });

    it('should handle PostgreSQL close errors gracefully', async () => {
      mockPostgresInstance.end.mockRejectedValueOnce(new Error('PostgreSQL close failed'));

      const { closeDatabases } = dbConfig;
      await closeDatabases();

      expect(consoleErrorSpy).toHaveBeenCalledWith(
        '❌ Error closing database connections:',
        expect.any(Error)
      );
    });
  });

  describe('redisHelpers.getJSON()', () => {
    it('should get and parse JSON from Redis', async () => {
      const testData = { id: 1, name: 'Test' };
      mockRedisInstance.get.mockResolvedValue(JSON.stringify(testData));

      const { redisHelpers } = dbConfig;
      const result = await redisHelpers.getJSON('test-key');

      expect(mockRedisInstance.get).toHaveBeenCalledWith('test-key');
      expect(result).toEqual(testData);
    });

    it('should return null if key does not exist', async () => {
      mockRedisInstance.get.mockResolvedValue(null);

      const { redisHelpers } = dbConfig;
      const result = await redisHelpers.getJSON('non-existent');

      expect(result).toBeNull();
    });

    it('should return null for invalid JSON', async () => {
      mockRedisInstance.get.mockResolvedValue('invalid json {');

      const { redisHelpers } = dbConfig;
      const result = await redisHelpers.getJSON('bad-json');

      expect(result).toBeNull();
    });

    it('should handle complex nested objects', async () => {
      const complexData = {
        user: { id: 1, name: 'Test' },
        items: [{ id: 1 }, { id: 2 }],
        metadata: { timestamp: Date.now() },
      };
      mockRedisInstance.get.mockResolvedValue(JSON.stringify(complexData));

      const { redisHelpers } = dbConfig;
      const result = await redisHelpers.getJSON('complex-key');

      expect(result).toEqual(complexData);
    });
  });

  describe('redisHelpers.setJSON()', () => {
    it('should set JSON in Redis without TTL', async () => {
      const testData = { id: 1, name: 'Test' };

      const { redisHelpers } = dbConfig;
      await redisHelpers.setJSON('test-key', testData);

      expect(mockRedisInstance.set).toHaveBeenCalledWith(
        'test-key',
        JSON.stringify(testData)
      );
    });

    it('should set JSON in Redis with TTL', async () => {
      const testData = { id: 1, name: 'Test' };

      const { redisHelpers } = dbConfig;
      await redisHelpers.setJSON('test-key', testData, 3600);

      expect(mockRedisInstance.setex).toHaveBeenCalledWith(
        'test-key',
        3600,
        JSON.stringify(testData)
      );
    });

    it('should handle complex objects', async () => {
      const complexData = {
        nested: { deep: { value: 'test' } },
        array: [1, 2, 3],
      };

      const { redisHelpers } = dbConfig;
      await redisHelpers.setJSON('complex-key', complexData);

      expect(mockRedisInstance.set).toHaveBeenCalledWith(
        'complex-key',
        JSON.stringify(complexData)
      );
    });

    it('should handle arrays', async () => {
      const arrayData = [1, 2, 3, { id: 4 }];

      const { redisHelpers } = dbConfig;
      await redisHelpers.setJSON('array-key', arrayData);

      expect(mockRedisInstance.set).toHaveBeenCalledWith(
        'array-key',
        JSON.stringify(arrayData)
      );
    });
  });

  describe('redisHelpers.acquireLock()', () => {
    it('should acquire lock successfully', async () => {
      mockRedisInstance.set.mockResolvedValue('OK');

      const { redisHelpers } = dbConfig;
      const result = await redisHelpers.acquireLock('test-resource');

      expect(mockRedisInstance.set).toHaveBeenCalledWith(
        'lock:test-resource',
        expect.any(Number),
        'PX',
        5000,
        'NX'
      );
      expect(result).toBe(true);
    });

    it('should fail to acquire lock if already locked', async () => {
      mockRedisInstance.set.mockResolvedValue(null);

      const { redisHelpers } = dbConfig;
      const result = await redisHelpers.acquireLock('test-resource');

      expect(result).toBe(false);
    });

    it('should use custom TTL', async () => {
      mockRedisInstance.set.mockResolvedValue('OK');

      const { redisHelpers } = dbConfig;
      await redisHelpers.acquireLock('test-resource', 10000);

      expect(mockRedisInstance.set).toHaveBeenCalledWith(
        'lock:test-resource',
        expect.any(Number),
        'PX',
        10000,
        'NX'
      );
    });

    it('should use default TTL of 5000ms', async () => {
      mockRedisInstance.set.mockResolvedValue('OK');

      const { redisHelpers } = dbConfig;
      await redisHelpers.acquireLock('test-resource');

      expect(mockRedisInstance.set).toHaveBeenCalledWith(
        'lock:test-resource',
        expect.any(Number),
        'PX',
        5000,
        'NX'
      );
    });
  });

  describe('redisHelpers.releaseLock()', () => {
    it('should release lock successfully', async () => {
      mockRedisInstance.del.mockResolvedValue(1);

      const { redisHelpers } = dbConfig;
      await redisHelpers.releaseLock('test-resource');

      expect(mockRedisInstance.del).toHaveBeenCalledWith('lock:test-resource');
    });

    it('should handle releasing non-existent lock', async () => {
      mockRedisInstance.del.mockResolvedValue(0);

      const { redisHelpers } = dbConfig;
      await expect(redisHelpers.releaseLock('non-existent')).resolves.not.toThrow();

      expect(mockRedisInstance.del).toHaveBeenCalledWith('lock:non-existent');
    });
  });

  describe('redisHelpers.incrementWithExpiry()', () => {
    it('should increment key with expiry', async () => {
      const { redisHelpers } = dbConfig;
      const result = await redisHelpers.incrementWithExpiry('counter', 60);

      expect(mockRedisInstance.multi).toHaveBeenCalled();
      expect(result).toBe(5);
    });

    it('should return 0 if exec returns null', async () => {
      const mockMulti = {
        incr: jest.fn().mockReturnThis(),
        expire: jest.fn().mockReturnThis(),
        exec: jest.fn().mockResolvedValue(null),
      };
      mockRedisInstance.multi.mockReturnValue(mockMulti);

      const { redisHelpers } = dbConfig;
      const result = await redisHelpers.incrementWithExpiry('counter', 60);

      expect(result).toBe(0);
    });

    it('should handle first increment correctly', async () => {
      const mockMulti = {
        incr: jest.fn().mockReturnThis(),
        expire: jest.fn().mockReturnThis(),
        exec: jest.fn().mockResolvedValue([[null, 1], [null, 'OK']]),
      };
      mockRedisInstance.multi.mockReturnValue(mockMulti);

      const { redisHelpers } = dbConfig;
      const result = await redisHelpers.incrementWithExpiry('new-counter', 120);

      expect(mockMulti.incr).toHaveBeenCalledWith('new-counter');
      expect(mockMulti.expire).toHaveBeenCalledWith('new-counter', 120);
      expect(result).toBe(1);
    });
  });

  describe('Default Export', () => {
    it('should export default object with all components', () => {
      expect(dbConfig.default).toBeDefined();
      expect(dbConfig.default.db).toBeDefined();
      expect(dbConfig.default.redis).toBeDefined();
      expect(dbConfig.default.redisPub).toBeDefined();
      expect(dbConfig.default.redisSub).toBeDefined();
      expect(dbConfig.default.initialize).toBe(dbConfig.initializeDatabases);
      expect(dbConfig.default.close).toBe(dbConfig.closeDatabases);
      expect(dbConfig.default.helpers).toBe(dbConfig.redisHelpers);
    });

    it('should have all helper functions in default export', () => {
      const { helpers } = dbConfig.default;
      expect(helpers.getJSON).toBeDefined();
      expect(helpers.setJSON).toBeDefined();
      expect(helpers.acquireLock).toBeDefined();
      expect(helpers.releaseLock).toBeDefined();
      expect(helpers.incrementWithExpiry).toBeDefined();
    });
  });

  describe('Integration Tests', () => {
    it('should initialize and close databases successfully', async () => {
      const { initializeDatabases, closeDatabases } = dbConfig;

      await initializeDatabases();
      await closeDatabases();

      expect(mockRedisInstance.connect).toHaveBeenCalled();
      expect(mockRedisInstance.quit).toHaveBeenCalled();
    });

    it('should handle lock acquire and release workflow', async () => {
      mockRedisInstance.set.mockResolvedValue('OK');
      mockRedisInstance.del.mockResolvedValue(1);

      const { redisHelpers } = dbConfig;
      const acquired = await redisHelpers.acquireLock('workflow-test');
      expect(acquired).toBe(true);

      await redisHelpers.releaseLock('workflow-test');
      expect(mockRedisInstance.del).toHaveBeenCalledWith('lock:workflow-test');
    });

    it('should handle JSON get-set workflow', async () => {
      const testData = { workflow: 'test', value: 123 };

      const { redisHelpers } = dbConfig;
      await redisHelpers.setJSON('workflow-key', testData, 300);

      mockRedisInstance.get.mockResolvedValue(JSON.stringify(testData));
      const retrieved = await redisHelpers.getJSON('workflow-key');

      expect(retrieved).toEqual(testData);
    });
  });
});