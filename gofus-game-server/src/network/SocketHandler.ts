import { Server as HTTPServer } from 'http';
import { Server, Socket } from 'socket.io';
import jwt from 'jsonwebtoken';
import { serverConfig } from '@/config/server.config';
import { GAME_CONSTANTS } from '@/config/game.config';
import { log } from '@/utils/Logger';
import { redis, REDIS_KEYS } from '@/config/database.config';

interface AuthenticatedSocket extends Socket {
  userId?: string;
  characterId?: string;
  playerId?: string;
  authenticated?: boolean;
}

interface AuthPayload {
  token: string;
  characterId?: string;
}

interface JWTPayload {
  userId: string;
  accountId: string;
  iat?: number;
  exp?: number;
}

export class SocketHandler {
  private io: Server;
  private connectedPlayers: Map<string, AuthenticatedSocket>;
  private playerSocketMap: Map<string, string>; // playerId -> socketId
  private rateLimiters: Map<string, number[]>; // socketId -> timestamps

  constructor(httpServer: HTTPServer) {
    this.io = new Server(httpServer, serverConfig.websocket);
    this.connectedPlayers = new Map();
    this.playerSocketMap = new Map();
    this.rateLimiters = new Map();

    this.setupMiddleware();
    this.setupEventHandlers();

    log.info('Socket.IO server initialized');
  }

  private setupMiddleware(): void {
    // Authentication middleware
    this.io.use(async (socket: AuthenticatedSocket, next) => {
      try {
        const token = socket.handshake.auth.token;

        if (!token) {
          return next(new Error('No token provided'));
        }

        // Verify JWT token
        const decoded = jwt.verify(
          token,
          serverConfig.security.jwtSecret
        ) as JWTPayload;

        // Check if session exists in Redis
        const sessionKey = `${REDIS_KEYS.SESSION}${decoded.userId}`;
        const session = await redis.get(sessionKey);

        if (!session) {
          return next(new Error('Session expired'));
        }

        socket.userId = decoded.userId;
        socket.authenticated = true;

        log.network('Socket authenticated', socket.id, {
          userId: decoded.userId,
        });

        next();
      } catch (error) {
        log.error('Socket authentication failed', { error, socketId: socket.id });
        next(new Error('Authentication failed'));
      }
    });

    // Rate limiting middleware
    this.io.use((socket: AuthenticatedSocket, next) => {
      if (!this.checkRateLimit(socket.id)) {
        return next(new Error('Rate limit exceeded'));
      }
      next();
    });
  }

  private setupEventHandlers(): void {
    this.io.on('connection', (socket: AuthenticatedSocket) => {
      log.info('Client connected', {
        socketId: socket.id,
        userId: socket.userId
      });

      // Store connection
      this.connectedPlayers.set(socket.id, socket);

      // Setup individual socket handlers
      this.setupSocketHandlers(socket);

      // Handle disconnection
      socket.on('disconnect', (reason) => {
        this.handleDisconnect(socket, reason);
      });
    });
  }

  private setupSocketHandlers(socket: AuthenticatedSocket): void {
    // Authentication complete
    socket.on('authenticate', async (data: AuthPayload) => {
      await this.handleAuthentication(socket, data);
    });

    // Movement events
    socket.on('movement:request', (data) => {
      this.handleMovementRequest(socket, data);
    });

    // Combat events
    socket.on('combat:action', (data) => {
      this.handleCombatAction(socket, data);
    });

    // Chat events
    socket.on('chat:message', (data) => {
      this.handleChatMessage(socket, data);
    });

    socket.on('chat:private', (data) => {
      this.handlePrivateMessage(socket, data);
    });

    // Map events
    socket.on('map:enter', (data) => {
      this.handleMapEnter(socket, data);
    });

    socket.on('map:leave', (data) => {
      this.handleMapLeave(socket, data);
    });

    // System events
    socket.on('ping', () => {
      socket.emit('pong', { timestamp: Date.now() });
    });

    // Error handler
    socket.on('error', (error) => {
      log.error('Socket error', {
        socketId: socket.id,
        error: error.message,
      });
    });
  }

  private async handleAuthentication(
    socket: AuthenticatedSocket,
    data: AuthPayload
  ): Promise<void> {
    try {
      const { characterId } = data;

      if (!characterId) {
        socket.emit('auth:error', { message: 'Character ID required' });
        return;
      }

      // Verify character ownership
      // This would typically check the database
      socket.characterId = characterId;
      socket.playerId = `${socket.userId}:${characterId}`;

      // Map player to socket
      this.playerSocketMap.set(socket.playerId, socket.id);

      // Store player data in Redis
      const playerKey = `${REDIS_KEYS.PLAYER}${socket.playerId}`;
      await redis.setex(
        playerKey,
        3600,
        JSON.stringify({
          socketId: socket.id,
          characterId,
          userId: socket.userId,
          connectedAt: Date.now(),
        })
      );

      // Join character-specific room
      socket.join(`player:${socket.playerId}`);

      socket.emit('auth:success', {
        playerId: socket.playerId,
        characterId,
      });

      log.player(socket.playerId, 'authenticated', { characterId });
    } catch (error) {
      log.exception(error as Error, 'handleAuthentication');
      socket.emit('auth:error', { message: 'Authentication failed' });
    }
  }

  private handleMovementRequest(
    socket: AuthenticatedSocket,
    data: any
  ): void {
    if (!this.validatePlayer(socket)) return;

    // Rate limit check
    if (!this.checkActionRateLimit(socket.id, 'movement')) {
      socket.emit('movement:error', { message: 'Too many movement requests' });
      return;
    }

    log.player(socket.playerId!, 'movement', data);

    // Movement logic would be handled by MovementManager
    // This is just the network layer
    socket.emit('movement:processing', { requestId: data.requestId });
  }

