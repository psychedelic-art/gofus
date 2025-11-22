import { Server as SocketIOServer, Socket } from 'socket.io';
import { Redis } from 'ioredis';
import { log } from '@/utils/Logger';
import { MapManager } from './MapManager';

export interface MovementRequest {
  socketId: string;
  characterId: number;
  mapId: number;
  fromCell: number;
  toCell: number;
  path: number[];
  timestamp: number;
}

export interface PathValidationResult {
  valid: boolean;
  reason?: string;
  invalidCell?: number;
  cost?: number;
}

export class MovementManager {
  private movementQueue: MovementRequest[];
  private processingMovement: Map<string, boolean>; // Track if player is currently moving
  private readonly MAX_QUEUE_SIZE = 100;

  constructor(
    private io: SocketIOServer,
    private redis: Redis,
    private mapManager: MapManager
  ) {
    this.movementQueue = [];
    this.processingMovement = new Map();
  }

  public async initialize(mapManager: MapManager): Promise<void> {
    log.info('MovementManager initializing...');

    // Start processing queue every 50ms (20 Hz update rate)
    setInterval(() => {
      this.processQueue();
    }, 50);

    log.info('MovementManager initialized with 20Hz update rate');
  }

  /**
   * Handle a movement request from a player
   */
  public async handleMovement(socket: Socket, data: { path: number[] }): Promise<void> {
    const characterId = socket.data.character?.id;
    const mapId = socket.data.character?.map_id;
    const currentCell = socket.data.character?.cell_id || 0;

    if (!characterId || !mapId) {
      socket.emit('movement:error', { error: 'Character data not loaded' });
      log.warn(`Movement request from ${socket.id} without character data`);
      return;
    }

    if (!data.path || data.path.length === 0) {
      socket.emit('movement:error', { error: 'Invalid path' });
      log.warn(`Empty path from character ${characterId}`);
      return;
    }

    // Check if already moving
    if (this.processingMovement.get(socket.id)) {
      socket.emit('movement:error', { error: 'Already moving' });
      log.warn(`Character ${characterId} already moving`);
      return;
    }

    // Validate path
    const validation = await this.validatePath(mapId, currentCell, data.path);

    if (!validation.valid) {
      socket.emit('movement:error', {
        error: validation.reason,
        invalidCell: validation.invalidCell
      });
      log.warn(`Invalid path from character ${characterId}: ${validation.reason}`);
      return;
    }

    // Create movement request
    const request: MovementRequest = {
      socketId: socket.id,
      characterId: characterId,
      mapId: mapId,
      fromCell: currentCell,
      toCell: data.path[data.path.length - 1],
      path: data.path,
      timestamp: Date.now()
    };

    // Add to queue
    if (this.movementQueue.length < this.MAX_QUEUE_SIZE) {
      this.movementQueue.push(request);
      this.processingMovement.set(socket.id, true);

      log.info(`Queued movement for character ${characterId}: ${currentCell} -> ${request.toCell} (${data.path.length} cells)`);
    } else {
      socket.emit('movement:error', { error: 'Movement queue full' });
      log.warn(`Movement queue full, rejected request from character ${characterId}`);
    }
  }

  /**
   * Validate a movement path
   */
  private async validatePath(mapId: number, startCell: number, path: number[]): Promise<PathValidationResult> {
    // Check if path starts from current position or adjacent
    const firstCell = path[0];
    if (firstCell !== startCell && !this.areAdjacent(startCell, firstCell)) {
      return {
        valid: false,
        reason: 'Path does not start from current cell',
        invalidCell: firstCell
      };
    }

    // Validate each cell in path
    let previousCell = startCell;
    let totalCost = 0;

    for (const cellId of path) {
      // Check cell bounds
      if (cellId < 0 || cellId >= 560) {
        return {
          valid: false,
          reason: 'Cell out of bounds',
          invalidCell: cellId
        };
      }

      // Check if walkable
      const walkable = await this.mapManager.isCellWalkable(mapId, cellId);
      if (!walkable) {
        return {
          valid: false,
          reason: 'Cell not walkable',
          invalidCell: cellId
        };
      }

      // Check if adjacent to previous cell
      if (!this.areAdjacent(previousCell, cellId) && previousCell !== cellId) {
        return {
          valid: false,
          reason: 'Path contains non-adjacent cells',
          invalidCell: cellId
        };
      }

      // Calculate cost
      const movementCost = await this.mapManager.getMovementCost(mapId, cellId);
      totalCost += movementCost;

      previousCell = cellId;
    }

    return {
      valid: true,
      cost: totalCost
    };
  }

