using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Compression;

namespace GOFUS.Networking
{
    /// <summary>
    /// Optimizes network communication for MMO-scale gameplay.
    /// Implements message batching, compression, and prioritization.
    /// Target: <100ms latency, <200KB/s bandwidth, 20-30 messages/second.
    /// </summary>
    public class NetworkOptimizer : MonoBehaviour
    {
        public enum MessagePriority
        {
            Critical,   // Combat actions, death (sent immediately)
            High,       // Movement, interactions (batched, high frequency)
            Medium,     // Chat, UI updates (batched, medium frequency)
            Low         // Cosmetic, background (batched, low frequency)
        }

        [Header("Message Batching")]
        [SerializeField] private float batchInterval = 0.05f; // 20 batches/second
        [SerializeField] private int maxBatchSize = 10;
        [SerializeField] private bool enableBatching = true;

        [Header("Compression")]
        [SerializeField] private bool enableCompression = true;
        [SerializeField] private int compressionThreshold = 512; // bytes

        [Header("Prioritization")]
        [SerializeField] private bool enablePrioritization = true;
        [SerializeField] private int criticalMessagesPerBatch = 10;
        [SerializeField] private int highMessagesPerBatch = 5;
        [SerializeField] private int mediumMessagesPerBatch = 3;
        [SerializeField] private int lowMessagesPerBatch = 2;

        [Header("Statistics")]
        [SerializeField] private int messagesSent;
        [SerializeField] private int messagesReceived;
        [SerializeField] private long bytesReduced;
        [SerializeField] private float averageLatency;
        [SerializeField] private int currentQueueSize;

        private Queue<NetworkMessage> outgoingQueue = new Queue<NetworkMessage>();
        private Dictionary<MessagePriority, Queue<NetworkMessage>> priorityQueues = new Dictionary<MessagePriority, Queue<NetworkMessage>>();

        private float lastBatchTime;
        private List<float> latencySamples = new List<float>(100);

        private void Awake()
        {
            InitializePriorityQueues();
        }

        private void InitializePriorityQueues()
        {
            foreach (MessagePriority priority in Enum.GetValues(typeof(MessagePriority)))
            {
                priorityQueues[priority] = new Queue<NetworkMessage>();
            }
        }

        private void Update()
        {
            // Process batched messages
            if (enableBatching && Time.time - lastBatchTime >= batchInterval)
            {
                ProcessMessageBatch();
                lastBatchTime = Time.time;
            }

            // Update queue size
            currentQueueSize = GetTotalQueueSize();
        }

        /// <summary>
        /// Queues a message for sending.
        /// </summary>
        /// <param name="eventName">Network event name</param>
        /// <param name="data">Message data</param>
        /// <param name="priority">Message priority level</param>
        public void QueueMessage(string eventName, object data, MessagePriority priority = MessagePriority.Medium)
        {
            NetworkMessage message = new NetworkMessage
            {
                eventName = eventName,
                data = data,
                priority = priority,
                timestamp = Time.time,
                sendTimestamp = DateTime.UtcNow
            };

            if (priority == MessagePriority.Critical && !enableBatching)
            {
                // Send critical messages immediately
                SendMessageImmediate(message);
            }
            else if (enablePrioritization)
            {
                priorityQueues[priority].Enqueue(message);
            }
            else
            {
                outgoingQueue.Enqueue(message);
            }
        }

        /// <summary>
        /// Sends a message immediately without batching.
        /// Use for critical actions only.
        /// </summary>
        private void SendMessageImmediate(NetworkMessage message)
        {
            byte[] payload = SerializeMessage(message);

            if (enableCompression && payload.Length > compressionThreshold)
            {
                payload = CompressData(payload);
            }

            NetworkManager.Instance?.Emit(message.eventName, payload);
            messagesSent++;
        }

