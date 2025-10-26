# GOFUS Unity Client - Enhanced Implementation Guide with Hybrid Combat & TDD

## 📊 Research-Based Architecture Decisions

### Hybrid Combat System Architecture
Based on 2024 game design trends (Metaphor: ReFantazio, Lost Hellden, FF7 Remake):

```
┌─────────────────────────────────────────────────────────────┐
│                  Hybrid Combat System                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   Real-Time Mode              Turn-Based Mode               │
│  ┌──────────────┐           ┌──────────────┐              │
│  │ Free Movement│ <-------> │ Grid Movement │              │
│  │ Basic Attacks│  Toggle   │ AP/MP System  │              │
│  │ Quick Dispatch│          │ Spell Casting │              │
│  └──────────────┘           └──────────────┘              │
│         │                           │                       │
│         └───────────┬───────────────┘                       │
│                     ▼                                        │
│            Stack Machine State Manager                       │
│            - Combat State Stack                              │
│            - Seamless Transitions                            │
│            - Timeline Predictions                            │
└─────────────────────────────────────────────────────────────┘
```

### Network Architecture (Socket.IO Best Practices)
```
┌─────────────────────────────────────────────────────────────┐
│                 SocketIOUnity Implementation                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   Unity Client                    Game Server                │
│  ┌─────────────┐                ┌──────────────┐           │
│  │SocketIOUnity│ WebSocket/3001 │ Socket.IO    │           │
│  │ Thread-Safe │◄──────────────►│ Node.js      │           │
│  │ Auto-Reconnect               │ Real-time    │           │
│  └─────────────┘                └──────────────┘           │
│        │                                                     │
│   Main Thread Marshalling                                    │
│   - Update/LateUpdate/FixedUpdate                           │
│   - Event Queue System                                       │
└─────────────────────────────────────────────────────────────┘
```

---

## 🧪 Test-Driven Development Structure

### Test Organization
```
gofus-client/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   └── Tests/
│   │       ├── EditMode/
│   │       │   ├── EditMode.asmdef
│   │       │   ├── Core/
│   │       │   │   ├── GameManagerTests.cs
│   │       │   │   └── ConfigurationTests.cs
│   │       │   ├── Networking/
│   │       │   │   ├── PacketHandlerTests.cs
│   │       │   │   └── MessageSerializationTests.cs
│   │       │   ├── Map/
│   │       │   │   ├── IsometricHelperTests.cs
│   │       │   │   ├── PathfindingTests.cs
│   │       │   │   └── CellGridTests.cs
│   │       │   └── Combat/
│   │       │       ├── DamageCalculatorTests.cs
│   │       │       ├── TurnManagerTests.cs
│   │       │       └── HybridStateTests.cs
│   │       └── PlayMode/
│   │           ├── PlayMode.asmdef
│   │           ├── Integration/
│   │           │   ├── NetworkIntegrationTests.cs
│   │           │   └── CombatFlowTests.cs
│   │           └── Performance/
│   │               ├── MapRenderingTests.cs
│   │               └── EntitySpawnTests.cs
```

---

## 📋 Component Implementation with Tests

### Phase 1: Core Infrastructure with TDD

#### 1.1 GameManager Tests & Implementation

**Test First (EditMode):**
```csharp
// GameManagerTests.cs
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameManagerTests
{
    private GameManager gameManager;

    [SetUp]
    public void Setup()
    {
        GameObject go = new GameObject();
        gameManager = go.AddComponent<GameManager>();
    }

    [Test]
    public void GameManager_Singleton_OnlyOneInstance()
    {
        GameObject go2 = new GameObject();
        GameManager gm2 = go2.AddComponent<GameManager>();

        Assert.AreEqual(GameManager.Instance, gameManager);
        Assert.IsNull(gm2);
    }

    [Test]
    public void GameManager_StateTransition_ValidTransitions()
    {
        gameManager.ChangeState(GameState.Login);
        Assert.AreEqual(GameState.Login, gameManager.CurrentState);

        gameManager.ChangeState(GameState.CharacterSelection);
        Assert.AreEqual(GameState.CharacterSelection, gameManager.CurrentState);
    }

    [Test]
    public void GameManager_Configuration_LoadsCorrectly()
    {
        var config = gameManager.LoadConfiguration();
        Assert.IsNotNull(config);
        Assert.AreEqual("ws://localhost:3001", config.ServerUrl);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameManager.gameObject);
    }
}
```

