using Core.Game;
using TowerDefense.Game;
using TowerDefense.UI.HUD;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Services.UI;
using GameUIState = TowerDefense.UI.HUD.GameUI.State;

namespace TowerDefense.UI
{
	/// <summary>
	/// In-game pause menu
	/// </summary>
	public class PauseMenu : MonoBehaviour
	{
		/// <summary>
		/// Enum to represent state of pause menu
		/// </summary>
		protected enum State
		{
			Open,
			LevelSelectPressed,
			RestartPressed,
			Closed
		}

		/// <summary>
		/// The CanvasGroup that holds the pause menu UI
		/// </summary>
		public Canvas pauseMenuCanvas;

		public Text titleText;
		
		public Text descriptionText;

		/// <summary>
		/// The buttons present in the pause menu
		/// </summary>
		public Button levelSelectConfirmButton;

		public Button restartConfirmButton;
		
		public Button levelSelectButton;
		
		public Button restartButton;

		public Image topPanel;

		/// <summary>
		/// Color to change the top panel to highlight confirmation button
		/// </summary>
		public Color topPanelDisabledColor = new Color(1, 1, 1, 1);
		
		[Header("Animation Settings")]
		[Tooltip("Thời gian animation khi show/hide pause menu (giây)")]
		public float animationDuration = 0.3f;
		[Tooltip("Loại animation cho pause menu")]
		public UIAnimationHelper.AnimationType animationType = UIAnimationHelper.AnimationType.Fade;

		/// <summary>
		/// State of pause menu
		/// </summary>
		protected State m_State;

		/// <summary>
		/// If the pause menu was opened/closed this frame
		/// </summary>
		bool m_MenuChangedThisFrame;

		/// <summary>
		/// Open the pause menu với animation mượt mà
		/// </summary>
		public void OpenPauseMenu()
		{
			// Enable canvas trước
			if (pauseMenuCanvas != null)
			{
				pauseMenuCanvas.enabled = true;
			}

			LevelItem level = GameManager.instance.GetLevelForCurrentScene();
			if (level == null)
			{
				return;
			}
			if (titleText != null)
			{
				titleText.text = level.name;
			}
			if (descriptionText != null)
			{
				descriptionText.text = level.description;
			}

			// Show với animation
			if (pauseMenuCanvas != null)
			{
				UIAnimationHelper.ShowPanel(pauseMenuCanvas.gameObject, animationType, animationDuration);
			}

			m_State = State.Open;
		}

		/// <summary>
		/// Fired when GameUI's State changes
		/// </summary>
		/// <param name="oldState">The State that GameUI is leaving</param>
		/// <param name="newState">The State that GameUI is entering</param>
		protected void OnGameUIStateChanged(GameUIState oldState, GameUIState newState)
		{
			m_MenuChangedThisFrame = true;
			if (newState == GameUIState.Paused)
			{
				OpenPauseMenu();
			}
			else
			{
				ClosePauseMenu();
			}
		}

		/// <summary>
		/// Level select button pressed, display/hide confirmation button với animation
		/// </summary>
		public void LevelSelectPressed()
		{
			bool open = m_State == State.Open;
			restartButton.interactable = !open;
			
			// Animate top panel color change
			if (topPanel != null)
			{
				topPanel.DOColor(open ? topPanelDisabledColor : Color.white, 0.2f);
			}
			
			// Show/hide confirmation button với animation
			if (levelSelectConfirmButton != null)
			{
				if (open)
				{
					UIAnimationHelper.ShowPanel(levelSelectConfirmButton.gameObject, animationType, animationDuration * 0.7f);
				}
				else
				{
					UIAnimationHelper.HidePanel(levelSelectConfirmButton.gameObject, animationType, animationDuration * 0.7f);
				}
			}
			
			m_State = open ? State.LevelSelectPressed : State.Open;
		}

