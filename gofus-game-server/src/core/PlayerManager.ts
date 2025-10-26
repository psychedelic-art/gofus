import { redis, REDIS_KEYS, redisHelpers } from '@/config/database.config';
import { log } from '@/utils/Logger';

export interface PlayerSession {
  playerId: string;
  characterId: string;
  userId: string;
  socketId: string;
  mapId: number;
  cellId: number;
  level: number;
  classId: number;
  stats: PlayerStats;
  connectedAt: number;
  lastActivity: number;
  isInCombat: boolean;
  currentBattleId?: string;
}

export interface PlayerStats {
  vitality: number;
  wisdom: number;
  strength: number;
  intelligence: number;
  chance: number;
  agility: number;
  hp: number;
  maxHp: number;
  mp: number;
  maxMp: number;
  ap: number;
  maxAp: number;
}

export class PlayerManager {
  private activePlayers: Map<string, PlayerSession>;
  private playersByMap: Map<number, Set<string>>;
  private playersByCharacter: Map<string, string>; // characterId -> playerId

  constructor() {
    this.activePlayers = new Map();
    this.playersByMap = new Map();
    this.playersByCharacter = new Map();
  }

  public async initialize(): Promise<void> {
    log.info('Initializing PlayerManager...');

    // Load any persisted player sessions from Redis
    await this.loadPersistedSessions();

    log.info('PlayerManager initialized');
  }

  private async loadPersistedSessions(): Promise<void> {
    try {
      // Scan for existing player sessions in Redis
      const pattern = `${REDIS_KEYS.PLAYER}*`;
      const keys = await this.scanKeys(pattern);

      for (const key of keys) {
        const session = await redisHelpers.getJSON<PlayerSession>(key);
        if (session) {
          // Validate session is still valid
          if (this.isSessionValid(session)) {
            this.activePlayers.set(session.playerId, session);
            this.indexPlayer(session);
          } else {
            // Clean up expired session
            await redis.del(key);
          }
        }
      }

      log.info(`Loaded ${this.activePlayers.size} persisted sessions`);
    } catch (error) {
      log.exception(error as Error, 'PlayerManager.loadPersistedSessions');
    }
  }

  private async scanKeys(pattern: string): Promise<string[]> {
    const keys: string[] = [];
    let cursor = '0';

    do {
      const [newCursor, foundKeys] = await redis.scan(
        cursor,
        'MATCH',
        pattern,
        'COUNT',
        100
      );
      cursor = newCursor;
      keys.push(...foundKeys);
    } while (cursor !== '0');

    return keys;
  }

  private isSessionValid(session: PlayerSession): boolean {
    const now = Date.now();
    const sessionAge = now - session.connectedAt;
    const lastActivityAge = now - session.lastActivity;

    // Session is valid if connected within last 24 hours
    // and had activity within last hour
    return sessionAge < 86400000 && lastActivityAge < 3600000;
  }

  public async addPlayer(
    playerId: string,
    characterData: any,
    socketId: string
  ): Promise<PlayerSession> {
    const session: PlayerSession = {
      playerId,
      characterId: characterData.id,
      userId: characterData.accountId,
      socketId,
      mapId: characterData.mapId || 7411,
      cellId: characterData.cellId || 311,
      level: characterData.level || 1,
      classId: characterData.classId,
      stats: this.calculateStats(characterData),
      connectedAt: Date.now(),
      lastActivity: Date.now(),
      isInCombat: false,
    };

    // Store in memory
    this.activePlayers.set(playerId, session);
    this.indexPlayer(session);

    // Persist to Redis
    await this.persistSession(session);

    log.player(playerId, 'session_created', {
      characterId: session.characterId,
      mapId: session.mapId,
    });

    return session;
  }

  private calculateStats(characterData: any): PlayerStats {
    const baseStats = characterData.stats || {};
    const level = characterData.level || 1;

    // Calculate max HP/MP based on vitality/wisdom and level
    const baseHp = 50 + level * 5;
    const vitalityBonus = (baseStats.vitality || 0) * 1;
    const maxHp = baseHp + vitalityBonus;

    const baseMp = 3;
    const wisdomBonus = Math.floor((baseStats.wisdom || 0) / 10);
    const maxMp = baseMp + wisdomBonus;

    return {
      vitality: baseStats.vitality || 0,
      wisdom: baseStats.wisdom || 0,
      strength: baseStats.strength || 0,
      intelligence: baseStats.intelligence || 0,
      chance: baseStats.chance || 0,
      agility: baseStats.agility || 0,
      hp: characterData.currentHp || maxHp,
      maxHp,
      mp: characterData.currentMp || maxMp,
      maxMp,
      ap: 6,
      maxAp: 6 + Math.floor((baseStats.wisdom || 0) / 100),
    };
  }

  private indexPlayer(session: PlayerSession): void {
    // Index by map
    if (!this.playersByMap.has(session.mapId)) {
      this.playersByMap.set(session.mapId, new Set());
    }
    this.playersByMap.get(session.mapId)!.add(session.playerId);

    // Index by character
    this.playersByCharacter.set(session.characterId, session.playerId);
  }