**Implementation:**
```csharp
// GameManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public enum GameState
{
    Initializing,
    Login,
    CharacterSelection,
    InGame,
    Battle_TurnBased,
    Battle_RealTime
}

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameState currentState = GameState.Initializing;
    [SerializeField] private GameConfiguration configuration;

    private Dictionary<GameState, IGameStateHandler> stateHandlers;
    private Stack<GameState> stateHistory;

    public GameState CurrentState => currentState;
    public GameConfiguration Config => configuration;

    // Events
    public event Action<GameState, GameState> OnStateChanged;
    public event Action<bool> OnCombatModeChanged; // true = turn-based, false = real-time

    protected override void Awake()
    {
        base.Awake();
        InitializeStateHandlers();
        stateHistory = new Stack<GameState>();
        configuration = LoadConfiguration();
    }

    private void InitializeStateHandlers()
    {
        stateHandlers = new Dictionary<GameState, IGameStateHandler>
        {
            { GameState.Login, new LoginStateHandler() },
            { GameState.CharacterSelection, new CharSelectStateHandler() },
            { GameState.InGame, new InGameStateHandler() },
            { GameState.Battle_TurnBased, new BattleTurnBasedStateHandler() },
            { GameState.Battle_RealTime, new BattleRealTimeStateHandler() }
        };
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        var oldState = currentState;

        // Exit current state
        if (stateHandlers.ContainsKey(currentState))
            stateHandlers[currentState].OnExit();

        // Enter new state
        stateHistory.Push(currentState);
        currentState = newState;

        if (stateHandlers.ContainsKey(newState))
            stateHandlers[newState].OnEnter();

        OnStateChanged?.Invoke(oldState, newState);

        // Handle combat mode changes
        if (newState == GameState.Battle_TurnBased || newState == GameState.Battle_RealTime)
        {
            OnCombatModeChanged?.Invoke(newState == GameState.Battle_TurnBased);
        }
    }

    public void ToggleCombatMode()
    {
        if (currentState == GameState.Battle_TurnBased)
            ChangeState(GameState.Battle_RealTime);
        else if (currentState == GameState.Battle_RealTime)
            ChangeState(GameState.Battle_TurnBased);
    }

    public GameConfiguration LoadConfiguration()
    {
        // Load from StreamingAssets/config.json
        string configPath = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
        if (System.IO.File.Exists(configPath))
        {
            string json = System.IO.File.ReadAllText(configPath);
            return JsonUtility.FromJson<GameConfiguration>(json);
        }

        // Return default config
        return new GameConfiguration
        {
            ServerUrl = "ws://localhost:3001",
            ApiUrl = "http://localhost:3000/api",
            EnableHybridCombat = true,
            DefaultCombatMode = "turn-based"
        };
    }
}

[Serializable]
public class GameConfiguration
{
    public string ServerUrl;
    public string ApiUrl;
    public bool EnableHybridCombat;
    public string DefaultCombatMode;
    public int MaxReconnectAttempts = 5;
    public float ReconnectDelay = 2f;
}
```

---

### Phase 2: Network Layer with SocketIOUnity

#### 2.1 NetworkManager Tests & Implementation

**Test First (EditMode):**
```csharp
// NetworkManagerTests.cs
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class NetworkManagerTests
{
    private NetworkManager networkManager;

    [SetUp]
    public void Setup()
    {
        GameObject go = new GameObject();
        networkManager = go.AddComponent<NetworkManager>();
    }

    [Test]
    public void NetworkManager_MessageQueue_ThreadSafe()
    {
        var testMessage = new TestMessage { data = "test" };
        networkManager.EnqueueMessage(testMessage);

        Assert.AreEqual(1, networkManager.MessageQueueCount);
    }

    [UnityTest]
    public IEnumerator NetworkManager_Reconnection_ExponentialBackoff()
    {
        networkManager.simulateFailure = true;
        networkManager.Connect();

        yield return new WaitForSeconds(1f);
        Assert.AreEqual(1, networkManager.ReconnectAttempts);

        yield return new WaitForSeconds(2f);
        Assert.AreEqual(2, networkManager.ReconnectAttempts);

        yield return new WaitForSeconds(4f);
        Assert.AreEqual(3, networkManager.ReconnectAttempts);
    }
}
```

