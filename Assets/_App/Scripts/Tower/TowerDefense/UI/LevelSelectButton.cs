using Core.Game;
using TowerDefense.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Services.Auth;
using Services.Core;
using Services.Managers;

namespace TowerDefense.UI
{
	/// <summary>
	/// The button for selecting a level
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class LevelSelectButton : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IPointerExitHandler
	{
		/// <summary>
		/// Reference to the required button component
		/// </summary>
		protected Button m_Button;

		/// <summary>
		/// The UI text element that displays the name of the level
		/// </summary>
		public TextMeshProUGUI titleDisplay;

		public TextMeshProUGUI description;

		public Sprite starAchieved;

		public Image[] stars;

		/// <summary>
		/// The image component to change color when level is locked (disabled)
		/// If null, will try to use button's targetGraphic or image component
		/// </summary>
		public Image buttonImage;

		/// <summary>
		/// Gray color for locked levels
		/// </summary>
		private static readonly Color LOCKED_COLOR = new Color(0.5f, 0.5f, 0.5f, 1f);

		/// <summary>
		/// Default/normal color for unlocked levels
		/// </summary>
		private Color m_OriginalColor = Color.white;

		protected MouseScroll m_MouseScroll;

		/// <summary>
		/// List of all Image components in children (excluding stars array)
		/// </summary>
		private Image[] m_ChildImages;

		/// <summary>
		/// List of all TextMeshProUGUI components in children
		/// </summary>
		private TextMeshProUGUI[] m_ChildTexts;

		/// <summary>
		/// Original colors of child Image components
		/// </summary>
		private Color[] m_OriginalImageColors;

		/// <summary>
		/// Original colors of child TextMeshProUGUI components
		/// </summary>
		private Color[] m_OriginalTextColors;

		[Header("DOTween Animation Settings")]
		[SerializeField] private float hoverScale = 1.05f;
		[SerializeField] private float hoverDuration = 0.2f;
		[SerializeField] private Ease hoverEase = Ease.OutQuad;
		[SerializeField] private float clickScale = 0.95f;
		[SerializeField] private float clickDuration = 0.1f;
		[SerializeField] private Ease clickEase = Ease.OutBack;

		/// <summary>
		/// Original scale of the button
		/// </summary>
		private Vector3 m_OriginalScale;

		/// <summary>
		/// Current active tween for hover animation
		/// </summary>
		private Tween m_HoverTween;

		/// <summary>
		/// Current active tween for click animation
		/// </summary>
		private Tween m_ClickTween;

		/// <summary>
		/// The data concerning the level this button displays
		/// </summary>
		protected LevelItem m_Item;

		/// <summary>
		/// The index of this level in the level list (-1 if not found)
		/// </summary>
		protected int m_LevelIndex = -1;

		/// <summary>
		/// When the user clicks the button, change the scene
		/// Only works if button is interactable (level is unlocked)
		/// </summary>
		public void ButtonClicked()
		{
			if (m_Button != null && !m_Button.interactable)
			{
				// Level is locked, don't allow click
				return;
			}

			// Play click animation and delay scene change to allow animation to play
			PlayClickAnimation(() => ChangeScenes());
		}

		/// <summary>
		/// A method for assigning the data from item to the button
		/// </summary>
		/// <param name="item">
		/// The data with the information concerning the level
		/// </param>
		public void Initialize(LevelItem item, MouseScroll mouseScroll)
		{
			LazyLoad();
			if (titleDisplay == null)
			{
				return;
			}
			m_Item = item;
			titleDisplay.text = item.name;
			description.text = item.description;
			
			// Find level index in level list
			m_LevelIndex = GetLevelIndex(item);
			
			// Setup button onClick listener
			SetupButtonListener();
			
			// Setup button state and stars
			UpdateButtonState();
			HasPlayedState();
			
			m_MouseScroll = mouseScroll;
		}

		/// <summary>
		/// Gets the index of this level in the level list
		/// </summary>
		protected int GetLevelIndex(LevelItem item)
		{
			if (item == null || string.IsNullOrEmpty(item.id))
			{
				return -1;
			}

			GameManager gameManager = GameManager.instance;
			if (gameManager == null || gameManager.levelList == null)
			{
				return -1;
			}

			int levelCount = gameManager.levelList.Count;
			for (int i = 0; i < levelCount; i++)
			{
				LevelItem levelItem = gameManager.levelList[i];
				if (levelItem != null && levelItem.id == item.id)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Updates button state based on maxLevel: disable if level >= maxLevel and set gray color
		/// Note: maxLevel represents the next level index that should be unlocked
		/// So if maxLevel = 1, only level with index < 1 (i.e., index 0) is unlocked
		/// </summary>
		protected void UpdateButtonState()
		{
			GameManager gameManager = GameManager.instance;
			if (gameManager == null || m_Button == null)
			{
				return;
			}

			int maxLevel = gameManager.GetMaxLevel();
			// maxLevel is the next level index to unlock, so level is locked if index >= maxLevel
			bool isLocked = (m_LevelIndex >= maxLevel);

			// Enable/disable button
			m_Button.interactable = !isLocked;

			// Initialize child components if not already done
			InitializeChildComponents();

			// Get image component for color change
			Image imageToColor = buttonImage;
			if (imageToColor == null && m_Button.targetGraphic != null)
			{
				imageToColor = m_Button.targetGraphic as Image;
			}
			if (imageToColor == null)
			{
				imageToColor = m_Button.GetComponent<Image>();
			}

			if (imageToColor != null)
			{
				// Store original color before changing (only if not already stored or if color is still original)
				if (m_OriginalColor == Color.white || m_OriginalColor == LOCKED_COLOR)
				{
					m_OriginalColor = imageToColor.color;
				}

				if (isLocked)
				{
					// Set gray color for locked level
					imageToColor.color = LOCKED_COLOR;
				}
				else
				{
					// Restore original color for unlocked level
					imageToColor.color = m_OriginalColor;
				}
			}

			// Update all child Image components
			UpdateChildImagesColor(isLocked);

			// Update all child TextMeshProUGUI components
			UpdateChildTextsColor(isLocked);
		}

		/// <summary>
		/// Public method to refresh stars display
		/// Can be called when level progress is loaded from database
		/// </summary>
		public void RefreshStars()
		{
			HasPlayedState();
		}

		/// <summary>
		/// Configures the feedback concerning if the player has played
		/// Loads stars from user's level progress trên database
		/// </summary>
		protected void HasPlayedState()
		{
			if (stars == null || starAchieved == null || m_Item == null)
			{
				return;
			}
			
			int starsForLevel = 0;
			bool foundInDatabase = false;
			
			// Ưu tiên lấy từ user.LevelProgress.LevelStars trên database
			var serviceLocator = ServiceLocator.Instance;
			if (serviceLocator != null)
			{
				var authService = serviceLocator.GetService<IAuthService>();
				if (authService != null && authService.IsAuthenticated && authService.CurrentUser != null)
				{
					var levelProgress = authService.CurrentUser.LevelProgress;
					if (levelProgress != null && levelProgress.LevelStars != null)
					{
						// Lấy stars từ LevelProgress.LevelStars dictionary
						if (levelProgress.LevelStars.ContainsKey(m_Item.id))
						{
							starsForLevel = levelProgress.LevelStars[m_Item.id];
							foundInDatabase = true;
						}
					}
				}
			}
			
			// Fallback: nếu không tìm thấy trong database, lấy từ GameManager (local data)
			if (!foundInDatabase)
			{
				GameManager gameManager = GameManager.instance;
				if (gameManager != null)
				{
					starsForLevel = gameManager.GetStarsForLevel(m_Item.id);
				}
			}
			
			// Set sprite for achieved stars
			for (int i = 0; i < stars.Length; i++)
			{
				if (i < starsForLevel)
				{
					stars[i].sprite = starAchieved;
				}
				// Note: Stars not achieved will keep their default sprite from Inspector
				// If you need to reset them explicitly, add a defaultStarSprite field
			}
		}

		/// <summary>
		/// Changes the scene to the scene name provided by m_Item
		/// </summary>
		protected void ChangeScenes()
		{
			if (m_Item == null)
			{
				Debug.LogError("[LevelSelectButton] Cannot change scene: m_Item is null");
				return;
			}

			if (string.IsNullOrEmpty(m_Item.sceneName))
			{
				Debug.LogError($"[LevelSelectButton] Cannot change scene: sceneName is null or empty for level {m_Item.name}");
				return;
			}

			Debug.Log($"[LevelSelectButton] Loading scene: {m_Item.sceneName} for level: {m_Item.name}");
			SceneManager.LoadScene(m_Item.sceneName);
		}

		/// <summary>
		/// Ensure <see cref="m_Button"/> is not null
		/// </summary>
		protected void LazyLoad()
		{
			if (m_Button == null)
			{
				m_Button = GetComponent<Button>();
			}

			// Initialize buttonImage if not set and button has targetGraphic
			if (buttonImage == null && m_Button != null && m_Button.targetGraphic != null)
			{
				buttonImage = m_Button.targetGraphic as Image;
			}

			// Store original color
			if (buttonImage != null && m_OriginalColor == Color.white)
			{
				m_OriginalColor = buttonImage.color;
			}

			// Store original scale
			if (m_OriginalScale == Vector3.zero)
			{
				m_OriginalScale = transform.localScale;
			}
		}

		/// <summary>
		/// Setup onClick listener for the button
		/// </summary>
		private void SetupButtonListener()
		{
			if (m_Button == null)
			{
				Debug.LogWarning("[LevelSelectButton] Button component is null, cannot setup listener");
				return;
			}

			// Remove existing listeners to avoid duplicates
			m_Button.onClick.RemoveAllListeners();
			
			// Add ButtonClicked as listener
			m_Button.onClick.AddListener(ButtonClicked);
		}

		/// <summary>
		/// Initialize all child Image and TextMeshProUGUI components and store their original colors
		/// </summary>
		private void InitializeChildComponents()
		{
			// Only initialize once
			if (m_ChildImages != null && m_ChildTexts != null)
			{
				return;
			}

			// Get all Image components in children (excluding this component and stars)
			System.Collections.Generic.List<Image> imageList = new System.Collections.Generic.List<Image>();
			Image[] allImages = GetComponentsInChildren<Image>(true);
			foreach (Image img in allImages)
			{
				// Skip Image component of this GameObject (button itself)
				if (img.transform == transform)
				{
					continue;
				}
				// Skip stars array
				if (stars != null && System.Array.IndexOf(stars, img) >= 0)
				{
					continue;
				}
				imageList.Add(img);
			}
			m_ChildImages = imageList.ToArray();

			// Get all TextMeshProUGUI components in children (excluding this component)
			System.Collections.Generic.List<TextMeshProUGUI> textList = new System.Collections.Generic.List<TextMeshProUGUI>();
			TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
			foreach (TextMeshProUGUI text in allTexts)
			{
				// Skip TextMeshProUGUI component of this GameObject (if any)
				if (text.transform == transform)
				{
					continue;
				}
				textList.Add(text);
			}
			m_ChildTexts = textList.ToArray();

			// Store original colors
			if (m_ChildImages != null)
			{
				m_OriginalImageColors = new Color[m_ChildImages.Length];
				for (int i = 0; i < m_ChildImages.Length; i++)
				{
					if (m_ChildImages[i] != null)
					{
						m_OriginalImageColors[i] = m_ChildImages[i].color;
					}
				}
			}

			if (m_ChildTexts != null)
			{
				m_OriginalTextColors = new Color[m_ChildTexts.Length];
				for (int i = 0; i < m_ChildTexts.Length; i++)
				{
					if (m_ChildTexts[i] != null)
					{
						m_OriginalTextColors[i] = m_ChildTexts[i].color;
					}
				}
			}
		}

		/// <summary>
		/// Update color of all child Image components based on locked state
		/// </summary>
		/// <param name="isLocked">Whether the level is locked</param>
		private void UpdateChildImagesColor(bool isLocked)
		{
			if (m_ChildImages == null || m_OriginalImageColors == null)
			{
				return;
			}

			for (int i = 0; i < m_ChildImages.Length; i++)
			{
				if (m_ChildImages[i] != null)
				{
					if (isLocked)
					{
						m_ChildImages[i].color = LOCKED_COLOR;
					}
					else
					{
						m_ChildImages[i].color = m_OriginalImageColors[i];
					}
				}
			}
		}

		/// <summary>
		/// Update color of all child TextMeshProUGUI components based on locked state
		/// </summary>
		/// <param name="isLocked">Whether the level is locked</param>
		private void UpdateChildTextsColor(bool isLocked)
		{
			if (m_ChildTexts == null || m_OriginalTextColors == null)
			{
				return;
			}

			for (int i = 0; i < m_ChildTexts.Length; i++)
			{
				if (m_ChildTexts[i] != null)
				{
					if (isLocked)
					{
						m_ChildTexts[i].color = LOCKED_COLOR;
					}
					else
					{
						m_ChildTexts[i].color = m_OriginalTextColors[i];
					}
				}
			}
		}

		/// <summary>
		/// Remove all listeners on the button before destruction
		/// </summary>
		protected void OnDestroy()
		{
			if (m_Button != null)
			{
				m_Button.onClick.RemoveAllListeners();
			}

			// Kill all active tweens to prevent memory leaks
			if (m_HoverTween != null && m_HoverTween.IsActive())
			{
				m_HoverTween.Kill();
			}

			if (m_ClickTween != null && m_ClickTween.IsActive())
			{
				m_ClickTween.Kill();
			}
		}

		/// <summary>
		/// Implementation of ISelectHandler
		/// </summary>
		/// <param name="eventData">Select event data</param>
		public void OnSelect(BaseEventData eventData)
		{
			m_MouseScroll.SelectChild(this);
		}

		/// <summary>
		/// Implementation of IPointerEnterHandler - called when mouse enters the button
		/// </summary>
		/// <param name="eventData">Pointer event data</param>
		public void OnPointerEnter(PointerEventData eventData)
		{
			if (m_Button != null && !m_Button.interactable)
			{
				// Don't animate if button is locked
				return;
			}

			PlayHoverAnimation(true);
		}

		/// <summary>
		/// Implementation of IPointerExitHandler - called when mouse leaves the button
		/// </summary>
		/// <param name="eventData">Pointer event data</param>
		public void OnPointerExit(PointerEventData eventData)
		{
			PlayHoverAnimation(false);
		}

		/// <summary>
		/// Plays hover animation (scale up on enter, scale back on exit)
		/// </summary>
		/// <param name="isEntering">True when entering, false when exiting</param>
		private void PlayHoverAnimation(bool isEntering)
		{
			LazyLoad();

			// Kill existing hover tween
			if (m_HoverTween != null && m_HoverTween.IsActive())
			{
				m_HoverTween.Kill();
			}

			Vector3 targetScale = isEntering ? m_OriginalScale * hoverScale : m_OriginalScale;

			m_HoverTween = transform.DOScale(targetScale, hoverDuration)
				.SetEase(hoverEase);
		}

		/// <summary>
		/// Plays click animation (scale down then bounce back)
		/// </summary>
		/// <param name="onComplete">Callback to execute after animation completes</param>
		private void PlayClickAnimation(System.Action onComplete = null)
		{
			LazyLoad();

			// Kill existing click tween
			if (m_ClickTween != null && m_ClickTween.IsActive())
			{
				m_ClickTween.Kill();
			}

			// Create sequence: scale down then bounce back
			Sequence clickSequence = DOTween.Sequence();
			
			// Scale down
			clickSequence.Append(transform.DOScale(m_OriginalScale * clickScale, clickDuration * 0.5f)
				.SetEase(Ease.InQuad));
			
			// Scale back to original (with bounce effect)
			clickSequence.Append(transform.DOScale(m_OriginalScale, clickDuration)
				.SetEase(clickEase));

			// Execute callback after animation completes
			if (onComplete != null)
			{
				clickSequence.OnComplete(() => onComplete());
			}

			m_ClickTween = clickSequence;
		}
	}
}