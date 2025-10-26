# SocketHandler Test Suite

Comprehensive Jest test suite for the `SocketHandler` class located at `src/network/SocketHandler.ts`.

## Overview

This test suite provides complete coverage of the SocketHandler functionality, including:
- Socket.IO connection management
- JWT authentication and authorization
- Rate limiting mechanisms
- Real-time event handling (movement, combat, chat, maps)
- Player session management
- Redis integration
- Error handling and edge cases

## Test Statistics

- **Total Tests**: 50
- **Test Suites**: 1
- **Coverage Areas**: 13 major categories

## Test Categories

### 1. Constructor Tests (4 tests)
- Socket.IO server initialization
- Middleware setup verification
- Event handler registration
- Logging initialization

### 2. Authentication Middleware (4 tests)
- Token validation (missing, invalid, expired)
- JWT verification
- Redis session validation
- Successful authentication flow

### 3. Rate Limiting Middleware (2 tests)
- Normal request flow
- Rate limit enforcement

### 4. Socket Connection (2 tests)
- Connection logging
- Event handler setup

### 5. Player Authentication (3 tests)
- Character ID validation
- Successful player authentication
- Error handling during authentication

### 6. Movement Events (3 tests)
- Unauthenticated request rejection
- Valid movement processing
- Rate limit enforcement for movement

### 7. Combat Events (2 tests)
- Unauthenticated request rejection
- Valid combat action processing

### 8. Chat Events (5 tests)
- Unauthenticated message rejection
- Valid message broadcasting
- Empty message validation
- Max length validation
- Chat rate limiting

### 9. Private Messaging (3 tests)
- Unauthenticated message rejection
- Sending to online players
- Offline player handling

### 10. Map Events (3 tests)
- Map entry handling
- Map exit handling
- Unauthenticated event rejection

### 11. Disconnection Handling (2 tests)
- Player data cleanup
- Graceful handling without player ID

### 12. Ping/Pong Events (1 test)
- Latency monitoring

### 13. Error Events (1 test)
- Socket error logging

### 14. Public Methods (6 tests)
- `broadcastToMap()` - Broadcasting to all players in a map
- `sendToPlayer()` - Sending events to specific players
- `getOnlinePlayerCount()` - Player count tracking
- `getIO()` - Socket.IO instance access

### 15. JWT Token Validation (3 tests)
- Expired token handling
- Malformed token handling
- Valid token verification

### 16. Redis Operations (4 tests)
- Player data storage with correct keys
- TTL configuration
- Data cleanup on disconnect
- Connection error handling

### 17. Rate Limiting Edge Cases (2 tests)
- Rate limit reset after time window
- Independent rate limits per action type

## Running the Tests

### Run all SocketHandler tests
```bash
npm test -- src/network/__tests__/SocketHandler.test.ts
```

### Run with coverage
```bash
npm run test:coverage -- src/network/__tests__/SocketHandler.test.ts
```

### Run in watch mode
```bash
npm run test:watch -- src/network/__tests__/SocketHandler.test.ts
```

### Run silently (summary only)
```bash
npm test -- src/network/__tests__/SocketHandler.test.ts --silent
```

## Mocked Dependencies

The test suite mocks the following dependencies:

1. **socket.io** - Socket.IO server and socket instances
2. **jsonwebtoken** - JWT token verification
3. **Redis** - Redis client operations (get, set, setex, del)
4. **Logger** - Winston logger instance
5. **Config files** - Server and game configuration

## Key Testing Patterns

### 1. Middleware Testing
Tests verify that middleware functions correctly intercept and process socket connections:

```typescript
it('should reject connection when no token is provided', async () => {
  const mockNext = jest.fn();
  mockSocket.handshake.auth.token = undefined;

  await authMiddleware(mockSocket, mockNext);

  expect(mockNext).toHaveBeenCalledWith(new Error('No token provided'));
});
```

### 2. Event Handler Testing
Tests verify that event handlers are properly registered and execute correct logic:

```typescript
it('should process valid movement request', () => {
  const movementData = { requestId: 'req-1', x: 10, y: 20 };

  movementHandler(movementData);

  expect(log.player).toHaveBeenCalledWith('user-123:char-456', 'movement', movementData);
  expect(mockSocket.emit).toHaveBeenCalledWith('movement:processing', {
    requestId: 'req-1',
  });
});
```

### 3. Rate Limiting Testing
Tests verify that rate limiting correctly prevents abuse:

```typescript
it('should reject movement request when rate limit exceeded', () => {
  // Exhaust rate limit
  for (let i = 0; i < 30; i++) {
    movementHandler({ requestId: `req-${i}`, x: 10, y: 20 });
  }

  movementHandler({ requestId: 'req-final', x: 10, y: 20 });

  expect(mockSocket.emit).toHaveBeenCalledWith('movement:error', {
    message: 'Too many movement requests',
  });
});
```

### 4. Async Operation Testing
Tests verify Redis and other async operations:

```typescript
it('should store player data in Redis with correct key', async () => {
  (redis.setex as jest.Mock).mockResolvedValue('OK');

  await authenticateHandler({ characterId: 'char-456' });

  expect(redis.setex).toHaveBeenCalledWith(
    'player:user-123:char-456',
    3600,
    expect.stringContaining('"socketId":"test-socket-id"')
  );
});
```

## Test Data Conventions

- **Socket ID**: `test-socket-id`
- **User ID**: `user-123`
- **Character ID**: `char-456`
- **Player ID**: `user-123:char-456` (format: `userId:characterId`)
- **JWT Token**: `valid-token`
- **JWT Secret**: `test-secret`
- **Map ID**: `7411`
- **Session TTL**: `3600` seconds (1 hour)

## Error Scenarios Covered

1. Missing authentication token
2. Invalid/expired JWT tokens
3. Expired Redis sessions
4. Rate limit violations
5. Invalid message content
6. Offline player messaging
7. Unauthenticated event attempts
8. Redis connection failures
9. Malformed requests

## Success Scenarios Covered

1. Complete authentication flow
2. Player session creation
3. Movement event processing
4. Combat action handling
5. Chat message broadcasting
6. Private message delivery
7. Map transitions
8. Graceful disconnection
9. Public method operations
10. Rate limit resets

## Configuration

The tests use the following configuration values:

```typescript
serverConfig = {
  websocket: {
    cors: { origin: '*', credentials: true },
    pingTimeout: 60000,
    pingInterval: 25000,
  },
  security: {
    jwtSecret: 'test-secret',
    rateLimitWindow: 60000,
    rateLimitMaxRequests: 100,
  },
};

GAME_CONSTANTS = {
  CHAT_MESSAGE_MAX_LENGTH: 255,
};
```

## Future Enhancements

Potential areas for additional test coverage:

1. **Multi-player scenarios** - Testing interactions between multiple connected players
2. **Stress testing** - Testing with large numbers of concurrent connections
3. **Network failures** - Testing reconnection and recovery scenarios
4. **Performance testing** - Testing response times under load
5. **Integration tests** - Testing with real Redis and Socket.IO servers
6. **Security testing** - Testing for injection attacks and malicious payloads

## Troubleshooting

### Common Issues

1. **Tests timing out**
   - Increase Jest timeout: `jest.setTimeout(10000)`
   - Check for unresolved promises

2. **Mock not working**
   - Ensure mocks are defined before imports
   - Clear mocks between tests with `jest.clearAllMocks()`

3. **Redis errors**
   - Verify Redis mocks are properly configured
   - Check that async operations are properly awaited

## Contributing

When adding new tests:

1. Follow existing naming conventions
2. Group related tests in `describe` blocks
3. Use descriptive test names that explain what is being tested
4. Include both success and failure scenarios
5. Clean up state in `beforeEach` hooks
6. Document complex test scenarios with comments

## License

Part of the GOFUS Game Server project.