  private handleCombatAction(
    socket: AuthenticatedSocket,
    data: any
  ): void {
    if (!this.validatePlayer(socket)) return;

    log.player(socket.playerId!, 'combat_action', data);

    // Combat logic would be handled by CombatManager
    socket.emit('combat:processing', { actionId: data.actionId });
  }

  private handleChatMessage(
    socket: AuthenticatedSocket,
    data: any
  ): void {
    if (!this.validatePlayer(socket)) return;

    // Rate limit chat messages
    if (!this.checkActionRateLimit(socket.id, 'chat', 10)) {
      socket.emit('chat:error', { message: 'Sending messages too quickly' });
      return;
    }

    const { channel, message } = data;

    // Validate message
    if (!message || message.length > GAME_CONSTANTS.CHAT_MESSAGE_MAX_LENGTH) {
      socket.emit('chat:error', { message: 'Invalid message' });
      return;
    }

    log.player(socket.playerId!, 'chat', { channel, message });

    // Broadcast to channel
    this.io.to(`channel:${channel}`).emit('chat:message', {
      playerId: socket.playerId,
      channel,
      message,
      timestamp: Date.now(),
    });
  }

  private handlePrivateMessage(
    socket: AuthenticatedSocket,
    data: any
  ): void {
    if (!this.validatePlayer(socket)) return;

    const { targetPlayerId, message } = data;

    // Find target socket
    const targetSocketId = this.playerSocketMap.get(targetPlayerId);
    if (!targetSocketId) {
      socket.emit('chat:error', { message: 'Player not online' });
      return;
    }

    log.player(socket.playerId!, 'private_message', { targetPlayerId });

    // Send to target
    this.io.to(targetSocketId).emit('chat:private', {
      from: socket.playerId,
      message,
      timestamp: Date.now(),
    });
  }

  private handleMapEnter(
    socket: AuthenticatedSocket,
    data: any
  ): void {
    if (!this.validatePlayer(socket)) return;

    const { mapId } = data;

    // Join map room
    socket.join(`map:${mapId}`);

    log.player(socket.playerId!, 'map_enter', { mapId });

    // Notify other players in map
    socket.to(`map:${mapId}`).emit('entity:spawn', {
      playerId: socket.playerId,
      mapId,
      timestamp: Date.now(),
    });
  }

  private handleMapLeave(
    socket: AuthenticatedSocket,
    data: any
  ): void {
    if (!this.validatePlayer(socket)) return;

    const { mapId } = data;

    // Leave map room
    socket.leave(`map:${mapId}`);

    log.player(socket.playerId!, 'map_leave', { mapId });

    // Notify other players
    this.io.to(`map:${mapId}`).emit('entity:despawn', {
      playerId: socket.playerId,
      mapId,
      timestamp: Date.now(),
    });
  }

  private handleDisconnect(
    socket: AuthenticatedSocket,
    reason: string
  ): void {
    log.info('Client disconnected', {
      socketId: socket.id,
      userId: socket.userId,
      reason,
    });

    // Clean up player data
    if (socket.playerId) {
      this.playerSocketMap.delete(socket.playerId);

      // Remove from Redis
      const playerKey = `${REDIS_KEYS.PLAYER}${socket.playerId}`;
      redis.del(playerKey);

      log.player(socket.playerId, 'disconnected', { reason });
    }

    // Remove from connected players
    this.connectedPlayers.delete(socket.id);
    this.rateLimiters.delete(socket.id);
  }

  private validatePlayer(socket: AuthenticatedSocket): boolean {
    if (!socket.authenticated || !socket.playerId) {
      socket.emit('error', { message: 'Not authenticated' });
      return false;
    }
    return true;
  }

  private checkRateLimit(socketId: string): boolean {
    const now = Date.now();
    const timestamps = this.rateLimiters.get(socketId) || [];

    // Remove old timestamps
    const recentTimestamps = timestamps.filter(
      (ts) => now - ts < serverConfig.security.rateLimitWindow
    );

    if (recentTimestamps.length >= serverConfig.security.rateLimitMaxRequests) {
      return false;
    }

    recentTimestamps.push(now);
    this.rateLimiters.set(socketId, recentTimestamps);

    return true;
  }

  private checkActionRateLimit(
    socketId: string,
    action: string,
    maxPerMinute: number = 30
  ): boolean {
    const key = `${socketId}:${action}`;
    const now = Date.now();
    const timestamps = this.rateLimiters.get(key) || [];

    const recentTimestamps = timestamps.filter(
      (ts) => now - ts < 60000 // 1 minute
    );

    if (recentTimestamps.length >= maxPerMinute) {
      return false;
    }

    recentTimestamps.push(now);
    this.rateLimiters.set(key, recentTimestamps);

    return true;
  }

  // Public methods for other managers to use
  public broadcastToMap(mapId: number, event: string, data: any): void {
    this.io.to(`map:${mapId}`).emit(event, data);
  }

  public sendToPlayer(playerId: string, event: string, data: any): void {
    const socketId = this.playerSocketMap.get(playerId);
    if (socketId) {
      this.io.to(socketId).emit(event, data);
    }
  }

  public getOnlinePlayerCount(): number {
    return this.playerSocketMap.size;
  }

  public getIO(): Server {
    return this.io;
  }
}

export default SocketHandler;