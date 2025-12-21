using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChillTapBuddy.Core;
using ChillTapBuddy.Timer;

namespace ChillTapBuddy.UI
{
    /// <summary>
    /// Manages all UI updates including timer display, points, streak,
    /// unlock list, and toast notifications.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("Timer UI")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text timerStatusText;
        [SerializeField] private Image timerProgressFill;

        [Header("Control Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private TMP_Text startButtonText;

        [Header("Stats UI")]
        [SerializeField] private TMP_Text pointsText;
        [SerializeField] private TMP_Text streakText;
        [SerializeField] private TMP_Text sessionTapsText;

        [Header("Unlocks UI")]
        [SerializeField] private Transform unlockListContainer;
        [SerializeField] private GameObject unlockItemPrefab;

        [Header("Toast")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private float toastDuration = 3f;

        [Header("Reward Popup")]
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private TMP_Text rewardTotalText;
        [SerializeField] private TMP_Text rewardBreakdownText;
        [SerializeField] private Button rewardCloseButton;

        public static UIController Instance { get; private set; }

        private Coroutine toastCoroutine;
        private List<UnlockItemUI> unlockItemUIs = new List<UnlockItemUI>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeUI();
            SetupButtonListeners();
            RefreshUnlockList();
        }

        /// <summary>
        /// Initializes UI to default state.
        /// </summary>
        private void InitializeUI()
        {
            if (toastPanel != null)
            {
                toastPanel.SetActive(false);
            }

            if (rewardPopup != null)
            {
                rewardPopup.SetActive(false);
            }

            UpdateTimerDisplay(FocusTimer.Instance?.TotalDuration ?? 1500);
            UpdateButtonStates(false, false);
            RefreshStatsDisplay();
        }

        /// <summary>
        /// Sets up button click listeners.
        /// </summary>
        private void SetupButtonListeners()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }

            if (stopButton != null)
            {
                stopButton.onClick.AddListener(OnStopButtonClicked);
            }

            if (rewardCloseButton != null)
            {
                rewardCloseButton.onClick.AddListener(CloseRewardPopup);
            }
        }

        /// <summary>
        /// Updates the timer display with remaining time.
        /// </summary>
        public void UpdateTimerDisplay(int remainingSeconds)
        {
            if (timerText != null)
            {
                timerText.text = FocusTimer.FormatTime(remainingSeconds);
            }

            if (timerProgressFill != null && FocusTimer.Instance != null)
            {
                float progress = (float)remainingSeconds / FocusTimer.Instance.TotalDuration;
                timerProgressFill.fillAmount = progress;
            }
        }

        /// <summary>
        /// Updates the timer status text.
        /// </summary>
        public void UpdateTimerStatus(string status)
        {
            if (timerStatusText != null)
            {
                timerStatusText.text = status;
            }
        }

        /// <summary>
        /// Updates button states based on timer state.
        /// </summary>
        public void UpdateButtonStates(bool isRunning, bool isPaused)
        {
            if (startButton != null)
            {
                startButton.interactable = !isRunning || isPaused;
            }

            if (startButtonText != null)
            {
                startButtonText.text = isPaused ? "Resume" : "Start Focus";
            }

            if (pauseButton != null)
            {
                pauseButton.interactable = isRunning && !isPaused;
            }

            if (stopButton != null)
            {
                stopButton.interactable = isRunning;
            }
        }

        /// <summary>
        /// Refreshes all stats displays from save data.
        /// </summary>
        public void RefreshStatsDisplay()
        {
            SaveData data = SaveService.Instance?.CurrentData;
            if (data == null) return;

            if (pointsText != null)
            {
                pointsText.text = $"{data.totalPoints:N0}";
            }

            if (streakText != null)
            {
                int bonusPercent = RewardSystem.Instance?.GetCurrentStreakBonusPercent() ?? 0;
                streakText.text = data.streak > 0 
                    ? $"{data.streak} day streak (+{bonusPercent}%)" 
                    : "No streak";
            }
        }

        /// <summary>
        /// Updates the session tap counter display.
        /// </summary>
        public void UpdateSessionTaps(int tapCount, int maxTaps)
        {
            if (sessionTapsText != null)
            {
                sessionTapsText.text = $"Taps: {tapCount}/{maxTaps}";
            }
        }