        private void ProcessMessageBatch()
        {
            List<NetworkMessage> batch = new List<NetworkMessage>();

            // Select messages based on priority
            if (enablePrioritization)
            {
                batch = SelectPrioritizedMessages();
            }
            else
            {
                // Simple FIFO batching
                while (outgoingQueue.Count > 0 && batch.Count < maxBatchSize)
                {
                    batch.Add(outgoingQueue.Dequeue());
                }
            }

            if (batch.Count == 0) return;

            // Serialize batch
            byte[] payload = SerializeBatch(batch);

            // Compress if beneficial
            if (enableCompression && payload.Length > compressionThreshold)
            {
                byte[] compressed = CompressData(payload);
                int saved = payload.Length - compressed.Length;
                bytesReduced += saved;
                payload = compressed;
            }

            // Send batch
            SendBatch(payload, batch.Count);
            messagesSent += batch.Count;
        }

        private List<NetworkMessage> SelectPrioritizedMessages()
        {
            List<NetworkMessage> batch = new List<NetworkMessage>();

            // Critical messages (highest priority)
            int criticalAdded = 0;
            while (priorityQueues[MessagePriority.Critical].Count > 0 &&
                   criticalAdded < criticalMessagesPerBatch &&
                   batch.Count < maxBatchSize)
            {
                batch.Add(priorityQueues[MessagePriority.Critical].Dequeue());
                criticalAdded++;
            }

            // High priority messages
            int highAdded = 0;
            while (priorityQueues[MessagePriority.High].Count > 0 &&
                   highAdded < highMessagesPerBatch &&
                   batch.Count < maxBatchSize)
            {
                batch.Add(priorityQueues[MessagePriority.High].Dequeue());
                highAdded++;
            }

            // Medium priority messages
            int mediumAdded = 0;
            while (priorityQueues[MessagePriority.Medium].Count > 0 &&
                   mediumAdded < mediumMessagesPerBatch &&
                   batch.Count < maxBatchSize)
            {
                batch.Add(priorityQueues[MessagePriority.Medium].Dequeue());
                mediumAdded++;
            }

            // Low priority messages (fill remaining slots)
            int lowAdded = 0;
            while (priorityQueues[MessagePriority.Low].Count > 0 &&
                   lowAdded < lowMessagesPerBatch &&
                   batch.Count < maxBatchSize)
            {
                batch.Add(priorityQueues[MessagePriority.Low].Dequeue());
                lowAdded++;
            }

            return batch;
        }

