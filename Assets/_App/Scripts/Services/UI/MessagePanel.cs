using UnityEngine;
using TMPro;
using Core.Utilities;
using System.Collections;

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

        private Coroutine autoClearCoroutine;

        protected override void Awake()
        {
            base.Awake();
            
            // Keep MessagePanel alive across scenes
            DontDestroyOnLoad(gameObject);
            
            // Ensure panel is visible
            if (messagePanel != null)
            {
                messagePanel.SetActive(true);
            }
            
            // Initialize message text
            if (messageText != null)
            {
                messageText.text = "";
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

            // Set message
            messageText.text = message;
            messageText.color = isError ? errorMessageColor : normalMessageColor;

            // Show panel
            if (messagePanel != null)
            {
                messagePanel.SetActive(true);
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

            messageText.text = message;
            messageText.color = successMessageColor;

            if (messagePanel != null)
            {
                messagePanel.SetActive(true);
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

            messageText.text = message;
            messageText.color = warningMessageColor;

            if (messagePanel != null)
            {
                messagePanel.SetActive(true);
            }

            float messageDuration = duration > 0 ? duration : (duration == -1 ? -1 : defaultMessageDuration);
            if (messageDuration > 0)
            {
                autoClearCoroutine = StartCoroutine(AutoClearMessage(messageDuration));
            }

            Debug.Log($"[MessagePanel] Warning: {message}");
        }

        /// <summary>
        /// Xóa message hiện tại
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
        }

        /// <summary>
        /// Ẩn message panel
        /// </summary>
        public void HidePanel()
        {
            if (messagePanel != null)
            {
                messagePanel.SetActive(false);
            }
            ClearMessage();
        }

        /// <summary>
        /// Hiển thị message panel
        /// </summary>
        public void ShowPanel()
        {
            if (messagePanel != null)
            {
                messagePanel.SetActive(true);
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
    }
}

