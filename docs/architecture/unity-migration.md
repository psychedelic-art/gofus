# GOFUS Unity 2D Client Migration Guide
## From Flash/ActionScript to Unity

---

## Executive Summary

This document provides a comprehensive migration path from the current Flash/ActionScript-based client (Electron + SWF) to a modern Unity 2D client with WebSocket networking.

---

## Current Client Analysis

### Technology Stack
- **Framework:** Electron (Node.js desktop wrapper)
- **Game Engine:** Flash Player with SWF modules
- **Language:** ActionScript 3
- **Networking:** Custom TCP socket protocol
- **Assets:** SWF files (core.swf, soma.swf)

### Key Components (ActionScript)
- **Atouin:** Map rendering engine
- **Jerakine:** Core framework layer
- **Berilia:** UI system
- **Tiphon:** Animation engine
- **MessageReceiver:** Network packet handler

---

## Unity 2D Architecture

```
┌─────────────────────────────────────────────────┐
│               UNITY 2D CLIENT                    │
├─────────────────────────────────────────────────┤
│                                                  │
│  Presentation Layer          Business Logic     │
│  ┌──────────────┐           ┌───────────────┐  │
│  │ Unity UI     │           │ Game Systems  │  │
│  │ • Login      │           │ • Character   │  │
│  │ • Inventory  │───────────│ • Combat      │  │
│  │ • Chat       │           │ • Inventory   │  │
│  │ • Map View   │           │ • Spells      │  │
│  └──────────────┘           └───────────────┘  │
│                                    │            │
│  Rendering                   Network Layer      │
│  ┌──────────────┐           ┌───────────────┐  │
│  │ 2D Renderer  │           │ WebSocket     │  │
│  │ • Sprites    │           │ Client        │  │
│  │ • Tilemaps   │───────────│ • Socket.IO   │  │
│  │ • Animations │           │ • Packet      │  │
│  │ • Effects    │           │   Handler     │  │
│  └──────────────┘           └───────────────┘  │
│                                                  │
└─────────────────────────────────────────────────┘
```

---

## Project Setup

### Unity Configuration
```
Unity Version: 2022.3 LTS or 2023.2+
Render Pipeline: Universal Render Pipeline (URP) 2D
Platform: PC, Mac & Linux Standalone
Additional Platforms: WebGL (optional)
```

### Project Structure
```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs
│   │   │   ├── Singleton.cs
│   │   │   └── Constants.cs
│   │   ├── Networking/
│   │   │   ├── NetworkManager.cs
│   │   │   ├── PacketHandler.cs
│   │   │   ├── Protocols/
│   │   │   └── Messages/
│   │   ├── Player/
│   │   │   ├── PlayerController.cs
│   │   │   ├── PlayerAnimator.cs
│   │   │   └── PlayerStats.cs
│   │   ├── Map/
│   │   │   ├── MapRenderer.cs
│   │   │   ├── MapLoader.cs
│   │   │   ├── CellGrid.cs
│   │   │   └── Pathfinding.cs
│   │   ├── Combat/
│   │   │   ├── BattleManager.cs
│   │   │   ├── TurnManager.cs
│   │   │   └── SpellSystem.cs
│   │   ├── UI/
│   │   │   ├── UIManager.cs
│   │   │   ├── Screens/
│   │   │   └── Components/
│   │   └── Utils/
│   ├── Prefabs/
│   │   ├── Characters/
│   │   ├── UI/
│   │   ├── Effects/
│   │   └── Map/
│   ├── Resources/
│   │   ├── Sprites/
│   │   ├── Animations/
│   │   └── Data/
│   ├── Scenes/
│   │   ├── Main.unity
│   │   ├── Login.unity
│   │   └── Game.unity
│   └── Settings/
├── Plugins/
│   └── WebSocket/
└── StreamingAssets/
    └── Maps/
```

---

## Core Systems Implementation

