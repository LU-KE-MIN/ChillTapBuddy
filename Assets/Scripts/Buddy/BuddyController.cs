using System;
using UnityEngine;

namespace ChillTapBuddy.Buddy
{
    /// <summary>
    /// Handles buddy click detection, animation triggers, and tap throttling.
    /// Requires a Collider2D component on the same GameObject for click detection.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BuddyController : MonoBehaviour
    {
        [Header("Tap Settings")]
        [Tooltip("Minimum seconds between tap bonuses to prevent spam")]
        [SerializeField] private float tapCooldownSeconds = 60f;
        
        [Tooltip("Maximum taps that can earn bonus per session")]
        [SerializeField] private int maxBonusTapsPerSession = 5;

        [Header("Animation")]
        [Tooltip("Reference to the Animator component")]
        [SerializeField] private Animator animator;
        
        [Header("Visual Feedback")]
        [Tooltip("Scale multiplier when tapped")]
        [SerializeField] private float tapScaleBump = 1.1f;
        
        [Tooltip("Duration of scale bump animation")]
        [SerializeField] private float tapScaleDuration = 0.15f;

        public static BuddyController Instance { get; private set; }

        /// <summary>
        /// Fired when the buddy is tapped. Bool indicates if it's a bonus-eligible tap.
        /// </summary>
        public event Action<bool> OnBuddyTapped;

        /// <summary>
        /// Fired when a tap is attempted but on cooldown.
        /// </summary>
        public event Action<float> OnTapCooldown;

        private float lastBonusTapTime = float.MinValue;
        private int bonusTapsThisSession;
        private Vector3 originalScale;
        private bool isScaling;
        private float scaleTimer;

        // Animator parameter hashes for performance
        private static readonly int TapTrigger = Animator.StringToHash("Tap");
        private static readonly int IsFocusingBool = Animator.StringToHash("IsFocusing");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            originalScale = transform.localScale;
        }

        private void Update()
        {
            HandleClickInput();
            UpdateScaleAnimation();
        }

        /// <summary>
        /// Handles click detection using legacy Input API.
        /// </summary>
        private void HandleClickInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);

                if (hitCollider != null && hitCollider.gameObject == gameObject)
                {
                    HandleTap();
                }
            }
        }

        /// <summary>
        /// Alternative click detection via OnMouseDown (Unity's built-in).
        /// Uncomment if you prefer this approach over Update-based detection.
        /// </summary>
        // private void OnMouseDown()
        // {
        //     HandleTap();
        // }

        /// <summary>
        /// Processes a tap on the buddy.
        /// </summary>
        private void HandleTap()
        {
            TriggerTapAnimation();
            StartScaleBump();

            float timeSinceLastBonus = Time.time - lastBonusTapTime;
            bool isBonusEligible = timeSinceLastBonus >= tapCooldownSeconds 
                                   && bonusTapsThisSession < maxBonusTapsPerSession;

            if (isBonusEligible)
            {
                lastBonusTapTime = Time.time;
                bonusTapsThisSession++;
                Debug.Log($"[BuddyController] Bonus tap #{bonusTapsThisSession}/{maxBonusTapsPerSession}");
                OnBuddyTapped?.Invoke(true);
            }
            else
            {
                float remainingCooldown = tapCooldownSeconds - timeSinceLastBonus;
                if (remainingCooldown > 0)
                {
                    OnTapCooldown?.Invoke(remainingCooldown);
                }
                OnBuddyTapped?.Invoke(false);
            }
        }

        /// <summary>
        /// Triggers the tap animation on the Animator.
        /// </summary>
        private void TriggerTapAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(TapTrigger);
            }
        }

        /// <summary>
        /// Sets the focusing state for animation purposes.
        /// </summary>
        public void SetFocusingState(bool isFocusing)
        {
            if (animator != null)
            {
                animator.SetBool(IsFocusingBool, isFocusing);
            }
        }

        /// <summary>
        /// Starts the scale bump visual feedback.
        /// </summary>
        private void StartScaleBump()
        {
            isScaling = true;
            scaleTimer = 0f;
            transform.localScale = originalScale * tapScaleBump;
        }

        /// <summary>
        /// Updates the scale animation over time.
        /// </summary>
        private void UpdateScaleAnimation()
        {
            if (!isScaling) return;

            scaleTimer += Time.deltaTime;
            float t = scaleTimer / tapScaleDuration;

            if (t >= 1f)
            {
                transform.localScale = originalScale;
                isScaling = false;
            }
            else
            {
                float scale = Mathf.Lerp(tapScaleBump, 1f, t);
                transform.localScale = originalScale * scale;
            }
        }

        /// <summary>
        /// Resets the session tap counter. Call when a new focus session starts.
        /// </summary>
        public void ResetSessionTaps()
        {
            bonusTapsThisSession = 0;
            Debug.Log("[BuddyController] Session tap counter reset");
        }

        /// <summary>
        /// Gets the number of bonus taps recorded this session.
        /// </summary>
        public int GetBonusTapsThisSession()
        {
            return bonusTapsThisSession;
        }

        /// <summary>
        /// Gets the remaining cooldown time for the next bonus tap.
        /// </summary>
        public float GetRemainingCooldown()
        {
            float elapsed = Time.time - lastBonusTapTime;
            return Mathf.Max(0f, tapCooldownSeconds - elapsed);
        }

        /// <summary>
        /// Checks if a bonus tap is currently available.
        /// </summary>
        public bool CanBonusTap()
        {
            return GetRemainingCooldown() <= 0f && bonusTapsThisSession < maxBonusTapsPerSession;
        }

        /// <summary>
        /// Gets the maximum bonus taps allowed per session.
        /// </summary>
        public int GetMaxBonusTapsPerSession()
        {
            return maxBonusTapsPerSession;
        }
    }
}