		/// <summary>
		/// Restart button pressed, display/hide confirmation button với animation
		/// </summary>
		public void RestartPressed()
		{
			bool open = m_State == State.Open;
			levelSelectButton.interactable = !open;
			
			// Animate top panel color change
			if (topPanel != null)
			{
				topPanel.DOColor(open ? topPanelDisabledColor : Color.white, 0.2f);
			}
			
			// Show/hide confirmation button với animation
			if (restartConfirmButton != null)
			{
				if (open)
				{
					UIAnimationHelper.ShowPanel(restartConfirmButton.gameObject, animationType, animationDuration * 0.7f);
				}
				else
				{
					UIAnimationHelper.HidePanel(restartConfirmButton.gameObject, animationType, animationDuration * 0.7f);
				}
			}
			
			m_State = open ? State.RestartPressed : State.Open;
		}

		/// <summary>
		/// Close the pause menu với animation mượt mà
		/// </summary>
		public void ClosePauseMenu()
		{
			// Hide với animation
			if (pauseMenuCanvas != null)
			{
				UIAnimationHelper.HidePanel(pauseMenuCanvas.gameObject, animationType, animationDuration, () =>
				{
					SetPauseMenuCanvas(false);
				});
			}
			else
			{
				SetPauseMenuCanvas(false);
			}

			// Hide confirmation buttons với animation
			if (levelSelectConfirmButton != null && levelSelectConfirmButton.gameObject.activeSelf)
			{
				UIAnimationHelper.HidePanel(levelSelectConfirmButton.gameObject, animationType, animationDuration * 0.7f);
			}
			if (restartConfirmButton != null && restartConfirmButton.gameObject.activeSelf)
			{
				UIAnimationHelper.HidePanel(restartConfirmButton.gameObject, animationType, animationDuration * 0.7f);
			}
			
			levelSelectButton.interactable = true;
			restartButton.interactable = true;
			
			// Animate top panel color back
			if (topPanel != null)
			{
				topPanel.DOColor(Color.white, 0.2f);
			}

			m_State = State.Closed;
		}

		/// <summary>
		/// Hide the pause menu on awake
		/// </summary>
		protected void Awake()
		{
			SetPauseMenuCanvas(false);
			m_State = State.Closed;
		}

		/// <summary>
		/// Subscribe to GameUI's stateChanged event
		/// </summary>
		protected void Start()
		{
			if (GameUI.instanceExists)
			{
				GameUI.instance.stateChanged += OnGameUIStateChanged;
			}
		}

		/// <summary>
		/// Unpause the game if the game is paused and the Escape key is pressed
		/// </summary>
		protected virtual void Update()
		{
			if (m_MenuChangedThisFrame)
			{
				m_MenuChangedThisFrame = false;
				return;
			}

			if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && GameUI.instance.state == GameUIState.Paused)
			{
				Unpause();
			}
		}
		
		/// <summary>
		/// Kill all tweens khi destroy
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (pauseMenuCanvas != null)
			{
				UIAnimationHelper.KillTweens(pauseMenuCanvas.gameObject);
			}
			if (levelSelectConfirmButton != null)
			{
				UIAnimationHelper.KillTweens(levelSelectConfirmButton.gameObject);
			}
			if (restartConfirmButton != null)
			{
				UIAnimationHelper.KillTweens(restartConfirmButton.gameObject);
			}
			if (topPanel != null)
			{
				topPanel.DOKill();
			}
		}

		/// <summary>
		/// Show/Hide the pause menu canvas group
		/// </summary>
		protected void SetPauseMenuCanvas(bool enable)
		{
			pauseMenuCanvas.enabled = enable;
		}

		public void Pause()
		{
			if (GameUI.instanceExists)
			{
				GameUI.instance.Pause();
			}
		}

		public void Unpause()
		{
			if (GameUI.instanceExists)
			{
				GameUI.instance.Unpause();
			}
		}
	}
}