using UnityEngine;
using ChillTapBuddy.Core;
using ChillTapBuddy.Timer;
using ChillTapBuddy.Buddy;
using ChillTapBuddy.UI;

namespace ChillTapBuddy
{
    /// <summary>
    /// Main game orchestration class. Initializes all systems, handles UI callbacks,
    /// subscribes to timer and buddy events, and coordinates reward calculation.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FocusTimer focusTimer;
        [SerializeField] private BuddyController buddyController;
        [SerializeField] private InputTracker inputTracker;
        [SerializeField] private SaveService saveService;
        [SerializeField] private RewardSystem rewardSystem;
        [SerializeField] private UIController uiController;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tapSound;
        [SerializeField] private AudioClip sessionCompleteSound;
        [SerializeField] private AudioClip unlockSound;

        public static GameManager Instance { get; private set; }

        private int accumulatedActivityPoints;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            FindReferences();
        }

        private void Start()
        {
            InitializeSystems();
            SubscribeToEvents();
            ValidateStreak();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Finds references if not assigned in inspector.
        /// </summary>
        private void FindReferences()
        {
            if (focusTimer == null)
                focusTimer = FindObjectOfType<FocusTimer>();
            
            if (buddyController == null)
                buddyController = FindObjectOfType<BuddyController>();
            
            if (inputTracker == null)
                inputTracker = FindObjectOfType<InputTracker>();
            
            if (saveService == null)
                saveService = FindObjectOfType<SaveService>();
            
            if (rewardSystem == null)
                rewardSystem = FindObjectOfType<RewardSystem>();
            
            if (uiController == null)
                uiController = FindObjectOfType<UIController>();
        }

        /// <summary>
        /// Initializes all game systems.
        /// </summary>
        private void InitializeSystems()
        {
            Debug.Log("[GameManager] Initializing systems...");
            
            // Save service loads data automatically in Awake
            // UI controller initializes itself in Start
            
            Debug.Log("[GameManager] Systems initialized");
        }

        /// <summary>
        /// Subscribes to all relevant events from subsystems.
        /// </summary>
        private void SubscribeToEvents()
        {
            // Timer events
            if (focusTimer != null)
            {
                focusTimer.OnTickSeconds += HandleTimerTick;
                focusTimer.OnFocusCompleted += HandleFocusCompleted;
                focusTimer.OnTimerStarted += HandleTimerStarted;
                focusTimer.OnTimerPaused += HandleTimerPaused;
                focusTimer.OnTimerResumed += HandleTimerResumed;
                focusTimer.OnTimerStopped += HandleTimerStopped;
            }

            // Buddy events
            if (buddyController != null)
            {
                buddyController.OnBuddyTapped += HandleBuddyTapped;
                buddyController.OnTapCooldown += HandleTapCooldown;
            }

            // Input tracker events
            if (inputTracker != null)
            {
                inputTracker.OnActivityDetected += HandleActivityDetected;
            }

            // Reward system events
            if (rewardSystem != null)
            {
                rewardSystem.OnRewardCalculated += HandleRewardCalculated;
                rewardSystem.OnItemUnlocked += HandleItemUnlocked;
                rewardSystem.OnUnlockFailed += HandleUnlockFailed;
            }

            // Save service events
            if (saveService != null)
            {
                saveService.OnDataSaved += HandleDataSaved;
            }
        }

        /// <summary>
        /// Unsubscribes from all events to prevent memory leaks.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (focusTimer != null)
            {
                focusTimer.OnTickSeconds -= HandleTimerTick;
                focusTimer.OnFocusCompleted -= HandleFocusCompleted;
                focusTimer.OnTimerStarted -= HandleTimerStarted;
                focusTimer.OnTimerPaused -= HandleTimerPaused;
                focusTimer.OnTimerResumed -= HandleTimerResumed;
                focusTimer.OnTimerStopped -= HandleTimerStopped;
            }

            if (buddyController != null)
            {
                buddyController.OnBuddyTapped -= HandleBuddyTapped;
                buddyController.OnTapCooldown -= HandleTapCooldown;
            }

            if (inputTracker != null)
            {
                inputTracker.OnActivityDetected -= HandleActivityDetected;
            }

            if (rewardSystem != null)
            {
                rewardSystem.OnRewardCalculated -= HandleRewardCalculated;
                rewardSystem.OnItemUnlocked -= HandleItemUnlocked;
                rewardSystem.OnUnlockFailed -= HandleUnlockFailed;
            }

            if (saveService != null)
            {
                saveService.OnDataSaved -= HandleDataSaved;
            }
        }

        /// <summary>
        /// Validates and potentially resets streak on app start.
        /// </summary>
        private void ValidateStreak()
        {
            saveService?.ValidateStreak();
            uiController?.RefreshStatsDisplay();
        }

        #region UI Button Callbacks

        /// <summary>
        /// Called when the Start Focus button is clicked.
        /// </summary>
        public void OnStartFocusClicked()
        {
            if (focusTimer == null) return;

            if (focusTimer.IsPaused)
            {
                focusTimer.ResumeTimer();
            }
            else
            {
                focusTimer.StartTimer();
            }
        }

        /// <summary>
        /// Called when the Pause button is clicked.
        /// </summary>
        public void OnPauseFocusClicked()
        {
            focusTimer?.PauseTimer();
        }

        /// <summary>
        /// Called when the Stop button is clicked.
        /// </summary>
        public void OnStopFocusClicked()
        {
            focusTimer?.StopTimer();
        }

        #endregion

        #region Timer Event Handlers

        private void HandleTimerTick(int remainingSeconds)
        {
            uiController?.UpdateTimerDisplay(remainingSeconds);
        }

        private void HandleTimerStarted()
        {
            Debug.Log("[GameManager] Focus session started");
            
            // Reset session counters
            buddyController?.ResetSessionTaps();
            inputTracker?.ResetSessionBonuses();
            accumulatedActivityPoints = 0;
            
            // Start tracking
            inputTracker?.StartTracking();
            
            // Update buddy animation state
            buddyController?.SetFocusingState(true);
            
            // Update UI
            uiController?.UpdateTimerStatus("Focusing...");
            uiController?.UpdateButtonStates(true, false);
            uiController?.UpdateSessionTaps(0, buddyController?.GetMaxBonusTapsPerSession() ?? 5);
        }

        private void HandleTimerPaused()
        {
            Debug.Log("[GameManager] Focus session paused");
            
            inputTracker?.PauseTracking();
            buddyController?.SetFocusingState(false);
            
            uiController?.UpdateTimerStatus("Paused");
            uiController?.UpdateButtonStates(true, true);
        }

        private void HandleTimerResumed()
        {
            Debug.Log("[GameManager] Focus session resumed");
            
            inputTracker?.ResumeTracking();
            buddyController?.SetFocusingState(true);
            
            uiController?.UpdateTimerStatus("Focusing...");
            uiController?.UpdateButtonStates(true, false);
        }

        private void HandleTimerStopped()
        {
            Debug.Log("[GameManager] Focus session stopped (cancelled)");
            
            inputTracker?.StopTracking();
            buddyController?.SetFocusingState(false);
            
            uiController?.UpdateTimerStatus("Ready");
            uiController?.UpdateButtonStates(false, false);
            uiController?.UpdateTimerDisplay(focusTimer.TotalDuration);
            uiController?.ShowToast("Session cancelled");
        }

        private void HandleFocusCompleted()
        {
            Debug.Log("[GameManager] Focus session completed!");
            
            inputTracker?.StopTracking();
            buddyController?.SetFocusingState(false);
            
            // Calculate rewards
            int tapBonuses = buddyController?.GetBonusTapsThisSession() ?? 0;
            rewardSystem?.CalculateSessionReward(tapBonuses, accumulatedActivityPoints);
            
            // Play completion sound
            PlaySound(sessionCompleteSound);
            
            // Update UI
            uiController?.UpdateTimerStatus("Complete!");
            uiController?.UpdateButtonStates(false, false);
            uiController?.UpdateTimerDisplay(focusTimer.TotalDuration);
        }

        #endregion

        #region Buddy Event Handlers

        private void HandleBuddyTapped(bool isBonusEligible)
        {
            PlaySound(tapSound);
            
            if (isBonusEligible)
            {
                focusTimer?.RecordTap();
                
                int currentTaps = buddyController?.GetBonusTapsThisSession() ?? 0;
                int maxTaps = buddyController?.GetMaxBonusTapsPerSession() ?? 5;
                uiController?.UpdateSessionTaps(currentTaps, maxTaps);
                
                if (focusTimer != null && focusTimer.IsRunning)
                {
                    uiController?.ShowToast("+5 tap bonus!");
                }
            }
        }

        private void HandleTapCooldown(float remainingCooldown)
        {
            int seconds = Mathf.CeilToInt(remainingCooldown);
            uiController?.ShowToast($"Tap cooldown: {seconds}s");
        }

        #endregion

        #region Input Tracker Event Handlers

        private void HandleActivityDetected(int points)
        {
            accumulatedActivityPoints += points;
            Debug.Log($"[GameManager] Activity bonus: +{points}, total accumulated: {accumulatedActivityPoints}");
        }

        #endregion

        #region Reward System Event Handlers

        private void HandleRewardCalculated(RewardBreakdown breakdown)
        {
            Debug.Log($"[GameManager] Reward calculated: {breakdown}");
            
            uiController?.RefreshStatsDisplay();
            uiController?.ShowRewardPopup(breakdown);
            uiController?.RefreshUnlockList();
        }

        private void HandleItemUnlocked(UnlockDefinition def)
        {
            Debug.Log($"[GameManager] Item unlocked: {def.displayName}");
            
            PlaySound(unlockSound);
            uiController?.ShowToast($"Unlocked: {def.displayName}!");
            uiController?.RefreshUnlockList();
            uiController?.RefreshStatsDisplay();
        }

        private void HandleUnlockFailed(UnlockDefinition def, int pointsNeeded)
        {
            uiController?.ShowToast($"Need {pointsNeeded} more points for {def.displayName}");
        }

        #endregion

        #region Save Event Handlers

        private void HandleDataSaved()
        {
            // Optional: visual feedback that data was saved
        }

        #endregion

        #region Audio Helpers

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Debug Commands

        /// <summary>
        /// Adds debug points for testing. Call from Unity console or debug UI.
        /// </summary>
        [ContextMenu("Debug: Add 100 Points")]
        public void DebugAddPoints()
        {
            saveService?.AddPoints(100);
            uiController?.RefreshStatsDisplay();
            uiController?.ShowToast("Debug: +100 points");
        }

        /// <summary>
        /// Clears all save data for testing.
        /// </summary>
        [ContextMenu("Debug: Clear Save Data")]
        public void DebugClearSave()
        {
            saveService?.ClearSave();
            uiController?.RefreshStatsDisplay();
            uiController?.RefreshUnlockList();
            uiController?.ShowToast("Debug: Save data cleared");
        }

        /// <summary>
        /// Forces session completion for testing.
        /// </summary>
        [ContextMenu("Debug: Complete Session Now")]
        public void DebugCompleteSession()
        {
            if (focusTimer != null && focusTimer.IsRunning)
            {
                // This is a hack for testing - normally you'd wait for the timer
                HandleFocusCompleted();
            }
            else
            {
                Debug.Log("[GameManager] No active session to complete");
            }
        }

        #endregion
    }
}