  private unindexPlayer(session: PlayerSession): void {
    // Remove from map index
    const mapPlayers = this.playersByMap.get(session.mapId);
    if (mapPlayers) {
      mapPlayers.delete(session.playerId);
      if (mapPlayers.size === 0) {
        this.playersByMap.delete(session.mapId);
      }
    }

    // Remove from character index
    this.playersByCharacter.delete(session.characterId);
  }

  private async persistSession(session: PlayerSession): Promise<void> {
    const key = `${REDIS_KEYS.PLAYER}${session.playerId}`;
    await redisHelpers.setJSON(key, session, 86400); // 24 hour TTL
  }

  public async removePlayer(playerId: string): Promise<void> {
    const session = this.activePlayers.get(playerId);
    if (!session) return;

    // Remove from indexes
    this.unindexPlayer(session);

    // Remove from memory
    this.activePlayers.delete(playerId);

    // Remove from Redis
    const key = `${REDIS_KEYS.PLAYER}${playerId}`;
    await redis.del(key);

    log.player(playerId, 'session_removed');
  }

  public getPlayer(playerId: string): PlayerSession | undefined {
    return this.activePlayers.get(playerId);
  }

  public getPlayerByCharacterId(characterId: string): PlayerSession | undefined {
    const playerId = this.playersByCharacter.get(characterId);
    return playerId ? this.activePlayers.get(playerId) : undefined;
  }

  public getPlayersInMap(mapId: number): PlayerSession[] {
    const playerIds = this.playersByMap.get(mapId);
    if (!playerIds) return [];

    const players: PlayerSession[] = [];
    for (const playerId of playerIds) {
      const player = this.activePlayers.get(playerId);
      if (player) {
        players.push(player);
      }
    }

    return players;
  }

  public async updatePlayerPosition(
    playerId: string,
    mapId: number,
    cellId: number
  ): Promise<void> {
    const session = this.activePlayers.get(playerId);
    if (!session) return;

    // Update map index if map changed
    if (session.mapId !== mapId) {
      this.unindexPlayer(session);
      session.mapId = mapId;
      session.cellId = cellId;
      this.indexPlayer(session);
    } else {
      session.cellId = cellId;
    }

    session.lastActivity = Date.now();

    // Update in Redis
    await this.persistSession(session);

    log.player(playerId, 'position_updated', { mapId, cellId });
  }

  public async updatePlayerStats(
    playerId: string,
    stats: Partial<PlayerStats>
  ): Promise<void> {
    const session = this.activePlayers.get(playerId);
    if (!session) return;

    session.stats = { ...session.stats, ...stats };
    session.lastActivity = Date.now();

    await this.persistSession(session);

    log.player(playerId, 'stats_updated', stats);
  }

  public async setPlayerInCombat(
    playerId: string,
    battleId: string | null
  ): Promise<void> {
    const session = this.activePlayers.get(playerId);
    if (!session) return;

    session.isInCombat = battleId !== null;
    session.currentBattleId = battleId || undefined;
    session.lastActivity = Date.now();

    await this.persistSession(session);

    log.player(playerId, 'combat_status', {
      isInCombat: session.isInCombat,
      battleId,
    });
  }

  public async updateActivity(playerId: string): Promise<void> {
    const session = this.activePlayers.get(playerId);
    if (!session) return;

    session.lastActivity = Date.now();
    await this.persistSession(session);
  }

  public async saveAll(): Promise<void> {
    const savePromises: Promise<void>[] = [];

    for (const session of this.activePlayers.values()) {
      savePromises.push(this.persistSession(session));
    }

    await Promise.all(savePromises);

    log.info(`Saved ${this.activePlayers.size} player sessions`);
  }

  public async disconnectAll(): Promise<void> {
    const disconnectPromises: Promise<void>[] = [];

    for (const playerId of this.activePlayers.keys()) {
      disconnectPromises.push(this.removePlayer(playerId));
    }

    await Promise.all(disconnectPromises);

    log.info('All players disconnected');
  }

  public async cleanup(): Promise<void> {
    await this.saveAll();
    this.activePlayers.clear();
    this.playersByMap.clear();
    this.playersByCharacter.clear();
  }

  // Statistics
  public getOnlineCount(): number {
    return this.activePlayers.size;
  }

  public getMapPopulation(mapId: number): number {
    return this.playersByMap.get(mapId)?.size || 0;
  }

  public getActivePlayerIds(): string[] {
    return Array.from(this.activePlayers.keys());
  }

  public getStatistics() {
    const stats = {
      totalOnline: this.activePlayers.size,
      byMap: {} as Record<number, number>,
      inCombat: 0,
      averageLevel: 0,
    };

    let totalLevel = 0;

    for (const session of this.activePlayers.values()) {
      if (session.isInCombat) stats.inCombat++;
      totalLevel += session.level;
    }

    for (const [mapId, players] of this.playersByMap.entries()) {
      stats.byMap[mapId] = players.size;
    }

    stats.averageLevel = this.activePlayers.size > 0
      ? Math.round(totalLevel / this.activePlayers.size)
      : 0;

    return stats;
  }
}

export default PlayerManager;