### 1. Network Manager (Replacing MessageReceiver.as)
```csharp
// Scripts/Networking/NetworkManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

public class NetworkManager : Singleton<NetworkManager>
{
    private WebSocket websocket;
    private Queue<NetworkMessage> messageQueue = new Queue<NetworkMessage>();
    private Dictionary<string, Action<object>> handlers = new Dictionary<string, Action<object>>();

    [Header("Connection Settings")]
    [SerializeField] private string serverUrl = "ws://localhost:3001";
    [SerializeField] private bool autoReconnect = true;
    [SerializeField] private float reconnectDelay = 3f;

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnError;

    protected override void Awake()
    {
        base.Awake();
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        // Authentication
        RegisterHandler("HelloConnectMessage", OnHelloConnect);
        RegisterHandler("IdentificationSuccessMessage", OnIdentificationSuccess);
        RegisterHandler("IdentificationFailedMessage", OnIdentificationFailed);

        // Character
        RegisterHandler("CharactersListMessage", OnCharactersList);
        RegisterHandler("CharacterSelectedSuccessMessage", OnCharacterSelected);

        // Map
        RegisterHandler("CurrentMapMessage", OnCurrentMap);
        RegisterHandler("GameContextCreateMessage", OnGameContextCreate);
        RegisterHandler("GameContextRemoveElementMessage", OnRemoveElement);

        // Movement
        RegisterHandler("GameMapMovementMessage", OnMapMovement);

        // Combat
        RegisterHandler("GameFightStartingMessage", OnFightStarting);
        RegisterHandler("GameFightTurnStartMessage", OnTurnStart);

        // Chat
        RegisterHandler("ChatServerMessage", OnChatMessage);
    }

    public async void Connect()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () => {
            Debug.Log("Connected to server");
            OnConnected?.Invoke();
        };

        websocket.OnError += (e) => {
            Debug.LogError($"WebSocket Error: {e}");
            OnError?.Invoke(e);
        };

        websocket.OnClose += (e) => {
            Debug.Log("Disconnected from server");
            OnDisconnected?.Invoke();

            if (autoReconnect)
            {
                Invoke(nameof(Connect), reconnectDelay);
            }
        };

        websocket.OnMessage += (bytes) => {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            NetworkMessage msg = JsonConvert.DeserializeObject<NetworkMessage>(json);
            messageQueue.Enqueue(msg);
        };

        await websocket.Connect();
    }

    public async void SendMessage<T>(T message) where T : class
    {
        if (websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket not connected");
            return;
        }

        var packet = new NetworkPacket
        {
            type = typeof(T).Name,
            data = message
        };

        string json = JsonConvert.SerializeObject(packet);
        await websocket.SendText(json);
    }

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
        #endif

        // Process queued messages on main thread
        while (messageQueue.Count > 0)
        {
            NetworkMessage msg = messageQueue.Dequeue();
            ProcessMessage(msg);
        }
    }

    private void ProcessMessage(NetworkMessage msg)
    {
        if (handlers.TryGetValue(msg.type, out Action<object> handler))
        {
            handler?.Invoke(msg.data);
        }
        else
        {
            Debug.LogWarning($"No handler for message type: {msg.type}");
        }
    }

    public void RegisterHandler(string messageType, Action<object> handler)
    {
        handlers[messageType] = handler;
    }

    // Handler implementations
    private void OnIdentificationSuccess(object data)
    {
        var msg = JsonConvert.DeserializeObject<IdentificationSuccessMessage>(data.ToString());
        GameManager.Instance.SetAccountData(msg.accountId, msg.nickname);
        UIManager.Instance.ShowCharacterSelection();
    }

    private void OnCharactersList(object data)
    {
        var msg = JsonConvert.DeserializeObject<CharactersListMessage>(data.ToString());
        CharacterSelectionUI.Instance.DisplayCharacters(msg.characters);
    }

    private void OnCurrentMap(object data)
    {
        var msg = JsonConvert.DeserializeObject<CurrentMapMessage>(data.ToString());
        MapManager.Instance.LoadMap(msg.mapId);
    }

    private void OnMapMovement(object data)
    {
        var msg = JsonConvert.DeserializeObject<GameMapMovementMessage>(data.ToString());
        EntityManager.Instance.MoveEntity(msg.actorId, msg.keyMovements);
    }
}

[Serializable]
public class NetworkMessage
{
    public string type;
    public object data;
}

[Serializable]
public class NetworkPacket
{
    public string type;
    public object data;
}
```

