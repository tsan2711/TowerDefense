using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

namespace Services.UI
{
    /// <summary>
    /// Utility class để quản lý các animation UI với DOTween
    /// Cung cấp các method tiện lợi để làm mượt các UI transitions
    /// </summary>
    public static class UIAnimationHelper
    {
        /// <summary>
        /// Animation duration mặc định (giây)
        /// </summary>
        public const float DEFAULT_DURATION = 0.35f;

        /// <summary>
        /// Ease type mặc định cho fade animations - mượt mà và tự nhiên
        /// </summary>
        public const Ease DEFAULT_FADE_EASE = Ease.OutQuart;
        
        /// <summary>
        /// Ease type mặc định cho scale animations - có bounce nhẹ
        /// </summary>
        public const Ease DEFAULT_SCALE_EASE = Ease.OutBack;
        
        /// <summary>
        /// Ease type mặc định cho slide animations - mượt với spring effect
        /// </summary>
        public const Ease DEFAULT_SLIDE_EASE = Ease.OutCubic;

        /// <summary>
        /// Fade in một panel/GameObject với CanvasGroup - mượt mà và đẹp
        /// </summary>
        public static Tween FadeIn(CanvasGroup canvasGroup, float duration = DEFAULT_DURATION, Ease ease = DEFAULT_FADE_EASE, System.Action onComplete = null)
        {
            if (canvasGroup == null) return null;

            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Tween tween = canvasGroup.DOFade(1f, duration)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    onComplete?.Invoke();
                });