        private byte[] SerializeMessage(NetworkMessage message)
        {
            string json = JsonUtility.ToJson(message);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        private byte[] SerializeBatch(List<NetworkMessage> batch)
        {
            MessageBatch messageBatch = new MessageBatch { messages = batch };
            string json = JsonUtility.ToJson(messageBatch);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        private byte[] CompressData(byte[] data)
        {
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return outputStream.ToArray();
            }
        }

        private byte[] DecompressData(byte[] data)
        {
            using (MemoryStream inputStream = new MemoryStream(data))
            using (GZipStream gzip = new GZipStream(inputStream, CompressionMode.Decompress))
            using (MemoryStream outputStream = new MemoryStream())
            {
                gzip.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

        private void SendBatch(byte[] payload, int messageCount)
        {
            NetworkManager.Instance?.EmitBatch(payload);
        }

        /// <summary>
        /// Records latency for a round-trip message.
        /// </summary>
        public void RecordLatency(float latencyMs)
        {
            latencySamples.Add(latencyMs);

            if (latencySamples.Count > 100)
            {
                latencySamples.RemoveAt(0);
            }

            // Calculate rolling average
            float sum = 0;
            foreach (float sample in latencySamples)
            {
                sum += sample;
            }
            averageLatency = sum / latencySamples.Count;
        }

        /// <summary>
        /// Records received message (for stats).
        /// </summary>
        public void RecordReceivedMessage()
        {
            messagesReceived++;
        }

        /// <summary>
        /// Gets network statistics.
        /// </summary>
        public NetworkStats GetStats()
        {
            return new NetworkStats
            {
                messagesSent = messagesSent,
                messagesReceived = messagesReceived,
                bytesReduced = bytesReduced,
                averageLatency = averageLatency,
                queueSize = currentQueueSize,
                compressionRatio = CalculateCompressionRatio()
            };
        }

        private float CalculateCompressionRatio()
        {
            // Simplified - track total bytes sent vs bytes reduced
            if (messagesSent == 0) return 1f;
            return 1f - ((float)bytesReduced / (messagesSent * 100)); // Approximate
        }

        private int GetTotalQueueSize()
        {
            int total = outgoingQueue.Count;
            foreach (var queue in priorityQueues.Values)
            {
                total += queue.Count;
            }
            return total;
        }

        /// <summary>
        /// Clears all message queues.
        /// </summary>
        public void ClearQueues()
        {
            outgoingQueue.Clear();
            foreach (var queue in priorityQueues.Values)
            {
                queue.Clear();
            }
            currentQueueSize = 0;
        }

        /// <summary>
        /// Logs network statistics.
        /// </summary>
        public void LogStats()
        {
            NetworkStats stats = GetStats();
            Debug.Log($"=== Network Optimizer Statistics ===");
            Debug.Log($"Messages Sent: {stats.messagesSent}");
            Debug.Log($"Messages Received: {stats.messagesReceived}");
            Debug.Log($"Bytes Reduced: {stats.bytesReduced / 1024}KB");
            Debug.Log($"Average Latency: {stats.averageLatency:F1}ms");
            Debug.Log($"Queue Size: {stats.queueSize}");
            Debug.Log($"Compression Ratio: {stats.compressionRatio:F2}");
        }

        private void OnDestroy()
        {
            ClearQueues();
        }
    }

    /// <summary>
    /// Individual network message.
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        public string eventName;
        public object data;
        public NetworkOptimizer.MessagePriority priority;
        public float timestamp;
        public DateTime sendTimestamp;
    }

    /// <summary>
    /// Batch of network messages.
    /// </summary>
    [Serializable]
    public class MessageBatch
    {
        public List<NetworkMessage> messages;
    }

    /// <summary>
    /// Network statistics data.
    /// </summary>
    [Serializable]
    public struct NetworkStats
    {
        public int messagesSent;
        public int messagesReceived;
        public long bytesReduced;
        public float averageLatency;
        public int queueSize;
        public float compressionRatio;

        public override string ToString()
        {
            return $"Sent: {messagesSent}, Received: {messagesReceived}, Latency: {averageLatency:F1}ms, Queue: {queueSize}";
        }
    }

    /// <summary>
    /// Extension methods for NetworkManager integration.
    /// </summary>
    public static class NetworkOptimizerExtensions
    {
        /// <summary>
        /// Sends a movement update (high priority).
        /// </summary>
        public static void SendMovement(this NetworkOptimizer optimizer, Vector2 position, int direction)
        {
            optimizer.QueueMessage("player:move", new { x = position.x, y = position.y, dir = direction },
                NetworkOptimizer.MessagePriority.High);
        }

        /// <summary>
        /// Sends a combat action (critical priority).
        /// </summary>
        public static void SendCombatAction(this NetworkOptimizer optimizer, string action, int targetId)
        {
            optimizer.QueueMessage("combat:action", new { action, targetId },
                NetworkOptimizer.MessagePriority.Critical);
        }

        /// <summary>
        /// Sends a chat message (medium priority).
        /// </summary>
        public static void SendChatMessage(this NetworkOptimizer optimizer, string message, string channel)
        {
            optimizer.QueueMessage("chat:message", new { message, channel },
                NetworkOptimizer.MessagePriority.Medium);
        }

        /// <summary>
        /// Sends a cosmetic update (low priority).
        /// </summary>
        public static void SendCosmeticUpdate(this NetworkOptimizer optimizer, string cosmeticData)
        {
            optimizer.QueueMessage("cosmetic:update", new { data = cosmeticData },
                NetworkOptimizer.MessagePriority.Low);
        }
    }
}
