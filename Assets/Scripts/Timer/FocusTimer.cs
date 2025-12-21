using System;
using UnityEngine;

namespace ChillTapBuddy.Timer
{
    /// <summary>
    /// Manages the Pomodoro focus timer with configurable duration.
    /// Fires events on each tick and on completion.
    /// </summary>
    public class FocusTimer : MonoBehaviour
    {
        [Header("Timer Settings")]
        [Tooltip("Duration in seconds for a full focus session (default: 1500 = 25 minutes)")]
        [SerializeField] private int focusDurationSeconds = 1500;
        
        [Tooltip("Enable demo mode for quick 60-second sessions during testing")]
        [SerializeField] private bool demoMode = true;
        
        [Tooltip("Duration in seconds for demo mode")]
        [SerializeField] private int demoDurationSeconds = 60;

        public static FocusTimer Instance { get; private set; }

        /// <summary>
        /// Fired every second with the remaining time in seconds.
        /// </summary>
        public event Action<int> OnTickSeconds;

        /// <summary>
        /// Fired when a focus session completes successfully.
        /// </summary>
        public event Action OnFocusCompleted;

        /// <summary>
        /// Fired when the timer is started.
        /// </summary>
        public event Action OnTimerStarted;

        /// <summary>
        /// Fired when the timer is paused.
        /// </summary>
        public event Action OnTimerPaused;

        /// <summary>
        /// Fired when the timer is stopped (cancelled).
        /// </summary>
        public event Action OnTimerStopped;

        /// <summary>
        /// Fired when the timer is resumed from pause.
        /// </summary>
        public event Action OnTimerResumed;

        public int RemainingSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public int TotalDuration => GetDuration();

        private float elapsedSinceLastTick;
        private int sessionTapCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResetTimer();
        }

        private void Update()
        {
            if (!IsRunning || IsPaused)
            {
                return;
            }

            elapsedSinceLastTick += Time.deltaTime;

            if (elapsedSinceLastTick >= 1f)
            {
                elapsedSinceLastTick -= 1f;
                RemainingSeconds--;

                OnTickSeconds?.Invoke(RemainingSeconds);

                if (RemainingSeconds <= 0)
                {
                    CompleteSession();
                }
            }
        }

        /// <summary>
        /// Returns the appropriate duration based on demo mode setting.
        /// </summary>
        private int GetDuration()
        {
#if UNITY_EDITOR
            if (demoMode)
            {
                return demoDurationSeconds;
            }
#endif
            return focusDurationSeconds;
        }

        /// <summary>
        /// Starts or restarts the focus timer.
        /// </summary>
        public void StartTimer()
        {
            if (IsRunning && !IsPaused)
            {
                Debug.Log("[FocusTimer] Timer already running");
                return;
            }

            if (IsPaused)
            {
                ResumeTimer();
                return;
            }

            ResetTimer();
            IsRunning = true;
            IsPaused = false;
            sessionTapCount = 0;

            Debug.Log($"[FocusTimer] Started with {RemainingSeconds} seconds");
            OnTimerStarted?.Invoke();
            OnTickSeconds?.Invoke(RemainingSeconds);
        }

        /// <summary>
        /// Pauses the timer without resetting progress.
        /// </summary>
        public void PauseTimer()
        {
            if (!IsRunning || IsPaused)
            {
                Debug.Log("[FocusTimer] Cannot pause - timer not running or already paused");
                return;
            }

            IsPaused = true;
            Debug.Log($"[FocusTimer] Paused at {RemainingSeconds} seconds remaining");
            OnTimerPaused?.Invoke();
        }

        /// <summary>
        /// Resumes a paused timer.
        /// </summary>
        public void ResumeTimer()
        {
            if (!IsRunning || !IsPaused)
            {
                Debug.Log("[FocusTimer] Cannot resume - timer not paused");
                return;
            }

            IsPaused = false;
            Debug.Log($"[FocusTimer] Resumed with {RemainingSeconds} seconds remaining");
            OnTimerResumed?.Invoke();
        }

        /// <summary>
        /// Stops and resets the timer without completing.
        /// </summary>
        public void StopTimer()
        {
            if (!IsRunning)
            {
                Debug.Log("[FocusTimer] Timer not running");
                return;
            }

            Debug.Log("[FocusTimer] Timer stopped (cancelled)");
            IsRunning = false;
            IsPaused = false;
            ResetTimer();
            OnTimerStopped?.Invoke();
        }

        /// <summary>
        /// Resets the timer to its initial duration.
        /// </summary>
        private void ResetTimer()
        {
            RemainingSeconds = GetDuration();
            elapsedSinceLastTick = 0f;
        }

        /// <summary>
        /// Completes the session and fires completion event.
        /// </summary>
        private void CompleteSession()
        {
            IsRunning = false;
            IsPaused = false;
            RemainingSeconds = 0;

            Debug.Log($"[FocusTimer] Session completed! Tap count: {sessionTapCount}");
            OnFocusCompleted?.Invoke();
            
            ResetTimer();
        }

        /// <summary>
        /// Records a tap during the current session.
        /// </summary>
        public void RecordTap()
        {
            if (IsRunning && !IsPaused)
            {
                sessionTapCount++;
            }
        }

        /// <summary>
        /// Gets the number of taps recorded in the current session.
        /// </summary>
        public int GetSessionTapCount()
        {
            return sessionTapCount;
        }

        /// <summary>
        /// Formats seconds into MM:SS string.
        /// </summary>
        public static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Toggle demo mode at runtime (editor only).
        /// </summary>
        public void SetDemoMode(bool enabled)
        {
            demoMode = enabled;
            if (!IsRunning)
            {
                ResetTimer();
            }
        }

        /// <summary>
        /// Returns whether demo mode is currently enabled.
        /// </summary>
        public bool IsDemoMode()
        {
#if UNITY_EDITOR
            return demoMode;
#else
            return false;
#endif
        }
    }
}