            return tween;
        }

        /// <summary>
        /// Fade out một panel/GameObject với CanvasGroup - mượt mà và đẹp
        /// </summary>
        public static Tween FadeOut(CanvasGroup canvasGroup, float duration = DEFAULT_DURATION, Ease ease = DEFAULT_FADE_EASE, System.Action onComplete = null)
        {
            if (canvasGroup == null) return null;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Tween tween = canvasGroup.DOFade(0f, duration)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    canvasGroup.gameObject.SetActive(false);
                    onComplete?.Invoke();
                });

            return tween;
        }

        /// <summary>
        /// Slide in một panel từ một hướng với animation mượt mà và đẹp
        /// </summary>
        public static Tween SlideIn(RectTransform rectTransform, SlideDirection direction = SlideDirection.Left, float duration = DEFAULT_DURATION, Ease ease = DEFAULT_SLIDE_EASE, System.Action onComplete = null)
        {
            if (rectTransform == null) return null;

            Vector2 startPos = GetSlideStartPosition(rectTransform, direction);
            Vector2 targetPos = rectTransform.anchoredPosition;

            rectTransform.gameObject.SetActive(true);
            rectTransform.anchoredPosition = startPos;

            // Kết hợp slide với fade để đẹp hơn
            CanvasGroup canvasGroup = rectTransform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = rectTransform.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(rectTransform.DOAnchorPos(targetPos, duration).SetEase(ease));
            sequence.Join(canvasGroup.DOFade(1f, duration).SetEase(DEFAULT_FADE_EASE));
            sequence.OnComplete(() => onComplete?.Invoke());

            return sequence;
        }

        /// <summary>
        /// Slide out một panel theo một hướng với animation mượt mà và đẹp
        /// </summary>
        public static Tween SlideOut(RectTransform rectTransform, SlideDirection direction = SlideDirection.Left, float duration = DEFAULT_DURATION, Ease ease = DEFAULT_SLIDE_EASE, System.Action onComplete = null)
        {
            if (rectTransform == null) return null;

            Vector2 targetPos = GetSlideStartPosition(rectTransform, direction);

            // Kết hợp slide với fade để đẹp hơn
            CanvasGroup canvasGroup = rectTransform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = rectTransform.gameObject.AddComponent<CanvasGroup>();
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Append(rectTransform.DOAnchorPos(targetPos, duration).SetEase(ease));
            sequence.Join(canvasGroup.DOFade(0f, duration).SetEase(DEFAULT_FADE_EASE));
            sequence.OnComplete(() =>
            {
                rectTransform.gameObject.SetActive(false);
                onComplete?.Invoke();
            });

            return sequence;
        }

        /// <summary>
        /// Scale in một panel với bounce effect đẹp và kết hợp fade
        /// </summary>
        public static Tween ScaleIn(Transform transform, float duration = DEFAULT_DURATION, Ease ease = DEFAULT_SCALE_EASE, System.Action onComplete = null)
        {
            if (transform == null) return null;

            transform.gameObject.SetActive(true);
            transform.localScale = Vector3.zero;

            // Kết hợp scale với fade để đẹp hơn
            CanvasGroup canvasGroup = transform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = transform.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(Vector3.one, duration).SetEase(ease));
            sequence.Join(canvasGroup.DOFade(1f, duration * 0.8f).SetEase(DEFAULT_FADE_EASE));
            sequence.OnComplete(() => onComplete?.Invoke());

            return sequence;
        }

        /// <summary>
        /// Scale out một panel với animation mượt và kết hợp fade
        /// </summary>
        public static Tween ScaleOut(Transform transform, float duration = DEFAULT_DURATION, Ease ease = Ease.InBack, System.Action onComplete = null)
        {
            if (transform == null) return null;

            // Kết hợp scale với fade để đẹp hơn
            CanvasGroup canvasGroup = transform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = transform.gameObject.AddComponent<CanvasGroup>();
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(Vector3.zero, duration).SetEase(ease));
            sequence.Join(canvasGroup.DOFade(0f, duration * 0.8f).SetEase(DEFAULT_FADE_EASE));
            sequence.OnComplete(() =>
            {
                transform.gameObject.SetActive(false);
                transform.localScale = Vector3.one; // Reset scale
                onComplete?.Invoke();
            });

            return sequence;
        }

        /// <summary>
        /// Switch giữa 2 panels với animation mượt mà - overlap animations để đẹp hơn
        /// </summary>
        public static void SwitchPanels(GameObject hidePanel, GameObject showPanel, float duration = DEFAULT_DURATION, System.Action onComplete = null)
        {
            if (hidePanel == null && showPanel == null) return;

            Sequence sequence = DOTween.Sequence();

            // Fade out panel cũ
            if (hidePanel != null)
            {
                CanvasGroup hideCanvasGroup = GetOrAddCanvasGroup(hidePanel);
                if (hideCanvasGroup != null)
                {
                    sequence.Append(FadeOut(hideCanvasGroup, duration * 0.7f));
                }
                else
                {
                    // Nếu không có CanvasGroup, dùng scale out
                    sequence.Append(ScaleOut(hidePanel.transform, duration * 0.7f));
                }
            }

            // Fade in panel mới - overlap một chút với fade out để mượt hơn
            if (showPanel != null)
            {
                CanvasGroup showCanvasGroup = GetOrAddCanvasGroup(showPanel);
                if (showCanvasGroup != null)
                {
                    // Bắt đầu fade in sớm hơn một chút (overlap) để mượt hơn
                    sequence.Insert(duration * 0.3f, FadeIn(showCanvasGroup, duration));
                }
                else
                {
                    // Nếu không có CanvasGroup, dùng scale in
                    sequence.Insert(duration * 0.3f, ScaleIn(showPanel.transform, duration));
                }
            }

            if (onComplete != null)
            {
                sequence.OnComplete(() => onComplete.Invoke());
            }
        }

        /// <summary>
        /// Switch giữa 2 panels với slide animation
        /// </summary>
        public static void SwitchPanelsSlide(GameObject hidePanel, GameObject showPanel, SlideDirection hideDirection, SlideDirection showDirection, float duration = DEFAULT_DURATION, System.Action onComplete = null)
        {
            if (hidePanel == null && showPanel == null) return;

            Sequence sequence = DOTween.Sequence();

            // Slide out panel cũ
            if (hidePanel != null)
            {
                RectTransform hideRect = hidePanel.GetComponent<RectTransform>();
                if (hideRect != null)
                {
                    sequence.Append(SlideOut(hideRect, hideDirection, duration));
                }
            }

            // Slide in panel mới
            if (showPanel != null)
            {
                RectTransform showRect = showPanel.GetComponent<RectTransform>();
                if (showRect != null)
                {
                    sequence.Append(SlideIn(showRect, showDirection, duration));
                }
            }

            if (onComplete != null)
            {
                sequence.OnComplete(() => onComplete.Invoke());
            }
        }

        /// <summary>
        /// Show một panel với animation
        /// </summary>
        public static Tween ShowPanel(GameObject panel, AnimationType animationType = AnimationType.Fade, float duration = DEFAULT_DURATION, System.Action onComplete = null)
        {
            if (panel == null) return null;

            switch (animationType)
            {
                case AnimationType.Fade:
                    CanvasGroup canvasGroup = GetOrAddCanvasGroup(panel);
                    return FadeIn(canvasGroup, duration, DEFAULT_FADE_EASE, onComplete);

                case AnimationType.Scale:
                    return ScaleIn(panel.transform, duration, DEFAULT_SCALE_EASE, onComplete);

                case AnimationType.SlideLeft:
                    RectTransform rectTransform = panel.GetComponent<RectTransform>();
                    return rectTransform != null ? SlideIn(rectTransform, SlideDirection.Left, duration, DEFAULT_SLIDE_EASE, onComplete) : null;

                case AnimationType.SlideRight:
                    rectTransform = panel.GetComponent<RectTransform>();
                    return rectTransform != null ? SlideIn(rectTransform, SlideDirection.Right, duration, DEFAULT_SLIDE_EASE, onComplete) : null;

                case AnimationType.SlideTop:
                    rectTransform = panel.GetComponent<RectTransform>();
                    return rectTransform != null ? SlideIn(rectTransform, SlideDirection.Top, duration, DEFAULT_SLIDE_EASE, onComplete) : null;

                case AnimationType.SlideBottom:
                    rectTransform = panel.GetComponent<RectTransform>();
                    return rectTransform != null ? SlideIn(rectTransform, SlideDirection.Bottom, duration, DEFAULT_SLIDE_EASE, onComplete) : null;

                default:
                    panel.SetActive(true);
                    onComplete?.Invoke();
                    return null;
            }
        }

        /// <summary>
        /// Hide một panel với animation
        /// </summary>
        public static Tween HidePanel(GameObject panel, AnimationType animationType = AnimationType.Fade, float duration = DEFAULT_DURATION, System.Action onComplete = null)
        {
            if (panel == null) return null;

            switch (animationType)
            {
                case AnimationType.Fade:
                    CanvasGroup canvasGroup = GetOrAddCanvasGroup(panel);
                    return FadeOut(canvasGroup, duration, DEFAULT_FADE_EASE, onComplete);

                case AnimationType.Scale:
                    return ScaleOut(panel.transform, duration, Ease.InBack, onComplete);

                case AnimationType.SlideLeft:
                    RectTransform rectTransform = panel.GetComponent<RectTransform>();
                    return rectTransform != null ? SlideOut(rectTransform, SlideDirection.Left, duration, DEFAULT_SLIDE_EASE, onComplete) : null;

                case AnimationType.SlideRight:
                    rectTransform = panel.GetComponent<RectTransform>();
                    return rectTransform != null ? SlideOut(rectTransform, SlideDirection.Right, duration, DEFAULT_SLIDE_EASE, onComplete) : null;

                case AnimationType.SlideTop:
                    rectTransform = panel.GetComponent<RectTransform>();
                    return rectTransform != null ? SlideOut(rectTransform, SlideDirection.Top, duration, DEFAULT_SLIDE_EASE, onComplete) : null;

                case AnimationType.SlideBottom:
                    rectTransform = panel.GetComponent<RectTransform>();
                    return rectTransform != null ? SlideOut(rectTransform, SlideDirection.Bottom, duration, DEFAULT_SLIDE_EASE, onComplete) : null;

                default:
                    panel.SetActive(false);
                    onComplete?.Invoke();
                    return null;
            }
        }

        /// <summary>
        /// Lấy hoặc thêm CanvasGroup vào GameObject
        /// </summary>
        private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            if (go == null) return null;

            CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = go.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }

        /// <summary>
        /// Tính toán vị trí bắt đầu cho slide animation
        /// </summary>
        private static Vector2 GetSlideStartPosition(RectTransform rectTransform, SlideDirection direction)
        {
            RectTransform parent = rectTransform.parent as RectTransform;
            if (parent == null) return rectTransform.anchoredPosition;

            Vector2 startPos = rectTransform.anchoredPosition;

            switch (direction)
            {
                case SlideDirection.Left:
                    startPos.x = -parent.rect.width - rectTransform.rect.width;
                    break;
                case SlideDirection.Right:
                    startPos.x = parent.rect.width + rectTransform.rect.width;
                    break;
                case SlideDirection.Top:
                    startPos.y = parent.rect.height + rectTransform.rect.height;
                    break;
                case SlideDirection.Bottom:
                    startPos.y = -parent.rect.height - rectTransform.rect.height;
                    break;
            }

            return startPos;
        }

        /// <summary>
        /// Kill tất cả tweens của một GameObject
        /// </summary>
        public static void KillTweens(GameObject go)
        {
            if (go != null)
            {
                // Kill tweens trên GameObject và tất cả components của nó
                DOTween.Kill(go);
                
                // Kill tweens trên Transform nếu có
                Transform transform = go.transform;
                if (transform != null)
                {
                    DOTween.Kill(transform);
                }
                
                // Kill tweens trên RectTransform nếu có
                RectTransform rectTransform = go.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    DOTween.Kill(rectTransform);
                }
                
                // Kill tweens trên CanvasGroup nếu có
                CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    DOTween.Kill(canvasGroup);
                }
            }
        }

        /// <summary>
        /// Enum để định nghĩa hướng slide
        /// </summary>
        public enum SlideDirection
        {
            Left,
            Right,
            Top,
            Bottom
        }

        /// <summary>
        /// Enum để định nghĩa loại animation
        /// </summary>
        public enum AnimationType
        {
            None,
            Fade,
            Scale,
            SlideLeft,
            SlideRight,
            SlideTop,
            SlideBottom
        }
    }
}
