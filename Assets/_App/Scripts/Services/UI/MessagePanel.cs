using UnityEngine;
using TMPro;
using Core.Utilities;
using System.Collections;
using DG.Tweening;

namespace Services.UI
{
    /// <summary>
    /// Singleton message panel để quản lý các message trong game
    /// Hiển thị thông báo, lỗi, và các message khác
    /// </summary>
    public class MessagePanel : Singleton<MessagePanel>
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private GameObject messagePanel;

        [Header("Settings")]
        [SerializeField] private float defaultMessageDuration = 3f;
        [SerializeField] private Color normalMessageColor = Color.white;
        [SerializeField] private Color errorMessageColor = Color.red;
        [SerializeField] private Color successMessageColor = Color.green;
        [SerializeField] private Color warningMessageColor = Color.yellow;
        
        [Header("Animation Settings")]
        [Tooltip("Thời gian animation khi show/hide message panel (giây)")]
        [SerializeField] private float animationDuration = 0.25f;
        [Tooltip("Loại animation cho message panel")]
        [SerializeField] private UIAnimationHelper.AnimationType animationType = UIAnimationHelper.AnimationType.Fade;

        private Coroutine autoClearCoroutine;
        private Tween currentShowTween;
        private Tween currentHideTween;

        protected override void Awake()
        {
            base.Awake();
            
            // Keep MessagePanel alive across scenes
            DontDestroyOnLoad(gameObject);
            
            // Initialize message text
            if (messageText != null)
            {
                messageText.text = "";
            }
        }
        
        private void Start()
        {
            // Đảm bảo panel luôn inactive khi bắt đầu play (sau khi tất cả scripts đã khởi tạo)
            EnsurePanelInactive();
        }
        
        private void OnEnable()
        {
            // Đảm bảo panel inactive mỗi khi object được enable (tránh trường hợp panel active từ scene trước)
            EnsurePanelInactive();
        }
        
