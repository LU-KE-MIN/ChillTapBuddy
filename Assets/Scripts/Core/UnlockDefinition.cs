using UnityEngine;

namespace ChillTapBuddy.Core
{
    /// <summary>
    /// ScriptableObject defining an unlockable item.
    /// Create instances via Assets > Create > ChillTapBuddy > Unlock Definition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnlock", menuName = "ChillTapBuddy/Unlock Definition")]
    public class UnlockDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this unlock")]
        public string id;
        
        [Tooltip("Display name shown in UI")]
        public string displayName;
        
        [Tooltip("Description of the item")]
        [TextArea(2, 4)]
        public string description;

        [Header("Cost")]
        [Tooltip("Points required to unlock this item")]
        public int costPoints;

        [Header("Visuals")]
        [Tooltip("Preview sprite shown in the unlock list")]
        public Sprite previewSprite;
        
        [Tooltip("Actual sprite to apply when equipped (for skins)")]
        public Sprite equipSprite;

        [Header("Type")]
        [Tooltip("Category of unlock")]
        public UnlockType unlockType = UnlockType.Skin;

        /// <summary>
        /// Validates the ScriptableObject in the editor.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = name.ToLowerInvariant().Replace(" ", "_");
            }
        }
    }

    /// <summary>
    /// Categories of unlockable items.
    /// </summary>
    public enum UnlockType
    {
        Skin,
        Background,
        Accessory,
        Special
    }
}
