using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using GOFUS.Core;

namespace GOFUS.Networking
{
    [Serializable]
    public class ServerConfig
    {
        public string BackendUrl = "https://gofus-backend.vercel.app";
        public string GameServerUrl = "wss://gofus-game-server-production.up.railway.app";
        public string LocalBackendUrl = "http://localhost:3000";
        public string LocalGameServerUrl = "ws://localhost:3001";
        public bool UseProductionServers = true;
    }

    [Serializable]
    public class HealthCheckResponse
    {
        public string status;
        public string timestamp;
        public float uptime;
        public string version;
        public string environment;
    }

    [Serializable]
    public class GameServerHealthResponse
    {
        public string status;
        public string timestamp;
        public float uptime;
        public Metrics metrics;
    }

    [Serializable]
    public class Metrics
    {
        public int onlinePlayers;
        public int activeMaps;
        public int activeBattles;
        public int tickCount;
        public float lastTickDuration;
    }

    public class NetworkManager : Singleton<NetworkManager>
    {
        [SerializeField] private ServerConfig config;
        private bool isConnectedToBackend;
        private bool isConnectedToGameServer;
        private WebSocketConnection wsConnection;

        public ServerConfig Config => config;
        public bool IsConnectedToBackend => isConnectedToBackend;
        public bool IsConnectedToGameServer => isConnectedToGameServer;
        public string CurrentBackendUrl => config.UseProductionServers ? config.BackendUrl : config.LocalBackendUrl;
        public string CurrentGameServerUrl => config.UseProductionServers ? config.GameServerUrl : config.LocalGameServerUrl;

        // Events
        public event Action<bool> OnBackendConnectionChanged;
        public event Action<bool> OnGameServerConnectionChanged;
        public event Action<string> OnError;

        protected override void Awake()
        {
            base.Awake();
            config = new ServerConfig();
            wsConnection = new WebSocketConnection();
        }

        private void Start()
        {
            StartCoroutine(InitializeConnections());
        }

        private IEnumerator InitializeConnections()
        {
            yield return StartCoroutine(CheckBackendHealth());
            yield return StartCoroutine(CheckGameServerHealth());

            if (isConnectedToBackend && isConnectedToGameServer)
            {
                ConnectToGameServer();
            }
        }

        public IEnumerator CheckBackendHealth()
        {
            string healthUrl = $"{CurrentBackendUrl}/api/health";
            Debug.Log($"Checking backend health at: {healthUrl}");

            using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        HealthCheckResponse response = JsonUtility.FromJson<HealthCheckResponse>(request.downloadHandler.text);
                        isConnectedToBackend = response.status == "healthy";
                        Debug.Log($"Backend Status: {response.status}, Version: {response.version}, Environment: {response.environment}");
                        OnBackendConnectionChanged?.Invoke(isConnectedToBackend);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse backend health response: {e.Message}");
                        isConnectedToBackend = false;
                    }
                }
                else
                {
                    Debug.LogError($"Backend health check failed: {request.error}");
                    isConnectedToBackend = false;
                    OnError?.Invoke($"Backend connection failed: {request.error}");
                }
            }
        }

        public IEnumerator CheckGameServerHealth()
        {
            // For WebSocket server, we'll check the HTTP health endpoint
            string healthUrl = CurrentGameServerUrl.Replace("wss://", "https://").Replace("ws://", "http://") + "/health";
            Debug.Log($"Checking game server health at: {healthUrl}");

            using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        GameServerHealthResponse response = JsonUtility.FromJson<GameServerHealthResponse>(request.downloadHandler.text);
                        isConnectedToGameServer = response.status == "ok";
                        Debug.Log($"Game Server Status: {response.status}, Online Players: {response.metrics.onlinePlayers}, Active Battles: {response.metrics.activeBattles}");
                        OnGameServerConnectionChanged?.Invoke(isConnectedToGameServer);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse game server health response: {e.Message}");
                        isConnectedToGameServer = false;
                    }
                }
                else
                {
                    Debug.LogError($"Game server health check failed: {request.error}");
                    isConnectedToGameServer = false;
                    OnError?.Invoke($"Game server connection failed: {request.error}");
                }
            }
        }

        public void ConnectToGameServer()
        {
            if (wsConnection != null)
            {
                wsConnection.Connect(CurrentGameServerUrl);
            }
        }

        public void DisconnectFromGameServer()
        {
            if (wsConnection != null)
            {
                wsConnection.Disconnect();
            }
        }

        // API Calls to Backend
        public IEnumerator Login(string username, string password, Action<bool, string> callback)
        {
            string loginUrl = $"{CurrentBackendUrl}/api/auth/login";

            var loginData = new Dictionary<string, string>
            {
                {"username", username},
                {"password", password}
            };

            string jsonData = JsonUtility.ToJson(loginData);

            using (UnityWebRequest request = UnityWebRequest.Post(loginUrl, jsonData, "application/json"))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, request.downloadHandler.text);
                }
                else
                {
                    callback?.Invoke(false, request.error);
                }
            }
        }

        public IEnumerator GetCharacters(string token, Action<bool, string> callback)
        {
            string charactersUrl = $"{CurrentBackendUrl}/api/characters";

            using (UnityWebRequest request = UnityWebRequest.Get(charactersUrl))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, request.downloadHandler.text);
                }
                else
                {
                    callback?.Invoke(false, request.error);
                }
            }
        }

        // Send message to game server via WebSocket
        public void SendToGameServer(string eventName, object data)
        {
            if (wsConnection != null && wsConnection.IsConnected)
            {
                wsConnection.Send(eventName, data);
            }
            else
            {
                Debug.LogWarning("Not connected to game server");
            }
        }

        // Emit method for NetworkOptimizer compatibility
        public void Emit(string eventName, object data)
        {
            SendToGameServer(eventName, data);
        }

        // EmitBatch method for batched messages
        public void EmitBatch(byte[] payload)
        {
            if (wsConnection != null && wsConnection.IsConnected)
            {
                wsConnection.SendBatch(payload);
            }
            else
            {
                Debug.LogWarning("Not connected to game server - batch not sent");
            }
        }

        // Connect to backend (for UserFlowValidator)
        public IEnumerator ConnectToBackend()
        {
            yield return CheckBackendHealth();
            if (!isConnectedToBackend)
            {
                OnError?.Invoke("Failed to connect to backend");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                // Resume - reconnect if needed
                StartCoroutine(InitializeConnections());
            }
        }

        protected override void OnDestroy()
        {
            DisconnectFromGameServer();
        }
    }

    // WebSocket connection handler (simplified for testing)
    public class WebSocketConnection
    {
        private bool isConnected;
        public bool IsConnected => isConnected;

        public void Connect(string url)
        {
            Debug.Log($"Connecting to WebSocket: {url}");
            // In a real implementation, this would use a WebSocket library
            isConnected = true;
        }

        public void Disconnect()
        {
            Debug.Log("Disconnecting from WebSocket");
            isConnected = false;
        }

        public void Send(string eventName, object data)
        {
            Debug.Log($"Sending {eventName}: {JsonUtility.ToJson(data)}");
        }

        public void SendBatch(byte[] payload)
        {
            Debug.Log($"Sending batch: {payload.Length} bytes");
            // In a real implementation, this would send the batched payload via WebSocket
        }
    }
}