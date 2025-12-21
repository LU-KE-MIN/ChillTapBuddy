using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChillTapBuddy.Core
{
    /// <summary>
    /// Data model for persistent save data.
    /// Serialized to JSON and stored in PlayerPrefs.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int totalPoints;
        public int streak;
        public List<string> unlockedIds;
        public string equippedSkinId;
        public string equippedBgId;
        public int sessionsCompleted;
        public string lastSessionDate;

        public SaveData()
        {
            totalPoints = 0;
            streak = 0;
            unlockedIds = new List<string>();
            equippedSkinId = "default_skin";
            equippedBgId = "default_bg";
            sessionsCompleted = 0;
            lastSessionDate = "";
        }
    }

    /// <summary>
    /// Handles saving and loading game data using PlayerPrefs with JSON serialization.
    /// Implements singleton pattern for global access.
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        private const string SAVE_KEY = "ChillTapBuddy_SaveData";

        public static SaveService Instance { get; private set; }

        public SaveData CurrentData { get; private set; }

        public event Action OnDataLoaded;
        public event Action OnDataSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Load();
        }

        /// <summary>
        /// Loads save data from PlayerPrefs. Creates new data if none exists.
        /// </summary>
        public void Load()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                try
                {
                    CurrentData = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"[SaveService] Loaded save data: {CurrentData.totalPoints} points, {CurrentData.streak} streak");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveService] Failed to parse save data, creating new: {e.Message}");
                    CurrentData = new SaveData();
                }
            }
            else
            {
                Debug.Log("[SaveService] No save data found, creating new");
                CurrentData = new SaveData();
            }

            OnDataLoaded?.Invoke();
        }

        /// <summary>
        /// Saves current data to PlayerPrefs as JSON.
        /// </summary>
        public void Save()
        {
            if (CurrentData == null)
            {
                Debug.LogWarning("[SaveService] No data to save");
                return;
            }

            string json = JsonUtility.ToJson(CurrentData, true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();

            Debug.Log($"[SaveService] Saved: {json}");
            OnDataSaved?.Invoke();
        }

        /// <summary>
        /// Updates points and saves immediately.
        /// </summary>
        public void AddPoints(int amount)
        {
            CurrentData.totalPoints += amount;
            Save();
        }

        /// <summary>
        /// Updates streak count and saves.
        /// </summary>
        public void UpdateStreak(int newStreak)
        {
            CurrentData.streak = newStreak;
            Save();
        }

        /// <summary>
        /// Records a completed session and updates the last session date.
        /// </summary>
        public void RecordSessionCompleted()
        {
            CurrentData.sessionsCompleted++;
            CurrentData.lastSessionDate = DateTime.Now.ToString("yyyy-MM-dd");
            Save();
        }

        /// <summary>
        /// Checks if an item is unlocked.
        /// </summary>
        public bool IsUnlocked(string itemId)
        {
            return CurrentData.unlockedIds != null && CurrentData.unlockedIds.Contains(itemId);
        }

        /// <summary>
        /// Unlocks an item and saves.
        /// </summary>
        public bool TryUnlock(string itemId, int cost)
        {
            if (IsUnlocked(itemId))
            {
                Debug.Log($"[SaveService] Item {itemId} already unlocked");
                return false;
            }

            if (CurrentData.totalPoints < cost)
            {
                Debug.Log($"[SaveService] Not enough points to unlock {itemId}. Need {cost}, have {CurrentData.totalPoints}");
                return false;
            }

            CurrentData.totalPoints -= cost;
            CurrentData.unlockedIds.Add(itemId);
            Save();

            Debug.Log($"[SaveService] Unlocked {itemId} for {cost} points");
            return true;
        }

        /// <summary>
        /// Equips a skin if it's unlocked.
        /// </summary>
        public bool EquipSkin(string skinId)
        {
            if (!IsUnlocked(skinId) && skinId != "default_skin")
            {
                return false;
            }

            CurrentData.equippedSkinId = skinId;
            Save();
            return true;
        }

        /// <summary>
        /// Equips a background if it's unlocked.
        /// </summary>
        public bool EquipBackground(string bgId)
        {
            if (!IsUnlocked(bgId) && bgId != "default_bg")
            {
                return false;
            }

            CurrentData.equippedBgId = bgId;
            Save();
            return true;
        }

        /// <summary>
        /// Clears all save data and resets to defaults.
        /// </summary>
        public void ClearSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            CurrentData = new SaveData();
            Save();
            Debug.Log("[SaveService] Save data cleared");
        }

        /// <summary>
        /// Checks if the streak should be reset based on the last session date.
        /// Call this on app start to handle streak breaks.
        /// </summary>
        public void ValidateStreak()
        {
            if (string.IsNullOrEmpty(CurrentData.lastSessionDate))
            {
                return;
            }

            if (DateTime.TryParse(CurrentData.lastSessionDate, out DateTime lastSession))
            {
                int daysSinceLastSession = (DateTime.Now.Date - lastSession.Date).Days;
                
                if (daysSinceLastSession > 1)
                {
                    Debug.Log($"[SaveService] Streak broken - {daysSinceLastSession} days since last session");
                    CurrentData.streak = 0;
                    Save();
                }
            }
        }
    }
}
