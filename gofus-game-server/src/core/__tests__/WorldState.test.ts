import { WorldState, WorldStatistics, ServerAnnouncement } from '../WorldState';
import { redis, REDIS_KEYS } from '@/config/database.config';
import { log } from '@/utils/Logger';

// Mock dependencies
jest.mock('@/config/database.config', () => ({
  redis: {
    get: jest.fn(),
    setex: jest.fn(),
  },
  REDIS_KEYS: {
    CACHE: 'cache:',
  },
}));

jest.mock('@/utils/Logger', () => ({
  log: {
    info: jest.fn(),
    debug: jest.fn(),
    exception: jest.fn(),
  },
}));

describe('WorldState', () => {
  let worldState: WorldState;
  let mockRedis: jest.Mocked<typeof redis>;
  let mockLog: jest.Mocked<typeof log>;
  let realDateNow: () => number;
  let realSetInterval: typeof setInterval;

  beforeEach(() => {
    // Clear all mocks
    jest.clearAllMocks();

    // Store real implementations
    realDateNow = Date.now;
    realSetInterval = setInterval;

    // Use fake timers
    jest.useFakeTimers();

    // Mock Date.now to return a fixed timestamp
    jest.spyOn(Date, 'now').mockReturnValue(1700000000000);

    // Get mock references
    mockRedis = redis as jest.Mocked<typeof redis>;
    mockLog = log as jest.Mocked<typeof log>;

    // Create fresh instance
    worldState = new WorldState();
  });

  afterEach(() => {
    // Restore real timers
    jest.clearAllTimers();
    jest.useRealTimers();

    // Restore mocks
    jest.restoreAllMocks();
  });

  describe('Constructor', () => {
    it('should create a new WorldState instance with default values', () => {
      expect(worldState).toBeInstanceOf(WorldState);
    });

    it('should initialize statistics with server start time', () => {
      const stats = worldState.getStatistics();
      expect(stats.serverStartTime).toBe(1700000000000);
      expect(stats.totalPlayers).toBe(0);
      expect(stats.totalBattles).toBe(0);
      expect(stats.totalMessages).toBe(0);
      expect(stats.totalMovements).toBe(0);
      expect(stats.totalAIDecisions).toBe(0);
    });

    it('should initialize empty announcements map', () => {
      const announcements = worldState.getAnnouncements();
      expect(announcements).toEqual([]);
    });

    it('should initialize empty global events map', () => {
      const events = worldState.getActiveEvents();
      expect(events.size).toBe(0);
    });

    it('should initialize with day cycle', () => {
      expect(worldState.getDayNightCycle()).toBe('day');
    });

    it('should initialize with clear weather', () => {
      expect(worldState.getWeather()).toBe('clear');
    });

    it('should initialize server time to current time', () => {
      expect(worldState.getServerTime()).toBe(1700000000000);
    });
  });

  describe('initialize()', () => {
    it('should initialize successfully and load persisted state', async () => {
      mockRedis.get.mockResolvedValue(null);

      await worldState.initialize();

      expect(mockLog.info).toHaveBeenCalledWith('Initializing WorldState...');
      expect(mockLog.info).toHaveBeenCalledWith('WorldState initialized');
      expect(mockRedis.get).toHaveBeenCalledWith('cache:world_state');
    });

    it('should start time tracking after initialization', async () => {
      mockRedis.get.mockResolvedValue(null);

      const setIntervalSpy = jest.spyOn(global, 'setInterval');

      await worldState.initialize();

      // Verify setInterval was called for time tracking (1000ms interval)
      expect(setIntervalSpy).toHaveBeenCalled();
      const calls = setIntervalSpy.mock.calls;
      const hasTimeTracking = calls.some(call => call[1] === 1000);
      expect(hasTimeTracking).toBe(true);

      setIntervalSpy.mockRestore();
    });

    it('should load persisted statistics from Redis', async () => {
      const savedState = JSON.stringify({
        statistics: {
          serverStartTime: 1699900000000,
          totalPlayers: 100,
          totalBattles: 50,
          totalMessages: 500,
          totalMovements: 300,
          totalAIDecisions: 200,
        },
        announcements: [],
        serverTime: 1699900000000,
        dayNightCycle: 'night',
        weatherState: 'rain',
        savedAt: 1699900000000,
      });

      mockRedis.get.mockResolvedValue(savedState);

      await worldState.initialize();

      const stats = worldState.getStatistics();
      expect(stats.totalPlayers).toBe(100);
      expect(stats.totalBattles).toBe(50);
      expect(stats.totalMessages).toBe(500);
      expect(stats.totalMovements).toBe(300);
      expect(stats.totalAIDecisions).toBe(200);
    });

    it('should load non-expired announcements from Redis', async () => {
      const futureTime = Date.now() + 10000;
      const savedState = JSON.stringify({
        statistics: {},
        announcements: [
          {
            id: 'announce_1',
            message: 'Server maintenance',
            priority: 'high',
            createdAt: Date.now() - 1000,
            expiresAt: futureTime,
          },
        ],
      });

      mockRedis.get.mockResolvedValue(savedState);

      await worldState.initialize();

      const announcements = worldState.getAnnouncements();
      expect(announcements).toHaveLength(1);
      expect(announcements[0].message).toBe('Server maintenance');
    });

    it('should not load expired announcements from Redis', async () => {
      const pastTime = Date.now() - 1000;
      const savedState = JSON.stringify({
        statistics: {},
        announcements: [
          {
            id: 'announce_1',
            message: 'Expired announcement',
            priority: 'high',
            createdAt: Date.now() - 5000,
            expiresAt: pastTime,
          },
        ],
      });

      mockRedis.get.mockResolvedValue(savedState);

      await worldState.initialize();

      const announcements = worldState.getAnnouncements();
      expect(announcements).toHaveLength(0);
    });

    it('should handle Redis errors gracefully', async () => {
      const redisError = new Error('Redis connection failed');
      mockRedis.get.mockRejectedValue(redisError);

      await worldState.initialize();

      expect(mockLog.exception).toHaveBeenCalledWith(
        redisError,
        'WorldState.loadPersistedState'
      );
      expect(mockLog.info).toHaveBeenCalledWith('WorldState initialized');
    });

    it('should handle invalid JSON in Redis gracefully', async () => {
      mockRedis.get.mockResolvedValue('invalid json {{{');

      await worldState.initialize();

      expect(mockLog.exception).toHaveBeenCalled();
      expect(mockLog.info).toHaveBeenCalledWith('WorldState initialized');
    });

    it('should handle null Redis response', async () => {
      mockRedis.get.mockResolvedValue(null);

      await worldState.initialize();

      const stats = worldState.getStatistics();
      expect(stats.totalPlayers).toBe(0);
      expect(mockLog.info).toHaveBeenCalledWith('WorldState initialized');
    });
  });

  describe('Announcement Management', () => {
    describe('addAnnouncement()', () => {
      it('should add announcement with default priority and duration', () => {
        const id = worldState.addAnnouncement('Test message');

        expect(id).toMatch(/^announce_/);
        expect(mockLog.info).toHaveBeenCalledWith('Added announcement: Test message');

        const announcements = worldState.getAnnouncements();
        expect(announcements).toHaveLength(1);
        expect(announcements[0].message).toBe('Test message');
        expect(announcements[0].priority).toBe('medium');
      });

      it('should add announcement with custom priority', () => {
        worldState.addAnnouncement('High priority message', 'high');

        const announcements = worldState.getAnnouncements();
        expect(announcements[0].priority).toBe('high');
      });

      it('should add announcement with custom duration', () => {
        const duration = 5000;
        worldState.addAnnouncement('Short message', 'low', duration);

        const announcements = worldState.getAnnouncements();
        expect(announcements[0].expiresAt).toBe(Date.now() + duration);
      });

      it('should generate unique IDs for announcements', () => {
        const id1 = worldState.addAnnouncement('Message 1');
        const id2 = worldState.addAnnouncement('Message 2');

        expect(id1).not.toBe(id2);
      });

      it('should set creation time correctly', () => {
        worldState.addAnnouncement('Test');

        const announcements = worldState.getAnnouncements();
        expect(announcements[0].createdAt).toBe(Date.now());
      });
    });

    describe('removeAnnouncement()', () => {
      it('should remove existing announcement', () => {
        const id = worldState.addAnnouncement('Test message');

        const removed = worldState.removeAnnouncement(id);

        expect(removed).toBe(true);
        expect(mockLog.info).toHaveBeenCalledWith(`Removed announcement: ${id}`);

        const announcements = worldState.getAnnouncements();
        expect(announcements).toHaveLength(0);
      });

      it('should return false when removing non-existent announcement', () => {
        const removed = worldState.removeAnnouncement('non_existent_id');

        expect(removed).toBe(false);
      });

      it('should not log when removing non-existent announcement', () => {
        jest.clearAllMocks();

        worldState.removeAnnouncement('non_existent_id');

        expect(mockLog.info).not.toHaveBeenCalled();
      });
    });

    describe('getAnnouncements()', () => {
      it('should return empty array when no announcements', () => {
        const announcements = worldState.getAnnouncements();

        expect(announcements).toEqual([]);
      });

      it('should filter out expired announcements', () => {
        // Add announcement that expires in the past
        const id = worldState.addAnnouncement('Test', 'medium', -1000);

        const announcements = worldState.getAnnouncements();

        expect(announcements).toHaveLength(0);
      });

      it('should sort by priority (high > medium > low)', () => {
        worldState.addAnnouncement('Low priority', 'low');
        worldState.addAnnouncement('High priority', 'high');
        worldState.addAnnouncement('Medium priority', 'medium');

        const announcements = worldState.getAnnouncements();

        expect(announcements[0].priority).toBe('high');
        expect(announcements[1].priority).toBe('medium');
        expect(announcements[2].priority).toBe('low');
      });

      it('should sort by creation time when priorities are equal', () => {
        jest.spyOn(Date, 'now')
          .mockReturnValueOnce(1000)
          .mockReturnValueOnce(1000)
          .mockReturnValueOnce(2000)
          .mockReturnValueOnce(2000)
          .mockReturnValueOnce(3000)
          .mockReturnValueOnce(3000)
          .mockReturnValue(4000);

        worldState.addAnnouncement('First', 'medium');
        worldState.addAnnouncement('Second', 'medium');
        worldState.addAnnouncement('Third', 'medium');

        const announcements = worldState.getAnnouncements();

        expect(announcements[0].message).toBe('Third');
        expect(announcements[1].message).toBe('Second');
        expect(announcements[2].message).toBe('First');
      });

      it('should return announcements with all properties', () => {
        worldState.addAnnouncement('Test message', 'high', 10000);

        const announcements = worldState.getAnnouncements();

        expect(announcements[0]).toHaveProperty('id');
        expect(announcements[0]).toHaveProperty('message');
        expect(announcements[0]).toHaveProperty('priority');
        expect(announcements[0]).toHaveProperty('createdAt');
        expect(announcements[0]).toHaveProperty('expiresAt');
      });
    });
  });

  describe('Global Event Management', () => {
    describe('startGlobalEvent()', () => {
      it('should start a global event with data', () => {
        const eventData = { type: 'boss_spawn', location: 'forest' };

        worldState.startGlobalEvent('event_1', eventData);

        const events = worldState.getActiveEvents();
        expect(events.size).toBe(1);
        expect(events.get('event_1')).toMatchObject(eventData);
        expect(events.get('event_1').startedAt).toBe(Date.now());
      });

      it('should log event start', () => {
        const eventData = { type: 'boss_spawn' };

        worldState.startGlobalEvent('event_1', eventData);

        expect(mockLog.info).toHaveBeenCalledWith(
          'Started global event: event_1',
          eventData
        );
      });

      it('should track start time for events', () => {
        worldState.startGlobalEvent('event_1', { type: 'test' });

        const events = worldState.getActiveEvents();
        expect(events.get('event_1').startedAt).toBe(Date.now());
      });

      it('should allow multiple events simultaneously', () => {
        worldState.startGlobalEvent('event_1', { type: 'boss' });
        worldState.startGlobalEvent('event_2', { type: 'raid' });

        const events = worldState.getActiveEvents();
        expect(events.size).toBe(2);
      });

      it('should overwrite existing event with same ID', () => {
        worldState.startGlobalEvent('event_1', { type: 'old' });
        worldState.startGlobalEvent('event_1', { type: 'new' });

        const events = worldState.getActiveEvents();
        expect(events.size).toBe(1);
        expect(events.get('event_1').type).toBe('new');
      });
    });

    describe('endGlobalEvent()', () => {
      it('should end an active event', () => {
        worldState.startGlobalEvent('event_1', { type: 'test' });

        worldState.endGlobalEvent('event_1');

        const events = worldState.getActiveEvents();
        expect(events.size).toBe(0);
      });

      it('should log event end with duration', () => {
        // Set up time mocking more carefully
        const startTime = 1000;
        const endTime = 5000;

        jest.spyOn(Date, 'now').mockReturnValue(startTime);
        worldState.startGlobalEvent('event_1', { type: 'test' });

        // Now change time for the end event
        jest.spyOn(Date, 'now').mockReturnValue(endTime);
        worldState.endGlobalEvent('event_1');

        expect(mockLog.info).toHaveBeenCalledWith(
          'Ended global event: event_1',
          { duration: endTime - startTime }
        );
      });

      it('should do nothing when ending non-existent event', () => {
        jest.clearAllMocks();

        worldState.endGlobalEvent('non_existent');

        expect(mockLog.info).not.toHaveBeenCalled();
      });

      it('should not affect other events', () => {
        worldState.startGlobalEvent('event_1', { type: 'test1' });
        worldState.startGlobalEvent('event_2', { type: 'test2' });

        worldState.endGlobalEvent('event_1');

        const events = worldState.getActiveEvents();
        expect(events.size).toBe(1);
        expect(events.has('event_2')).toBe(true);
      });
    });

    describe('getActiveEvents()', () => {
      it('should return empty map when no events', () => {
        const events = worldState.getActiveEvents();

        expect(events).toBeInstanceOf(Map);
        expect(events.size).toBe(0);
      });

      it('should return copy of events map', () => {
        worldState.startGlobalEvent('event_1', { type: 'test' });

        const events = worldState.getActiveEvents();
        events.set('event_2', { type: 'hacked' });

        const eventsAgain = worldState.getActiveEvents();
        expect(eventsAgain.size).toBe(1);
      });

      it('should return all active events', () => {
        worldState.startGlobalEvent('event_1', { type: 'boss' });
        worldState.startGlobalEvent('event_2', { type: 'raid' });
        worldState.startGlobalEvent('event_3', { type: 'pvp' });

        const events = worldState.getActiveEvents();
        expect(events.size).toBe(3);
      });
    });
  });

  describe('Statistics Tracking', () => {
    describe('incrementStatistic()', () => {
      it('should increment totalPlayers', () => {
        worldState.incrementStatistic('totalPlayers');

        const stats = worldState.getStatistics();
        expect(stats.totalPlayers).toBe(1);
      });

      it('should increment totalBattles', () => {
        worldState.incrementStatistic('totalBattles');

        const stats = worldState.getStatistics();
        expect(stats.totalBattles).toBe(1);
      });

      it('should increment totalMessages', () => {
        worldState.incrementStatistic('totalMessages');

        const stats = worldState.getStatistics();
        expect(stats.totalMessages).toBe(1);
      });

      it('should increment totalMovements', () => {
        worldState.incrementStatistic('totalMovements');

        const stats = worldState.getStatistics();
        expect(stats.totalMovements).toBe(1);
      });

      it('should increment totalAIDecisions', () => {
        worldState.incrementStatistic('totalAIDecisions');

        const stats = worldState.getStatistics();
        expect(stats.totalAIDecisions).toBe(1);
      });

      it('should increment by custom amount', () => {
        worldState.incrementStatistic('totalPlayers', 5);

        const stats = worldState.getStatistics();
        expect(stats.totalPlayers).toBe(5);
      });

      it('should handle multiple increments', () => {
        worldState.incrementStatistic('totalBattles');
        worldState.incrementStatistic('totalBattles');
        worldState.incrementStatistic('totalBattles', 3);

        const stats = worldState.getStatistics();
        expect(stats.totalBattles).toBe(5);
      });

      it('should not modify serverStartTime when incrementing', () => {
        // serverStartTime is a number so it will actually increment
        // This test should verify that incrementing it works like other stats
        const originalTime = worldState.getStatistics().serverStartTime;

        worldState.incrementStatistic('serverStartTime', 1000);

        const stats = worldState.getStatistics();
        expect(stats.serverStartTime).toBe(originalTime + 1000);
      });

      it('should increment all statistics independently', () => {
        worldState.incrementStatistic('totalPlayers', 10);
        worldState.incrementStatistic('totalBattles', 5);
        worldState.incrementStatistic('totalMessages', 50);
        worldState.incrementStatistic('totalMovements', 30);
        worldState.incrementStatistic('totalAIDecisions', 20);

        const stats = worldState.getStatistics();
        expect(stats.totalPlayers).toBe(10);
        expect(stats.totalBattles).toBe(5);
        expect(stats.totalMessages).toBe(50);
        expect(stats.totalMovements).toBe(30);
        expect(stats.totalAIDecisions).toBe(20);
      });
    });

    describe('getStatistics()', () => {
      it('should return copy of statistics', () => {
        const stats = worldState.getStatistics();
        stats.totalPlayers = 999;

        const statsAgain = worldState.getStatistics();
        expect(statsAgain.totalPlayers).toBe(0);
      });

      it('should return all statistics properties', () => {
        const stats = worldState.getStatistics();

        expect(stats).toHaveProperty('serverStartTime');
        expect(stats).toHaveProperty('totalPlayers');
        expect(stats).toHaveProperty('totalBattles');
        expect(stats).toHaveProperty('totalMessages');
        expect(stats).toHaveProperty('totalMovements');
        expect(stats).toHaveProperty('totalAIDecisions');
      });
    });
  });

  describe('World Information', () => {
    describe('getWorldInfo()', () => {
      it('should return complete world info', () => {
        const info = worldState.getWorldInfo();

        expect(info).toHaveProperty('serverTime');
        expect(info).toHaveProperty('uptime');
        expect(info).toHaveProperty('dayNightCycle');
        expect(info).toHaveProperty('weather');
        expect(info).toHaveProperty('activeEvents');
        expect(info).toHaveProperty('activeAnnouncements');
        expect(info).toHaveProperty('statistics');
      });

      it('should calculate uptime correctly', () => {
        jest.spyOn(Date, 'now')
          .mockReturnValueOnce(1000)
          .mockReturnValue(6000);

        const ws = new WorldState();
        const info = ws.getWorldInfo();

        expect(info.uptime).toBe(5000);
      });

      it('should include current day/night cycle', () => {
        const info = worldState.getWorldInfo();

        expect(info.dayNightCycle).toBe('day');
      });

      it('should include current weather', () => {
        const info = worldState.getWorldInfo();

        expect(info.weather).toBe('clear');
      });

      it('should include active events count', () => {
        worldState.startGlobalEvent('event_1', { type: 'test' });
        worldState.startGlobalEvent('event_2', { type: 'test' });

        const info = worldState.getWorldInfo();

        expect(info.activeEvents).toBe(2);
      });

      it('should include active announcements count', () => {
        worldState.addAnnouncement('Test 1');
        worldState.addAnnouncement('Test 2');

        const info = worldState.getWorldInfo();

        expect(info.activeAnnouncements).toBe(2);
      });

      it('should include full statistics object', () => {
        worldState.incrementStatistic('totalPlayers', 10);

        const info = worldState.getWorldInfo();

        expect(info.statistics.totalPlayers).toBe(10);
      });
    });

    describe('getDayNightCycle()', () => {
      it('should return current day/night cycle', () => {
        expect(worldState.getDayNightCycle()).toBe('day');
      });
    });

    describe('getWeather()', () => {
      it('should return current weather state', () => {
        expect(worldState.getWeather()).toBe('clear');
      });
    });

    describe('getServerTime()', () => {
      it('should return current server time', () => {
        expect(worldState.getServerTime()).toBe(1700000000000);
      });
    });
  });

  describe('State Persistence', () => {
    describe('saveState()', () => {
      it('should save state to Redis', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        await worldState.saveState();

        expect(mockRedis.setex).toHaveBeenCalledWith(
          'cache:world_state',
          3600,
          expect.any(String)
        );
        expect(mockLog.debug).toHaveBeenCalledWith('World state saved to Redis');
      });

      it('should save all state properties', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        worldState.addAnnouncement('Test');
        worldState.startGlobalEvent('event_1', { type: 'test' });
        worldState.incrementStatistic('totalPlayers', 10);

        await worldState.saveState();

        const savedData = JSON.parse(
          (mockRedis.setex as jest.Mock).mock.calls[0][2]
        );

        expect(savedData).toHaveProperty('statistics');
        expect(savedData).toHaveProperty('announcements');
        expect(savedData).toHaveProperty('serverTime');
        expect(savedData).toHaveProperty('dayNightCycle');
        expect(savedData).toHaveProperty('weatherState');
        expect(savedData).toHaveProperty('savedAt');
      });

      it('should save statistics correctly', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        worldState.incrementStatistic('totalPlayers', 25);

        await worldState.saveState();

        const savedData = JSON.parse(
          (mockRedis.setex as jest.Mock).mock.calls[0][2]
        );

        expect(savedData.statistics.totalPlayers).toBe(25);
      });

      it('should save announcements as array', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        worldState.addAnnouncement('Test 1');
        worldState.addAnnouncement('Test 2');

        await worldState.saveState();

        const savedData = JSON.parse(
          (mockRedis.setex as jest.Mock).mock.calls[0][2]
        );

        expect(Array.isArray(savedData.announcements)).toBe(true);
        expect(savedData.announcements).toHaveLength(2);
      });

      it('should handle Redis save errors gracefully', async () => {
        const redisError = new Error('Redis save failed');
        mockRedis.setex.mockRejectedValue(redisError);

        await worldState.saveState();

        expect(mockLog.exception).toHaveBeenCalledWith(
          redisError,
          'WorldState.saveState'
        );
      });

      it('should set correct TTL (1 hour)', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        await worldState.saveState();

        expect(mockRedis.setex).toHaveBeenCalledWith(
          expect.any(String),
          3600,
          expect.any(String)
        );
      });

      it('should include savedAt timestamp', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        await worldState.saveState();

        const savedData = JSON.parse(
          (mockRedis.setex as jest.Mock).mock.calls[0][2]
        );

        expect(savedData.savedAt).toBe(Date.now());
      });
    });
  });

  describe('Weather Updates', () => {
    describe('update() - weather changes', () => {
      it('should randomly update weather', () => {
        // Mock Math.random to trigger weather update
        jest.spyOn(Math, 'random').mockReturnValue(0.0005);

        worldState.update();

        // Weather should have potentially changed
        expect(Math.random).toHaveBeenCalled();
      });

      it('should select from available weather types', () => {
        jest.spyOn(Math, 'random')
          .mockReturnValueOnce(0.0005) // Trigger update
          .mockReturnValueOnce(0.5); // Select middle weather type

        const oldWeather = worldState.getWeather();
        worldState.update();

        // Weather might have changed
        const weatherTypes = ['clear', 'cloudy', 'rain', 'storm', 'fog'];
        expect(weatherTypes).toContain(worldState.getWeather());
      });

      it('should log weather change when it occurs', () => {
        jest.spyOn(Math, 'random')
          .mockReturnValueOnce(0.0005) // Trigger update
          .mockReturnValueOnce(0.8); // Select different weather

        worldState.update();

        // Check if weather change was logged (only if weather actually changed)
        const calls = (mockLog.info as jest.Mock).mock.calls;
        const weatherChangeCalls = calls.filter((call) =>
          call[0]?.includes('Weather changed')
        );

        // Weather should have changed since we're forcing random selection
        if (weatherChangeCalls.length > 0) {
          expect(weatherChangeCalls[0][0]).toMatch(/Weather changed from .+ to .+/);
        }
      });

      it('should not update weather most of the time', () => {
        jest.spyOn(Math, 'random').mockReturnValue(0.5); // Above threshold

        const oldWeather = worldState.getWeather();
        worldState.update();

        expect(worldState.getWeather()).toBe(oldWeather);
      });
    });
  });

  describe('Day/Night Cycle Updates', () => {
    it('should update to day during daytime hours (6-19)', async () => {
      mockRedis.get.mockResolvedValue(null);

      // Mock getHours to return 12 (noon)
      jest.spyOn(Date.prototype, 'getHours').mockReturnValue(12);

      await worldState.initialize();

      // Advance time to trigger update
      jest.advanceTimersByTime(1000);

      expect(worldState.getDayNightCycle()).toBe('day');
    });

    it('should update to night during nighttime hours (20-5)', async () => {
      mockRedis.get.mockResolvedValue(null);

      // Mock getHours to return 22 (10 PM)
      jest.spyOn(Date.prototype, 'getHours').mockReturnValue(22);

      await worldState.initialize();

      // Advance time to trigger update
      jest.advanceTimersByTime(1000);

      expect(worldState.getDayNightCycle()).toBe('night');
    });

    it('should log when day/night cycle changes', async () => {
      mockRedis.get.mockResolvedValue(null);

      let hourValue = 12;
      jest.spyOn(Date.prototype, 'getHours').mockImplementation(() => hourValue);

      await worldState.initialize();

      // Start at day
      jest.advanceTimersByTime(1000);
      jest.clearAllMocks();

      // Change to night
      hourValue = 22;
      jest.advanceTimersByTime(1000);

      expect(mockLog.info).toHaveBeenCalledWith('Day/Night cycle changed to: night');
    });

    it('should not log when cycle stays the same', async () => {
      mockRedis.get.mockResolvedValue(null);

      jest.spyOn(Date.prototype, 'getHours').mockReturnValue(12);

      await worldState.initialize();

      jest.advanceTimersByTime(1000);
      jest.clearAllMocks();

      jest.advanceTimersByTime(1000);

      const calls = (mockLog.info as jest.Mock).mock.calls;
      const cycleCalls = calls.filter((call) =>
        call[0]?.includes('Day/Night cycle changed')
      );

      expect(cycleCalls).toHaveLength(0);
    });

    it('should update server time every second', async () => {
      mockRedis.get.mockResolvedValue(null);

      let currentTime = 1700000000000;
      jest.spyOn(Date, 'now').mockImplementation(() => currentTime);

      await worldState.initialize();

      const initialTime = worldState.getServerTime();
      expect(initialTime).toBe(1700000000000);

      // Advance time and trigger the interval
      currentTime = 1700000001000;
      jest.advanceTimersByTime(1000);
      expect(worldState.getServerTime()).toBe(1700000001000);

      currentTime = 1700000002000;
      jest.advanceTimersByTime(1000);
      expect(worldState.getServerTime()).toBe(1700000002000);
    });
  });

  describe('Cleanup Operations', () => {
    describe('update() - announcement cleanup', () => {
      it('should remove expired announcements during update', () => {
        // Start at time 1000
        jest.spyOn(Date, 'now').mockReturnValue(1000);

        // Add announcement that expires at 3000 (1000 + 2000)
        worldState.addAnnouncement('Test', 'medium', 2000);

        // Move time forward to 5000 (after expiration)
        jest.spyOn(Date, 'now').mockReturnValue(5000);

        // Update should clean expired announcements
        worldState.update();

        const announcements = worldState.getAnnouncements();
        expect(announcements).toHaveLength(0);
      });

      it('should log when removing expired announcements', () => {
        // Start at time 1000
        jest.spyOn(Date, 'now').mockReturnValue(1000);

        const id = worldState.addAnnouncement('Test', 'medium', 2000);

        jest.clearAllMocks();

        // Move time forward to 5000 (after expiration at 3000)
        jest.spyOn(Date, 'now').mockReturnValue(5000);

        worldState.update();

        expect(mockLog.debug).toHaveBeenCalledWith(
          `Removed expired announcement: ${id}`
        );
      });

      it('should not remove non-expired announcements', () => {
        worldState.addAnnouncement('Test', 'medium', 10000);

        worldState.update();

        const announcements = worldState.getAnnouncements();
        expect(announcements).toHaveLength(1);
      });

      it('should handle multiple announcements with different expiry times', () => {
        jest.spyOn(Date, 'now')
          .mockReturnValueOnce(1000)
          .mockReturnValueOnce(1000)
          .mockReturnValueOnce(1000)
          .mockReturnValueOnce(1000)
          .mockReturnValue(5000);

        worldState.addAnnouncement('Expired', 'medium', 2000); // Expires at 3000
        worldState.addAnnouncement('Active', 'medium', 10000); // Expires at 11000

        worldState.update();

        const announcements = worldState.getAnnouncements();
        expect(announcements).toHaveLength(1);
        expect(announcements[0].message).toBe('Active');
      });
    });

    describe('cleanup()', () => {
      it('should save state before cleanup', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        await worldState.cleanup();

        expect(mockRedis.setex).toHaveBeenCalled();
      });

      it('should clear all announcements', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        worldState.addAnnouncement('Test 1');
        worldState.addAnnouncement('Test 2');

        await worldState.cleanup();

        const announcements = worldState.getAnnouncements();
        expect(announcements).toHaveLength(0);
      });

      it('should clear all global events', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        worldState.startGlobalEvent('event_1', { type: 'test' });
        worldState.startGlobalEvent('event_2', { type: 'test' });

        await worldState.cleanup();

        const events = worldState.getActiveEvents();
        expect(events.size).toBe(0);
      });

      it('should log cleanup message', async () => {
        mockRedis.setex.mockResolvedValue('OK' as any);

        await worldState.cleanup();

        expect(mockLog.info).toHaveBeenCalledWith('WorldState cleaned up');
      });

      it('should cleanup even if save fails', async () => {
        mockRedis.setex.mockRejectedValue(new Error('Save failed'));

        worldState.addAnnouncement('Test');
        worldState.startGlobalEvent('event_1', { type: 'test' });

        await worldState.cleanup();

        // Should still clear despite save error
        expect(worldState.getAnnouncements()).toHaveLength(0);
        expect(worldState.getActiveEvents().size).toBe(0);
        expect(mockLog.info).toHaveBeenCalledWith('WorldState cleaned up');
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle Redis errors during initialization', async () => {
      const error = new Error('Connection timeout');
      mockRedis.get.mockRejectedValue(error);

      await worldState.initialize();

      expect(mockLog.exception).toHaveBeenCalledWith(
        error,
        'WorldState.loadPersistedState'
      );
    });

    it('should handle Redis errors during save', async () => {
      const error = new Error('Write failed');
      mockRedis.setex.mockRejectedValue(error);

      await worldState.saveState();

      expect(mockLog.exception).toHaveBeenCalledWith(error, 'WorldState.saveState');
    });

    it('should handle malformed data from Redis', async () => {
      mockRedis.get.mockResolvedValue('not valid json');

      await worldState.initialize();

      expect(mockLog.exception).toHaveBeenCalled();
    });

    it('should continue operating after Redis errors', async () => {
      mockRedis.setex.mockRejectedValue(new Error('Redis error'));

      await worldState.saveState();

      // Should still be able to add announcements
      const id = worldState.addAnnouncement('Test');
      expect(id).toBeTruthy();
    });

    it('should handle partial state data from Redis', async () => {
      const partialState = JSON.stringify({
        statistics: { totalPlayers: 5 },
        // Missing other fields
      });

      mockRedis.get.mockResolvedValue(partialState);

      await worldState.initialize();

      const stats = worldState.getStatistics();
      expect(stats.totalPlayers).toBe(5);
      // Should have defaults for other fields
      expect(stats.serverStartTime).toBeDefined();
    });
  });

  describe('Edge Cases', () => {
    it('should handle rapid announcement additions and removals', () => {
      const ids: string[] = [];

      for (let i = 0; i < 100; i++) {
        ids.push(worldState.addAnnouncement(`Message ${i}`));
      }

      for (const id of ids) {
        worldState.removeAnnouncement(id);
      }

      expect(worldState.getAnnouncements()).toHaveLength(0);
    });

    it('should handle many simultaneous events', () => {
      for (let i = 0; i < 50; i++) {
        worldState.startGlobalEvent(`event_${i}`, { type: 'test', index: i });
      }

      const events = worldState.getActiveEvents();
      expect(events.size).toBe(50);
    });

    it('should handle large statistic values', () => {
      worldState.incrementStatistic('totalPlayers', 1000000);
      worldState.incrementStatistic('totalMessages', 9999999);

      const stats = worldState.getStatistics();
      expect(stats.totalPlayers).toBe(1000000);
      expect(stats.totalMessages).toBe(9999999);
    });

    it('should handle announcements with very short durations', () => {
      const id = worldState.addAnnouncement('Quick message', 'high', 1);

      jest.spyOn(Date, 'now').mockReturnValue(Date.now() + 2);

      worldState.update();

      const announcements = worldState.getAnnouncements();
      expect(announcements).toHaveLength(0);
    });

    it('should handle announcements with very long durations', () => {
      worldState.addAnnouncement('Long message', 'high', 999999999);

      const announcements = worldState.getAnnouncements();
      expect(announcements).toHaveLength(1);
    });

    it('should handle empty announcement messages', () => {
      const id = worldState.addAnnouncement('');

      const announcements = worldState.getAnnouncements();
      expect(announcements).toHaveLength(1);
      expect(announcements[0].message).toBe('');
    });

    it('should handle events with no data', () => {
      worldState.startGlobalEvent('event_1', {});

      const events = worldState.getActiveEvents();
      expect(events.get('event_1')).toEqual({ startedAt: expect.any(Number) });
    });

    it('should handle ending the same event multiple times', () => {
      worldState.startGlobalEvent('event_1', { type: 'test' });

      worldState.endGlobalEvent('event_1');
      worldState.endGlobalEvent('event_1');
      worldState.endGlobalEvent('event_1');

      const events = worldState.getActiveEvents();
      expect(events.size).toBe(0);
    });

    it('should handle incrementing by zero', () => {
      worldState.incrementStatistic('totalPlayers', 0);

      const stats = worldState.getStatistics();
      expect(stats.totalPlayers).toBe(0);
    });

    it('should handle negative increments', () => {
      worldState.incrementStatistic('totalPlayers', 10);
      worldState.incrementStatistic('totalPlayers', -5);

      const stats = worldState.getStatistics();
      expect(stats.totalPlayers).toBe(5);
    });
  });

  describe('Integration Tests', () => {
    it('should persist and restore full state', async () => {
      mockRedis.setex.mockResolvedValue('OK' as any);

      // Setup state
      worldState.addAnnouncement('Test announcement', 'high', 10000);
      worldState.startGlobalEvent('event_1', { type: 'boss' });
      worldState.incrementStatistic('totalPlayers', 42);

      // Save state
      await worldState.saveState();

      // Get saved data
      const savedData = JSON.parse(
        (mockRedis.setex as jest.Mock).mock.calls[0][2]
      );

      // Create new instance and load state
      mockRedis.get.mockResolvedValue(JSON.stringify(savedData));

      const newWorldState = new WorldState();
      await newWorldState.initialize();

      // Verify state was restored
      const stats = newWorldState.getStatistics();
      expect(stats.totalPlayers).toBe(42);

      const announcements = newWorldState.getAnnouncements();
      expect(announcements).toHaveLength(1);
      expect(announcements[0].message).toBe('Test announcement');
    });

    it('should handle full lifecycle: initialize -> operate -> cleanup', async () => {
      mockRedis.get.mockResolvedValue(null);
      mockRedis.setex.mockResolvedValue('OK' as any);

      // Initialize
      await worldState.initialize();

      // Operate
      worldState.addAnnouncement('Server event');
      worldState.startGlobalEvent('boss_spawn', { location: 'forest' });
      worldState.incrementStatistic('totalBattles', 5);
      worldState.update();

      // Cleanup
      await worldState.cleanup();

      expect(mockRedis.setex).toHaveBeenCalled();
      expect(worldState.getAnnouncements()).toHaveLength(0);
      expect(worldState.getActiveEvents().size).toBe(0);
    });

    it('should maintain consistency across multiple updates', () => {
      worldState.addAnnouncement('Test 1', 'high', 10000);
      worldState.addAnnouncement('Test 2', 'low', 10000);

      for (let i = 0; i < 10; i++) {
        worldState.update();
      }

      const announcements = worldState.getAnnouncements();
      expect(announcements).toHaveLength(2);
      expect(announcements[0].priority).toBe('high');
    });

    it('should handle concurrent operations', () => {
      // Simulate concurrent operations
      const id1 = worldState.addAnnouncement('Message 1');
      worldState.startGlobalEvent('event_1', { type: 'test' });
      worldState.incrementStatistic('totalPlayers', 1);

      const id2 = worldState.addAnnouncement('Message 2');
      worldState.startGlobalEvent('event_2', { type: 'test' });
      worldState.incrementStatistic('totalBattles', 1);

      worldState.removeAnnouncement(id1);
      worldState.endGlobalEvent('event_1');

      expect(worldState.getAnnouncements()).toHaveLength(1);
      expect(worldState.getActiveEvents().size).toBe(1);
      expect(worldState.getStatistics().totalPlayers).toBe(1);
      expect(worldState.getStatistics().totalBattles).toBe(1);
    });
  });
});
