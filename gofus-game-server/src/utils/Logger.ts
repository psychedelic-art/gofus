import winston from 'winston';
import path from 'path';
import { serverConfig } from '@/config/server.config';

const { combine, timestamp, printf, colorize, errors } = winston.format;

// Custom log format
const logFormat = printf(({ level, message, timestamp, stack, ...metadata }) => {
  let msg = `${timestamp} [${level}]: ${message}`;

  if (Object.keys(metadata).length > 0) {
    msg += ` ${JSON.stringify(metadata)}`;
  }

  if (stack) {
    msg += `\n${stack}`;
  }

  return msg;
});

// Create logger instance
class GameLogger {
  private logger: winston.Logger;

  constructor() {
    this.logger = winston.createLogger({
      level: serverConfig.monitoring.logLevel,
      format: combine(
        errors({ stack: true }),
        timestamp({ format: 'YYYY-MM-DD HH:mm:ss' }),
        logFormat
      ),
      transports: this.getTransports(),
    });
  }

  private getTransports(): winston.transport[] {
    const transports: winston.transport[] = [];

    // Console transport (always available)
    transports.push(
      new winston.transports.Console({
        format: combine(
          colorize(),
          timestamp({ format: 'HH:mm:ss' }),
          logFormat
        ),
      })
    );

    // File transport only in development or when LOG_TO_FILE is explicitly set
    // In production containers (Railway, Docker), logs are captured from stdout/stderr
    if (serverConfig.environment !== 'production' && process.env.LOG_TO_FILE === 'true') {
      try {
        const fs = require('fs');
        const logsDir = path.join(process.cwd(), 'logs');

        // Only add file transports if we can write to the directory
        if (!fs.existsSync(logsDir)) {
          fs.mkdirSync(logsDir, { recursive: true });
        }

        transports.push(
          new winston.transports.File({
            filename: path.join(logsDir, 'error.log'),
            level: 'error',
            maxsize: 10485760, // 10MB
            maxFiles: 5,
          })
        );

        transports.push(
          new winston.transports.File({
            filename: path.join(logsDir, serverConfig.monitoring.logFile),
            maxsize: 10485760, // 10MB
            maxFiles: 10,
          })
        );
      } catch (error) {
        // If we can't create log files, just use console
        console.warn('Could not create log files, using console only:', error);
      }
    }

    return transports;
  }

  // Log methods
  error(message: string, meta?: any): void {
    this.logger.error(message, meta);
  }

  warn(message: string, meta?: any): void {
    this.logger.warn(message, meta);
  }

  info(message: string, meta?: any): void {
    this.logger.info(message, meta);
  }

  debug(message: string, meta?: any): void {
    this.logger.debug(message, meta);
  }

  // Generic log method with custom level
  log(level: string, message: string, meta?: any): void {
    this.logger.log(level, message, meta);
  }

  // Specialized logging methods
  logPlayerAction(playerId: string, action: string, data?: any): void {
    this.logger.info(`Player ${playerId} performed ${action}`, {
      playerId,
      action,
      details: data,
    });
  }

  logGameEvent(event: string, data?: any): void {
    this.logger.info(`Game Event: ${event}`, data);
  }

  logCombat(battleId: string, event: string, data?: any): void {
    this.logger.info(`Combat event: ${event}`, {
      battleId,
      event,
      ...data,
    });
  }

  logAI(entityId: string, decision: string, data?: any): void {
    this.logger.debug(`AI decision: ${decision}`, {
      entityId,
      decision,
      ...data,
    });
  }

  logPerformance(operation: string, duration: number, data?: any): void {
    const level = duration > 1000 ? 'warn' : 'debug';
    this.logger.log(level, `Performance: ${operation} took ${duration}ms`, {
      operation,
      duration,
      ...data,
    });
  }

  logNetwork(event: string, socketId: string, data?: any): void {
    this.logger.debug(`Network event: ${event}`, {
      socketId,
      event,
      ...data,
    });
  }

  logError(error: Error, context?: string): void {
    this.logger.error(`Error${context ? ` in ${context}` : ''}: ${error.message}`, {
      stack: error.stack,
      context,
    });
  }

  // Performance timing helper
  startTimer(): () => number {
    const start = Date.now();
    return () => Date.now() - start;
  }
}

// Export singleton instance
export const Logger = new GameLogger();

// Export convenience methods
export const log = {
  error: (message: string, meta?: any) => Logger.error(message, meta),
  warn: (message: string, meta?: any) => Logger.warn(message, meta),
  info: (message: string, meta?: any) => Logger.info(message, meta),
  debug: (message: string, meta?: any) => Logger.debug(message, meta),
  player: (playerId: string, action: string, data?: any) =>
    Logger.logPlayerAction(playerId, action, data),
  combat: (battleId: string, event: string, data?: any) =>
    Logger.logCombat(battleId, event, data),
  ai: (entityId: string, decision: string, data?: any) =>
    Logger.logAI(entityId, decision, data),
  performance: (operation: string, duration: number, data?: any) =>
    Logger.logPerformance(operation, duration, data),
  network: (event: string, socketId: string, data?: any) =>
    Logger.logNetwork(event, socketId, data),
  exception: (error: Error, context?: string) =>
    Logger.logError(error, context),
  timer: () => Logger.startTimer(),
};

export default Logger;