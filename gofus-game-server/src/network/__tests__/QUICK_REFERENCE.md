# SocketHandler Tests - Quick Reference Card

## 🚀 Quick Start

```bash
# Run tests
npm test -- src/network/__tests__/SocketHandler.test.ts

# With coverage
npm run test:coverage -- src/network/__tests__/SocketHandler.test.ts

# Watch mode
npm run test:watch -- src/network/__tests__/SocketHandler.test.ts
```

## 📊 Test Coverage

| Metric     | Coverage |
|------------|----------|
| Statements | 99.35%   |
| Branches   | 97.87%   |
| Functions  | 100%     |
| Lines      | 100%     |

## 📝 Test Categories (50 Total)

| Category                  | Tests | Status |
|---------------------------|-------|--------|
| Constructor               | 4     | ✅     |
| Authentication Middleware | 4     | ✅     |
| Rate Limiting            | 2     | ✅     |
| Socket Connection        | 2     | ✅     |
| Player Authentication    | 3     | ✅     |
| Movement Events          | 3     | ✅     |
| Combat Events            | 2     | ✅     |
| Chat Events              | 5     | ✅     |
| Private Messaging        | 3     | ✅     |
| Map Events               | 3     | ✅     |
| Disconnection Handling   | 2     | ✅     |
| Ping/Pong               | 1     | ✅     |
| Error Events            | 1     | ✅     |
| Public Methods          | 6     | ✅     |
| JWT Validation          | 3     | ✅     |
| Redis Operations        | 4     | ✅     |
| Rate Limiting Edge Cases| 2     | ✅     |

## 🧪 Test Data

```typescript
// Standard test data
const TEST_DATA = {
  socketId: 'test-socket-id',
  userId: 'user-123',
  characterId: 'char-456',
  playerId: 'user-123:char-456', // Format: userId:characterId
  token: 'valid-token',
  jwtSecret: 'test-secret',
  mapId: 7411,
  sessionTTL: 3600, // 1 hour in seconds
};
```

## 🎯 Key Test Patterns

### Test Authentication
```typescript
it('should authenticate successfully', async () => {
  mockSocket.userId = 'user-123';
  mockSocket.authenticated = true;
  (redis.setex as jest.Mock).mockResolvedValue('OK');

  await authenticateHandler({ characterId: 'char-456' });

  expect(mockSocket.emit).toHaveBeenCalledWith('auth:success', {
    playerId: 'user-123:char-456',
    characterId: 'char-456',
  });
});
```

### Test Event Handler
```typescript
it('should process valid event', () => {
  mockSocket.authenticated = true;
  mockSocket.playerId = 'user-123:char-456';

  eventHandler(testData);

  expect(mockSocket.emit).toHaveBeenCalledWith('event:response', expectedData);
});
```

### Test Error Handling
```typescript
it('should reject unauthenticated request', () => {
  mockSocket.authenticated = false;

  eventHandler(testData);

  expect(mockSocket.emit).toHaveBeenCalledWith('error', {
    message: 'Not authenticated',
  });
});
```

### Test Rate Limiting
```typescript
it('should enforce rate limit', () => {
  for (let i = 0; i < 30; i++) {
    eventHandler({ requestId: `req-${i}` });
  }

  eventHandler({ requestId: 'req-final' });

  expect(mockSocket.emit).toHaveBeenCalledWith('event:error', {
    message: 'Rate limit exceeded',
  });
});
```

## 🔧 Mocked Dependencies

```typescript
jest.mock('socket.io');              // Socket.IO Server & Socket
jest.mock('jsonwebtoken');           // JWT verification
jest.mock('@/config/database.config'); // Redis client
jest.mock('@/utils/Logger');         // Logger instance
jest.mock('@/config/server.config'); // Server config
jest.mock('@/config/game.config');   // Game constants
```

## 🎭 Mock Setup

