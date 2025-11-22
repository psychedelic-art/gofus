# 📡 GOFUS Backend API Reference

## Quick Reference for Unity Client Development

### 🔐 Authentication Endpoints

#### Login
```http
POST http://localhost:3000/api/auth/login
Content-Type: application/json

{
  "login": "username",
  "password": "password123"
}

Response:
{
  "token": "eyJhbGc...",
  "accountId": "uuid",
  "message": "Login successful"
}
```

#### Register
```http
POST http://localhost:3000/api/auth/register
Content-Type: application/json

{
  "login": "username",
  "password": "password123",
  "email": "optional@email.com"
}

Response:
{
  "accountId": "uuid",
  "message": "Account created successfully"
}
```

### 👥 Character Endpoints

#### Get All Characters
```http
GET http://localhost:3000/api/characters
Authorization: Bearer <token>

Response:
[
  {
    "id": 1,
    "name": "HeroName",
    "level": 1,
    "classId": 1,
    "sex": true,
    "mapId": 7411,
    "cellId": 285
  }
]
```

#### Create Character
```http
POST http://localhost:3000/api/characters
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "NewHero",
  "classId": 1,
  "sex": true
}

Response:
{
  "characterId": 2,
  "message": "Character created successfully"
}
```

### 🎮 WebSocket Connection (Game Server)

#### Connect and Authenticate
```javascript
// Unity C# equivalent needed
const socket = io('ws://localhost:3001', {
  auth: {
    token: 'your-jwt-token'
  }
});

socket.emit('authenticate', {
  characterId: 1
});

socket.on('auth:success', (data) => {
  console.log('Connected as player:', data.playerId);
});
```

### 📍 Movement
```javascript
socket.emit('movement:request', {
  requestId: 'unique-id',
  mapId: 7411,
  cellId: 300,
  direction: 2
});

socket.on('movement:processing', (data) => {
  // Movement accepted
});
```

### 💬 Chat
```javascript
socket.emit('chat:message', {
  channel: 'global',
  message: 'Hello world!'
});

socket.on('chat:message', (data) => {
  // { playerId, channel, message, timestamp }
});
```

### ⚔️ Combat
```javascript
socket.emit('combat:action', {
  actionId: 'unique-id',
  actionType: 'spell',
  targetId: 'enemy-id',
  spellId: 1
});

socket.on('combat:update', (data) => {
  // { fightId, currentTurn, actions[] }
});
```

---

## 🎭 Character Classes

| ID | Class Name | Type |
|----|------------|------|
| 1 | Feca | Tank/Support |
| 2 | Osamodas | Summoner |
| 3 | Enutrof | Treasure Hunter |
| 4 | Sram | Assassin |
| 5 | Xelor | Time Mage |
| 6 | Ecaflip | Gambler |
| 7 | Eniripsa | Healer |
| 8 | Iop | Warrior |
| 9 | Cra | Archer |
| 10 | Sadida | Nature Mage |
| 11 | Sacrieur | Berserker |
| 12 | Pandawa | Brawler |

---

## 🗺️ Map System

### Cell IDs
- Grid: 14x20 = 560 cells
- Cell IDs: 0-559
- Isometric layout

### Directions
```
    7  0  1
     \ | /
  6 - ● - 2
     / | \
    5  4  3
```

---

## 📊 Character Stats

### Base Stats
- **Vitality**: Health points
- **Wisdom**: Experience gain
- **Strength**: Physical damage
- **Intelligence**: Spell damage
- **Chance**: Critical hit
- **Agility**: Dodge

### Resources
- **HP**: Health (base 50 + vitality)
- **AP**: Action Points (6 per turn)
- **MP**: Movement Points (3 per turn)

### Currency
- **Kamas**: In-game gold
- **Ogrinas**: Premium currency

---

## 🔧 Testing the APIs

### Using Postman/Insomnia

1. **Test Login**
```
POST http://localhost:3000/api/auth/login
Body: { "login": "test", "password": "test123" }
```

2. **Get Characters**
```
GET http://localhost:3000/api/characters
Header: Authorization: Bearer <token-from-login>
```

### Using Unity

```csharp
using UnityEngine.Networking;

public IEnumerator TestLogin()
{
    var data = new { login = "test", password = "test123" };
    var json = JsonUtility.ToJson(data);

    using (UnityWebRequest www = UnityWebRequest.Post("http://localhost:3000/api/auth/login", json))
    {
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + www.downloadHandler.text);
        }
    }
}
```

---

## 🚀 Quick Start Checklist

### Backend Setup
- [ ] gofus-backend running on port 3000
- [ ] gofus-game-server running on port 3001
- [ ] Database connected (Supabase/PostgreSQL)
- [ ] Redis running (for sessions)

### Test Endpoints
- [ ] Can POST to /api/auth/login
- [ ] Can GET /api/characters with token
- [ ] Can connect WebSocket to port 3001

### Unity Implementation Order
1. [ ] HTTP client for REST API
2. [ ] Login/Register flow
3. [ ] Character list/creation
4. [ ] WebSocket client for game server
5. [ ] Movement synchronization
6. [ ] Chat system
7. [ ] Combat system

---

## 📝 Notes

- JWT tokens expire after 24 hours
- Rate limiting: 10 requests/second for auth
- WebSocket reconnection uses exponential backoff
- All timestamps are in UTC
- Character names must be unique
- Max 5 characters per account (configurable)