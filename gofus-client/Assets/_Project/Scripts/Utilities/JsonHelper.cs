using System;
using UnityEngine;

namespace GOFUS
{
    /// <summary>
    /// Helper class to handle JSON array serialization in Unity
    /// Unity's JsonUtility doesn't directly support top-level arrays
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Convert a JSON array string to array of objects
        /// </summary>
        public static T[] FromJson<T>(string json)
        {
            // Check if it's an array
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
            {
                // Not an array, try to parse as single object
                try
                {
                    T singleObject = JsonUtility.FromJson<T>(json);
                    return new T[] { singleObject };
                }
                catch
                {
                    Debug.LogError($"[JsonHelper] Failed to parse JSON: {json}");
                    return null;
                }
            }

            // Wrap the array in an object
            string wrapped = "{\"Items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper.Items;
        }

        /// <summary>
        /// Convert an array of objects to JSON array string
        /// </summary>
        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T> { Items = array };
            string json = JsonUtility.ToJson(wrapper);

            // Remove the wrapper and return just the array
            int startIndex = json.IndexOf("[");
            int endIndex = json.LastIndexOf("]");

            if (startIndex != -1 && endIndex != -1)
            {
                return json.Substring(startIndex, endIndex - startIndex + 1);
            }

            return "[]";
        }

        /// <summary>
        /// Convert an array of objects to pretty-printed JSON
        /// </summary>
        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T> { Items = array };
            string json = JsonUtility.ToJson(wrapper, prettyPrint);

            // Remove the wrapper and return just the array
            int startIndex = json.IndexOf("[");
            int endIndex = json.LastIndexOf("]");

            if (startIndex != -1 && endIndex != -1)
            {
                return json.Substring(startIndex, endIndex - startIndex + 1);
            }

            return "[]";
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }
}