**Implementation:**
```csharp
// NetworkManager.cs
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class NetworkManager : Singleton<NetworkManager>
{
    private SocketIOUnity socket;
    private ConcurrentQueue<INetworkMessage> messageQueue;
    private Dictionary<string, Action<JObject>> messageHandlers;

    [SerializeField] private bool isConnected = false;
    [SerializeField] private int reconnectAttempts = 0;
    private float reconnectDelay = 1f;
    private Coroutine reconnectCoroutine;

    // Test helpers
    public bool simulateFailure = false;
    public int MessageQueueCount => messageQueue.Count;
    public int ReconnectAttempts => reconnectAttempts;

    // Events
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnError;

    protected override void Awake()
    {
        base.Awake();
        messageQueue = new ConcurrentQueue<INetworkMessage>();
        messageHandlers = new Dictionary<string, Action<JObject>>();
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        // Authentication
        RegisterHandler("authenticated", OnAuthenticated);
        RegisterHandler("authError", OnAuthError);

        // Character
        RegisterHandler("charactersList", OnCharactersList);
        RegisterHandler("characterSelected", OnCharacterSelected);

        // Map & Movement
        RegisterHandler("mapData", OnMapData);
        RegisterHandler("playerMove", OnPlayerMove);
        RegisterHandler("entityUpdate", OnEntityUpdate);

        // Combat
        RegisterHandler("battleStart", OnBattleStart);
        RegisterHandler("turnStart", OnTurnStart);
        RegisterHandler("actionResult", OnActionResult);
        RegisterHandler("battleEnd", OnBattleEnd);

        // Hybrid Combat Events
        RegisterHandler("combatModeSwitch", OnCombatModeSwitch);
        RegisterHandler("realTimeAction", OnRealTimeAction);

        // Chat
        RegisterHandler("chatMessage", OnChatMessage);
    }

    public async void Connect()
    {
        if (socket != null && isConnected) return;

        if (simulateFailure)
        {
            StartReconnection();
            return;
        }

        var uri = new Uri(GameManager.Instance.Config.ServerUrl);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
            {
                {"token", PlayerPrefs.GetString("authToken", "")},
                {"version", Application.version}
            },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            ReconnectionDelay = 2000,
            ReconnectionDelayMax = 5000,
            ReconnectionAttempts = 5
        });

        // Set Unity thread scope
        socket.JsonSerializer = new NewtonsoftJsonSerializer();
        socket.UnityThreadScope = UnityThreadScope.Update;

        // Connection events
        socket.OnConnected += OnSocketConnected;
        socket.OnDisconnected += OnSocketDisconnected;
        socket.OnError += OnSocketError;
        socket.OnReconnectAttempt += OnReconnectAttempt;

        // Register all message handlers
        foreach (var handler in messageHandlers)
        {
            socket.On(handler.Key, response =>
            {
                EnqueueMessage(new NetworkMessage
                {
                    Type = handler.Key,
                    Data = response.GetValue<JObject>()
                });
            });
        }

        await socket.ConnectAsync();
    }

    private void OnSocketConnected(object sender, EventArgs e)
    {
        isConnected = true;
        reconnectAttempts = 0;
        OnConnected?.Invoke();
        Debug.Log("Connected to game server");
    }

    private void OnSocketDisconnected(object sender, string reason)
    {
        isConnected = false;
        OnDisconnected?.Invoke();
        Debug.Log($"Disconnected: {reason}");
        StartReconnection();
    }

    private void StartReconnection()
    {
        if (reconnectCoroutine == null)
        {
            reconnectCoroutine = StartCoroutine(ReconnectWithBackoff());
        }
    }

    private IEnumerator ReconnectWithBackoff()
    {
        while (reconnectAttempts < GameManager.Instance.Config.MaxReconnectAttempts)
        {
            reconnectAttempts++;
            float delay = Mathf.Pow(2, reconnectAttempts - 1) * reconnectDelay;

            Debug.Log($"Reconnecting in {delay} seconds... (Attempt {reconnectAttempts})");
            yield return new WaitForSeconds(delay);

            Connect();

            if (isConnected) break;
        }

        if (!isConnected)
        {
            OnError?.Invoke("Failed to reconnect after maximum attempts");
        }

        reconnectCoroutine = null;
    }

    private void Update()
    {
        // Process message queue on main thread
        while (messageQueue.TryDequeue(out INetworkMessage message))
        {
            if (messageHandlers.ContainsKey(message.Type))
            {
                messageHandlers[message.Type]?.Invoke(message.Data as JObject);
            }
        }
    }

    public void EnqueueMessage(INetworkMessage message)
    {
        messageQueue.Enqueue(message);
    }

    public void RegisterHandler(string messageType, Action<JObject> handler)
    {
        if (!messageHandlers.ContainsKey(messageType))
        {
            messageHandlers[messageType] = handler;
        }
    }

    // Send methods
    public void Send(string eventName, object data = null)
    {
        if (socket != null && isConnected)
        {
            if (data != null)
                socket.EmitAsync(eventName, data);
            else
                socket.EmitAsync(eventName);
        }
    }

    public void SendMovement(int cellId)
    {
        Send("requestMove", new { cellId });
    }

    public void SendSpellCast(int spellId, int targetCellId)
    {
        Send("requestSpellCast", new { spellId, targetCellId });
    }

    public void SendCombatAction(string actionType, object actionData)
    {
        Send("combatAction", new { type = actionType, data = actionData });
    }

    // Hybrid combat specific
    public void RequestCombatModeSwitch(bool turnBased)
    {
        Send("requestCombatMode", new { mode = turnBased ? "turn-based" : "real-time" });
    }

    public void SendRealTimeAttack(int targetId)
    {
        Send("realTimeAttack", new { targetId, timestamp = Time.time });
    }

    private void OnDestroy()
    {
        if (socket != null)
        {
            socket.Disconnect();
            socket.Dispose();
        }
    }

    // Message handlers
    private void OnAuthenticated(JObject data) { /* Implementation */ }
    private void OnAuthError(JObject data) { /* Implementation */ }
    private void OnCharactersList(JObject data) { /* Implementation */ }
    private void OnCharacterSelected(JObject data) { /* Implementation */ }
    private void OnMapData(JObject data) { /* Implementation */ }
    private void OnPlayerMove(JObject data) { /* Implementation */ }
    private void OnEntityUpdate(JObject data) { /* Implementation */ }
    private void OnBattleStart(JObject data) { /* Implementation */ }
    private void OnTurnStart(JObject data) { /* Implementation */ }
    private void OnActionResult(JObject data) { /* Implementation */ }
    private void OnBattleEnd(JObject data) { /* Implementation */ }
    private void OnCombatModeSwitch(JObject data) { /* Implementation */ }
    private void OnRealTimeAction(JObject data) { /* Implementation */ }
    private void OnChatMessage(JObject data) { /* Implementation */ }
    private void OnSocketError(object sender, string error) { OnError?.Invoke(error); }
    private void OnReconnectAttempt(object sender, int attempt) { Debug.Log($"Reconnect attempt {attempt}"); }
}

public interface INetworkMessage
{
    string Type { get; }
    object Data { get; }
}

public class NetworkMessage : INetworkMessage
{
    public string Type { get; set; }
    public object Data { get; set; }
}

public class TestMessage : INetworkMessage
{
    public string Type => "test";
    public object Data => data;
    public string data;
}
```

---

### Phase 3: Hybrid Combat System

#### 3.1 HybridCombatManager Tests & Implementation

**Test First (EditMode & PlayMode):**
```csharp
// HybridCombatManagerTests.cs
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class HybridCombatManagerTests
{
    private HybridCombatManager combatManager;

    [SetUp]
    public void Setup()
    {
        GameObject go = new GameObject();
        combatManager = go.AddComponent<HybridCombatManager>();
    }

    [Test]
    public void CombatManager_ModeSwitch_SeamlessTransition()
    {
        combatManager.InitializeBattle(CombatMode.TurnBased);
        Assert.AreEqual(CombatMode.TurnBased, combatManager.CurrentMode);

        combatManager.SwitchMode();
        Assert.AreEqual(CombatMode.RealTime, combatManager.CurrentMode);
    }

    [Test]
    public void CombatManager_WeakEnemyDetection_AutoRealTime()
    {
        var weakEnemy = new Enemy { Level = 1, Health = 10 };
        var strongEnemy = new Enemy { Level = 10, Health = 100 };

        Assert.IsTrue(combatManager.ShouldAutoRealTime(weakEnemy));
        Assert.IsFalse(combatManager.ShouldAutoRealTime(strongEnemy));
    }

    [UnityTest]
    public IEnumerator CombatManager_ATBGauge_FillsInRealTime()
    {
        combatManager.InitializeBattle(CombatMode.RealTime);
        var fighter = combatManager.AddFighter(new Fighter { Speed = 100 });

        float initialATB = fighter.ATBGauge;
        yield return new WaitForSeconds(1f);

        Assert.Greater(fighter.ATBGauge, initialATB);
    }

    [Test]
    public void CombatManager_Timeline_PredictsActions()
    {
        combatManager.InitializeBattle(CombatMode.TurnBased);
        var timeline = combatManager.GetActionTimeline(3); // Next 3 actions

        Assert.AreEqual(3, timeline.Count);
        Assert.IsNotNull(timeline[0].Fighter);
        Assert.Greater(timeline[0].TimeUntilAction, 0f);
    }
}
```

