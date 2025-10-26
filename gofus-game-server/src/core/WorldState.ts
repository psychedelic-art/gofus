import { redis, REDIS_KEYS } from '@/config/database.config';
import { log } from '@/utils/Logger';

export interface WorldStatistics {
  serverStartTime: number;
  totalPlayers: number;
  totalBattles: number;
  totalMessages: number;
  totalMovements: number;
  totalAIDecisions: number;
}

export interface ServerAnnouncement {
  id: string;
  message: string;
  priority: 'low' | 'medium' | 'high';
  createdAt: number;
  expiresAt: number;
}

export class WorldState {
  private statistics: WorldStatistics;
  private announcements: Map<string, ServerAnnouncement>;
  private globalEvents: Map<string, any>;
  private serverTime: number;
  private dayNightCycle: 'day' | 'night';
  private weatherState: string;

  constructor() {
    this.statistics = {
      serverStartTime: Date.now(),
      totalPlayers: 0,
      totalBattles: 0,
      totalMessages: 0,
      totalMovements: 0,
      totalAIDecisions: 0,
    };

    this.announcements = new Map();
    this.globalEvents = new Map();
    this.serverTime = Date.now();
    this.dayNightCycle = 'day';
    this.weatherState = 'clear';
  }

  public async initialize(): Promise<void> {
    log.info('Initializing WorldState...');

    // Load persisted world state from Redis
    await this.loadPersistedState();

    // Start time tracking
    this.startTimeTracking();

    log.info('WorldState initialized');
  }

  private async loadPersistedState(): Promise<void> {
    try {
      const stateKey = `${REDIS_KEYS.CACHE}world_state`;
      const savedState = await redis.get(stateKey);

      if (savedState) {
        const parsed = JSON.parse(savedState);
        this.statistics = { ...this.statistics, ...parsed.statistics };

        // Load announcements
        if (parsed.announcements) {
          for (const announcement of parsed.announcements) {
            if (announcement.expiresAt > Date.now()) {
              this.announcements.set(announcement.id, announcement);
            }
          }
        }

        log.info('Loaded persisted world state');
      }
    } catch (error) {
      log.exception(error as Error, 'WorldState.loadPersistedState');
    }
  }

  private startTimeTracking(): void {
    // Update server time every second
    setInterval(() => {
      this.serverTime = Date.now();
      this.updateDayNightCycle();
    }, 1000);
  }

  private updateDayNightCycle(): void {
    const hour = new Date().getHours();
    const newCycle = hour >= 6 && hour < 20 ? 'day' : 'night';

    if (newCycle !== this.dayNightCycle) {
      this.dayNightCycle = newCycle;
      log.info(`Day/Night cycle changed to: ${newCycle}`);
    }
  }

  public update(): void {
    // Clean expired announcements
    const now = Date.now();
    for (const [id, announcement] of this.announcements.entries()) {
      if (announcement.expiresAt <= now) {
        this.announcements.delete(id);
        log.debug(`Removed expired announcement: ${id}`);
      }
    }

    // Update weather (simplified - could be more complex)
    if (Math.random() < 0.001) { // 0.1% chance per tick
      this.updateWeather();
    }
  }

  private updateWeather(): void {
    const weatherTypes = ['clear', 'cloudy', 'rain', 'storm', 'fog'];
    const oldWeather = this.weatherState;
    this.weatherState = weatherTypes[Math.floor(Math.random() * weatherTypes.length)];

    if (oldWeather !== this.weatherState) {
      log.info(`Weather changed from ${oldWeather} to ${this.weatherState}`);
    }
  }

  public async saveState(): Promise<void> {
    try {
      const stateKey = `${REDIS_KEYS.CACHE}world_state`;
      const state = {
        statistics: this.statistics,
        announcements: Array.from(this.announcements.values()),
        serverTime: this.serverTime,
        dayNightCycle: this.dayNightCycle,
        weatherState: this.weatherState,
        savedAt: Date.now(),
      };

      await redis.setex(stateKey, 3600, JSON.stringify(state));
      log.debug('World state saved to Redis');
    } catch (error) {
      log.exception(error as Error, 'WorldState.saveState');
    }
  }

  // Announcement management
  public addAnnouncement(
    message: string,
    priority: 'low' | 'medium' | 'high' = 'medium',
    duration: number = 3600000 // 1 hour default
  ): string {
    const id = `announce_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    const announcement: ServerAnnouncement = {
      id,
      message,
      priority,
      createdAt: Date.now(),
      expiresAt: Date.now() + duration,
    };

    this.announcements.set(id, announcement);
    log.info(`Added announcement: ${message}`);

    return id;
  }

  public removeAnnouncement(id: string): boolean {
    const removed = this.announcements.delete(id);
    if (removed) {
      log.info(`Removed announcement: ${id}`);
    }
    return removed;
  }

  public getAnnouncements(): ServerAnnouncement[] {
    return Array.from(this.announcements.values())
      .filter(a => a.expiresAt > Date.now())
      .sort((a, b) => {
        // Sort by priority then by creation time
        const priorityOrder = { high: 0, medium: 1, low: 2 };
        if (priorityOrder[a.priority] !== priorityOrder[b.priority]) {
          return priorityOrder[a.priority] - priorityOrder[b.priority];
        }
        return b.createdAt - a.createdAt;
      });
  }

  // Global event management
  public startGlobalEvent(eventId: string, eventData: any): void {
    this.globalEvents.set(eventId, {
      ...eventData,
      startedAt: Date.now(),
    });

    log.info(`Started global event: ${eventId}`, eventData);
  }

  public endGlobalEvent(eventId: string): void {
    const event = this.globalEvents.get(eventId);
    if (event) {
      this.globalEvents.delete(eventId);
      const duration = Date.now() - event.startedAt;
      log.info(`Ended global event: ${eventId}`, { duration });
    }
  }

  public getActiveEvents(): Map<string, any> {
    return new Map(this.globalEvents);
  }

  // Statistics tracking
  public incrementStatistic(stat: keyof WorldStatistics, amount: number = 1): void {
    if (typeof this.statistics[stat] === 'number') {
      (this.statistics[stat] as number) += amount;
    }
  }

  public getStatistics(): WorldStatistics {
    return { ...this.statistics };
  }

  // World information
  public getWorldInfo() {
    return {
      serverTime: this.serverTime,
      uptime: Date.now() - this.statistics.serverStartTime,
      dayNightCycle: this.dayNightCycle,
      weather: this.weatherState,
      activeEvents: this.globalEvents.size,
      activeAnnouncements: this.announcements.size,
      statistics: this.statistics,
    };
  }

  public getDayNightCycle(): 'day' | 'night' {
    return this.dayNightCycle;
  }

  public getWeather(): string {
    return this.weatherState;
  }

  public getServerTime(): number {
    return this.serverTime;
  }

  // Cleanup
  public async cleanup(): Promise<void> {
    await this.saveState();
    this.announcements.clear();
    this.globalEvents.clear();
    log.info('WorldState cleaned up');
  }
}

export default WorldState;