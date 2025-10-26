import { createServer, Server as HTTPServer } from 'http';
import { SocketHandler } from '@/network/SocketHandler';
import { PlayerManager } from '@/core/PlayerManager';
import { WorldState } from '@/core/WorldState';
import { MapManager } from '@/managers/MapManager';
import { CombatManager } from '@/managers/CombatManager';
import { MovementManager } from '@/managers/MovementManager';
import { ChatManager } from '@/managers/ChatManager';
import { AIManager } from '@/managers/AIManager';
import { initializeDatabases, closeDatabases } from '@/config/database.config';
import { serverConfig, gameConfig } from '@/config/server.config';
import { log } from '@/utils/Logger';

export class GameServer {
  private httpServer: HTTPServer;
  private socketHandler: SocketHandler;
  private playerManager: PlayerManager;
  private worldState: WorldState;
  private mapManager: MapManager;
  private combatManager: CombatManager;
  private movementManager: MovementManager;
  private chatManager: ChatManager;
  private aiManager: AIManager;

  private isRunning: boolean = false;
  private tickInterval: NodeJS.Timeout | null = null;
  private saveInterval: NodeJS.Timeout | null = null;
  private lastTickTime: number = 0;
  private tickCount: number = 0;

  constructor() {
    this.httpServer = createServer();
    this.socketHandler = new SocketHandler(this.httpServer);
    this.playerManager = new PlayerManager();
    this.worldState = new WorldState();
    this.mapManager = new MapManager();
    this.combatManager = new CombatManager();
    this.movementManager = new MovementManager();
    this.chatManager = new ChatManager(this.socketHandler);
    this.aiManager = new AIManager();
  }

  public async start(): Promise<void> {
    try {
      log.info('Starting GOFUS Game Server...');

      // Initialize databases
      await initializeDatabases();

      // Initialize managers
      await this.initializeManagers();

      // Start HTTP server
      await this.startHTTPServer();

      // Start game loop
      this.startGameLoop();

      // Start periodic save
      this.startPeriodicSave();

      // Setup graceful shutdown
      this.setupGracefulShutdown();

      this.isRunning = true;

      log.info(`✅ Game Server started on port ${serverConfig.port}`);
      log.info(`Server ID: ${serverConfig.serverId}`);
      log.info(`Region: ${serverConfig.region}`);
      log.info(`Environment: ${serverConfig.environment}`);
      log.info(`Tick Rate: ${serverConfig.performance.tickRate} TPS`);
    } catch (error) {
      log.exception(error as Error, 'GameServer.start');
      throw error;
    }
  }

  private async initializeManagers(): Promise<void> {
    log.info('Initializing managers...');

    // Initialize in dependency order
    await this.worldState.initialize();
    await this.mapManager.initialize();
    await this.playerManager.initialize();
    await this.combatManager.initialize(this.mapManager);
    await this.movementManager.initialize(this.mapManager);
    await this.aiManager.initialize(this.mapManager, this.combatManager);

    // Connect managers to socket handler
    this.connectManagersToSocket();

    log.info('✅ All managers initialized');
  }

  private connectManagersToSocket(): void {
    // This would typically connect manager events to socket events
    // For now, we'll just log it
    log.info('Connecting managers to socket handler');
  }