**Implementation:**
```csharp
// HybridCombatManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CombatMode
{
    TurnBased,
    RealTime,
    Transitioning
}

public class HybridCombatManager : Singleton<HybridCombatManager>
{
    // Stack Machine for state management (inspired by modern games)
    private Stack<CombatState> stateStack;
    private CombatMode currentMode;
    private bool isInCombat;

    // Fighters
    private List<Fighter> allFighters;
    private List<Fighter> playerTeam;
    private List<Fighter> enemyTeam;
    private Fighter currentTurnFighter;

    // Turn-based specific
    private Queue<Fighter> turnQueue;
    private float turnTimer;
    private const float TURN_TIME_LIMIT = 30f;

    // Real-time specific
    private Dictionary<Fighter, float> atbGauges;
    private float globalTimeScale = 1f;

    // Timeline prediction (Phantom Brigade style)
    private List<PredictedAction> actionTimeline;

    // Hybrid features
    private float modeSwitchCooldown = 0f;
    private const float MODE_SWITCH_DELAY = 2f;
    private bool autoRealTimeForWeak = true;

    public CombatMode CurrentMode => currentMode;
    public bool IsInCombat => isInCombat;
    public Fighter CurrentTurn => currentTurnFighter;

    // Events
    public event Action<CombatMode> OnModeChanged;
    public event Action<Fighter> OnTurnStarted;
    public event Action<CombatResult> OnCombatEnded;
    public event Action<Fighter, float> OnATBFilled;

    protected override void Awake()
    {
        base.Awake();
        stateStack = new Stack<CombatState>();
        allFighters = new List<Fighter>();
        atbGauges = new Dictionary<Fighter, float>();
        actionTimeline = new List<PredictedAction>();
    }

    public void InitializeBattle(CombatMode mode, List<Fighter> players, List<Fighter> enemies)
    {
        isInCombat = true;
        currentMode = mode;

        playerTeam = players;
        enemyTeam = enemies;
        allFighters.Clear();
        allFighters.AddRange(players);
        allFighters.AddRange(enemies);

        // Check for auto real-time mode for weak enemies
        if (autoRealTimeForWeak && enemies.All(e => ShouldAutoRealTime(e)))
        {
            mode = CombatMode.RealTime;
            ShowNotification("Quick battle mode activated!");
        }

        // Initialize based on mode
        if (mode == CombatMode.TurnBased)
        {
            InitializeTurnBased();
        }
        else
        {
            InitializeRealTime();
        }

        // Push initial state
        stateStack.Clear();
        stateStack.Push(new CombatState { Mode = mode, Fighters = allFighters.ToList() });

        OnModeChanged?.Invoke(mode);
    }

    public void InitializeBattle(CombatMode mode)
    {
        // Test helper
        InitializeBattle(mode, new List<Fighter>(), new List<Fighter>());
    }

    private void InitializeTurnBased()
    {
        // Calculate initiative and create turn order
        turnQueue = new Queue<Fighter>(
            allFighters.OrderByDescending(f => f.Initiative + UnityEngine.Random.Range(0, 20))
        );

        // Start first turn
        if (turnQueue.Count > 0)
        {
            StartNextTurn();
        }
    }

    private void InitializeRealTime()
    {
        // Initialize ATB gauges
        atbGauges.Clear();
        foreach (var fighter in allFighters)
        {
            atbGauges[fighter] = 0f;
        }
    }

    public void SwitchMode()
    {
        if (!isInCombat || currentMode == CombatMode.Transitioning) return;
        if (modeSwitchCooldown > 0) return;

        StartCoroutine(TransitionMode());
    }

    private IEnumerator TransitionMode()
    {
        var oldMode = currentMode;
        currentMode = CombatMode.Transitioning;

        // Visual transition effect
        UIManager.Instance.ShowTransition("Switching combat mode...");

        yield return new WaitForSeconds(0.5f);

        // Determine new mode
        var newMode = oldMode == CombatMode.TurnBased ? CombatMode.RealTime : CombatMode.TurnBased;

        // Save current state
        stateStack.Push(new CombatState
        {
            Mode = newMode,
            Fighters = allFighters.ToList(),
            ATBStates = new Dictionary<Fighter, float>(atbGauges)
        });

        // Switch to new mode
        if (newMode == CombatMode.TurnBased)
        {
            ConvertToTurnBased();
        }
        else
        {
            ConvertToRealTime();
        }

        currentMode = newMode;
        modeSwitchCooldown = MODE_SWITCH_DELAY;

        UIManager.Instance.HideTransition();
        OnModeChanged?.Invoke(newMode);
    }

    private void ConvertToTurnBased()
    {
        // Convert ATB gauges to turn order
        var fightersByATB = allFighters.OrderByDescending(f =>
            atbGauges.ContainsKey(f) ? atbGauges[f] : 0f
        ).ToList();

        turnQueue = new Queue<Fighter>(fightersByATB);
        StartNextTurn();
    }

    private void ConvertToRealTime()
    {
        // Convert turn order to ATB advantages
        float baseATB = 50f;
        float bonus = 25f;

        for (int i = 0; i < allFighters.Count; i++)
        {
            var fighter = allFighters[i];
            if (fighter == currentTurnFighter)
            {
                atbGauges[fighter] = 100f; // Current turn fighter gets full ATB
            }
            else
            {
                // Give ATB based on turn order position
                int position = turnQueue.ToList().IndexOf(fighter);
                atbGauges[fighter] = baseATB - (position * 10f);
            }
        }
    }

    public bool ShouldAutoRealTime(Enemy enemy)
    {
        // Auto real-time for enemies 5+ levels below player
        var playerLevel = playerTeam.FirstOrDefault()?.Level ?? 1;
        return enemy.Level < playerLevel - 5 || enemy.Health < 50;
    }

    public Fighter AddFighter(Fighter fighter)
    {
        allFighters.Add(fighter);
        if (currentMode == CombatMode.RealTime)
        {
            atbGauges[fighter] = 0f;
        }
        return fighter;
    }

    private void Update()
    {
        if (!isInCombat) return;

        // Update cooldowns
        if (modeSwitchCooldown > 0)
            modeSwitchCooldown -= Time.deltaTime;

        if (currentMode == CombatMode.TurnBased)
        {
            UpdateTurnBased();
        }
        else if (currentMode == CombatMode.RealTime)
        {
            UpdateRealTime();
        }

        // Update timeline predictions
        UpdateActionTimeline();
    }

    private void UpdateTurnBased()
    {
        if (currentTurnFighter == null) return;

        turnTimer += Time.deltaTime;

        // Auto end turn after time limit
        if (turnTimer >= TURN_TIME_LIMIT)
        {
            EndTurn();
        }
    }

    private void UpdateRealTime()
    {
        // Update all ATB gauges
        foreach (var fighter in allFighters)
        {
            if (!fighter.IsAlive) continue;

            float fillRate = (fighter.Speed / 100f) * globalTimeScale;
            atbGauges[fighter] += fillRate * Time.deltaTime * 100f;

            // Cap at 100
            if (atbGauges[fighter] >= 100f)
            {
                atbGauges[fighter] = 100f;
                OnATBFilled?.Invoke(fighter, 100f);

                // AI takes action immediately
                if (fighter.IsAI)
                {
                    ExecuteAIAction(fighter);
                    atbGauges[fighter] = 0f; // Reset after action
                }
            }
        }
    }

    private void UpdateActionTimeline()
    {
        actionTimeline.Clear();

        if (currentMode == CombatMode.TurnBased)
        {
            // Predict next turns
            var tempQueue = new Queue<Fighter>(turnQueue);
            float time = 0f;

            for (int i = 0; i < 5 && tempQueue.Count > 0; i++)
            {
                var fighter = tempQueue.Dequeue();
                actionTimeline.Add(new PredictedAction
                {
                    Fighter = fighter,
                    TimeUntilAction = time,
                    ActionType = "Turn"
                });
                time += TURN_TIME_LIMIT;
            }
        }
        else if (currentMode == CombatMode.RealTime)
        {
            // Predict ATB fills
            foreach (var kvp in atbGauges.OrderByDescending(x => x.Value))
            {
                var fighter = kvp.Key;
                var currentATB = kvp.Value;
                var fillRate = (fighter.Speed / 100f) * globalTimeScale;
                var timeToFill = (100f - currentATB) / (fillRate * 100f);

                actionTimeline.Add(new PredictedAction
                {
                    Fighter = fighter,
                    TimeUntilAction = timeToFill,
                    ActionType = "ATB Ready"
                });
            }

            actionTimeline = actionTimeline.OrderBy(a => a.TimeUntilAction).Take(5).ToList();
        }
    }

    public List<PredictedAction> GetActionTimeline(int count)
    {
        return actionTimeline.Take(count).ToList();
    }

    // Turn-based actions
    public void EndTurn()
    {
        if (currentMode != CombatMode.TurnBased) return;

        // Add fighter back to queue
        if (currentTurnFighter != null)
        {
            turnQueue.Enqueue(currentTurnFighter);
        }

        StartNextTurn();
    }

    private void StartNextTurn()
    {
        if (turnQueue.Count == 0)
        {
            // Rebuild turn queue
            InitializeTurnBased();
            return;
        }

        currentTurnFighter = turnQueue.Dequeue();
        turnTimer = 0f;

        // Reset AP/MP
        currentTurnFighter.ActionPoints = currentTurnFighter.MaxActionPoints;
        currentTurnFighter.MovementPoints = currentTurnFighter.MaxMovementPoints;

        OnTurnStarted?.Invoke(currentTurnFighter);
    }

    // Real-time actions
    public bool TryExecuteRealTimeAction(Fighter fighter, CombatAction action)
    {
        if (currentMode != CombatMode.RealTime) return false;
        if (atbGauges[fighter] < action.ATBCost) return false;

        // Execute action
        ExecuteAction(fighter, action);

        // Consume ATB
        atbGauges[fighter] -= action.ATBCost;

        return true;
    }

    private void ExecuteAIAction(Fighter fighter)
    {
        // Simple AI for real-time mode
        var target = GetNearestEnemy(fighter);
        if (target != null)
        {
            var action = new CombatAction
            {
                Type = ActionType.BasicAttack,
                Target = target,
                ATBCost = 50f
            };

            TryExecuteRealTimeAction(fighter, action);
        }
    }

    private void ExecuteAction(Fighter source, CombatAction action)
    {
        // Common action execution for both modes
        switch (action.Type)
        {
            case ActionType.BasicAttack:
                DealDamage(source, action.Target, source.AttackDamage);
                break;
            case ActionType.Spell:
                CastSpell(source, action.SpellId, action.Target);
                break;
            case ActionType.Move:
                MoveFighter(source, action.TargetCell);
                break;
        }
    }

    private Fighter GetNearestEnemy(Fighter source)
    {
        var enemies = source.IsPlayerTeam ? enemyTeam : playerTeam;
        return enemies.Where(e => e.IsAlive).OrderBy(e =>
            Vector3.Distance(source.Position, e.Position)
        ).FirstOrDefault();
    }

    private void DealDamage(Fighter source, Fighter target, int damage)
    {
        target.TakeDamage(damage);

        // Check for death
        if (!target.IsAlive)
        {
            RemoveFighter(target);
            CheckBattleEnd();
        }
    }

    private void CastSpell(Fighter source, int spellId, Fighter target)
    {
        // Spell casting logic
        var spell = SpellDatabase.GetSpell(spellId);
        if (spell != null)
        {
            spell.Execute(source, target);
        }
    }

    private void MoveFighter(Fighter fighter, int cellId)
    {
        // Movement logic
        fighter.MoveTo(cellId);
    }

    private void RemoveFighter(Fighter fighter)
    {
        allFighters.Remove(fighter);
        if (playerTeam.Contains(fighter)) playerTeam.Remove(fighter);
        if (enemyTeam.Contains(fighter)) enemyTeam.Remove(fighter);
        if (atbGauges.ContainsKey(fighter)) atbGauges.Remove(fighter);
    }

    private void CheckBattleEnd()
    {
        bool playersAlive = playerTeam.Any(f => f.IsAlive);
        bool enemiesAlive = enemyTeam.Any(f => f.IsAlive);

        if (!playersAlive || !enemiesAlive)
        {
            EndBattle(playersAlive);
        }
    }

    private void EndBattle(bool victory)
    {
        isInCombat = false;
        var result = new CombatResult
        {
            Victory = victory,
            Experience = CalculateExperience(),
            Loot = GenerateLoot()
        };

        OnCombatEnded?.Invoke(result);

        // Return to previous game state
        GameManager.Instance.ChangeState(GameState.InGame);
    }

    private int CalculateExperience()
    {
        return enemyTeam.Where(e => !e.IsAlive).Sum(e => e.ExperienceValue);
    }

    private List<Item> GenerateLoot()
    {
        var loot = new List<Item>();
        foreach (var enemy in enemyTeam.Where(e => !e.IsAlive))
        {
            loot.AddRange(enemy.GenerateLoot());
        }
        return loot;
    }

    private void ShowNotification(string message)
    {
        Debug.Log(message);
        // UIManager.Instance.ShowNotification(message);
    }
}

// Supporting classes
public class CombatState
{
    public CombatMode Mode;
    public List<Fighter> Fighters;
    public Dictionary<Fighter, float> ATBStates;
}

public class PredictedAction
{
    public Fighter Fighter;
    public float TimeUntilAction;
    public string ActionType;
}

public class CombatAction
{
    public ActionType Type;
    public Fighter Target;
    public int TargetCell;
    public int SpellId;
    public float ATBCost = 50f;
}

public enum ActionType
{
    BasicAttack,
    Spell,
    Move,
    Item,
    Defend
}

public class CombatResult
{
    public bool Victory;
    public int Experience;
    public List<Item> Loot;
}

public class Fighter : MonoBehaviour
{
    public int Level = 1;
    public int Health;
    public int MaxHealth;
    public int ActionPoints;
    public int MaxActionPoints = 6;
    public int MovementPoints;
    public int MaxMovementPoints = 3;
    public int AttackDamage = 10;
    public float Speed = 100f;
    public float Initiative;
    public float ATBGauge;
    public bool IsAI;
    public bool IsPlayerTeam;
    public bool IsAlive => Health > 0;
    public Vector3 Position => transform.position;
    public int ExperienceValue = 100;

    public void TakeDamage(int damage)
    {
        Health = Mathf.Max(0, Health - damage);
    }

    public void MoveTo(int cellId)
    {
        // Movement implementation
    }

    public List<Item> GenerateLoot()
    {
        // Loot generation
        return new List<Item>();
    }
}

public class Enemy : Fighter
{
    // Enemy specific properties
}

public class Item
{
    public string Name;
    public int Value;
}

public static class SpellDatabase
{
    public static Spell GetSpell(int id)
    {
        // Return spell by ID
        return null;
    }
}

public class Spell
{
    public void Execute(Fighter source, Fighter target)
    {
        // Spell execution
    }
}
```

