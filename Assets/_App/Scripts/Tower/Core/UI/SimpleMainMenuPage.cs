using UnityEngine;
using DG.Tweening;
using Services.UI;

namespace Core.UI
{
	/// <summary>
	/// Basic class for simple main menu pages that just turns on and off
	/// </summary>
	public class SimpleMainMenuPage : MonoBehaviour, IMainMenuPage
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
		/// Deactivates this page với animation mượt mà
		/// </summary>
		public virtual void Hide()
		{
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
		/// Activates this page với animation mượt mà
		/// </summary>
		public virtual void Show()
		{
			GameObject targetObject = canvas != null ? canvas.gameObject : gameObject;
			if (targetObject != null)
			{
				// Enable object/canvas trước
				if (canvas != null)
				{
					canvas.enabled = true;
				}
				else
				{
					targetObject.SetActive(true);
				}
				
				// Show với animation
				UIAnimationHelper.ShowPanel(targetObject, animationType, animationDuration);
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
	}
}