### 2. Map System (Replacing Atouin)
```csharp
// Scripts/Map/MapRenderer.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapRenderer : MonoBehaviour
{
    [Header("Tilemap References")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap objectsTilemap;
    [SerializeField] private Tilemap collisionTilemap;

    [Header("Map Settings")]
    [SerializeField] private Vector2 cellSize = new Vector2(86f, 43f); // Dofus isometric cell size
    [SerializeField] private int mapWidth = 14;
    [SerializeField] private int mapHeight = 20;

    private MapData currentMapData;
    private CellGrid cellGrid;
    private Dictionary<int, TileBase> tileCache = new Dictionary<int, TileBase>();

    public void LoadMap(int mapId)
    {
        // Load map data (from server or local cache)
        currentMapData = MapLoader.LoadMapData(mapId);

        // Clear existing tiles
        ClearMap();

        // Initialize cell grid
        cellGrid = new CellGrid(mapWidth, mapHeight);

        // Render map layers
        RenderGround();
        RenderObjects();
        SetupCollisions();

        // Position camera
        CameraController.Instance.SetMapBounds(GetMapBounds());
    }

    private void RenderGround()
    {
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int cellId = GetCellId(x, y);
                CellData cell = currentMapData.cells[cellId];

                // Convert isometric coordinates
                Vector3Int tilePos = CartesianToIsometric(x, y);

                // Get or load tile
                TileBase tile = GetTileForCell(cell);
                groundTilemap.SetTile(tilePos, tile);

                // Set walkability
                cellGrid.SetWalkable(cellId, cell.mov && !cell.los);
            }
        }
    }

    private Vector3Int CartesianToIsometric(int x, int y)
    {
        // Dofus isometric conversion
        int isoX = (x - y) * (int)(cellSize.x / 2);
        int isoY = (x + y) * (int)(cellSize.y / 2);
        return new Vector3Int(isoX, isoY, 0);
    }

    public Vector3 CellIdToWorldPosition(int cellId)
    {
        int x = cellId % mapWidth;
        int y = cellId / mapWidth;
        Vector3Int isoPos = CartesianToIsometric(x, y);
        return grid.CellToWorld(isoPos) + new Vector3(cellSize.x / 2, cellSize.y / 2, 0);
    }

    public int WorldPositionToCellId(Vector3 worldPos)
    {
        Vector3Int cellPos = grid.WorldToCell(worldPos);
        // Convert back from isometric
        int x = (cellPos.x / (int)(cellSize.x / 2) + cellPos.y / (int)(cellSize.y / 2)) / 2;
        int y = (cellPos.y / (int)(cellSize.y / 2) - cellPos.x / (int)(cellSize.x / 2)) / 2;
        return y * mapWidth + x;
    }

    private int GetCellId(int x, int y)
    {
        return y * mapWidth + x;
    }

    public List<int> GetPath(int startCell, int endCell)
    {
        return Pathfinding.FindPath(cellGrid, startCell, endCell, mapWidth, mapHeight);
    }
}

[Serializable]
public class MapData
{
    public int id;
    public int width;
    public int height;
    public string key; // Decryption key for old maps
    public CellData[] cells;
    public List<InteractiveElement> elements;
}

[Serializable]
public class CellData
{
    public int id;
    public bool mov; // Walkable
    public bool los; // Line of sight blocking
    public int groundLevel;
    public int movement; // Movement cost
}
```

