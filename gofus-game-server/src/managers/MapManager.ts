import { Server as SocketIOServer } from 'socket.io';
import { Redis } from 'ioredis';
import { log } from '@/utils/Logger';
import fetch from 'node-fetch';

export interface CellData {
  cellId: number;
  walkable: boolean;
  lineOfSight: boolean;
  level: number;
  movementCost: number;
  interactive: boolean;
  coordX: number;
  coordY: number;
}

export interface MapData {
  id: number;
  name?: string;
  x?: number;
  y?: number;
  width: number;
  height: number;
  cells: CellData[];
  adjacentMaps?: {
    top?: number;
    bottom?: number;
    left?: number;
    right?: number;
  };
}

export interface MapInstance {
  mapId: number;
  mapData: MapData;
  players: Set<string>; // Socket IDs of players on this map
  entities: Map<string, any>; // NPCs, monsters, objects
  lastUpdate: Date;
}

export class MapManager {
  private mapInstances: Map<number, MapInstance>;
  private mapDataCache: Map<number, MapData>;
  private readonly CACHE_TTL = 300000; // 5 minutes in milliseconds
  private readonly BACKEND_URL: string;

  constructor(
    private io: SocketIOServer,
    private redis: Redis
  ) {
    this.mapInstances = new Map();
    this.mapDataCache = new Map();
    this.BACKEND_URL = process.env.BACKEND_URL || 'https://gofus-backend.vercel.app';
  }

  public async initialize(): Promise<void> {
    log.info('MapManager initializing...');

    // Load commonly used maps into cache
    const commonMapIds = [7411, 7410, 7412, 7339, 7340]; // Test maps
    for (const mapId of commonMapIds) {
      try {
        await this.loadMapData(mapId);
        log.info(`Preloaded map ${mapId}`);
      } catch (error) {
        log.error(`Failed to preload map ${mapId}:`, error);
      }
    }

    log.info(`MapManager initialized with ${this.mapDataCache.size} maps cached`);
  }

  /**
   * Load map data from backend API or Redis cache
   */
  public async loadMapData(mapId: number): Promise<MapData> {
    // Check memory cache first
    const cached = this.mapDataCache.get(mapId);
    if (cached) {
      log.debug(`Map ${mapId} loaded from memory cache`);
      return cached;
    }

    // Check Redis cache
    try {
      const redisKey = `map:${mapId}`;
      const redisData = await this.redis.get(redisKey);

      if (redisData) {
        const mapData = JSON.parse(redisData) as MapData;
        this.mapDataCache.set(mapId, mapData);
        log.debug(`Map ${mapId} loaded from Redis cache`);
        return mapData;
      }
    } catch (error) {
      log.warn(`Redis cache miss for map ${mapId}:`, error);
    }

    // Fetch from backend API
    try {
      const url = `${this.BACKEND_URL}/api/maps/${mapId}`;
      log.info(`Fetching map ${mapId} from ${url}`);

      const response = await fetch(url);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const apiResponse = await response.json() as any;

      if (!apiResponse.success || !apiResponse.map) {
        throw new Error('Invalid API response format');
      }

      const mapData: MapData = {
        id: apiResponse.map.id,
        name: apiResponse.map.name,
        x: apiResponse.map.x,
        y: apiResponse.map.y,
        width: apiResponse.map.width || 14,
        height: apiResponse.map.height || 20,
        cells: apiResponse.map.cells || [],
        adjacentMaps: apiResponse.map.adjacentMaps
      };

      // Validate cell data
      if (mapData.cells.length < 560) {
        log.warn(`Map ${mapId} has only ${mapData.cells.length} cells, expected 560. Filling missing cells.`);
        // Fill missing cells with default data
        for (let i = mapData.cells.length; i < 560; i++) {
          mapData.cells.push({
            cellId: i,
            walkable: true,
            lineOfSight: true,
            level: 0,
            movementCost: 1,
            interactive: false,
            coordX: i % mapData.width,
            coordY: Math.floor(i / mapData.width)
          });
        }
      }

      // Cache in Redis (5 minute TTL)
      const redisKey = `map:${mapId}`;
      await this.redis.setex(redisKey, 300, JSON.stringify(mapData));

      // Cache in memory
      this.mapDataCache.set(mapId, mapData);

      log.info(`Map ${mapId} loaded from API and cached (${mapData.cells.length} cells)`);
      return mapData;

    } catch (error) {
      log.error(`Failed to load map ${mapId} from API:`, error);

      // Return fallback test map
      return this.createFallbackMap(mapId);
    }
  }

  /**
   * Create a fallback test map when API fails
   */
  private createFallbackMap(mapId: number): MapData {
    log.warn(`Creating fallback test map for ${mapId}`);

    const cells: CellData[] = [];
    for (let i = 0; i < 560; i++) {
      cells.push({
        cellId: i,
        walkable: Math.random() > 0.1, // 90% walkable
        lineOfSight: true,
        level: 0,
        movementCost: 1,
        interactive: false,
        coordX: i % 14,
        coordY: Math.floor(i / 14)
      });
    }

    return {
      id: mapId,
      name: `Fallback Map ${mapId}`,
      width: 14,
      height: 20,
      cells: cells
    };
  }

