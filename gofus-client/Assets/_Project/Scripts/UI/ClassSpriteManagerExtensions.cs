using UnityEngine;
using GOFUS.Rendering;

namespace GOFUS.UI
{
    /// <summary>
    /// Extension methods for ClassSpriteManager to support layered character rendering
    /// </summary>
    public static class ClassSpriteManagerExtensions
    {
        /// <summary>
        /// Create a character renderer instance for a specific class
        /// </summary>
        /// <param name="manager">The ClassSpriteManager instance</param>
        /// <param name="classId">The class ID (1-12)</param>
        /// <param name="isMale">Whether the character is male</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="position">Position to place the character</param>
        /// <returns>The created CharacterLayerRenderer instance</returns>
        public static CharacterLayerRenderer CreateCharacterRenderer(
            this ClassSpriteManager manager,
            int classId,
            bool isMale = true,
            Transform parent = null,
            Vector3? position = null)
        {
            string className = manager.GetClassName(classId);
            GameObject characterObj = new GameObject($"Character_{className}_{(isMale ? "M" : "F")}");

            if (parent != null)
                characterObj.transform.SetParent(parent);

            characterObj.transform.position = position ?? Vector3.zero;

            // Add CharacterLayerRenderer component
            CharacterLayerRenderer renderer = characterObj.AddComponent<CharacterLayerRenderer>();
            renderer.SetClass(classId, isMale);

            Debug.Log($"[ClassSpriteManager] Created character renderer for {className}");

            return renderer;
        }
    }
}
