using Core.Game;
using TowerDefense.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace TowerDefense.UI
{
	/// <summary>
	/// The button for selecting a level
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class LevelSelectButton : MonoBehaviour, ISelectHandler
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
			ChangeScenes();
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
		}

		/// <summary>
		/// Configures the feedback concerning if the player has played
		/// Loads stars from user's level progress
		/// </summary>
		protected void HasPlayedState()
		{
			GameManager gameManager = GameManager.instance;
			if (gameManager == null || stars == null || starAchieved == null)
			{
				return;
			}
			
			int starsForLevel = gameManager.GetStarsForLevel(m_Item.id);
			
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
		}

		/// <summary>
		/// Implementation of ISelectHandler
		/// </summary>
		/// <param name="eventData">Select event data</param>
		public void OnSelect(BaseEventData eventData)
		{
			m_MouseScroll.SelectChild(this);
		}
	}
}