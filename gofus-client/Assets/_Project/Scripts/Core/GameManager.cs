using System;
using System.Collections.Generic;
using UnityEngine;
using GOFUS.Combat;
using GOFUS.Networking;

namespace GOFUS.Core
{
    public enum GameState
    {
        Initializing,
        Login,
        CharacterSelection,
        InGame,
        Battle_TurnBased,
        Battle_RealTime
    }

    [Serializable]
    public class GameConfiguration
    {
        public string ServerUrl = "ws://localhost:3001";
        public string ApiUrl = "http://localhost:3000/api";
        public bool EnableHybridCombat = true;
        public string DefaultCombatMode = "turn-based";
        public int MaxReconnectAttempts = 5;
        public float ReconnectDelay = 2f;
        public bool EnableRealTimeSpellCasting = true;
        public float SpellQueueTime = 0.5f;
    }

    public interface IGameStateHandler
    {
        void OnEnter();
        void OnExit();
        void OnUpdate();
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
        public event Action<bool> OnCombatModeChanged;

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
            // Try to load from StreamingAssets
            string configPath = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");

            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    return JsonUtility.FromJson<GameConfiguration>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load configuration: {e.Message}");
                }
            }

            // Return default config
            return new GameConfiguration();
        }

        private void Update()
        {
            // Update current state
            if (stateHandlers.ContainsKey(currentState))
            {
                stateHandlers[currentState].OnUpdate();
            }
        }

        public void ReturnToPreviousState()
        {
            if (stateHistory.Count > 0)
            {
                ChangeState(stateHistory.Pop());
            }
        }
    }

    // State Handlers
    public class LoginStateHandler : IGameStateHandler
    {
        public void OnEnter()
        {
            Debug.Log("Entering Login State");
        }

        public void OnExit()
        {
            Debug.Log("Exiting Login State");
        }

        public void OnUpdate() { }
    }

    public class CharSelectStateHandler : IGameStateHandler
    {
        public void OnEnter()
        {
            Debug.Log("Entering Character Selection State");
        }

        public void OnExit()
        {
            Debug.Log("Exiting Character Selection State");
        }

        public void OnUpdate() { }
    }

    public class InGameStateHandler : IGameStateHandler
    {
        public void OnEnter()
        {
            Debug.Log("Entering InGame State");
        }

        public void OnExit()
        {
            Debug.Log("Exiting InGame State");
        }

        public void OnUpdate() { }
    }

    public class BattleTurnBasedStateHandler : IGameStateHandler
    {
        public void OnEnter()
        {
            Debug.Log("Entering Turn-Based Battle State");
            HybridCombatManager.Instance?.SetMode(CombatMode.TurnBased);
        }

        public void OnExit()
        {
            Debug.Log("Exiting Turn-Based Battle State");
        }

        public void OnUpdate() { }
    }

    public class BattleRealTimeStateHandler : IGameStateHandler
    {
        public void OnEnter()
        {
            Debug.Log("Entering Real-Time Battle State");
            HybridCombatManager.Instance?.SetMode(CombatMode.RealTime);
        }

        public void OnExit()
        {
            Debug.Log("Exiting Real-Time Battle State");
        }

        public void OnUpdate() { }
    }
}