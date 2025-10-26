import { drizzle } from 'drizzle-orm/postgres-js';
import postgres from 'postgres';
import Redis from 'ioredis';
import { databaseConfig } from './server.config';

// PostgreSQL connection
const sql = postgres(databaseConfig.postgresql.connectionString, {
  max: databaseConfig.postgresql.poolSize,
  idle_timeout: databaseConfig.postgresql.idleTimeoutMillis,
  connect_timeout: 10,
});

// Drizzle ORM instance
export const db = drizzle(sql);

// Redis connections
export const redis = new Redis({
  host: databaseConfig.redis.host,
  port: databaseConfig.redis.port,
  password: databaseConfig.redis.password,
  retryStrategy: databaseConfig.redis.retryStrategy,
  lazyConnect: true,
});

// Redis pub/sub connections
export const redisPub = new Redis({
  host: databaseConfig.redis.host,
  port: databaseConfig.redis.port,
  password: databaseConfig.redis.password,
  lazyConnect: true,
});

export const redisSub = new Redis({
  host: databaseConfig.redis.host,
  port: databaseConfig.redis.port,
  password: databaseConfig.redis.password,
  lazyConnect: true,
});

// Redis key prefixes
export const REDIS_KEYS = {
  SESSION: 'session:',
  PLAYER: 'player:',
  MAP_INSTANCE: 'map:',
  BATTLE: 'battle:',
  CHAT_HISTORY: 'chat:',
  RATE_LIMIT: 'ratelimit:',
  CACHE: 'cache:',
  LOCK: 'lock:',
  QUEUE: 'queue:',
};

// Initialize database connections
export async function initializeDatabases(): Promise<void> {
  try {
    // Try to connect to Redis (optional for testing)
    try {
      await redis.connect();
      await redisPub.connect();
      await redisSub.connect();
      console.log('✅ Connected to Redis');
    } catch (redisError) {
      console.warn('⚠️ Redis connection failed, running without Redis:', redisError);
      // Continue without Redis for testing
    }

    // Try to test PostgreSQL connection (optional for testing)
    try {
      await sql`SELECT 1`;
      console.log('✅ Connected to PostgreSQL');
    } catch (pgError) {
      console.warn('⚠️ PostgreSQL connection failed, running without database:', pgError);
      // Continue without PostgreSQL for testing
    }
  } catch (error) {
    console.error('❌ Critical database error:', error);
    // Don't throw - allow server to start for testing
  }
}

// Cleanup database connections
export async function closeDatabases(): Promise<void> {
  try {
    await redis.quit();
    await redisPub.quit();
    await redisSub.quit();
    await sql.end();
    console.log('✅ Database connections closed');
  } catch (error) {
    console.error('❌ Error closing database connections:', error);
  }
}

// Redis helper functions
export const redisHelpers = {
  // Get with JSON parsing
  async getJSON<T>(key: string): Promise<T | null> {
    const value = await redis.get(key);
    if (!value) return null;
    try {
      return JSON.parse(value) as T;
    } catch {
      return null;
    }
  },

  // Set with JSON stringification
  async setJSON<T>(
    key: string,
    value: T,
    ttl?: number
  ): Promise<void> {
    const json = JSON.stringify(value);
    if (ttl) {
      await redis.setex(key, ttl, json);
    } else {
      await redis.set(key, json);
    }
  },

  // Acquire lock
  async acquireLock(
    key: string,
    ttl: number = 5000
  ): Promise<boolean> {
    const lockKey = `${REDIS_KEYS.LOCK}${key}`;
    const result = await redis.set(
      lockKey,
      Date.now(),
      'PX',
      ttl,
      'NX'
    );
    return result === 'OK';
  },

  // Release lock
  async releaseLock(key: string): Promise<void> {
    const lockKey = `${REDIS_KEYS.LOCK}${key}`;
    await redis.del(lockKey);
  },

  // Increment with expiry
  async incrementWithExpiry(
    key: string,
    ttl: number
  ): Promise<number> {
    const multi = redis.multi();
    multi.incr(key);
    multi.expire(key, ttl);
    const results = await multi.exec();
    return results?.[0]?.[1] as number || 0;
  },
};

export default {
  db,
  redis,
  redisPub,
  redisSub,
  initialize: initializeDatabases,
  close: closeDatabases,
  helpers: redisHelpers,
};