---

### Phase 4: Map System with Tests

#### 4.1 IsometricHelper Tests & Implementation

**Test First (EditMode):**
```csharp
// IsometricHelperTests.cs
using NUnit.Framework;
using UnityEngine;

public class IsometricHelperTests
{
    [Test]
    public void CellIdToPosition_CorrectConversion()
    {
        // Dofus uses 14x20 grid with specific cell numbering
        Vector3 pos = IsometricHelper.CellIdToWorldPosition(0);
        Assert.AreEqual(new Vector3(0, 0, 0), pos);

        pos = IsometricHelper.CellIdToWorldPosition(27); // Top-right corner
        Assert.AreEqual(new Vector3(559, 279.5f, 0), pos);
    }

    [Test]
    public void WorldToCell_CorrectConversion()
    {
        int cellId = IsometricHelper.WorldPositionToCellId(new Vector3(0, 0, 0));
        Assert.AreEqual(0, cellId);

        cellId = IsometricHelper.WorldPositionToCellId(new Vector3(559, 279.5f, 0));
        Assert.AreEqual(27, cellId);
    }

    [Test]
    public void GetNeighbors_ReturnsCorrectCells()
    {
        var neighbors = IsometricHelper.GetNeighborCells(200); // Center cell
        Assert.AreEqual(8, neighbors.Count); // Should have 8 neighbors

        neighbors = IsometricHelper.GetNeighborCells(0); // Corner cell
        Assert.Less(neighbors.Count, 8); // Corner has fewer neighbors
    }

    [Test]
    public void CalculateDistance_DiagonalMovement()
    {
        int distance = IsometricHelper.GetDistance(0, 27);
        Assert.AreEqual(13, distance); // Diagonal distance
    }
}
```