### 3. Character Controller
```csharp
// Scripts/Player/PlayerController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool isMoving = false;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private Queue<Vector3> movementPath = new Queue<Vector3>();
    private int currentCellId;
    private PlayerData playerData;

    public int CellId => currentCellId;
    public bool IsLocalPlayer { get; private set; }

    public void Initialize(PlayerData data, bool isLocal = false)
    {
        playerData = data;
        IsLocalPlayer = isLocal;

        // Set initial position
        currentCellId = data.cellId;
        Vector3 worldPos = MapManager.Instance.CellIdToWorldPosition(currentCellId);
        transform.position = worldPos;

        // Load character sprite based on class and gender
        LoadCharacterSprite(data.breed, data.sex);

        // Set name
        if (TryGetComponent<PlayerNameDisplay>(out var nameDisplay))
        {
            nameDisplay.SetName(data.name);
            nameDisplay.SetLevel(data.level);
        }
    }

    public void MoveTo(List<int> path)
    {
        if (isMoving) return;

        movementPath.Clear();
        foreach (int cellId in path)
        {
            Vector3 worldPos = MapManager.Instance.CellIdToWorldPosition(cellId);
            movementPath.Enqueue(worldPos);
        }

        if (movementPath.Count > 0)
        {
            StartCoroutine(MoveAlongPath());
        }
    }

    private IEnumerator MoveAlongPath()
    {
        isMoving = true;
        animator.SetBool("IsWalking", true);

        while (movementPath.Count > 0)
        {
            Vector3 targetPos = movementPath.Dequeue();

            // Update facing direction
            UpdateDirection(targetPos - transform.position);

            // Move to target
            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );
                yield return null;
            }

            transform.position = targetPos;
            currentCellId = MapManager.Instance.WorldPositionToCellId(targetPos);
        }

        animator.SetBool("IsWalking", false);
        isMoving = false;

        // Notify server if local player
        if (IsLocalPlayer)
        {
            NetworkManager.Instance.SendMessage(new MovementConfirmMessage
            {
                cellId = currentCellId
            });
        }
    }

    private void UpdateDirection(Vector3 moveDirection)
    {
        if (moveDirection.x > 0.1f)
        {
            spriteRenderer.flipX = false;
            animator.SetInteger("Direction", 1); // Right
        }
        else if (moveDirection.x < -0.1f)
        {
            spriteRenderer.flipX = true;
            animator.SetInteger("Direction", 3); // Left
        }
        else if (moveDirection.y > 0.1f)
        {
            animator.SetInteger("Direction", 0); // Up
        }
        else if (moveDirection.y < -0.1f)
        {
            animator.SetInteger("Direction", 2); // Down
        }
    }

    private void LoadCharacterSprite(int breed, bool isFemale)
    {
        // Load sprite based on breed (class) and gender
        string spritePath = $"Sprites/Characters/{GetBreedName(breed)}_{(isFemale ? "F" : "M")}";
        Sprite[] sprites = Resources.LoadAll<Sprite>(spritePath);

        if (sprites != null && sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[0];
        }
    }

    private string GetBreedName(int breed)
    {
        // Map breed IDs to names (from BreedEnum.as)
        switch (breed)
        {
            case 1: return "Feca";
            case 2: return "Osamodas";
            case 3: return "Enutrof";
            case 4: return "Sram";
            case 5: return "Xelor";
            case 6: return "Ecaflip";
            case 7: return "Eniripsa";
            case 8: return "Iop";
            case 9: return "Cra";
            case 10: return "Sadida";
            case 11: return "Sacrieur";
            case 12: return "Pandawa";
            default: return "Feca";
        }
    }
}

[Serializable]
public class PlayerData
{
    public int id;
    public string name;
    public int level;
    public int breed; // Class
    public bool sex; // Gender
    public int cellId;
    public int mapId;
}
```

### 4. UI System (Replacing Berilia)
```csharp
// Scripts/UI/UIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [Header("Screens")]
    [SerializeField] private GameObject loginScreen;
    [SerializeField] private GameObject characterSelectionScreen;
    [SerializeField] private GameObject gameScreen;

    [Header("Game UI Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject spellsPanel;
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private GameObject mapPanel;

    private Stack<GameObject> screenStack = new Stack<GameObject>();
    private Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();

    protected override void Awake()
    {
        base.Awake();
        InitializePanels();
    }

    private void InitializePanels()
    {
        panels["inventory"] = inventoryPanel;
        panels["character"] = characterPanel;
        panels["spells"] = spellsPanel;
        panels["chat"] = chatPanel;
        panels["map"] = mapPanel;

        // Hide all panels initially
        foreach (var panel in panels.Values)
        {
            panel.SetActive(false);
        }
    }

    public void ShowScreen(GameObject screen)
    {
        // Hide current screen
        if (screenStack.Count > 0)
        {
            screenStack.Peek().SetActive(false);
        }

        // Show new screen
        screen.SetActive(true);
        screenStack.Push(screen);
    }

    public void ShowLogin()
    {
        ShowScreen(loginScreen);
    }

    public void ShowCharacterSelection()
    {
        ShowScreen(characterSelectionScreen);
    }

    public void ShowGame()
    {
        ShowScreen(gameScreen);

        // Show default game panels
        ShowPanel("chat");
        ShowPanel("map");
    }

    public void ShowPanel(string panelName)
    {
        if (panels.TryGetValue(panelName, out GameObject panel))
        {
            panel.SetActive(true);
        }
    }

    public void HidePanel(string panelName)
    {
        if (panels.TryGetValue(panelName, out GameObject panel))
        {
            panel.SetActive(false);
        }
    }

    public void TogglePanel(string panelName)
    {
        if (panels.TryGetValue(panelName, out GameObject panel))
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    // Keyboard shortcuts
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            TogglePanel("inventory");
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            TogglePanel("character");
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            TogglePanel("spells");
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            if (chatPanel.activeSelf)
            {
                ChatUI.Instance.FocusInput();
            }
        }
    }
}
```