        /// <summary>
        /// Refreshes the unlock list UI.
        /// </summary>
        public void RefreshUnlockList()
        {
            if (unlockListContainer == null || unlockItemPrefab == null)
            {
                return;
            }

            // Clear existing items
            foreach (Transform child in unlockListContainer)
            {
                Destroy(child.gameObject);
            }
            unlockItemUIs.Clear();

            // Create items for all unlocks
            List<UnlockDefinition> allUnlocks = RewardSystem.Instance?.GetAllUnlocks();
            if (allUnlocks == null) return;

            foreach (var def in allUnlocks)
            {
                GameObject itemObj = Instantiate(unlockItemPrefab, unlockListContainer);
                UnlockItemUI itemUI = itemObj.GetComponent<UnlockItemUI>();
                
                if (itemUI != null)
                {
                    bool isUnlocked = SaveService.Instance.IsUnlocked(def.id);
                    itemUI.Setup(def, isUnlocked);
                    unlockItemUIs.Add(itemUI);
                }
            }
        }

        /// <summary>
        /// Shows a toast notification message.
        /// </summary>
        public void ShowToast(string message)
        {
            if (toastPanel == null || toastText == null) return;

            if (toastCoroutine != null)
            {
                StopCoroutine(toastCoroutine);
            }

            toastText.text = message;
            toastCoroutine = StartCoroutine(ShowToastCoroutine());
        }

        private IEnumerator ShowToastCoroutine()
        {
            toastPanel.SetActive(true);
            yield return new WaitForSeconds(toastDuration);
            toastPanel.SetActive(false);
            toastCoroutine = null;
        }

        /// <summary>
        /// Shows the reward popup with session results.
        /// </summary>
        public void ShowRewardPopup(RewardBreakdown breakdown)
        {
            if (rewardPopup == null) return;

            if (rewardTotalText != null)
            {
                rewardTotalText.text = $"+{breakdown.TotalPoints}";
            }

            if (rewardBreakdownText != null)
            {
                string breakdownStr = $"Base: +{breakdown.BasePoints}\n";
                
                if (breakdown.TapBonus > 0)
                {
                    breakdownStr += $"Tap Bonus: +{breakdown.TapBonus}\n";
                }
                
                if (breakdown.ActivityBonus > 0)
                {
                    breakdownStr += $"Activity: +{breakdown.ActivityBonus}\n";
                }
                
                if (breakdown.StreakBonus > 0)
                {
                    breakdownStr += $"Streak (x{breakdown.StreakLevel}): +{breakdown.StreakBonus}";
                }

                rewardBreakdownText.text = breakdownStr;
            }

            rewardPopup.SetActive(true);
        }

        /// <summary>
        /// Closes the reward popup.
        /// </summary>
        public void CloseRewardPopup()
        {
            if (rewardPopup != null)
            {
                rewardPopup.SetActive(false);
            }
        }

        // Button callbacks - delegated to GameManager
        private void OnStartButtonClicked()
        {
            GameManager.Instance?.OnStartFocusClicked();
        }

        private void OnPauseButtonClicked()
        {
            GameManager.Instance?.OnPauseFocusClicked();
        }

        private void OnStopButtonClicked()
        {
            GameManager.Instance?.OnStopFocusClicked();
        }
    }

    /// <summary>
    /// UI component for individual unlock list items.
    /// Attach to the unlock item prefab.
    /// </summary>
    public class UnlockItemUI : MonoBehaviour
    {
        [SerializeField] private Image previewImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button unlockButton;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject unlockedCheckmark;

        private UnlockDefinition definition;
        private bool isUnlocked;

        /// <summary>
        /// Sets up the unlock item UI with definition data.
        /// </summary>
        public void Setup(UnlockDefinition def, bool unlocked)
        {
            definition = def;
            isUnlocked = unlocked;

            if (previewImage != null && def.previewSprite != null)
            {
                previewImage.sprite = def.previewSprite;
            }

            if (nameText != null)
            {
                nameText.text = def.displayName;
            }

            if (costText != null)
            {
                costText.text = unlocked ? "Owned" : $"{def.costPoints} pts";
            }

            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!unlocked);
            }

            if (unlockedCheckmark != null)
            {
                unlockedCheckmark.SetActive(unlocked);
            }

            if (unlockButton != null)
            {
                unlockButton.interactable = !unlocked;
                unlockButton.onClick.RemoveAllListeners();
                unlockButton.onClick.AddListener(OnUnlockClicked);
            }
        }

        private void OnUnlockClicked()
        {
            if (definition != null && !isUnlocked)
            {
                bool success = RewardSystem.Instance.TryUnlockItem(definition);
                if (success)
                {
                    Setup(definition, true);
                    UIController.Instance?.RefreshStatsDisplay();
                }
            }
        }
    }
}