```typescript
// Socket mock
mockSocket = {
  id: 'test-socket-id',
  handshake: { auth: { token: 'valid-token' } },
  on: jest.fn(),
  emit: jest.fn(),
  join: jest.fn(),
  leave: jest.fn(),
  to: jest.fn().mockReturnThis(),
};

// Redis mock
(redis as any) = {
  get: jest.fn(),
  set: jest.fn(),
  setex: jest.fn(),
  del: jest.fn(),
};

// JWT mock
(jwt.verify as jest.Mock).mockReturnValue({
  userId: 'user-123',
  accountId: 'acc-123',
});
```

## 📋 Test Checklist

### Before Running
- [ ] All dependencies installed (`npm install`)
- [ ] TypeScript compiled (or using ts-jest)
- [ ] Environment variables set (optional for tests)

### After Adding Tests
- [ ] All tests pass
- [ ] Coverage > 95%
- [ ] No console warnings
- [ ] Test names are descriptive
- [ ] Both success and error cases covered
- [ ] Documentation updated

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| Tests timeout | Increase Jest timeout or check for unresolved promises |
| Mock not working | Ensure mock is defined before class instantiation |
| Redis errors | Verify Redis mocks are configured correctly |
| Coverage gaps | Check for uncovered branches or error paths |

## 📚 Documentation Files

- **SocketHandler.test.ts** - Main test file (33KB, 920+ lines)
- **README.md** - Detailed documentation (8.1KB)
- **TEST_SUMMARY.md** - Executive summary (11KB)
- **QUICK_REFERENCE.md** - This file

## 🔍 What's Tested

### ✅ Core Functionality
- Socket connection & initialization
- JWT authentication & authorization
- Redis session management
- Rate limiting (connection & action-based)
- Event handler registration

### ✅ Game Events
- Movement requests
- Combat actions
- Chat messages (public & private)
- Map transitions
- Player authentication

### ✅ Public API
- `broadcastToMap()` - Broadcast to map room
- `sendToPlayer()` - Send to specific player
- `getOnlinePlayerCount()` - Get player count
- `getIO()` - Get Socket.IO instance

### ✅ Error Scenarios
- Missing/invalid tokens
- Expired sessions
- Rate limit violations
- Invalid messages
- Offline players
- Redis failures

## 🎯 Coverage Goals

- Statements: ≥ 95% (Current: 99.35% ✅)
- Branches: ≥ 95% (Current: 97.87% ✅)
- Functions: ≥ 95% (Current: 100% ✅)
- Lines: ≥ 95% (Current: 100% ✅)

## ⚡ Performance

- Average test time: 1-7ms
- Full suite: ~5 seconds
- With coverage: ~15 seconds

## 🔐 Security Tests

- [x] JWT token validation
- [x] Session verification
- [x] Rate limiting enforcement
- [x] Input validation
- [x] Authorization checks
- [x] Error message sanitization

## 📊 Test Results Summary

```
Test Suites: 1 passed, 1 total
Tests:       50 passed, 50 total
Snapshots:   0 total
Time:        ~5 seconds
```

## 💡 Tips

1. **Run specific test**: Add `.only` to test
   ```typescript
   it.only('should test this', () => { ... });
   ```

2. **Skip test**: Add `.skip` to test
   ```typescript
   it.skip('should test this later', () => { ... });
   ```

3. **Debug test**: Add `console.log` or use `--verbose`
   ```bash
   npm test -- --verbose
   ```

4. **Update snapshots**: Use `-u` flag
   ```bash
   npm test -- -u
   ```

5. **Watch specific file**: Use `--watch` with pattern
   ```bash
   npm test -- --watch SocketHandler
   ```

## 🚦 CI/CD Ready

```yaml
# Example GitHub Actions
- name: Run SocketHandler tests
  run: npm test -- src/network/__tests__/SocketHandler.test.ts --ci

- name: Upload coverage
  run: npm run test:coverage -- src/network/__tests__/SocketHandler.test.ts
```

## 📞 Support

For questions about the tests:
1. Check README.md for detailed documentation
2. Review TEST_SUMMARY.md for coverage analysis
3. Examine test file comments for specific patterns
4. Refer to Jest documentation: https://jestjs.io

---

**Last Updated**: 2025-10-25
**Test Status**: ✅ All Passing
**Coverage**: 🟢 Excellent (99%+)
**Maintenance**: 🟢 Well Documented