  /**
   * Check if two cells are adjacent (8-directional)
   */
  private areAdjacent(cell1: number, cell2: number): boolean {
    const width = 14;
    const x1 = cell1 % width;
    const y1 = Math.floor(cell1 / width);
    const x2 = cell2 % width;
    const y2 = Math.floor(cell2 / width);

    const dx = Math.abs(x2 - x1);
    const dy = Math.abs(y2 - y1);

    // Adjacent if difference is at most 1 in both directions
    return dx <= 1 && dy <= 1 && (dx + dy) > 0;
  }

  /**
   * Process queued movement requests
   */
  public processQueue(): void {
    if (this.movementQueue.length === 0) {
      return;
    }

    const request = this.movementQueue.shift();
    if (!request) return;

    // Process movement
    this.executeMovement(request).catch((error) => {
      log.error(`Error executing movement for character ${request.characterId}:`, error);
      this.processingMovement.delete(request.socketId);
    });
  }

  /**
   * Execute a validated movement request
   */
  private async executeMovement(request: MovementRequest): Promise<void> {
    try {
      log.debug(`Executing movement for character ${request.characterId}: ${request.path.length} cells`);

      // Update character position in Redis (fast update)
      const characterKey = `character:${request.characterId}:position`;
      await this.redis.setex(characterKey, 3600, JSON.stringify({
        mapId: request.mapId,
        cellId: request.toCell,
        timestamp: Date.now()
      }));

      // Broadcast movement to all players on the map
      this.mapManager.broadcastToMap(
        request.mapId,
        'player:movement',
        {
          characterId: request.characterId,
          path: request.path,
          fromCell: request.fromCell,
          toCell: request.toCell
        },
        request.socketId // Exclude the moving player
      );

      // Send confirmation to the moving player
      this.io.to(request.socketId).emit('movement:success', {
        toCell: request.toCell,
        path: request.path
      });

      // Update socket data
      const socket = this.io.sockets.sockets.get(request.socketId);
      if (socket && socket.data.character) {
        socket.data.character.cell_id = request.toCell;
      }

      log.info(`Character ${request.characterId} moved to cell ${request.toCell}`);

    } catch (error) {
      log.error(`Failed to execute movement for character ${request.characterId}:`, error);

      // Notify player of failure
      this.io.to(request.socketId).emit('movement:error', {
        error: 'Movement failed'
      });

    } finally {
      // Mark as no longer moving
      this.processingMovement.delete(request.socketId);
    }
  }

  /**
   * Cancel movement for a player (e.g., on disconnect)
   */
  public cancelMovement(socketId: string): void {
    // Remove from queue
    this.movementQueue = this.movementQueue.filter(req => req.socketId !== socketId);

    // Remove from processing map
    this.processingMovement.delete(socketId);

    log.debug(`Cancelled movement for socket ${socketId}`);
  }

  /**
   * Get queue statistics
   */
  public getQueueStats(): { queueSize: number; processing: number } {
    return {
      queueSize: this.movementQueue.length,
      processing: this.processingMovement.size
    };
  }

  public async cleanup(): Promise<void> {
    log.info('MovementManager cleaning up...');

    // Clear queue
    this.movementQueue = [];
    this.processingMovement.clear();

    log.info('MovementManager cleaned up');
  }
}

export default MovementManager;