  /**
   * Get or create a map instance
   */
  public async getMapInstance(mapId: number): Promise<MapInstance> {
    let instance = this.mapInstances.get(mapId);

    if (!instance) {
      // Create new instance
      const mapData = await this.loadMapData(mapId);

      instance = {
        mapId: mapId,
        mapData: mapData,
        players: new Set(),
        entities: new Map(),
        lastUpdate: new Date()
      };

      this.mapInstances.set(mapId, instance);
      log.info(`Created new map instance for map ${mapId}`);
    }

    return instance;
  }

  /**
   * Add a player to a map
   */
  public async addPlayerToMap(socketId: string, mapId: number, characterId: number): Promise<void> {
    const instance = await this.getMapInstance(mapId);
    instance.players.add(socketId);

    log.info(`Player ${characterId} (${socketId}) joined map ${mapId}. Total players: ${instance.players.size}`);

    // Store mapping in Redis for multi-server sync
    await this.redis.setex(`player:${socketId}:map`, 3600, mapId.toString());
  }

  /**
   * Remove a player from a map
   */
  public async removePlayerFromMap(socketId: string, mapId: number): Promise<void> {
    const instance = this.mapInstances.get(mapId);

    if (instance) {
      instance.players.delete(socketId);
      log.info(`Player (${socketId}) left map ${mapId}. Remaining players: ${instance.players.size}`);

      // Clean up empty map instances after 5 minutes
      if (instance.players.size === 0) {
        setTimeout(() => {
          if (instance.players.size === 0) {
            this.mapInstances.delete(mapId);
            log.info(`Cleaned up empty map instance ${mapId}`);
          }
        }, 300000);
      }
    }

    // Clean up Redis mapping
    await this.redis.del(`player:${socketId}:map`);
  }

  /**
   * Check if a cell is walkable
   */
  public async isCellWalkable(mapId: number, cellId: number): Promise<boolean> {
    const instance = await this.getMapInstance(mapId);

    if (cellId < 0 || cellId >= instance.mapData.cells.length) {
      return false;
    }

    return instance.mapData.cells[cellId].walkable;
  }

  /**
   * Get movement cost for a cell
   */
  public async getMovementCost(mapId: number, cellId: number): Promise<number> {
    const instance = await this.getMapInstance(mapId);

    if (cellId < 0 || cellId >= instance.mapData.cells.length) {
      return 999; // Impassable
    }

    return instance.mapData.cells[cellId].movementCost || 1;
  }

  /**
   * Get all players on a map
   */
  public getPlayersOnMap(mapId: number): string[] {
    const instance = this.mapInstances.get(mapId);
    return instance ? Array.from(instance.players) : [];
  }

  /**
   * Broadcast event to all players on a map
   */
  public broadcastToMap(mapId: number, event: string, data: any, excludeSocketId?: string): void {
    const instance = this.mapInstances.get(mapId);

    if (instance) {
      for (const socketId of instance.players) {
        if (socketId !== excludeSocketId) {
          this.io.to(socketId).emit(event, data);
        }
      }
      log.debug(`Broadcasted '${event}' to ${instance.players.size} players on map ${mapId}`);
    }
  }

  /**
   * Update all active map instances (called by game loop)
   */
  public updateAll(): void {
    const now = new Date();

    for (const [mapId, instance] of this.mapInstances.entries()) {
      // Update timestamp
      instance.lastUpdate = now;

      // Update entities (mobs, NPCs, etc.)
      // This would contain AI logic, spawn logic, etc.
      // For now, just a placeholder
    }
  }

  /**
   * Save all map states to database
   */
  public async saveAll(): Promise<void> {
    log.info(`Saving ${this.mapInstances.size} active map instances...`);

    for (const [mapId, instance] of this.mapInstances.entries()) {
      try {
        // Save instance state to Redis
        const stateKey = `map:${mapId}:state`;
        const state = {
          players: Array.from(instance.players),
          entities: Array.from(instance.entities.entries()),
          lastUpdate: instance.lastUpdate
        };

        await this.redis.setex(stateKey, 3600, JSON.stringify(state));

      } catch (error) {
        log.error(`Failed to save map ${mapId}:`, error);
      }
    }

    log.info('Map states saved successfully');
  }

  public getActiveInstanceCount(): number {
    return this.mapInstances.size;
  }

  public async cleanup(): Promise<void> {
    log.info('MapManager cleaning up...');

    // Save all states before cleanup
    await this.saveAll();

    // Clear instances
    this.mapInstances.clear();
    this.mapDataCache.clear();

    log.info('MapManager cleaned up');
  }
}

export default MapManager;