  private startHTTPServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      // Add health check endpoint
      this.httpServer.on('request', (req, res) => {
        if (req.url === '/health' && req.method === 'GET') {
          const metrics = this.getMetrics();
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({
            status: 'ok',
            timestamp: new Date().toISOString(),
            uptime: Math.floor(metrics.uptime),
            metrics: {
              onlinePlayers: metrics.onlinePlayers,
              activeMaps: metrics.activeMapInstances,
              activeBattles: metrics.activeBattles,
              tickCount: metrics.tickCount,
              lastTickDuration: metrics.lastTickDuration,
            }
          }));
        } else if (req.url === '/metrics' && req.method === 'GET') {
          const metrics = this.getMetrics();
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify(metrics));
        } else {
          // Let Socket.IO handle other requests
          return;
        }
      });

      this.httpServer.listen(serverConfig.port, () => {
        resolve();
      });

      this.httpServer.on('error', (error) => {
        reject(error);
      });
    });
  }

  private startGameLoop(): void {
    const tickRate = serverConfig.performance.tickRate;
    const tickInterval = 1000 / tickRate;

    log.info(`Starting game loop at ${tickRate} TPS`);

    this.tickInterval = setInterval(() => {
      this.gameTick();
    }, tickInterval);
  }

  private gameTick(): void {
    const startTime = Date.now();

    try {
      // Update world state
      this.worldState.update();

      // Update all map instances
      this.mapManager.updateAll();

      // Process combat
      this.combatManager.updateBattles();

      // Update AI
      this.aiManager.update();

      // Process movement queue
      this.movementManager.processQueue();

      // Update tick metrics
      this.tickCount++;
      const tickDuration = Date.now() - startTime;

      // Log performance warning if tick takes too long
      if (tickDuration > 50) {
        log.performance('Game tick', tickDuration, {
          tickCount: this.tickCount,
        });
      }

      this.lastTickTime = tickDuration;
    } catch (error) {
      log.exception(error as Error, 'GameServer.gameTick');
    }
  }

  private startPeriodicSave(): void {
    log.info(`Starting periodic save every ${gameConfig.persistence.saveInterval}ms`);

    this.saveInterval = setInterval(async () => {
      await this.saveWorldState();
    }, gameConfig.persistence.saveInterval);
  }

  private async saveWorldState(): Promise<void> {
    const timer = log.timer();

    try {
      log.info('Saving world state...');

      // Save player states
      await this.playerManager.saveAll();

      // Save map states
      await this.mapManager.saveAll();

      // Save combat states
      await this.combatManager.saveAll();

      const duration = timer();
      log.info(`✅ World state saved in ${duration}ms`);
    } catch (error) {
      log.exception(error as Error, 'GameServer.saveWorldState');
    }
  }

  private setupGracefulShutdown(): void {
    const shutdown = async (signal: string) => {
      log.info(`Received ${signal}, starting graceful shutdown...`);

      await this.stop();
      process.exit(0);
    };

    process.on('SIGTERM', () => shutdown('SIGTERM'));
    process.on('SIGINT', () => shutdown('SIGINT'));

    // Handle uncaught errors
    process.on('uncaughtException', (error) => {
      log.exception(error, 'Uncaught Exception');
      shutdown('uncaughtException');
    });

    process.on('unhandledRejection', (reason, promise) => {
      log.error('Unhandled Rejection', { reason, promise });
      shutdown('unhandledRejection');
    });
  }

  public async stop(): Promise<void> {
    if (!this.isRunning) return;

    log.info('Stopping game server...');

    this.isRunning = false;

    // Stop game loop
    if (this.tickInterval) {
      clearInterval(this.tickInterval);
      this.tickInterval = null;
    }

    // Stop periodic save
    if (this.saveInterval) {
      clearInterval(this.saveInterval);
      this.saveInterval = null;
    }

    // Save final state
    await this.saveWorldState();

    // Disconnect all players
    await this.playerManager.disconnectAll();

    // Cleanup managers
    await this.cleanupManagers();

    // Close HTTP server
    await new Promise<void>((resolve) => {
      this.httpServer.close(() => resolve());
    });

    // Close database connections
    await closeDatabases();

    log.info('✅ Game server stopped');
  }

  private async cleanupManagers(): Promise<void> {
    log.info('Cleaning up managers...');

    await this.aiManager.cleanup();
    await this.combatManager.cleanup();
    await this.movementManager.cleanup();
    await this.mapManager.cleanup();
    await this.playerManager.cleanup();
    await this.worldState.cleanup();

    log.info('✅ Managers cleaned up');
  }

  // Public getters for managers
  public getSocketHandler(): SocketHandler {
    return this.socketHandler;
  }

  public getPlayerManager(): PlayerManager {
    return this.playerManager;
  }

  public getWorldState(): WorldState {
    return this.worldState;
  }

  public getMapManager(): MapManager {
    return this.mapManager;
  }

  public getCombatManager(): CombatManager {
    return this.combatManager;
  }

  public getMovementManager(): MovementManager {
    return this.movementManager;
  }

  public getChatManager(): ChatManager {
    return this.chatManager;
  }

  public getAIManager(): AIManager {
    return this.aiManager;
  }

  // Server metrics
  public getMetrics() {
    return {
      serverId: serverConfig.serverId,
      uptime: process.uptime(),
      tickCount: this.tickCount,
      lastTickDuration: this.lastTickTime,
      onlinePlayers: this.socketHandler.getOnlinePlayerCount(),
      activeMapInstances: this.mapManager.getActiveInstanceCount(),
      activeBattles: this.combatManager.getActiveBattleCount(),
      memoryUsage: process.memoryUsage(),
    };
  }
}

export default GameServer;