**Implementation:**
```csharp
// IsometricHelper.cs
using System.Collections.Generic;
using UnityEngine;

public static class IsometricHelper
{
    // Dofus grid constants
    public const int GRID_WIDTH = 14;
    public const int GRID_HEIGHT = 20;
    public const int GRID_HEIGHT_HALF = 20; // Double height for diamond shape
    public const float CELL_WIDTH = 86f;
    public const float CELL_HEIGHT = 43f;
    public const float CELL_HALF_WIDTH = 43f;
    public const float CELL_HALF_HEIGHT = 21.5f;

    // Cell ID to grid coordinates (Dofus style)
    public static Vector2Int CellIdToGridCoords(int cellId)
    {
        // Dofus uses a specific cell numbering system
        int line = cellId / (GRID_WIDTH * 2 - 1);
        int column = cellId - line * (GRID_WIDTH * 2 - 1);

        // Adjust for diamond shape
        if (line % 2 == 1)
        {
            column = column * 2 + 1;
        }
        else
        {
            column = column * 2;
        }

        return new Vector2Int(column, line);
    }

    // Grid coordinates to Cell ID
    public static int GridCoordsToCellId(int x, int y)
    {
        if (x < 0 || x >= GRID_WIDTH * 2 || y < 0 || y >= GRID_HEIGHT_HALF)
            return -1;

        int cellId = y * (GRID_WIDTH * 2 - 1);

        if (y % 2 == 1)
        {
            cellId += (x - 1) / 2;
        }
        else
        {
            cellId += x / 2;
        }

        return cellId;
    }

    // Cell ID to world position (isometric projection)
    public static Vector3 CellIdToWorldPosition(int cellId)
    {
        Vector2Int gridCoords = CellIdToGridCoords(cellId);

        // Isometric conversion
        float x = (gridCoords.x - gridCoords.y) * CELL_HALF_WIDTH;
        float y = (gridCoords.x + gridCoords.y) * CELL_HALF_HEIGHT;

        return new Vector3(x, y, 0);
    }

    // World position to Cell ID
    public static int WorldPositionToCellId(Vector3 worldPos)
    {
        // Reverse isometric projection
        float x = worldPos.x / CELL_HALF_WIDTH;
        float y = worldPos.y / CELL_HALF_HEIGHT;

        int gridX = Mathf.RoundToInt((x + y) / 2);
        int gridY = Mathf.RoundToInt((y - x) / 2);

        return GridCoordsToCellId(gridX, gridY);
    }

    // Get all neighbor cells (8-directional)
    public static List<int> GetNeighborCells(int cellId)
    {
        List<int> neighbors = new List<int>();
        Vector2Int coords = CellIdToGridCoords(cellId);

        // All 8 directions
        int[,] directions = new int[,]
        {
            {-1, -1}, {0, -1}, {1, -1},
            {-1, 0},           {1, 0},
            {-1, 1},  {0, 1},  {1, 1}
        };

        for (int i = 0; i < 8; i++)
        {
            int newX = coords.x + directions[i, 0];
            int newY = coords.y + directions[i, 1];

            int neighborId = GridCoordsToCellId(newX, newY);
            if (neighborId >= 0 && neighborId < GRID_WIDTH * GRID_HEIGHT_HALF)
            {
                neighbors.Add(neighborId);
            }
        }

        return neighbors;
    }

    // Calculate distance between cells (Manhattan distance for Dofus)
    public static int GetDistance(int cellId1, int cellId2)
    {
        Vector2Int coords1 = CellIdToGridCoords(cellId1);
        Vector2Int coords2 = CellIdToGridCoords(cellId2);

        return Mathf.Abs(coords1.x - coords2.x) + Mathf.Abs(coords1.y - coords2.y);
    }

    // Get line of sight between two cells
    public static List<int> GetLineOfSight(int startCell, int endCell)
    {
        List<int> cells = new List<int>();

        Vector3 start = CellIdToWorldPosition(startCell);
        Vector3 end = CellIdToWorldPosition(endCell);

        float distance = Vector3.Distance(start, end);
        int steps = Mathf.CeilToInt(distance / CELL_HALF_WIDTH);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 point = Vector3.Lerp(start, end, t);
            int cellId = WorldPositionToCellId(point);

            if (cellId >= 0 && !cells.Contains(cellId))
            {
                cells.Add(cellId);
            }
        }

        return cells;
    }

    // Check if cell is walkable (obstacle detection)
    public static bool IsCellWalkable(int cellId, CellGrid grid)
    {
        if (cellId < 0 || cellId >= grid.Cells.Length)
            return false;

        return grid.Cells[cellId].IsWalkable && !grid.Cells[cellId].IsOccupied;
    }

    // Convert screen position to world position
    public static Vector3 ScreenToWorldPosition(Vector2 screenPos, Camera camera)
    {
        Ray ray = camera.ScreenPointToRay(screenPos);
        Plane groundPlane = new Plane(Vector3.forward, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    // Get cells in range (for spells/abilities)
    public static List<int> GetCellsInRange(int centerCell, int range, bool includeDiagonals = true)
    {
        List<int> cellsInRange = new List<int>();

        for (int cellId = 0; cellId < GRID_WIDTH * GRID_HEIGHT_HALF; cellId++)
        {
            int distance = GetDistance(centerCell, cellId);
            if (distance <= range)
            {
                cellsInRange.Add(cellId);
            }
        }

        return cellsInRange;
    }

    // Get cells in area of effect (cross, circle, line)
    public static List<int> GetAreaOfEffect(int targetCell, AreaShape shape, int size)
    {
        List<int> affectedCells = new List<int>();

        switch (shape)
        {
            case AreaShape.Cross:
                // Get cells in cross pattern
                for (int i = 1; i <= size; i++)
                {
                    affectedCells.AddRange(GetCrossPattern(targetCell, i));
                }
                break;

            case AreaShape.Circle:
                // Get cells in circle
                affectedCells = GetCellsInRange(targetCell, size);
                break;

            case AreaShape.Line:
                // Line implementation would require direction
                break;
        }

        affectedCells.Add(targetCell); // Include center
        return affectedCells;
    }

    private static List<int> GetCrossPattern(int centerCell, int distance)
    {
        List<int> cells = new List<int>();
        Vector2Int center = CellIdToGridCoords(centerCell);

        // Four cardinal directions
        int[,] directions = new int[,] { {0, -1}, {1, 0}, {0, 1}, {-1, 0} };

        for (int i = 0; i < 4; i++)
        {
            int newX = center.x + directions[i, 0] * distance;
            int newY = center.y + directions[i, 1] * distance;

            int cellId = GridCoordsToCellId(newX, newY);
            if (cellId >= 0)
            {
                cells.Add(cellId);
            }
        }

        return cells;
    }
}

public enum AreaShape
{
    Cross,
    Circle,
    Line,
    Square,
    Diamond
}

public class CellGrid
{
    public Cell[] Cells { get; private set; }

    public CellGrid()
    {
        Cells = new Cell[IsometricHelper.GRID_WIDTH * IsometricHelper.GRID_HEIGHT_HALF];
        for (int i = 0; i < Cells.Length; i++)
        {
            Cells[i] = new Cell { Id = i, IsWalkable = true };
        }
    }
}

public class Cell
{
    public int Id;
    public bool IsWalkable;
    public bool IsOccupied;
    public int MovementCost = 1;
}
```

