using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChillTapBuddy.Core
{
    /// <summary>
    /// Handles reward calculations, streak management, and unlock processing.
    /// Computes points based on session completion, tap bonuses, and streaks.
    /// </summary>
    public class RewardSystem : MonoBehaviour
    {
        [Header("Base Rewards")]
        [Tooltip("Points awarded for completing a focus session")]
        [SerializeField] private int baseSessionPoints = 100;
        
        [Tooltip("Points awarded per bonus tap during session")]
        [SerializeField] private int pointsPerTap = 5;

        [Header("Streak Bonuses")]
        [Tooltip("Additional percentage per streak level (e.g., 10 = 10% bonus per streak)")]
        [SerializeField] private int streakBonusPercent = 10;
        
        [Tooltip("Maximum streak bonus percentage cap")]
        [SerializeField] private int maxStreakBonusPercent = 100;

        [Header("Unlocks")]
        [Tooltip("List of all available unlock definitions")]
        [SerializeField] private List<UnlockDefinition> unlockDefinitions;

        public static RewardSystem Instance { get; private set; }

        /// <summary>
        /// Fired when points are awarded. Contains total points earned and breakdown.
        /// </summary>
        public event Action<RewardBreakdown> OnRewardCalculated;

        /// <summary>
        /// Fired when an item is successfully unlocked.
        /// </summary>
        public event Action<UnlockDefinition> OnItemUnlocked;

        /// <summary>
        /// Fired when an unlock attempt fails due to insufficient points.
        /// </summary>
        public event Action<UnlockDefinition, int> OnUnlockFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Calculates and awards points for a completed focus session.
        /// </summary>
        /// <param name="tapBonusCount">Number of bonus taps earned during the session</param>
        /// <param name="activityBonusPoints">Points from keyboard activity tracking</param>
        /// <returns>Breakdown of all points earned</returns>
        public RewardBreakdown CalculateSessionReward(int tapBonusCount, int activityBonusPoints = 0)
        {
            SaveData data = SaveService.Instance.CurrentData;
            
            // Calculate base points
            int basePoints = baseSessionPoints;
            
            // Calculate tap bonus
            int tapBonus = tapBonusCount * pointsPerTap;
            
            // Calculate streak bonus
            int currentStreak = data.streak + 1; // Increment for this completed session
            int streakPercent = Mathf.Min(currentStreak * streakBonusPercent, maxStreakBonusPercent);
            int streakBonus = Mathf.RoundToInt(basePoints * streakPercent / 100f);
            
            // Total
            int totalPoints = basePoints + tapBonus + activityBonusPoints + streakBonus;

            RewardBreakdown breakdown = new RewardBreakdown
            {
                BasePoints = basePoints,
                TapBonus = tapBonus,
                ActivityBonus = activityBonusPoints,
                StreakBonus = streakBonus,
                StreakLevel = currentStreak,
                TotalPoints = totalPoints
            };

            // Update save data
            SaveService.Instance.AddPoints(totalPoints);
            SaveService.Instance.UpdateStreak(currentStreak);
            SaveService.Instance.RecordSessionCompleted();

            Debug.Log($"[RewardSystem] Session complete! Base: {basePoints}, Taps: {tapBonus}, " +
                      $"Activity: {activityBonusPoints}, Streak ({currentStreak}x): {streakBonus}, " +
                      $"Total: {totalPoints}");

            OnRewardCalculated?.Invoke(breakdown);
            return breakdown;
        }

        /// <summary>
        /// Attempts to unlock an item by ID.
        /// </summary>
        /// <returns>True if unlock succeeded, false if insufficient points or already unlocked</returns>
        public bool TryUnlockItem(string itemId)
        {
            UnlockDefinition def = GetUnlockById(itemId);
            if (def == null)
            {
                Debug.LogWarning($"[RewardSystem] Unknown unlock ID: {itemId}");
                return false;
            }

            return TryUnlockItem(def);
        }

        /// <summary>
        /// Attempts to unlock an item by definition.
        /// </summary>
        public bool TryUnlockItem(UnlockDefinition def)
        {
            if (SaveService.Instance.IsUnlocked(def.id))
            {
                Debug.Log($"[RewardSystem] Item {def.id} already unlocked");
                return false;
            }

            int currentPoints = SaveService.Instance.CurrentData.totalPoints;
            
            if (currentPoints < def.costPoints)
            {
                int needed = def.costPoints - currentPoints;
                Debug.Log($"[RewardSystem] Cannot unlock {def.id}. Need {needed} more points");
                OnUnlockFailed?.Invoke(def, needed);
                return false;
            }

            if (SaveService.Instance.TryUnlock(def.id, def.costPoints))
            {
                Debug.Log($"[RewardSystem] Unlocked {def.displayName}!");
                OnItemUnlocked?.Invoke(def);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets an unlock definition by ID.
        /// </summary>
        public UnlockDefinition GetUnlockById(string id)
        {
            if (unlockDefinitions == null) return null;
            return unlockDefinitions.Find(u => u.id == id);
        }

        /// <summary>
        /// Gets all unlock definitions.
        /// </summary>
        public List<UnlockDefinition> GetAllUnlocks()
        {
            return unlockDefinitions ?? new List<UnlockDefinition>();
        }

        /// <summary>
        /// Gets all unlocked items.
        /// </summary>
        public List<UnlockDefinition> GetUnlockedItems()
        {
            List<UnlockDefinition> unlocked = new List<UnlockDefinition>();
            if (unlockDefinitions == null) return unlocked;

            foreach (var def in unlockDefinitions)
            {
                if (SaveService.Instance.IsUnlocked(def.id))
                {
                    unlocked.Add(def);
                }
            }

            return unlocked;
        }

        /// <summary>
        /// Gets all locked items sorted by cost.
        /// </summary>
        public List<UnlockDefinition> GetLockedItems()
        {
            List<UnlockDefinition> locked = new List<UnlockDefinition>();
            if (unlockDefinitions == null) return locked;

            foreach (var def in unlockDefinitions)
            {
                if (!SaveService.Instance.IsUnlocked(def.id))
                {
                    locked.Add(def);
                }
            }

            locked.Sort((a, b) => a.costPoints.CompareTo(b.costPoints));
            return locked;
        }

        /// <summary>
        /// Gets progress toward the next unlock as a percentage.
        /// </summary>
        public float GetNextUnlockProgress()
        {
            List<UnlockDefinition> locked = GetLockedItems();
            if (locked.Count == 0) return 1f;

            int currentPoints = SaveService.Instance.CurrentData.totalPoints;
            int nextCost = locked[0].costPoints;

            return Mathf.Clamp01((float)currentPoints / nextCost);
        }

        /// <summary>
        /// Gets the next unlock available for purchase, or null if all unlocked.
        /// </summary>
        public UnlockDefinition GetNextUnlock()
        {
            List<UnlockDefinition> locked = GetLockedItems();
            return locked.Count > 0 ? locked[0] : null;
        }

        /// <summary>
        /// Resets streak to zero. Call when user misses a day.
        /// </summary>
        public void ResetStreak()
        {
            SaveService.Instance.UpdateStreak(0);
            Debug.Log("[RewardSystem] Streak reset to 0");
        }

        /// <summary>
        /// Gets the current streak level.
        /// </summary>
        public int GetCurrentStreak()
        {
            return SaveService.Instance.CurrentData.streak;
        }

        /// <summary>
        /// Gets the bonus percentage for the current streak.
        /// </summary>
        public int GetCurrentStreakBonusPercent()
        {
            int streak = GetCurrentStreak();
            return Mathf.Min(streak * streakBonusPercent, maxStreakBonusPercent);
        }
    }

    /// <summary>
    /// Detailed breakdown of points earned from a session.
    /// </summary>
    [Serializable]
    public struct RewardBreakdown
    {
        public int BasePoints;
        public int TapBonus;
        public int ActivityBonus;
        public int StreakBonus;
        public int StreakLevel;
        public int TotalPoints;

        public override string ToString()
        {
            return $"Base: {BasePoints}, Taps: +{TapBonus}, Activity: +{ActivityBonus}, " +
                   $"Streak (x{StreakLevel}): +{StreakBonus} = {TotalPoints} total";
        }
    }
}
