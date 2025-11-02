using UnityEngine;
using DG.Tweening;
using Services.UI;

namespace Core.UI
{
	/// <summary>
	/// Abstract base class for menu pages which animates the process of enabling and disabling
	/// Handles activation/deactivation of the page
	/// </summary>
	public abstract class AnimatingMainMenuPage : MonoBehaviour, IMainMenuPage
	{
		/// <summary>
		/// Canvas to disable. If this object is set, then the canvas is disabled instead of the game object 
		/// </summary>
		public Canvas canvas;
		
		[Header("Animation Settings")]
		[Tooltip("Thời gian animation khi show/hide page (giây)")]
		public float animationDuration = 0.3f;
		[Tooltip("Loại animation cho page transition")]
		public UIAnimationHelper.AnimationType animationType = UIAnimationHelper.AnimationType.Fade;
		
		/// <summary>
		/// Deactivates this page
		/// </summary>
		public virtual void Hide()
		{
			BeginDeactivatingPage();
		}

		/// <summary>
		/// Activates this page
		/// </summary>
		public virtual void Show()
		{
			BeginActivatingPage();
		}

		/// <summary>
		/// Starts the deactivation process. e.g. begins fading page out. Call FinishedDeactivatingPage when done
		/// </summary>
		protected abstract void BeginDeactivatingPage();

		/// <summary>
		/// Ends the deactivation process and turns off the associated gameObject/canvas
		/// </summary>
		protected virtual void FinishedDeactivatingPage()
		{
			// Sử dụng DOTween để hide với animation mượt mà
			GameObject targetObject = canvas != null ? canvas.gameObject : gameObject;
			if (targetObject != null)
			{
				UIAnimationHelper.HidePanel(targetObject, animationType, animationDuration, () =>
				{
					if (canvas != null)
					{
						canvas.enabled = false;
					}
					else
					{
						targetObject.SetActive(false);
					}
				});
			}
		}

		/// <summary>
		/// Starts the activation process by turning on the associated gameObject/canvas.  Call FinishedActivatingPage when done
		/// </summary>
		protected virtual void BeginActivatingPage()
		{
			// Sử dụng DOTween để show với animation mượt mà
			GameObject targetObject = canvas != null ? canvas.gameObject : gameObject;
			if (targetObject != null)
			{
				// Enable object/canvas trước để animation có thể chạy
				if (canvas != null)
				{
					canvas.enabled = true;
				}
				else
				{
					targetObject.SetActive(true);
				}
				
				// Sau đó show với animation
				UIAnimationHelper.ShowPanel(targetObject, animationType, animationDuration, () =>
				{
					FinishedActivatingPage();
				});
			}
		}
		
		/// <summary>
		/// Kill all tweens khi destroy
		/// </summary>
		protected virtual void OnDestroy()
		{
			GameObject targetObject = canvas != null ? canvas.gameObject : gameObject;
			if (targetObject != null)
			{
				UIAnimationHelper.KillTweens(targetObject);
			}
		}

		/// <summary>
		/// Finishes the activation process. e.g. Turning on input
		/// </summary>
		protected abstract void FinishedActivatingPage();
	}
}