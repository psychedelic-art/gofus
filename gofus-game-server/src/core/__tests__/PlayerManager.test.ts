import { PlayerManager, PlayerSession, PlayerStats } from '../PlayerManager';
import { redis, REDIS_KEYS, redisHelpers } from '@/config/database.config';
import { log } from '@/utils/Logger';

// Mock dependencies
jest.mock('@/config/database.config', () => ({
  redis: {
    scan: jest.fn(),
    del: jest.fn(),
  },
  REDIS_KEYS: {
    PLAYER: 'player:',
  },
  redisHelpers: {
    getJSON: jest.fn(),
    setJSON: jest.fn(),
  },
}));

jest.mock('@/utils/Logger', () => ({
  log: {
    info: jest.fn(),
    exception: jest.fn(),
    player: jest.fn(),
  },
}));

describe('PlayerManager', () => {
  let playerManager: PlayerManager;
  let mockRedis: jest.Mocked<typeof redis>;
  let mockRedisHelpers: jest.Mocked<typeof redisHelpers>;
  let mockLog: jest.Mocked<typeof log>;

  // Test data
  const mockCharacterData = {
    id: 'char-123',
    accountId: 'user-456',
    mapId: 7411,
    cellId: 311,
    level: 25,
    classId: 1,
    currentHp: 200,
    currentMp: 10,
    stats: {
      vitality: 100,
      wisdom: 50,
      strength: 75,
      intelligence: 60,
      chance: 40,
      agility: 65,
    },
  };

  const mockPlayerSession: PlayerSession = {
    playerId: 'player-789',
    characterId: 'char-123',
    userId: 'user-456',
    socketId: 'socket-abc',
    mapId: 7411,
    cellId: 311,
    level: 25,
    classId: 1,
    stats: {
      vitality: 100,
      wisdom: 50,
      strength: 75,
      intelligence: 60,
      chance: 40,
      agility: 65,
      hp: 200,
      maxHp: 225,
      mp: 10,
      maxMp: 8,
      ap: 6,
      maxAp: 6,
    },
    connectedAt: Date.now(),
    lastActivity: Date.now(),
    isInCombat: false,
  };

  beforeEach(() => {
    // Clear all mocks before each test
    jest.clearAllMocks();

    // Reset module registry to get fresh instances
    jest.resetModules();

    // Get fresh mock instances
    mockRedis = redis as jest.Mocked<typeof redis>;
    mockRedisHelpers = redisHelpers as jest.Mocked<typeof redisHelpers>;
    mockLog = log as jest.Mocked<typeof log>;

    // Default mock implementations
    mockRedis.scan.mockResolvedValue(['0', []]);
    mockRedis.del.mockResolvedValue(1);
    mockRedisHelpers.getJSON.mockResolvedValue(null);
    mockRedisHelpers.setJSON.mockResolvedValue(undefined);

    // Create a fresh instance
    playerManager = new PlayerManager();
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  describe('Constructor', () => {
    it('should create a new PlayerManager instance', () => {
      expect(playerManager).toBeInstanceOf(PlayerManager);
    });

    it('should initialize empty collections', () => {
      expect(playerManager.getOnlineCount()).toBe(0);
      expect(playerManager.getActivePlayerIds()).toEqual([]);
    });
  });

  describe('initialize()', () => {
    it('should initialize successfully with no persisted sessions', async () => {
      mockRedis.scan.mockResolvedValue(['0', []]);

      await playerManager.initialize();

      expect(mockLog.info).toHaveBeenCalledWith('Initializing PlayerManager...');
      expect(mockLog.info).toHaveBeenCalledWith('PlayerManager initialized');
      expect(mockLog.info).toHaveBeenCalledWith('Loaded 0 persisted sessions');
    });

    it('should load valid persisted sessions from Redis', async () => {
      const validSession: PlayerSession = {
        ...mockPlayerSession,
        connectedAt: Date.now() - 1000,
        lastActivity: Date.now() - 500,
      };

      mockRedis.scan.mockResolvedValueOnce(['0', ['player:player-789']]);
      mockRedisHelpers.getJSON.mockResolvedValueOnce(validSession);

      await playerManager.initialize();

      expect(mockRedisHelpers.getJSON).toHaveBeenCalledWith('player:player-789');
      expect(mockLog.info).toHaveBeenCalledWith('Loaded 1 persisted sessions');
      expect(playerManager.getPlayer('player-789')).toEqual(validSession);
    });

    it('should clean up expired sessions', async () => {
      const expiredSession: PlayerSession = {
        ...mockPlayerSession,
        connectedAt: Date.now() - 86400001, // More than 24 hours
        lastActivity: Date.now() - 3600001, // More than 1 hour
      };

      mockRedis.scan.mockResolvedValueOnce(['0', ['player:player-expired']]);
      mockRedisHelpers.getJSON.mockResolvedValueOnce(expiredSession);

      await playerManager.initialize();

      expect(mockRedis.del).toHaveBeenCalledWith('player:player-expired');
      expect(mockLog.info).toHaveBeenCalledWith('Loaded 0 persisted sessions');
      expect(playerManager.getPlayer('player-expired')).toBeUndefined();
    });

    it('should handle multiple pages of scan results', async () => {
      const session1: PlayerSession = {
        ...mockPlayerSession,
        playerId: 'player-1',
        connectedAt: Date.now() - 1000,
        lastActivity: Date.now() - 500,
      };
      const session2: PlayerSession = {
        ...mockPlayerSession,
        playerId: 'player-2',
        connectedAt: Date.now() - 1000,
        lastActivity: Date.now() - 500,
      };

      mockRedis.scan
        .mockResolvedValueOnce(['cursor-1', ['player:player-1']])
        .mockResolvedValueOnce(['0', ['player:player-2']]);

      mockRedisHelpers.getJSON
        .mockResolvedValueOnce(session1)
        .mockResolvedValueOnce(session2);

      await playerManager.initialize();

      expect(mockRedis.scan).toHaveBeenCalledTimes(2);
      expect(mockLog.info).toHaveBeenCalledWith('Loaded 2 persisted sessions');
    });

    it('should handle errors during session loading', async () => {
      const error = new Error('Redis scan failed');
      mockRedis.scan.mockRejectedValueOnce(error);

      await playerManager.initialize();

      expect(mockLog.exception).toHaveBeenCalledWith(
        error,
        'PlayerManager.loadPersistedSessions'
      );
      expect(mockLog.info).toHaveBeenCalledWith('PlayerManager initialized');
    });

    it('should skip null sessions from Redis', async () => {
      mockRedis.scan.mockResolvedValueOnce(['0', ['player:player-null']]);
      mockRedisHelpers.getJSON.mockResolvedValueOnce(null);

      await playerManager.initialize();

      expect(mockLog.info).toHaveBeenCalledWith('Loaded 0 persisted sessions');
    });
  });

  describe('addPlayer()', () => {
    it('should add a new player session successfully', async () => {
      const session = await playerManager.addPlayer(
        'player-789',
        mockCharacterData,
        'socket-abc'
      );

      expect(session.playerId).toBe('player-789');
      expect(session.characterId).toBe('char-123');
      expect(session.userId).toBe('user-456');
      expect(session.socketId).toBe('socket-abc');
      expect(session.mapId).toBe(7411);
      expect(session.cellId).toBe(311);
      expect(session.level).toBe(25);
      expect(session.classId).toBe(1);
      expect(session.isInCombat).toBe(false);
      expect(session.connectedAt).toBeGreaterThan(0);
      expect(session.lastActivity).toBeGreaterThan(0);
    });

    it('should calculate stats correctly', async () => {
      const session = await playerManager.addPlayer(
        'player-789',
        mockCharacterData,
        'socket-abc'
      );

      expect(session.stats).toEqual({
        vitality: 100,
        wisdom: 50,
        strength: 75,
        intelligence: 60,
        chance: 40,
        agility: 65,
        hp: 200,
        maxHp: 275, // 50 + 25 * 5 + 100 * 1 = 175 + 100 = 275
        mp: 10,
        maxMp: 8, // 3 + floor(50 / 10) = 3 + 5 = 8
        ap: 6,
        maxAp: 6, // 6 + floor(50 / 100) = 6 + 0 = 6
      });
    });

    it('should use default values when character data is incomplete', async () => {
      const minimalCharacterData = {
        id: 'char-min',
        accountId: 'user-min',
        classId: 2,
      };

      const session = await playerManager.addPlayer(
        'player-min',
        minimalCharacterData,
        'socket-xyz'
      );

      expect(session.mapId).toBe(7411); // Default map
      expect(session.cellId).toBe(311); // Default cell
      expect(session.level).toBe(1); // Default level
      expect(session.stats.vitality).toBe(0);
      expect(session.stats.hp).toBeGreaterThan(0);
    });

    it('should persist session to Redis', async () => {
      const session = await playerManager.addPlayer(
        'player-789',
        mockCharacterData,
        'socket-abc'
      );

      expect(mockRedisHelpers.setJSON).toHaveBeenCalledWith(
        'player:player-789',
        session,
        86400
      );
    });

    it('should index player by map', async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');

      const playersInMap = playerManager.getPlayersInMap(7411);
      expect(playersInMap).toHaveLength(1);
      expect(playersInMap[0].playerId).toBe('player-789');
    });

    it('should index player by character ID', async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');

      const player = playerManager.getPlayerByCharacterId('char-123');
      expect(player).toBeDefined();
      expect(player!.playerId).toBe('player-789');
    });

    it('should log player session creation', async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');

      expect(mockLog.player).toHaveBeenCalledWith('player-789', 'session_created', {
        characterId: 'char-123',
        mapId: 7411,
      });
    });

    it('should handle multiple players on the same map', async () => {
      const characterData2 = { ...mockCharacterData, id: 'char-456' };

      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', characterData2, 'socket-2');

      const playersInMap = playerManager.getPlayersInMap(7411);
      expect(playersInMap).toHaveLength(2);
      expect(playerManager.getMapPopulation(7411)).toBe(2);
    });

    it('should calculate maxAp with wisdom bonus', async () => {
      const highWisdomCharacter = {
        ...mockCharacterData,
        stats: {
          ...mockCharacterData.stats,
          wisdom: 250, // Should give +2 maxAp
        },
      };

      const session = await playerManager.addPlayer(
        'player-wisdom',
        highWisdomCharacter,
        'socket-abc'
      );

      expect(session.stats.maxAp).toBe(8); // 6 + floor(250 / 100)
    });
  });

  describe('removePlayer()', () => {
    beforeEach(async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');
      jest.clearAllMocks();
    });

    it('should remove player from memory', async () => {
      await playerManager.removePlayer('player-789');

      expect(playerManager.getPlayer('player-789')).toBeUndefined();
    });

    it('should remove player from Redis', async () => {
      await playerManager.removePlayer('player-789');

      expect(mockRedis.del).toHaveBeenCalledWith('player:player-789');
    });

    it('should unindex player from map', async () => {
      await playerManager.removePlayer('player-789');

      const playersInMap = playerManager.getPlayersInMap(7411);
      expect(playersInMap).toHaveLength(0);
      expect(playerManager.getMapPopulation(7411)).toBe(0);
    });

    it('should unindex player from character index', async () => {
      await playerManager.removePlayer('player-789');

      const player = playerManager.getPlayerByCharacterId('char-123');
      expect(player).toBeUndefined();
    });

    it('should log player session removal', async () => {
      await playerManager.removePlayer('player-789');

      expect(mockLog.player).toHaveBeenCalledWith('player-789', 'session_removed');
    });

    it('should do nothing if player does not exist', async () => {
      await playerManager.removePlayer('nonexistent-player');

      expect(mockRedis.del).not.toHaveBeenCalled();
      expect(mockLog.player).not.toHaveBeenCalled();
    });

    it('should clean up map index when last player leaves', async () => {
      await playerManager.removePlayer('player-789');

      const playersInMap = playerManager.getPlayersInMap(7411);
      expect(playersInMap).toEqual([]);
    });

    it('should keep other players when removing one', async () => {
      const characterData2 = { ...mockCharacterData, id: 'char-456' };
      await playerManager.addPlayer('player-2', characterData2, 'socket-2');
      jest.clearAllMocks();

      await playerManager.removePlayer('player-789');

      expect(playerManager.getPlayer('player-2')).toBeDefined();
      expect(playerManager.getMapPopulation(7411)).toBe(1);
    });
  });

  describe('getPlayer()', () => {
    beforeEach(async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');
    });

    it('should retrieve an existing player session', () => {
      const player = playerManager.getPlayer('player-789');

      expect(player).toBeDefined();
      expect(player!.playerId).toBe('player-789');
      expect(player!.characterId).toBe('char-123');
    });

    it('should return undefined for non-existent player', () => {
      const player = playerManager.getPlayer('nonexistent-player');

      expect(player).toBeUndefined();
    });
  });

  describe('getPlayerByCharacterId()', () => {
    beforeEach(async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');
    });

    it('should retrieve player by character ID', () => {
      const player = playerManager.getPlayerByCharacterId('char-123');

      expect(player).toBeDefined();
      expect(player!.playerId).toBe('player-789');
      expect(player!.characterId).toBe('char-123');
    });

    it('should return undefined for non-existent character ID', () => {
      const player = playerManager.getPlayerByCharacterId('nonexistent-char');

      expect(player).toBeUndefined();
    });
  });

  describe('getPlayersInMap()', () => {
    it('should return empty array for map with no players', () => {
      const players = playerManager.getPlayersInMap(9999);

      expect(players).toEqual([]);
    });

    it('should return all players in a specific map', async () => {
      const characterData2 = { ...mockCharacterData, id: 'char-456' };
      const characterData3 = { ...mockCharacterData, id: 'char-789', mapId: 8000 };

      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', characterData2, 'socket-2');
      await playerManager.addPlayer('player-3', characterData3, 'socket-3');

      const playersInMap7411 = playerManager.getPlayersInMap(7411);
      const playersInMap8000 = playerManager.getPlayersInMap(8000);

      expect(playersInMap7411).toHaveLength(2);
      expect(playersInMap8000).toHaveLength(1);
      expect(playersInMap8000[0].mapId).toBe(8000);
    });

    it('should not return removed players', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', mockCharacterData, 'socket-2');
      await playerManager.removePlayer('player-1');

      const players = playerManager.getPlayersInMap(7411);

      expect(players).toHaveLength(1);
      expect(players[0].playerId).toBe('player-2');
    });
  });

  describe('updatePlayerPosition()', () => {
    beforeEach(async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');
      jest.clearAllMocks();
    });

    it('should update player position in same map', async () => {
      await playerManager.updatePlayerPosition('player-789', 7411, 450);

      const player = playerManager.getPlayer('player-789');
      expect(player!.mapId).toBe(7411);
      expect(player!.cellId).toBe(450);
    });

    it('should update player position and map when changing maps', async () => {
      await playerManager.updatePlayerPosition('player-789', 8000, 200);

      const player = playerManager.getPlayer('player-789');
      expect(player!.mapId).toBe(8000);
      expect(player!.cellId).toBe(200);
    });

    it('should reindex player when changing maps', async () => {
      await playerManager.updatePlayerPosition('player-789', 8000, 200);

      const playersInOldMap = playerManager.getPlayersInMap(7411);
      const playersInNewMap = playerManager.getPlayersInMap(8000);

      expect(playersInOldMap).toHaveLength(0);
      expect(playersInNewMap).toHaveLength(1);
      expect(playersInNewMap[0].playerId).toBe('player-789');
    });

    it('should update last activity timestamp', async () => {
      const before = Date.now();
      await playerManager.updatePlayerPosition('player-789', 7411, 450);
      const after = Date.now();

      const player = playerManager.getPlayer('player-789');
      expect(player!.lastActivity).toBeGreaterThanOrEqual(before);
      expect(player!.lastActivity).toBeLessThanOrEqual(after);
    });

    it('should persist updated session to Redis', async () => {
      await playerManager.updatePlayerPosition('player-789', 8000, 200);

      expect(mockRedisHelpers.setJSON).toHaveBeenCalledWith(
        'player:player-789',
        expect.objectContaining({
          mapId: 8000,
          cellId: 200,
        }),
        86400
      );
    });

    it('should log position update', async () => {
      await playerManager.updatePlayerPosition('player-789', 8000, 200);

      expect(mockLog.player).toHaveBeenCalledWith('player-789', 'position_updated', {
        mapId: 8000,
        cellId: 200,
      });
    });

    it('should do nothing if player does not exist', async () => {
      await playerManager.updatePlayerPosition('nonexistent-player', 8000, 200);

      expect(mockRedisHelpers.setJSON).not.toHaveBeenCalled();
      expect(mockLog.player).not.toHaveBeenCalled();
    });
  });

  describe('updatePlayerStats()', () => {
    beforeEach(async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');
      jest.clearAllMocks();
    });

    it('should update player stats partially', async () => {
      await playerManager.updatePlayerStats('player-789', {
        hp: 150,
        mp: 5,
      });

      const player = playerManager.getPlayer('player-789');
      expect(player!.stats.hp).toBe(150);
      expect(player!.stats.mp).toBe(5);
      expect(player!.stats.vitality).toBe(100); // Unchanged
    });

    it('should update multiple stats at once', async () => {
      await playerManager.updatePlayerStats('player-789', {
        vitality: 120,
        strength: 90,
        hp: 250,
        maxHp: 270,
      });

      const player = playerManager.getPlayer('player-789');
      expect(player!.stats.vitality).toBe(120);
      expect(player!.stats.strength).toBe(90);
      expect(player!.stats.hp).toBe(250);
      expect(player!.stats.maxHp).toBe(270);
    });

    it('should update last activity timestamp', async () => {
      const before = Date.now();
      await playerManager.updatePlayerStats('player-789', { hp: 150 });
      const after = Date.now();

      const player = playerManager.getPlayer('player-789');
      expect(player!.lastActivity).toBeGreaterThanOrEqual(before);
      expect(player!.lastActivity).toBeLessThanOrEqual(after);
    });

    it('should persist updated session to Redis', async () => {
      await playerManager.updatePlayerStats('player-789', { hp: 150 });

      expect(mockRedisHelpers.setJSON).toHaveBeenCalledWith(
        'player:player-789',
        expect.objectContaining({
          stats: expect.objectContaining({ hp: 150 }),
        }),
        86400
      );
    });

    it('should log stats update', async () => {
      const statsUpdate = { hp: 150, mp: 5 };
      await playerManager.updatePlayerStats('player-789', statsUpdate);

      expect(mockLog.player).toHaveBeenCalledWith('player-789', 'stats_updated', statsUpdate);
    });

    it('should do nothing if player does not exist', async () => {
      await playerManager.updatePlayerStats('nonexistent-player', { hp: 150 });

      expect(mockRedisHelpers.setJSON).not.toHaveBeenCalled();
      expect(mockLog.player).not.toHaveBeenCalled();
    });

    it('should preserve existing stats when updating', async () => {
      const originalStats = { ...playerManager.getPlayer('player-789')!.stats };
      await playerManager.updatePlayerStats('player-789', { hp: 150 });

      const player = playerManager.getPlayer('player-789');
      expect(player!.stats.hp).toBe(150);
      expect(player!.stats.vitality).toBe(originalStats.vitality);
      expect(player!.stats.wisdom).toBe(originalStats.wisdom);
    });
  });

  describe('setPlayerInCombat()', () => {
    beforeEach(async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');
      jest.clearAllMocks();
    });

    it('should set player in combat', async () => {
      await playerManager.setPlayerInCombat('player-789', 'battle-123');

      const player = playerManager.getPlayer('player-789');
      expect(player!.isInCombat).toBe(true);
      expect(player!.currentBattleId).toBe('battle-123');
    });

    it('should remove player from combat', async () => {
      await playerManager.setPlayerInCombat('player-789', 'battle-123');
      await playerManager.setPlayerInCombat('player-789', null);

      const player = playerManager.getPlayer('player-789');
      expect(player!.isInCombat).toBe(false);
      expect(player!.currentBattleId).toBeUndefined();
    });

    it('should update last activity timestamp', async () => {
      const before = Date.now();
      await playerManager.setPlayerInCombat('player-789', 'battle-123');
      const after = Date.now();

      const player = playerManager.getPlayer('player-789');
      expect(player!.lastActivity).toBeGreaterThanOrEqual(before);
      expect(player!.lastActivity).toBeLessThanOrEqual(after);
    });

    it('should persist updated session to Redis', async () => {
      await playerManager.setPlayerInCombat('player-789', 'battle-123');

      expect(mockRedisHelpers.setJSON).toHaveBeenCalledWith(
        'player:player-789',
        expect.objectContaining({
          isInCombat: true,
          currentBattleId: 'battle-123',
        }),
        86400
      );
    });

    it('should log combat status change', async () => {
      await playerManager.setPlayerInCombat('player-789', 'battle-123');

      expect(mockLog.player).toHaveBeenCalledWith('player-789', 'combat_status', {
        isInCombat: true,
        battleId: 'battle-123',
      });
    });

    it('should do nothing if player does not exist', async () => {
      await playerManager.setPlayerInCombat('nonexistent-player', 'battle-123');

      expect(mockRedisHelpers.setJSON).not.toHaveBeenCalled();
      expect(mockLog.player).not.toHaveBeenCalled();
    });

    it('should handle switching between battles', async () => {
      await playerManager.setPlayerInCombat('player-789', 'battle-123');
      await playerManager.setPlayerInCombat('player-789', 'battle-456');

      const player = playerManager.getPlayer('player-789');
      expect(player!.isInCombat).toBe(true);
      expect(player!.currentBattleId).toBe('battle-456');
    });
  });

  describe('updateActivity()', () => {
    beforeEach(async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');
      jest.clearAllMocks();
    });

    it('should update last activity timestamp', async () => {
      const player = playerManager.getPlayer('player-789');
      const originalActivity = player!.lastActivity;

      // Wait a bit to ensure timestamp difference
      await new Promise((resolve) => setTimeout(resolve, 10));
      await playerManager.updateActivity('player-789');

      const updatedPlayer = playerManager.getPlayer('player-789');
      expect(updatedPlayer!.lastActivity).toBeGreaterThan(originalActivity);
    });

    it('should persist updated session to Redis', async () => {
      await playerManager.updateActivity('player-789');

      expect(mockRedisHelpers.setJSON).toHaveBeenCalledWith(
        'player:player-789',
        expect.any(Object),
        86400
      );
    });

    it('should do nothing if player does not exist', async () => {
      await playerManager.updateActivity('nonexistent-player');

      expect(mockRedisHelpers.setJSON).not.toHaveBeenCalled();
    });
  });

  describe('saveAll()', () => {
    it('should save all active player sessions', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', mockCharacterData, 'socket-2');
      await playerManager.addPlayer('player-3', mockCharacterData, 'socket-3');
      jest.clearAllMocks();

      await playerManager.saveAll();

      expect(mockRedisHelpers.setJSON).toHaveBeenCalledTimes(3);
      expect(mockLog.info).toHaveBeenCalledWith('Saved 3 player sessions');
    });

    it('should save zero sessions when no players are online', async () => {
      await playerManager.saveAll();

      expect(mockRedisHelpers.setJSON).not.toHaveBeenCalled();
      expect(mockLog.info).toHaveBeenCalledWith('Saved 0 player sessions');
    });

    it('should handle save errors gracefully', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      mockRedisHelpers.setJSON.mockRejectedValueOnce(new Error('Redis error'));

      await expect(playerManager.saveAll()).rejects.toThrow('Redis error');
    });
  });

  describe('disconnectAll()', () => {
    it('should disconnect all players', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', mockCharacterData, 'socket-2');
      await playerManager.addPlayer('player-3', mockCharacterData, 'socket-3');
      jest.clearAllMocks();

      await playerManager.disconnectAll();

      expect(mockRedis.del).toHaveBeenCalledTimes(3);
      expect(playerManager.getOnlineCount()).toBe(0);
      expect(mockLog.info).toHaveBeenCalledWith('All players disconnected');
    });

    it('should handle no players gracefully', async () => {
      await playerManager.disconnectAll();

      expect(mockRedis.del).not.toHaveBeenCalled();
      expect(mockLog.info).toHaveBeenCalledWith('All players disconnected');
    });
  });

  describe('cleanup()', () => {
    it('should save all sessions and clear all collections', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', mockCharacterData, 'socket-2');
      jest.clearAllMocks();

      await playerManager.cleanup();

      expect(mockRedisHelpers.setJSON).toHaveBeenCalledTimes(2); // saveAll
      expect(playerManager.getOnlineCount()).toBe(0);
      expect(playerManager.getActivePlayerIds()).toEqual([]);
      expect(playerManager.getPlayersInMap(7411)).toEqual([]);
    });

    it('should clear all player indexes', async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');
      await playerManager.cleanup();

      expect(playerManager.getPlayer('player-789')).toBeUndefined();
      expect(playerManager.getPlayerByCharacterId('char-123')).toBeUndefined();
      expect(playerManager.getMapPopulation(7411)).toBe(0);
    });
  });

  describe('getOnlineCount()', () => {
    it('should return zero when no players are online', () => {
      expect(playerManager.getOnlineCount()).toBe(0);
    });

    it('should return correct count of online players', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', mockCharacterData, 'socket-2');

      expect(playerManager.getOnlineCount()).toBe(2);
    });

    it('should update count when players disconnect', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', mockCharacterData, 'socket-2');
      await playerManager.removePlayer('player-1');

      expect(playerManager.getOnlineCount()).toBe(1);
    });
  });

  describe('getMapPopulation()', () => {
    it('should return zero for empty map', () => {
      expect(playerManager.getMapPopulation(9999)).toBe(0);
    });

    it('should return correct population count', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', mockCharacterData, 'socket-2');

      expect(playerManager.getMapPopulation(7411)).toBe(2);
    });

    it('should update when players change maps', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.updatePlayerPosition('player-1', 8000, 200);

      expect(playerManager.getMapPopulation(7411)).toBe(0);
      expect(playerManager.getMapPopulation(8000)).toBe(1);
    });
  });

  describe('getActivePlayerIds()', () => {
    it('should return empty array when no players are online', () => {
      expect(playerManager.getActivePlayerIds()).toEqual([]);
    });

    it('should return all active player IDs', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', mockCharacterData, 'socket-2');
      await playerManager.addPlayer('player-3', mockCharacterData, 'socket-3');

      const activeIds = playerManager.getActivePlayerIds();
      expect(activeIds).toHaveLength(3);
      expect(activeIds).toContain('player-1');
      expect(activeIds).toContain('player-2');
      expect(activeIds).toContain('player-3');
    });
  });

  describe('getStatistics()', () => {
    it('should return empty statistics when no players online', () => {
      const stats = playerManager.getStatistics();

      expect(stats.totalOnline).toBe(0);
      expect(stats.byMap).toEqual({});
      expect(stats.inCombat).toBe(0);
      expect(stats.averageLevel).toBe(0);
    });

    it('should return correct statistics for active players', async () => {
      const char1 = { ...mockCharacterData, level: 10 };
      const char2 = { ...mockCharacterData, id: 'char-2', level: 20 };
      const char3 = { ...mockCharacterData, id: 'char-3', level: 30, mapId: 8000 };

      await playerManager.addPlayer('player-1', char1, 'socket-1');
      await playerManager.addPlayer('player-2', char2, 'socket-2');
      await playerManager.addPlayer('player-3', char3, 'socket-3');
      await playerManager.setPlayerInCombat('player-1', 'battle-1');

      const stats = playerManager.getStatistics();

      expect(stats.totalOnline).toBe(3);
      expect(stats.byMap[7411]).toBe(2);
      expect(stats.byMap[8000]).toBe(1);
      expect(stats.inCombat).toBe(1);
      expect(stats.averageLevel).toBe(20); // (10 + 20 + 30) / 3 = 20
    });

    it('should calculate average level correctly', async () => {
      const char1 = { ...mockCharacterData, level: 15 };
      const char2 = { ...mockCharacterData, id: 'char-2', level: 25 };

      await playerManager.addPlayer('player-1', char1, 'socket-1');
      await playerManager.addPlayer('player-2', char2, 'socket-2');

      const stats = playerManager.getStatistics();

      expect(stats.averageLevel).toBe(20); // (15 + 25) / 2 = 20
    });

    it('should count players in combat correctly', async () => {
      await playerManager.addPlayer('player-1', mockCharacterData, 'socket-1');
      await playerManager.addPlayer('player-2', mockCharacterData, 'socket-2');
      await playerManager.addPlayer('player-3', mockCharacterData, 'socket-3');
      await playerManager.setPlayerInCombat('player-1', 'battle-1');
      await playerManager.setPlayerInCombat('player-2', 'battle-2');

      const stats = playerManager.getStatistics();

      expect(stats.inCombat).toBe(2);
    });
  });

  describe('Session Validation', () => {
    it('should validate session based on connection time', async () => {
      const oldSession: PlayerSession = {
        ...mockPlayerSession,
        connectedAt: Date.now() - 86400001, // More than 24 hours
        lastActivity: Date.now() - 1000,
      };

      mockRedis.scan.mockResolvedValueOnce(['0', ['player:player-old']]);
      mockRedisHelpers.getJSON.mockResolvedValueOnce(oldSession);

      await playerManager.initialize();

      expect(mockRedis.del).toHaveBeenCalledWith('player:player-old');
    });

    it('should validate session based on last activity', async () => {
      const inactiveSession: PlayerSession = {
        ...mockPlayerSession,
        connectedAt: Date.now() - 1000,
        lastActivity: Date.now() - 3600001, // More than 1 hour
      };

      mockRedis.scan.mockResolvedValueOnce(['0', ['player:player-inactive']]);
      mockRedisHelpers.getJSON.mockResolvedValueOnce(inactiveSession);

      await playerManager.initialize();

      expect(mockRedis.del).toHaveBeenCalledWith('player:player-inactive');
    });

    it('should keep session that meets both criteria', async () => {
      const validSession: PlayerSession = {
        ...mockPlayerSession,
        playerId: 'player-valid',
        connectedAt: Date.now() - 1000,
        lastActivity: Date.now() - 500,
      };

      mockRedis.scan.mockResolvedValueOnce(['0', ['player:player-valid']]);
      mockRedisHelpers.getJSON.mockResolvedValueOnce(validSession);

      await playerManager.initialize();

      expect(mockRedis.del).not.toHaveBeenCalled();
      expect(playerManager.getPlayer('player-valid')).toBeDefined();
    });
  });

  describe('Error Scenarios', () => {
    it('should handle Redis persistence errors in addPlayer', async () => {
      const redisError = new Error('Redis connection failed');
      mockRedisHelpers.setJSON.mockRejectedValueOnce(redisError);

      await expect(
        playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc')
      ).rejects.toThrow('Redis connection failed');
    });

    it('should handle Redis deletion errors in removePlayer', async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');

      const redisError = new Error('Redis deletion failed');
      mockRedis.del.mockRejectedValueOnce(redisError);

      await expect(playerManager.removePlayer('player-789')).rejects.toThrow(
        'Redis deletion failed'
      );
    });

    it('should handle malformed character data gracefully', async () => {
      const badCharacterData: any = {
        id: 'char-bad',
        accountId: 'user-bad',
        classId: 1,
        stats: null, // Malformed
      };

      const session = await playerManager.addPlayer(
        'player-bad',
        badCharacterData,
        'socket-abc'
      );

      expect(session).toBeDefined();
      expect(session.stats).toBeDefined();
      expect(session.stats.vitality).toBe(0);
    });
  });

  describe('Concurrent Operations', () => {
    it('should handle concurrent player additions', async () => {
      const promises = [];
      for (let i = 0; i < 10; i++) {
        const charData = { ...mockCharacterData, id: `char-${i}` };
        promises.push(
          playerManager.addPlayer(`player-${i}`, charData, `socket-${i}`)
        );
      }

      await Promise.all(promises);

      expect(playerManager.getOnlineCount()).toBe(10);
    });

    it('should handle concurrent position updates', async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');

      const promises = [];
      for (let i = 0; i < 5; i++) {
        promises.push(
          playerManager.updatePlayerPosition('player-789', 7411, 100 + i)
        );
      }

      await Promise.all(promises);

      const player = playerManager.getPlayer('player-789');
      expect(player).toBeDefined();
      expect(player!.cellId).toBeGreaterThanOrEqual(100);
      expect(player!.cellId).toBeLessThan(105);
    });

    it('should handle concurrent stats updates', async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');

      const promises = [];
      for (let i = 0; i < 5; i++) {
        promises.push(
          playerManager.updatePlayerStats('player-789', { hp: 100 + i * 10 })
        );
      }

      await Promise.all(promises);

      const player = playerManager.getPlayer('player-789');
      expect(player).toBeDefined();
      expect(player!.stats.hp).toBeGreaterThanOrEqual(100);
    });
  });

  describe('Redis Integration', () => {
    it('should use correct Redis key pattern for player sessions', async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');

      expect(mockRedisHelpers.setJSON).toHaveBeenCalledWith(
        'player:player-789',
        expect.any(Object),
        86400
      );
    });

    it('should set correct TTL for player sessions', async () => {
      await playerManager.addPlayer('player-789', mockCharacterData, 'socket-abc');

      expect(mockRedisHelpers.setJSON).toHaveBeenCalledWith(
        expect.any(String),
        expect.any(Object),
        86400 // 24 hours
      );
    });

    it('should scan Redis with correct pattern', async () => {
      await playerManager.initialize();

      expect(mockRedis.scan).toHaveBeenCalledWith(
        expect.any(String),
        'MATCH',
        'player:*',
        'COUNT',
        100
      );
    });
  });
});