---

## 📅 Updated Development Timeline

### Week 1: Core Infrastructure + TDD Setup
- Day 1: Project setup, Unity Test Framework configuration
- Day 2: GameManager with tests (state machine, configuration)
- Day 3: Singleton pattern, event system with tests
- Day 4: Input system with tests
- Day 5: Localization with tests

### Week 2: Network Layer (SocketIOUnity)
- Day 1-2: NetworkManager with thread-safe implementation
- Day 3: Message serialization and handlers with tests
- Day 4: Reconnection logic with exponential backoff
- Day 5: Integration tests with game server

### Week 3: Hybrid Combat System
- Day 1-2: HybridCombatManager implementation
- Day 3: Stack machine state management
- Day 4: ATB system and timeline predictions
- Day 5: Mode switching and transitions

### Week 4: Map System
- Day 1-2: IsometricHelper with full test coverage
- Day 3: MapRenderer and CellGrid
- Day 4: A* pathfinding with tests
- Day 5: Camera system with Cinemachine

### Week 5: Character System
- Day 1-2: PlayerController with movement
- Day 3: Entity system and management
- Day 4: Stats and inventory
- Day 5: Animation system

### Week 6: Combat Integration
- Day 1-2: Turn-based combat flow
- Day 3: Real-time combat flow
- Day 4: Spell system and effects
- Day 5: Combat UI and feedback