### 5. Combat System
```csharp
// Scripts/Combat/BattleManager.cs
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : Singleton<BattleManager>
{
    [Header("Battle Settings")]
    [SerializeField] private GameObject battleUI;
    [SerializeField] private GameObject fighterPrefab;
    [SerializeField] private float turnTimeout = 120f;

    private BattleData currentBattle;
    private Dictionary<int, Fighter> fighters = new Dictionary<int, Fighter>();
    private int currentTurnFighterId;
    private float turnTimer;

    public bool IsInBattle => currentBattle != null;

    public void StartBattle(BattleData battleData)
    {
        currentBattle = battleData;

        // Show battle UI
        battleUI.SetActive(true);

        // Spawn fighters
        foreach (var fighterData in battleData.fighters)
        {
            SpawnFighter(fighterData);
        }

        // Initialize turn order
        StartTurn(battleData.turnOrder[0]);
    }

    private void SpawnFighter(FighterData data)
    {
        GameObject fighterGO = Instantiate(fighterPrefab);
        Fighter fighter = fighterGO.GetComponent<Fighter>();

        fighter.Initialize(data);

        // Position fighter on cell
        Vector3 worldPos = MapManager.Instance.CellIdToWorldPosition(data.cellId);
        fighterGO.transform.position = worldPos;

        fighters[data.id] = fighter;
    }

    public void StartTurn(int fighterId)
    {
        currentTurnFighterId = fighterId;
        turnTimer = turnTimeout;

        // Highlight current fighter
        foreach (var fighter in fighters.Values)
        {
            fighter.SetActive(fighter.Data.id == fighterId);
        }

        // Update UI
        BattleUI.Instance.UpdateTurn(fighters[fighterId].Data);

        // If local player's turn
        if (IsLocalPlayerTurn())
        {
            EnablePlayerActions();
        }
    }

    private bool IsLocalPlayerTurn()
    {
        return fighters[currentTurnFighterId].Data.id == GameManager.Instance.LocalPlayerId;
    }

    public void ExecuteSpell(int spellId, int targetCellId)
    {
        if (!IsLocalPlayerTurn()) return;

        // Send spell cast to server
        NetworkManager.Instance.SendMessage(new GameActionRequest
        {
            actionId = 300, // Spell cast
            spellId = spellId,
            cellId = targetCellId
        });
    }

    public void Move(List<int> path)
    {
        if (!IsLocalPlayerTurn()) return;

        // Send movement to server
        NetworkManager.Instance.SendMessage(new GameMapMovementRequestMessage
        {
            keyMovements = path
        });
    }

    public void PassTurn()
    {
        if (!IsLocalPlayerTurn()) return;

        NetworkManager.Instance.SendMessage(new GameFightTurnFinishMessage());
    }

    public void ProcessSpellEffect(SpellEffectData effect)
    {
        Fighter caster = fighters[effect.casterId];
        Fighter target = fighters[effect.targetId];

        // Play spell animation
        SpellAnimator.Instance.PlaySpell(effect.spellId, caster.transform.position, target.transform.position);

        // Apply damage/healing
        if (effect.damage > 0)
        {
            target.TakeDamage(effect.damage);
        }
        else if (effect.healing > 0)
        {
            target.Heal(effect.healing);
        }

        // Update UI
        BattleUI.Instance.UpdateFighterStats(target.Data);
    }

    public void EndBattle(BattleResultData result)
    {
        // Clean up fighters
        foreach (var fighter in fighters.Values)
        {
            Destroy(fighter.gameObject);
        }
        fighters.Clear();

        // Hide battle UI
        battleUI.SetActive(false);

        // Show results
        BattleResultUI.Instance.ShowResults(result);

        currentBattle = null;
    }

    private void Update()
    {
        if (IsInBattle)
        {
            turnTimer -= Time.deltaTime;

            if (turnTimer <= 0 && IsLocalPlayerTurn())
            {
                PassTurn();
            }

            // Update timer UI
            BattleUI.Instance.UpdateTimer(turnTimer);
        }
    }
}
```

