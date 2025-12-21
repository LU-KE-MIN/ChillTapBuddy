using System;
using UnityEngine;

namespace ChillTapBuddy.Buddy
{
    /// <summary>
    /// Samples keyboard activity at low frequency to provide small bonuses
    /// for maintaining typing activity during focus sessions.
    /// This is designed to be non-intrusive and not encourage constant typing checks.
    /// </summary>
    public class InputTracker : MonoBehaviour
    {
        [Header("Sampling Settings")]
        [Tooltip("How often to check for keyboard activity (seconds)")]
        [SerializeField] private float sampleIntervalSeconds = 10f;
        
        [Tooltip("Maximum activity bonuses per session")]
        [SerializeField] private int maxActivityBonusesPerSession = 6;
        
        [Tooltip("Points awarded per activity detection")]
        [SerializeField] private int pointsPerActivity = 2;

        public static InputTracker Instance { get; private set; }

        /// <summary>
        /// Fired when keyboard activity is detected during a sample window.
        /// </summary>
        public event Action<int> OnActivityDetected;

        /// <summary>
        /// Fired when max bonuses reached for the session.
        /// </summary>
        public event Action OnMaxBonusesReached;

        private float sampleTimer;
        private int activityBonusesThisSession;
        private bool wasKeyPressedThisWindow;
        private bool isTracking;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!isTracking)
            {
                return;
            }

            // Check for any key press during this window
            if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
            {
                wasKeyPressedThisWindow = true;
            }

            // Update sample timer
            sampleTimer += Time.deltaTime;

            if (sampleTimer >= sampleIntervalSeconds)
            {
                ProcessSampleWindow();
                sampleTimer = 0f;
            }
        }

        /// <summary>
        /// Processes the current sample window and awards bonus if activity was detected.
        /// </summary>
        private void ProcessSampleWindow()
        {
            if (wasKeyPressedThisWindow && activityBonusesThisSession < maxActivityBonusesPerSession)
            {
                activityBonusesThisSession++;
                Debug.Log($"[InputTracker] Activity detected! Bonus #{activityBonusesThisSession}/{maxActivityBonusesPerSession}");
                OnActivityDetected?.Invoke(pointsPerActivity);

                if (activityBonusesThisSession >= maxActivityBonusesPerSession)
                {
                    Debug.Log("[InputTracker] Max activity bonuses reached for this session");
                    OnMaxBonusesReached?.Invoke();
                }
            }

            wasKeyPressedThisWindow = false;
        }

        /// <summary>
        /// Starts tracking keyboard activity. Call when focus session begins.
        /// </summary>
        public void StartTracking()
        {
            isTracking = true;
            sampleTimer = 0f;
            wasKeyPressedThisWindow = false;
            Debug.Log("[InputTracker] Started tracking keyboard activity");
        }

        /// <summary>
        /// Pauses activity tracking. Call when focus session is paused.
        /// </summary>
        public void PauseTracking()
        {
            isTracking = false;
            Debug.Log("[InputTracker] Paused tracking");
        }

        /// <summary>
        /// Resumes activity tracking. Call when focus session is resumed.
        /// </summary>
        public void ResumeTracking()
        {
            isTracking = true;
            Debug.Log("[InputTracker] Resumed tracking");
        }

        /// <summary>
        /// Stops tracking and resets session counters. Call when focus session ends.
        /// </summary>
        public void StopTracking()
        {
            isTracking = false;
            wasKeyPressedThisWindow = false;
            Debug.Log($"[InputTracker] Stopped tracking. Total bonuses this session: {activityBonusesThisSession}");
        }

        /// <summary>
        /// Resets the session bonus counter. Call at the start of a new session.
        /// </summary>
        public void ResetSessionBonuses()
        {
            activityBonusesThisSession = 0;
            sampleTimer = 0f;
            wasKeyPressedThisWindow = false;
            Debug.Log("[InputTracker] Session bonuses reset");
        }

        /// <summary>
        /// Gets the number of activity bonuses earned this session.
        /// </summary>
        public int GetActivityBonusesThisSession()
        {
            return activityBonusesThisSession;
        }

        /// <summary>
        /// Gets the total potential points from keyboard bonuses this session.
        /// </summary>
        public int GetActivityPointsThisSession()
        {
            return activityBonusesThisSession * pointsPerActivity;
        }

        /// <summary>
        /// Gets the points awarded per activity detection.
        /// </summary>
        public int GetPointsPerActivity()
        {
            return pointsPerActivity;
        }

        /// <summary>
        /// Gets the maximum activity bonuses allowed per session.
        /// </summary>
        public int GetMaxBonusesPerSession()
        {
            return maxActivityBonusesPerSession;
        }

        /// <summary>
        /// Checks if more activity bonuses can be earned this session.
        /// </summary>
        public bool CanEarnMoreBonuses()
        {
            return activityBonusesThisSession < maxActivityBonusesPerSession;
        }
    }
}
