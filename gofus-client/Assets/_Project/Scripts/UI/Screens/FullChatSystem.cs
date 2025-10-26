using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GOFUS.UI;

namespace GOFUS.UI.Screens
{
    /// <summary>
    /// Full Chat System with multi-channel support
    /// Implements comprehensive chat functionality like Dofus
    /// </summary>
    public class FullChatSystem : UIScreen
    {
        #region Properties

        public ChatChannel ActiveChannel { get; private set; } = ChatChannel.General;
        public string PlayerName { get; private set; } = "You";
        public bool IsOnline { get; private set; } = true;

        private Dictionary<ChatChannel, List<ChatMessage>> channelMessages;
        private Dictionary<string, List<ChatMessage>> whisperConversations;
        private Dictionary<ChatChannel, int> unreadCounts;
        private HashSet<ChatChannel> joinedChannels;
        private HashSet<string> mutedPlayers;
        private HashSet<ChatChannel> blockedChannels;
        private HashSet<string> profanityWords;
        private List<string> knownPlayers;
        private Queue<ChatMessage> messageQueue;
        private List<string> inputHistory;
        private int historyIndex;

        // Settings
        private bool timestampsEnabled;
        private bool emotesEnabled;
        private bool profanityFilterEnabled;
        private bool spamFilterEnabled;
        private bool notificationsEnabled;
        private int maxHistorySize = 500;
        private int rateLimitCount = 5;
        private int rateLimitSeconds = 10;

        // Spam tracking
        private Dictionary<string, DateTime> lastMessageTime;
        private Dictionary<string, int> messageCount;
        private DateTime rateLimitResetTime;
        private int currentRateLimitCount;

        // Commands
        private Dictionary<string, Action<string[]>> commands;

        #endregion

        #region Events

        public event Action<ChatMessage> OnMessageReceived;
        public event Action<ChatMessage> OnMessageSent;
        public event Action<string> OnWhisperReceived;
        public event Action<ChatChannel> OnChannelChanged;
        public event Action<string> OnPlayerMentioned;

        #endregion

        #region UI Elements

        private ScrollRect chatScrollRect;
        private Transform messageContainer;
        private TMP_InputField inputField;
        private TextMeshProUGUI channelLabel;
        private Transform channelTabs;
        private GameObject messagePrefab;
        private Dictionary<ChatChannel, GameObject> channelTabObjects;

        #endregion

        #region Initialization

        public override void Initialize()
        {
            base.Initialize();
            InitializeChatSystem();
            CreateChatUI();
            RegisterDefaultCommands();
        }

        private void InitializeChatSystem()
        {
            channelMessages = new Dictionary<ChatChannel, List<ChatMessage>>();
            whisperConversations = new Dictionary<string, List<ChatMessage>>();
            unreadCounts = new Dictionary<ChatChannel, int>();
            joinedChannels = new HashSet<ChatChannel>();
            mutedPlayers = new HashSet<string>();
            blockedChannels = new HashSet<ChatChannel>();
            profanityWords = new HashSet<string>();
            knownPlayers = new List<string>();
            messageQueue = new Queue<ChatMessage>();
            inputHistory = new List<string>();
            lastMessageTime = new Dictionary<string, DateTime>();
            messageCount = new Dictionary<string, int>();
            commands = new Dictionary<string, Action<string[]>>();
            channelTabObjects = new Dictionary<ChatChannel, GameObject>();

            // Initialize all channels
            foreach (ChatChannel channel in Enum.GetValues(typeof(ChatChannel)))
            {
                channelMessages[channel] = new List<ChatMessage>();
                unreadCounts[channel] = 0;
            }

            // Auto-join default channels
            JoinChannel(ChatChannel.General);
            JoinChannel(ChatChannel.System);
        }

        private void CreateChatUI()
        {
            // Main chat window
            GameObject chatWindow = new GameObject("ChatWindow");
            chatWindow.transform.SetParent(transform, false);

            RectTransform windowRect = chatWindow.AddComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0, 0);
            windowRect.anchorMax = new Vector2(0, 0);
            windowRect.pivot = new Vector2(0, 0);
            windowRect.anchoredPosition = new Vector2(10, 10);
            windowRect.sizeDelta = new Vector2(400, 250);

            // Background
            Image bg = chatWindow.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            // Channel tabs
            CreateChannelTabs(chatWindow.transform);