        /// <summary>
        /// Đảm bảo panel luôn inactive và alpha = 0
        /// </summary>
        private void EnsurePanelInactive()
        {
            if (messagePanel != null && messagePanel.activeSelf)
            {
                messagePanel.SetActive(false);
                
                // Đảm bảo alpha = 0 nếu có CanvasGroup để animation hoạt động đúng
                CanvasGroup canvasGroup = messagePanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
            else if (messagePanel != null && !messagePanel.activeSelf)
            {
                // Ngay cả khi inactive, vẫn đảm bảo alpha = 0
                CanvasGroup canvasGroup = messagePanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }

        /// <summary>
        /// Hiển thị message với thời gian tự động xóa
        /// </summary>
        /// <param name="message">Nội dung message</param>
        /// <param name="isError">True nếu là lỗi</param>
        /// <param name="duration">Thời gian hiển thị (0 = dùng default duration, -1 = không tự động xóa)</param>
        public void ShowMessage(string message, bool isError = false, float duration = 0f)
        {
            Debug.Log($"[MessagePanel] ShowMessage: {message}, isError: {isError}, duration: {duration}");
            if (messageText == null)
            {
                Debug.LogWarning("[MessagePanel] MessageText component chưa được cấu hình!");
                return;
            }

            // Stop previous auto-clear coroutine if exists
            if (autoClearCoroutine != null)
            {
                StopCoroutine(autoClearCoroutine);
                autoClearCoroutine = null;
            }

            // Kill any existing animations
            if (currentShowTween != null && currentShowTween.IsActive())
            {
                currentShowTween.Kill();
            }
            if (currentHideTween != null && currentHideTween.IsActive())
            {
                currentHideTween.Kill();
            }
            if (messagePanel != null)
            {
                UIAnimationHelper.KillTweens(messagePanel);
            }

            // Set message
            messageText.text = message;
            messageText.color = isError ? errorMessageColor : normalMessageColor;

            // Show panel với animation mượt mà
            if (messagePanel != null)
            {
                currentShowTween = UIAnimationHelper.ShowPanel(messagePanel, animationType, animationDuration);
            }

            // Auto-clear message
            float messageDuration = duration > 0 ? duration : (duration == -1 ? -1 : defaultMessageDuration);
            if (messageDuration > 0)
            {
                autoClearCoroutine = StartCoroutine(AutoClearMessage(messageDuration));
            }

            Debug.Log($"[MessagePanel] {message}");
        }

        /// <summary>
        /// Hiển thị success message
        /// </summary>
        public void ShowSuccess(string message, float duration = 0f)
        {
            if (messageText == null)
            {
                Debug.LogWarning("[MessagePanel] MessageText component chưa được cấu hình!");
                return;
            }

            if (autoClearCoroutine != null)
            {
                StopCoroutine(autoClearCoroutine);
                autoClearCoroutine = null;
            }

            // Kill any existing animations
            if (currentShowTween != null && currentShowTween.IsActive())
            {
                currentShowTween.Kill();
            }
            if (currentHideTween != null && currentHideTween.IsActive())
            {
                currentHideTween.Kill();
            }
            if (messagePanel != null)
            {
                UIAnimationHelper.KillTweens(messagePanel);
            }

            messageText.text = message;
            messageText.color = successMessageColor;

            if (messagePanel != null)
            {
                currentShowTween = UIAnimationHelper.ShowPanel(messagePanel, animationType, animationDuration);
            }

            float messageDuration = duration > 0 ? duration : (duration == -1 ? -1 : defaultMessageDuration);
            if (messageDuration > 0)
            {
                autoClearCoroutine = StartCoroutine(AutoClearMessage(messageDuration));
            }

            Debug.Log($"[MessagePanel] Success: {message}");
        }

        /// <summary>
        /// Hiển thị warning message
        /// </summary>
        public void ShowWarning(string message, float duration = 0f)
        {
            if (messageText == null)
            {
                Debug.LogWarning("[MessagePanel] MessageText component chưa được cấu hình!");
                return;
            }

            if (autoClearCoroutine != null)
            {
                StopCoroutine(autoClearCoroutine);
                autoClearCoroutine = null;
            }

            // Kill any existing animations
            if (currentShowTween != null && currentShowTween.IsActive())
            {
                currentShowTween.Kill();
            }
            if (currentHideTween != null && currentHideTween.IsActive())
            {
                currentHideTween.Kill();
            }
            if (messagePanel != null)
            {
                UIAnimationHelper.KillTweens(messagePanel);
            }

            messageText.text = message;
            messageText.color = warningMessageColor;

            if (messagePanel != null)
            {
                currentShowTween = UIAnimationHelper.ShowPanel(messagePanel, animationType, animationDuration);
            }

            float messageDuration = duration > 0 ? duration : (duration == -1 ? -1 : defaultMessageDuration);
            if (messageDuration > 0)
            {
                autoClearCoroutine = StartCoroutine(AutoClearMessage(messageDuration));
            }

            Debug.Log($"[MessagePanel] Warning: {message}");
        }

        /// <summary>
        /// Xóa message hiện tại và ẩn panel
        /// </summary>
        public void ClearMessage()
        {
            if (autoClearCoroutine != null)
            {
                StopCoroutine(autoClearCoroutine);
                autoClearCoroutine = null;
            }

            if (messageText != null)
            {
                messageText.text = "";
            }
            
            // Ẩn panel khi clear message
            if (messagePanel != null && messagePanel.activeSelf)
            {
                HidePanel();
            }
        }

        /// <summary>
        /// Ẩn message panel với animation mượt mà
        /// </summary>
        public void HidePanel()
        {
            // Kill any existing animations
            if (currentShowTween != null && currentShowTween.IsActive())
            {
                currentShowTween.Kill();
            }
            if (currentHideTween != null && currentHideTween.IsActive())
            {
                currentHideTween.Kill();
            }
            
            if (messagePanel != null)
            {
                currentHideTween = UIAnimationHelper.HidePanel(messagePanel, animationType, animationDuration, () =>
                {
                    ClearMessage();
                });
            }
            else
            {
                ClearMessage();
            }
        }

        /// <summary>
        /// Hiển thị message panel với animation mượt mà
        /// </summary>
        public void ShowPanel()
        {
            // Kill any existing hide animations
            if (currentHideTween != null && currentHideTween.IsActive())
            {
                currentHideTween.Kill();
            }
            
            if (messagePanel != null)
            {
                currentShowTween = UIAnimationHelper.ShowPanel(messagePanel, animationType, animationDuration);
            }
        }

        /// <summary>
        /// Coroutine để tự động xóa message sau một khoảng thời gian
        /// </summary>
        private IEnumerator AutoClearMessage(float duration)
        {
            yield return new WaitForSeconds(duration);
            ClearMessage();
        }

        /// <summary>
        /// Kiểm tra xem MessagePanel có sẵn sàng không
        /// </summary>
        public static bool IsReady()
        {
            return instanceExists && instance != null && instance.messageText != null;
        }

        protected override void OnDestroy()
        {
            // Kill all tweens khi destroy
            if (currentShowTween != null && currentShowTween.IsActive())
            {
                currentShowTween.Kill();
            }
            if (currentHideTween != null && currentHideTween.IsActive())
            {
                currentHideTween.Kill();
            }
            if (messagePanel != null)
            {
                UIAnimationHelper.KillTweens(messagePanel);
            }
            
            base.OnDestroy();
        }
    }
}

