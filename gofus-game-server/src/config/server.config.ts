import dotenv from 'dotenv';
import { z } from 'zod';

// Load environment variables only in development
// In production (Railway), env vars are injected directly
if (process.env.NODE_ENV !== 'production') {
  dotenv.config();
}

// Environment configuration schema
const envSchema = z.object({
  // Server
  NODE_ENV: z.enum(['development', 'production', 'test']).default('development'),
  PORT: z.string().default('3001').transform(Number),
  GAME_SERVER_ID: z.string().default('gs-001'),
  REGION: z.string().default('us-east-1'),

  // Database
  DATABASE_URL: z.string().default('postgresql://localhost/test'),
  DIRECT_URL: z.string().optional(),

  // Redis (can use either REDIS_URL or individual vars)
  REDIS_URL: z.string().optional(),
  REDIS_HOST: z.string().default('localhost'),
  REDIS_PORT: z.string().default('6379').transform(Number),
  REDIS_PASSWORD: z.string().optional(),

  // Authentication
  JWT_SECRET: z.string().default('dev-secret-key'),
  API_URL: z.string().default('http://localhost:3000'),

  // Game Configuration
  MAX_PLAYERS_PER_MAP: z.string().default('50').transform(Number),
  TICK_RATE: z.string().default('20').transform(Number),
  SAVE_INTERVAL: z.string().default('300000').transform(Number),
  COMBAT_TIMEOUT: z.string().default('120000').transform(Number),
  MAP_INSTANCE_TIMEOUT: z.string().default('600000').transform(Number),
  AI_UPDATE_INTERVAL: z.string().default('100').transform(Number),

  // Performance
  MAX_CONNECTIONS: z.string().default('5000').transform(Number),
  WORKER_THREADS: z.string().default('4').transform(Number),
  USE_CLUSTERING: z.string().default('false').transform(val => val === 'true'),

  // Monitoring
  METRICS_PORT: z.string().default('9090').transform(Number),
  LOG_LEVEL: z.enum(['error', 'warn', 'info', 'debug']).default('info'),
  LOG_FILE: z.string().default('game-server.log'),

  // Security
  RATE_LIMIT_WINDOW: z.string().default('60000').transform(Number),
  RATE_LIMIT_MAX_REQUESTS: z.string().default('100').transform(Number),
  MAX_PACKET_SIZE: z.string().default('1024').transform(Number),

  // Development
  DEBUG: z.string().default('false').transform(val => val === 'true'),
  ENABLE_HOT_RELOAD: z.string().default('true').transform(val => val === 'true'),
});

// Parse and validate environment variables
const parseResult = envSchema.safeParse(process.env);

if (!parseResult.success) {
  console.error('❌ Invalid environment variables:');
  console.error(parseResult.error.format());
  process.exit(1);
}

export const config = parseResult.data;

// Log configuration on startup (without sensitive data)
console.log('🔧 Server Configuration:');
console.log('  NODE_ENV:', config.NODE_ENV);
console.log('  PORT:', config.PORT);
console.log('  GAME_SERVER_ID:', config.GAME_SERVER_ID);
console.log('  DATABASE_URL:', config.DATABASE_URL ? `${config.DATABASE_URL.substring(0, 20)}...` : 'NOT SET');
console.log('  REDIS_URL:', config.REDIS_URL ? `${config.REDIS_URL.substring(0, 15)}...` : 'NOT SET');
console.log('  REDIS_HOST:', config.REDIS_HOST);
console.log('  REDIS_PORT:', config.REDIS_PORT);

// Server configuration
export const serverConfig = {
  port: config.PORT,
  serverId: config.GAME_SERVER_ID,
  region: config.REGION,
  environment: config.NODE_ENV,

  // WebSocket configuration
  websocket: {
    cors: {
      origin: config.NODE_ENV === 'production'
        ? config.API_URL
        : '*',
      credentials: true,
    },
    pingTimeout: 60000,
    pingInterval: 25000,
    maxHttpBufferSize: config.MAX_PACKET_SIZE * 1024, // Convert to bytes
    transports: ['websocket', 'polling'] as any,
  },

  // Performance settings
  performance: {
    maxConnections: config.MAX_CONNECTIONS,
    workerThreads: config.WORKER_THREADS,
    useClustering: config.USE_CLUSTERING,
    tickRate: config.TICK_RATE,
  },

  // Security settings
  security: {
    rateLimitWindow: config.RATE_LIMIT_WINDOW,
    rateLimitMaxRequests: config.RATE_LIMIT_MAX_REQUESTS,
    jwtSecret: config.JWT_SECRET,
  },

  // Monitoring
  monitoring: {
    metricsPort: config.METRICS_PORT,
    logLevel: config.LOG_LEVEL,
    logFile: config.LOG_FILE,
  },
};

// Game configuration
export const gameConfig = {
  maps: {
    maxPlayersPerMap: config.MAX_PLAYERS_PER_MAP,
    instanceTimeout: config.MAP_INSTANCE_TIMEOUT,
  },

  combat: {
    timeout: config.COMBAT_TIMEOUT,
    maxTurnsPerBattle: 200,
    turnDuration: 30000, // 30 seconds per turn
  },

  ai: {
    updateInterval: config.AI_UPDATE_INTERVAL,
    maxAIPerTick: 100,
    difficultyLevels: {
      easy: 0.6,
      normal: 0.8,
      hard: 1.0,
      expert: 1.2,
      nightmare: 1.5,
    },
  },

  persistence: {
    saveInterval: config.SAVE_INTERVAL,
    batchSize: 100,
  },
};

// Parse Redis URL if provided
function parseRedisConfig() {
  if (config.REDIS_URL) {
    try {
      const url = new URL(config.REDIS_URL);
      const parsedConfig = {
        host: url.hostname,
        port: parseInt(url.port) || 6379,
        password: url.password || undefined,
      };
      console.log('✅ Parsed REDIS_URL:', { host: parsedConfig.host, port: parsedConfig.port, hasPassword: !!parsedConfig.password });
      return parsedConfig;
    } catch (error) {
      console.warn('⚠️ Failed to parse REDIS_URL, using individual vars:', error);
    }
  }

  const fallbackConfig = {
    host: config.REDIS_HOST,
    port: config.REDIS_PORT,
    password: config.REDIS_PASSWORD,
  };
  console.log('Using individual Redis vars:', { host: fallbackConfig.host, port: fallbackConfig.port, hasPassword: !!fallbackConfig.password });
  return fallbackConfig;
}

// Database configuration
export const databaseConfig = {
  postgresql: {
    connectionString: config.DATABASE_URL,
    directUrl: config.DIRECT_URL,
    poolSize: 20,
    idleTimeoutMillis: 30000,
  },

  redis: {
    ...parseRedisConfig(),
    retryStrategy: (times: number) => {
      const delay = Math.min(times * 50, 2000);
      return delay;
    },
  },
};

export default {
  server: serverConfig,
  game: gameConfig,
  database: databaseConfig,
};