            // Message area
            CreateMessageArea(chatWindow.transform);

            // Input field
            CreateInputField(chatWindow.transform);
        }

        private void CreateChannelTabs(Transform parent)
        {
            GameObject tabs = new GameObject("ChannelTabs");
            tabs.transform.SetParent(parent, false);

            RectTransform rect = tabs.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -5);
            rect.sizeDelta = new Vector2(-10, 25);

            HorizontalLayoutGroup layout = tabs.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            channelTabs = tabs.transform;

            // Create initial tabs
            foreach (ChatChannel channel in Enum.GetValues(typeof(ChatChannel)))
            {
                if (channel != ChatChannel.Whisper) // Whisper tabs created dynamically
                    CreateChannelTab(channel);
            }
        }

        private void CreateChannelTab(ChatChannel channel)
        {
            GameObject tab = new GameObject($"Tab_{channel}");
            tab.transform.SetParent(channelTabs, false);

            Button btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() => SetActiveChannel(channel));

            Image img = tab.AddComponent<Image>();
            img.color = GetChannelColor(channel) * 0.7f;

            // Tab text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tab.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = GetChannelShortName(channel);
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;

            // Size
            LayoutElement layout = tab.AddComponent<LayoutElement>();
            layout.preferredWidth = 50;

            // Unread indicator
            GameObject unreadObj = new GameObject("UnreadCount");
            unreadObj.transform.SetParent(tab.transform, false);

            RectTransform unreadRect = unreadObj.AddComponent<RectTransform>();
            unreadRect.anchorMin = new Vector2(1, 1);
            unreadRect.anchorMax = new Vector2(1, 1);
            unreadRect.pivot = new Vector2(1, 1);
            unreadRect.anchoredPosition = new Vector2(-2, -2);
            unreadRect.sizeDelta = new Vector2(16, 16);

            Image unreadBg = unreadObj.AddComponent<Image>();
            unreadBg.color = Color.red;

            TextMeshProUGUI unreadText = new GameObject("Count").AddComponent<TextMeshProUGUI>();
            unreadText.transform.SetParent(unreadObj.transform, false);
            unreadText.text = "0";
            unreadText.fontSize = 10;
            unreadText.alignment = TextAlignmentOptions.Center;

            unreadObj.SetActive(false);

            channelTabObjects[channel] = tab;
        }

        private void CreateMessageArea(Transform parent)
        {
            GameObject scrollView = new GameObject("MessageScrollView");
            scrollView.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(0, -15);
            scrollRect.sizeDelta = new Vector2(-10, -60);

            chatScrollRect = scrollView.AddComponent<ScrollRect>();
            chatScrollRect.vertical = true;
            chatScrollRect.horizontal = false;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(scrollView.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            messageContainer = content.transform;
            chatScrollRect.content = contentRect;
        }

        private void CreateInputField(Transform parent)
        {
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(parent, false);

            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 5);
            rect.sizeDelta = new Vector2(-10, 25);

            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1);

            inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.onSubmit.AddListener(ProcessInput);
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            // Placeholder
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(inputObj.transform, false);

            RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = new Vector2(5, 0);

            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Type a message...";
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            inputField.placeholder = placeholderText;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = new Vector2(5, 0);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 12;

            inputField.textComponent = text;
        }

        #endregion

        #region Message Handling

        public bool SendMessage(string text, ChatChannel channel)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            // Check spam filter
            if (spamFilterEnabled && IsSpam(text))
                return false;

            // Check rate limiting
            if (!CheckRateLimit())
                return false;

            // Apply profanity filter
            if (profanityFilterEnabled)
                text = FilterProfanity(text);

            var message = new ChatMessage
            {
                SenderName = PlayerName,
                SenderId = 0, // Local player
                Text = text,
                Channel = channel,
                Timestamp = DateTime.Now,
                IsLocal = true
            };

            ProcessMessage(message);

            if (!IsOnline)
            {
                messageQueue.Enqueue(message);
            }
            else
            {
                channelMessages[channel].Add(message);
                DisplayMessage(message);
                OnMessageSent?.Invoke(message);
            }

            // Maintain history limit
            TrimHistory(channel);

            return true;
        }

        public void ReceiveMessage(string senderName, string text, ChatChannel channel)
        {
            if (mutedPlayers.Contains(senderName)) return;
            if (blockedChannels.Contains(channel)) return;

            var message = new ChatMessage
            {
                SenderName = senderName,
                SenderId = senderName.GetHashCode(),
                Text = text,
                Channel = channel,
                Timestamp = DateTime.Now,
                IsLocal = false
            };

            ProcessMessage(message);
            channelMessages[channel].Add(message);

            if (channel != ActiveChannel)
            {
                unreadCounts[channel]++;
                UpdateChannelTabBadge(channel);
            }

            DisplayMessage(message);

            // Check for mentions
            if (message.ContainsMention && message.MentionsPlayerFlag)
            {
                OnPlayerMentioned?.Invoke(senderName);
                if (notificationsEnabled)
                    ShowNotification($"{senderName} mentioned you");
            }

            OnMessageReceived?.Invoke(message);
            TrimHistory(channel);
        }

        private void ProcessMessage(ChatMessage message)
        {
            // Format text with timestamp
            if (timestampsEnabled)
            {
                message.FormattedText = $"[{message.Timestamp:HH:mm}] {message.SenderName}: {message.Text}";
            }
            else
            {
                message.FormattedText = $"{message.SenderName}: {message.Text}";
            }

            // Parse emotes
            if (emotesEnabled)
            {
                ParseEmotes(message);
            }

            // Detect mentions
            DetectMentions(message);

            // Parse links
            ParseLinks(message);
        }

        private void ParseEmotes(ChatMessage message)
        {
            var emotePattern = @"(:\)|:\(|:D|:P|;\)|:o|:\||:\\|:@)";
            var matches = Regex.Matches(message.Text, emotePattern);
            message.ContainsEmotes = matches.Count > 0;
            message.EmoteCount = matches.Count;
        }

        private void DetectMentions(ChatMessage message)
        {
            var mentionPattern = @"@(\w+)";
            var matches = Regex.Matches(message.Text, mentionPattern);
            message.ContainsMention = matches.Count > 0;

            foreach (Match match in matches)
            {
                if (match.Groups[1].Value.Equals(PlayerName, StringComparison.OrdinalIgnoreCase))
                {
                    message.MentionsPlayerFlag = true;
                    break;
                }
            }
        }

        private void ParseLinks(ChatMessage message)
        {
            var linkPattern = @"https?://[^\s]+";
            var matches = Regex.Matches(message.Text, linkPattern);
            message.ContainsLinks = matches.Count > 0;

            if (message.Links == null)
                message.Links = new List<string>();
            else
                message.Links.Clear();

            foreach (Match match in matches)
            {
                message.Links.Add(match.Value);
            }
        }

        #endregion

        #region Channel Management

        public List<ChatChannel> GetAvailableChannels()
        {
            return Enum.GetValues(typeof(ChatChannel)).Cast<ChatChannel>().ToList();
        }

        public void SetActiveChannel(ChatChannel channel)
        {
            ActiveChannel = channel;
            unreadCounts[channel] = 0;
            UpdateChannelTabBadge(channel);
            RefreshMessageDisplay();
            OnChannelChanged?.Invoke(channel);
        }

        public void JoinChannel(ChatChannel channel)
        {
            joinedChannels.Add(channel);
        }

        public void LeaveChannel(ChatChannel channel)
        {
            joinedChannels.Remove(channel);
        }

        public bool IsInChannel(ChatChannel channel)
        {
            return joinedChannels.Contains(channel);
        }

        public int GetUnreadCount(ChatChannel channel)
        {
            return unreadCounts.ContainsKey(channel) ? unreadCounts[channel] : 0;
        }

        public Color GetChannelColor(ChatChannel channel)
        {
            switch (channel)
            {
                case ChatChannel.General: return Color.white;
                case ChatChannel.Party: return new Color(0.5f, 0.5f, 1f);
                case ChatChannel.Guild: return new Color(1f, 0.5f, 0f);
                case ChatChannel.Trade: return Color.yellow;
                case ChatChannel.System: return Color.green;
                case ChatChannel.Whisper: return new Color(1f, 0.5f, 1f);
                default: return Color.white;
            }
        }

        private string GetChannelShortName(ChatChannel channel)
        {
            switch (channel)
            {
                case ChatChannel.General: return "Gen";
                case ChatChannel.Party: return "Party";
                case ChatChannel.Guild: return "Guild";
                case ChatChannel.Trade: return "Trade";
                case ChatChannel.System: return "Sys";
                case ChatChannel.Whisper: return "W";
                default: return channel.ToString().Substring(0, 3);
            }
        }

        #endregion

        #region Whisper System

        public void SendWhisper(string targetPlayer, string text)
        {
            var message = new ChatMessage
            {
                SenderName = PlayerName,
                RecipientName = targetPlayer,
                Text = text,
                Channel = ChatChannel.Whisper,
                Timestamp = DateTime.Now,
                IsWhisper = true,
                IsLocal = true
            };

            if (!whisperConversations.ContainsKey(targetPlayer))
                whisperConversations[targetPlayer] = new List<ChatMessage>();

            whisperConversations[targetPlayer].Add(message);
            DisplayMessage(message);
        }

        public void ReceiveWhisper(string fromPlayer, string text)
        {
            var message = new ChatMessage
            {
                SenderName = fromPlayer,
                RecipientName = PlayerName,
                Text = text,
                Channel = ChatChannel.Whisper,
                Timestamp = DateTime.Now,
                IsWhisper = true,
                IsLocal = false
            };

            if (!whisperConversations.ContainsKey(fromPlayer))
                whisperConversations[fromPlayer] = new List<ChatMessage>();

            whisperConversations[fromPlayer].Add(message);
            DisplayMessage(message);

            if (notificationsEnabled)
                ShowNotification($"Whisper from {fromPlayer}");

            OnWhisperReceived?.Invoke(fromPlayer);
        }

        public List<ChatMessage> GetWhisperConversation(string player)
        {
            return whisperConversations.ContainsKey(player) ?
                new List<ChatMessage>(whisperConversations[player]) :
                new List<ChatMessage>();
        }

        public List<string> GetActiveWhispers()
        {
            return whisperConversations.Keys.ToList();
        }

        #endregion

        #region Filter and Moderation

        public void EnableProfanityFilter(bool enabled)
        {
            profanityFilterEnabled = enabled;
        }

        public void AddProfanityWord(string word)
        {
            profanityWords.Add(word.ToLower());
        }

        private string FilterProfanity(string text)
        {
            foreach (var word in profanityWords)
            {
                var pattern = $@"\b{Regex.Escape(word)}\b";
                text = Regex.Replace(text, pattern, new string('*', word.Length), RegexOptions.IgnoreCase);
            }
            return text;
        }

        public void EnableSpamFilter(bool enabled)
        {
            spamFilterEnabled = enabled;
        }

        private bool IsSpam(string text)
        {
            if (!spamFilterEnabled) return false;

            string key = $"{PlayerName}:{text.GetHashCode()}";

            if (!lastMessageTime.ContainsKey(key))
            {
                lastMessageTime[key] = DateTime.Now;
                messageCount[key] = 1;
                return false;
            }

            var timeSinceLast = DateTime.Now - lastMessageTime[key];
            if (timeSinceLast.TotalSeconds < 2)
            {
                messageCount[key]++;
                if (messageCount[key] > 3)
                    return true;
            }
            else
            {
                messageCount[key] = 1;
            }

            lastMessageTime[key] = DateTime.Now;
            return false;
        }

        public void MutePlayer(string playerName)
        {
            mutedPlayers.Add(playerName);
        }

        public void UnmutePlayer(string playerName)
        {
            mutedPlayers.Remove(playerName);
        }

        public void BlockChannel(ChatChannel channel)
        {
            blockedChannels.Add(channel);
        }

        public void UnblockChannel(ChatChannel channel)
        {
            blockedChannels.Remove(channel);
        }

        #endregion

        #region Command System

        public void ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                inputField?.SetTextWithoutNotify("");
                return;
            }

            AddToInputHistory(input);

            if (input.StartsWith("/"))
            {
                ProcessCommand(input);
            }
            else
            {
                SendMessage(input, ActiveChannel);
            }

            inputField?.SetTextWithoutNotify("");
        }

        private void ProcessCommand(string input)
        {
            var parts = input.Split(' ');
            var command = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            if (commands.ContainsKey(command))
            {
                commands[command](args);
            }
            else
            {
                // Handle built-in commands
                switch (command)
                {
                    case "/w":
                    case "/whisper":
                        if (args.Length >= 2)
                        {
                            var target = args[0];
                            var message = string.Join(" ", args.Skip(1));
                            SendWhisper(target, message);
                        }
                        break;

                    case "/p":
                    case "/party":
                        if (args.Length > 0)
                        {
                            var message = string.Join(" ", args);
                            SendMessage(message, ChatChannel.Party);
                        }
                        else
                        {
                            SetActiveChannel(ChatChannel.Party);
                        }
                        break;

                    case "/g":
                    case "/guild":
                        if (args.Length > 0)
                        {
                            var message = string.Join(" ", args);
                            SendMessage(message, ChatChannel.Guild);
                        }
                        else
                        {
                            SetActiveChannel(ChatChannel.Guild);
                        }
                        break;

                    case "/clear":
                        ClearChannel(ActiveChannel);
                        break;

                    case "/mute":
                        if (args.Length > 0)
                            MutePlayer(args[0]);
                        break;

                    case "/unmute":
                        if (args.Length > 0)
                            UnmutePlayer(args[0]);
                        break;

                    default:
                        SendMessage($"Unknown command: {command}", ChatChannel.System);
                        break;
                }
            }
        }

        public void RegisterCommand(string command, Action<string[]> handler)
        {
            commands[command.ToLower()] = handler;
        }

        private void RegisterDefaultCommands()
        {
            RegisterCommand("/help", (args) =>
            {
                SendMessage("Available commands:", ChatChannel.System);
                SendMessage("/w <player> <message> - Send whisper", ChatChannel.System);
                SendMessage("/p <message> - Send to party", ChatChannel.System);
                SendMessage("/g <message> - Send to guild", ChatChannel.System);
                SendMessage("/clear - Clear current channel", ChatChannel.System);
                SendMessage("/mute <player> - Mute player", ChatChannel.System);
            });
        }

        #endregion

        #region History and Search

        public List<ChatMessage> GetMessages(ChatChannel channel)
        {
            return channelMessages.ContainsKey(channel) ?
                new List<ChatMessage>(channelMessages[channel]) :
                new List<ChatMessage>();
        }

        public void SetMaxHistorySize(int size)
        {
            maxHistorySize = size;
            foreach (var channel in channelMessages.Keys)
            {
                TrimHistory(channel);
            }
        }

        private void TrimHistory(ChatChannel channel)
        {
            if (channelMessages[channel].Count > maxHistorySize)
            {
                int toRemove = channelMessages[channel].Count - maxHistorySize;
                channelMessages[channel].RemoveRange(0, toRemove);
            }
        }

        public void SaveChatHistory()
        {
            // Save to PlayerPrefs or file
            foreach (var kvp in channelMessages)
            {
                var messages = kvp.Value.Take(100).Select(m => JsonUtility.ToJson(m)).ToList();
                PlayerPrefs.SetString($"ChatHistory_{kvp.Key}", string.Join("|", messages));
            }
            PlayerPrefs.Save();
        }

        public void LoadChatHistory()
        {
            foreach (ChatChannel channel in Enum.GetValues(typeof(ChatChannel)))
            {
                string saved = PlayerPrefs.GetString($"ChatHistory_{channel}", "");
                if (!string.IsNullOrEmpty(saved))
                {
                    var messages = saved.Split('|').Select(s => JsonUtility.FromJson<ChatMessage>(s)).ToList();
                    channelMessages[channel] = messages;
                }
            }
        }

        public List<ChatMessage> SearchMessages(string keyword, ChatChannel? channel = null)
        {
            var results = new List<ChatMessage>();
            var channels = channel.HasValue ?
                new[] { channel.Value } :
                channelMessages.Keys.ToArray();

            foreach (var ch in channels)
            {
                var matches = channelMessages[ch].Where(m =>
                    m.Text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                results.AddRange(matches);
            }

            return results;
        }

        private void ClearChannel(ChatChannel channel)
        {
            channelMessages[channel].Clear();
            RefreshMessageDisplay();
        }

        #endregion

        #region Input Management

        public void AddToInputHistory(string text)
        {
            inputHistory.Add(text);
            if (inputHistory.Count > 50)
                inputHistory.RemoveAt(0);
            historyIndex = inputHistory.Count;
        }

        public List<string> GetInputHistory()
        {
            return new List<string>(inputHistory);
        }

        public string GetPreviousInput()
        {
            if (inputHistory.Count == 0) return "";

            historyIndex = Mathf.Max(0, historyIndex - 1);
            return inputHistory[historyIndex];
        }

        public string GetNextInput()
        {
            if (inputHistory.Count == 0) return "";

            historyIndex = Mathf.Min(inputHistory.Count - 1, historyIndex + 1);
            return inputHistory[historyIndex];
        }

        public void AddKnownPlayer(string playerName)
        {
            if (!knownPlayers.Contains(playerName))
                knownPlayers.Add(playerName);
        }

        public List<string> GetAutocompleteSuggestions(string prefix)
        {
            return knownPlayers.Where(p =>
                p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        #endregion

        #region Network Integration

        public void SetOnlineStatus(bool online)
        {
            IsOnline = online;

            if (online)
            {
                FlushMessageQueue();
            }
        }

        public Queue<ChatMessage> GetQueuedMessages()
        {
            return new Queue<ChatMessage>(messageQueue);
        }

        public void FlushMessageQueue()
        {
            while (messageQueue.Count > 0)
            {
                var message = messageQueue.Dequeue();
                channelMessages[message.Channel].Add(message);
                OnMessageSent?.Invoke(message);
            }
        }

        public void SetRateLimit(int count, int seconds)
        {
            rateLimitCount = count;
            rateLimitSeconds = seconds;
        }

        private bool CheckRateLimit()
        {
            if (DateTime.Now > rateLimitResetTime)
            {
                rateLimitResetTime = DateTime.Now.AddSeconds(rateLimitSeconds);
                currentRateLimitCount = 0;
            }

            currentRateLimitCount++;
            return currentRateLimitCount <= rateLimitCount;
        }

        #endregion

        #region UI Updates

        private void DisplayMessage(ChatMessage message)
        {
            if (message.Channel != ActiveChannel && message.Channel != ChatChannel.Whisper)
                return;

            GameObject messageObj = new GameObject($"Message_{message.Timestamp.Ticks}");
            messageObj.transform.SetParent(messageContainer, false);

            RectTransform rect = messageObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 20);

            TextMeshProUGUI text = messageObj.AddComponent<TextMeshProUGUI>();
            text.text = message.FormattedText;
            text.fontSize = 12;
            text.color = GetChannelColor(message.Channel);

            if (message.MentionsPlayerFlag)
                text.color = Color.yellow;

            ContentSizeFitter fitter = messageObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Auto-scroll to bottom
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }

        private void RefreshMessageDisplay()
        {
            // Clear current display
            foreach (Transform child in messageContainer)
                Destroy(child.gameObject);

            // Display messages for active channel
            var messages = GetMessages(ActiveChannel);
            foreach (var message in messages.TakeLast(50))
            {
                DisplayMessage(message);
            }
        }

        private void UpdateChannelTabBadge(ChatChannel channel)
        {
            if (!channelTabObjects.ContainsKey(channel)) return;

            var tab = channelTabObjects[channel];
            var unreadObj = tab.transform.Find("UnreadCount");
            if (unreadObj != null)
            {
                int count = GetUnreadCount(channel);
                unreadObj.gameObject.SetActive(count > 0);

                var text = unreadObj.Find("Count")?.GetComponent<TextMeshProUGUI>();
                if (text != null)
                    text.text = count > 99 ? "99+" : count.ToString();
            }
        }

        #endregion

        #region Settings

        public void EnableTimestamps(bool enabled)
        {
            timestampsEnabled = enabled;
        }

        public void EnableEmotes(bool enabled)
        {
            emotesEnabled = enabled;
        }

        public void EnableNotifications(bool enabled)
        {
            notificationsEnabled = enabled;
        }

        public void SetPlayerName(string name)
        {
            PlayerName = name;
        }

        #endregion

        #region Notifications

        private string latestNotification;
        private bool hasNewNotification;

        private void ShowNotification(string message)
        {
            latestNotification = message;
            hasNewNotification = true;
            // In real implementation, would show UI notification
        }

        public bool HasNewNotification()
        {
            return hasNewNotification;
        }

        public string GetLatestNotification()
        {
            hasNewNotification = false;
            return latestNotification;
        }

        #endregion
    }
}