---

## Asset Migration

### Extracting Assets from SWF

#### Tools Required
1. **JPEXS Free Flash Decompiler** - Extract sprites and sounds
2. **TexturePacker** - Create sprite atlases
3. **Audacity** - Convert audio formats

#### Extraction Process
```bash
# Extract sprites
java -jar ffdec.jar -export sprite "extracted/sprites" core.swf
java -jar ffdec.jar -export sprite "extracted/sprites" soma.swf

# Extract sounds
java -jar ffdec.jar -export sound "extracted/sounds" core.swf

# Extract shapes (vector graphics)
java -jar ffdec.jar -export shape "extracted/shapes" core.swf
```

#### Asset Organization
```
Resources/
├── Sprites/
│   ├── Characters/
│   │   ├── Feca_M/        # Male Feca sprites
│   │   ├── Feca_F/        # Female Feca sprites
│   │   └── ...            # Other classes
│   ├── Monsters/
│   ├── NPCs/
│   ├── Items/
│   ├── Spells/
│   └── UI/
├── Animations/
│   ├── Characters/
│   ├── Spells/
│   └── Effects/
├── Audio/
│   ├── Music/
│   ├── SFX/
│   └── Ambience/
└── Data/
    ├── Maps/
    ├── Items/
    └── Spells/
```

### Sprite Setup in Unity
```csharp
// Scripts/Utils/SpriteManager.cs
using System.Collections.Generic;
using UnityEngine;

public class SpriteManager : Singleton<SpriteManager>
{
    private Dictionary<string, Sprite[]> spriteCache = new Dictionary<string, Sprite[]>();

    public Sprite[] LoadCharacterSprites(int breed, bool isFemale)
    {
        string key = $"character_{breed}_{(isFemale ? "f" : "m")}";

        if (!spriteCache.ContainsKey(key))
        {
            string path = $"Sprites/Characters/{GetBreedName(breed)}_{(isFemale ? "F" : "M")}";
            spriteCache[key] = Resources.LoadAll<Sprite>(path);
        }

        return spriteCache[key];
    }

    public Sprite LoadItemSprite(int itemId)
    {
        string key = $"item_{itemId}";

        if (!spriteCache.ContainsKey(key))
        {
            string path = $"Sprites/Items/{itemId}";
            Sprite sprite = Resources.Load<Sprite>(path);
            spriteCache[key] = new Sprite[] { sprite };
        }

        return spriteCache[key]?[0];
    }

    public Sprite LoadSpellIcon(int spellId)
    {
        string key = $"spell_{spellId}";

        if (!spriteCache.ContainsKey(key))
        {
            string path = $"Sprites/Spells/{spellId}";
            Sprite sprite = Resources.Load<Sprite>(path);
            spriteCache[key] = new Sprite[] { sprite };
        }

        return spriteCache[key]?[0];
    }
}
```

---

## Animation System

### Character Animation Controller
```csharp
// Animator Controller Setup
/*
States:
- Idle (4 directions)
- Walk (4 directions)
- Run (4 directions)
- Attack (4 directions)
- Cast Spell
- Take Damage
- Death

Parameters:
- Direction (Int: 0=Up, 1=Right, 2=Down, 3=Left)
- IsWalking (Bool)
- IsRunning (Bool)
- Attack (Trigger)
- CastSpell (Trigger)
- TakeDamage (Trigger)
- Die (Trigger)
*/

// Scripts/Player/PlayerAnimator.cs
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetDirection(Vector2 direction)
    {
        int dir = 0;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal movement
            if (direction.x > 0)
            {
                dir = 1; // Right
                spriteRenderer.flipX = false;
            }
            else
            {
                dir = 3; // Left
                spriteRenderer.flipX = true;
            }
        }
        else
        {
            // Vertical movement
            if (direction.y > 0)
                dir = 0; // Up
            else
                dir = 2; // Down
        }

        animator.SetInteger("Direction", dir);
    }

    public void PlayAttackAnimation()
    {
        animator.SetTrigger("Attack");
    }

    public void PlaySpellAnimation(int spellId)
    {
        animator.SetTrigger("CastSpell");
        animator.SetInteger("SpellId", spellId);
    }

    public void PlayDeathAnimation()
    {
        animator.SetTrigger("Die");
    }
}
```

