using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOFUS.UI
{
    public enum ChatChannel
    {
        General,
        Trade,
        Guild,
        Party,
        Whisper,
        System
    }

    [Serializable]
    public class ChatMessage
    {
        // Core fields
        public string sender;
        public string content;
        public ChatChannel channel;
        public DateTime timestamp;
        public Color color;
        public bool isSystemMessage;

        // Additional fields for FullChatSystem
        public string SenderName;
        public int SenderId;
        public string RecipientName;
        public string Text;
        public ChatChannel Channel;
        public DateTime Timestamp;
        public bool IsWhisper;
        public bool IsLocal;
        public string FormattedText;
        public bool ContainsMention;
        public bool MentionsPlayerFlag;  // True if the message mentions the current player
        public int EmoteCount;
        public bool ContainsEmotes;
        public bool ContainsLinks;
        public List<string> Links;

        public ChatMessage()
        {
            // Parameterless constructor for object initializer
            this.sender = "";
            this.content = "";
            this.channel = ChatChannel.General;
            this.timestamp = DateTime.Now;
            this.isSystemMessage = false;
            this.color = Color.white;
            this.SenderName = "";
            this.SenderId = 0;
            this.RecipientName = "";
            this.Text = "";
            this.Channel = ChatChannel.General;
            this.Timestamp = DateTime.Now;
            this.IsWhisper = false;
            this.IsLocal = true;
            this.FormattedText = "";
            this.ContainsMention = false;
            this.MentionsPlayerFlag = false;
            this.EmoteCount = 0;
            this.ContainsEmotes = false;
            this.ContainsLinks = false;
            this.Links = new List<string>();
        }

        public ChatMessage(string sender, string content, ChatChannel channel)
        {
            this.sender = sender;
            this.content = content;
            this.channel = channel;
            this.timestamp = DateTime.Now;
            this.isSystemMessage = false;
            this.color = GetChannelColor(channel);
            this.SenderName = sender;
            this.SenderId = 0;
            this.RecipientName = "";
            this.Text = content;
            this.Channel = channel;
            this.Timestamp = DateTime.Now;
            this.IsWhisper = channel == ChatChannel.Whisper;
            this.IsLocal = true;
            this.FormattedText = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{content}</color>";
            this.ContainsMention = false;
            this.MentionsPlayerFlag = false;
            this.EmoteCount = 0;
            this.ContainsEmotes = false;
            this.ContainsLinks = false;
            this.Links = new List<string>();
        }

        public bool MentionsPlayer(string playerName)
        {
            return content.Contains($"@{playerName}");
        }

        public static Color GetChannelColor(ChatChannel channel)
        {
            switch (channel)
            {
                case ChatChannel.General:
                    return Color.white;
                case ChatChannel.Trade:
                    return new Color(0.8f, 0.6f, 0.2f); // Orange
                case ChatChannel.Guild:
                    return new Color(0.2f, 0.8f, 0.2f); // Green
                case ChatChannel.Party:
                    return new Color(0.4f, 0.6f, 1.0f); // Light Blue
                case ChatChannel.Whisper:
                    return new Color(1.0f, 0.4f, 0.8f); // Pink
                case ChatChannel.System:
                    return new Color(1.0f, 1.0f, 0.2f); // Yellow
                default:
                    return Color.white;
            }
        }
    }
}