### Week 7: UI Implementation
- Day 1-2: Screen management (login, character select)
- Day 3: Game HUD and panels
- Day 4: Chat system
- Day 5: Settings and options

### Week 8: Asset Migration & Polish
- Day 1-2: Asset extraction with JPEXS
- Day 3: Sprite processing and atlases
- Day 4: Audio integration
- Day 5: Performance optimization and final testing

---

## 🎮 Key Features from Research

### Hybrid Combat Features
1. **Auto Real-Time for Weak Enemies**: Automatically switch to real-time for enemies 5+ levels below
2. **Seamless Mode Switching**: Toggle between modes with cooldown
3. **ATB Gauge System**: Real-time gauge filling based on speed stat
4. **Timeline Predictions**: Show next 5 actions in queue
5. **Stack Machine States**: Preserve state when switching modes

### Network Features
1. **Thread-Safe Message Queue**: Concurrent queue for thread safety
2. **Auto-Reconnection**: Exponential backoff strategy
3. **Platform Support**: WebGL compatibility with NativeWebSocket
4. **Event Marshalling**: Unity main thread updates
5. **Connection Pooling**: Reuse connections efficiently

### Testing Strategy
1. **TDD Approach**: Write tests before implementation
2. **Dual Test Modes**: EditMode for unit tests, PlayMode for integration
3. **Mock Objects**: Simulate server responses
4. **Performance Tests**: Monitor frame rates and memory
5. **Automated CI/CD**: Run tests on every commit

---

## 🚀 Next Immediate Steps

1. **Create Unity Project with Tests**
```bash
# Unity Hub
- Create 2D URP project: gofus-client
- Install Test Framework package
- Setup assembly definitions
```

2. **Implement Core with TDD**
```bash
# Start with tests
- Write GameManager tests
- Implement GameManager
- Run tests to verify
```

3. **Setup SocketIOUnity**
```bash
# Install package
- Import SocketIOUnity
- Write NetworkManager tests
- Implement connection logic
```

4. **Build Hybrid Combat**
```bash
# Combat system
- Write combat tests
- Implement stack machine
- Test mode switching
```

This enhanced plan incorporates all research findings and provides a solid TDD foundation for the Unity client development.