---

## Performance Optimization

### Object Pooling
```csharp
// Scripts/Utils/ObjectPoolManager.cs
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    protected override void Awake()
    {
        base.Awake();
        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            GameObject parent = new GameObject($"Pool_{pool.tag}");

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, parent.transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist");
            return null;
        }

        GameObject obj = poolDictionary[tag].Dequeue();
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        poolDictionary[tag].Enqueue(obj);

        return obj;
    }

    public void Despawn(GameObject obj, string tag, float delay = 0f)
    {
        StartCoroutine(DespawnCoroutine(obj, tag, delay));
    }

    private IEnumerator DespawnCoroutine(GameObject obj, string tag, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }
}
```

### Sprite Atlas Configuration
```
1. Create Sprite Atlases for:
   - UI Elements
   - Character Sprites (per class)
   - Item Icons
   - Spell Icons
   - Map Tiles

2. Settings:
   - Max Texture Size: 4096x4096
   - Compression: High Quality
   - Filter Mode: Point (for pixel art)
   - Generate Mip Maps: False
```

---

## Platform-Specific Considerations

### Desktop (Windows/Mac/Linux)
```csharp
// Platform-specific code
#if UNITY_STANDALONE
    // Full resolution support
    Screen.SetResolution(1920, 1080, true);

    // File system access for cache
    string cachePath = Application.persistentDataPath;
#endif
```

### WebGL Build
```csharp
#if UNITY_WEBGL
    // WebGL specific optimizations
    Application.targetFrameRate = 30;

    // Use browser storage
    PlayerPrefs for small data

    // Compressed textures
    Use DXT compression
#endif
```

---

## Testing Strategy

### Unit Tests
```csharp
// Tests/NetworkManagerTests.cs
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NetworkManagerTests
{
    [Test]
    public void TestPacketSerialization()
    {
        var packet = new NetworkPacket
        {
            type = "TestMessage",
            data = new { value = 123 }
        };

        string json = JsonConvert.SerializeObject(packet);
        var decoded = JsonConvert.DeserializeObject<NetworkPacket>(json);

        Assert.AreEqual(packet.type, decoded.type);
    }

    [UnityTest]
    public IEnumerator TestConnection()
    {
        NetworkManager.Instance.Connect();

        yield return new WaitForSeconds(2f);

        Assert.IsTrue(NetworkManager.Instance.IsConnected);
    }
}
```

---

## Migration Timeline

| Week | Phase | Tasks |
|------|-------|-------|
| 1-2 | Project Setup | Unity project, packages, folder structure |
| 3-4 | Networking | WebSocket client, packet handlers, protocols |
| 5-6 | Map System | Map rendering, pathfinding, isometric grid |
| 7-8 | Character System | Player controller, animations, movement |
| 9-10 | UI Implementation | All game screens and panels |
| 11-12 | Combat System | Battle manager, spell system, turn-based logic |
| 13-14 | Asset Migration | Extract from SWF, import to Unity, optimize |
| 15-16 | Testing & Polish | Bug fixes, performance optimization, builds |

---

## Required Unity Packages

### Package Manager
```json
{
  "dependencies": {
    "com.unity.2d.sprite": "1.0.0",
    "com.unity.2d.tilemap": "1.0.0",
    "com.unity.2d.tilemap.extras": "3.1.0",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.ugui": "1.0.0",
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.addressables": "1.21.0",
    "com.unity.cinemachine": "2.9.0"
  }
}
```

### Third-Party Assets
1. **NativeWebSocket** - WebSocket client
2. **DOTween** - Animation tweening (free)
3. **A* Pathfinding Project** - Advanced pathfinding ($100)
4. **Odin Inspector** - Editor enhancement ($55)

---

## Conclusion

This Unity migration provides:
- ✅ Cross-platform support (PC, Mac, Linux, WebGL)
- ✅ Modern rendering pipeline
- ✅ Better performance than Flash
- ✅ Native C# performance
- ✅ Active Unity community
- ✅ No Flash dependencies
- ✅ Mobile-ready architecture

The migration preserves all core gameplay while modernizing the technology stack for long-term sustainability.