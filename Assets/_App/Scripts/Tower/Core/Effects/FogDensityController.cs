using UnityEngine;

namespace Core.Effects
{
	/// <summary>
	/// Script điều khiển fog density một cách mượt mà và random
	/// Tăng giảm density trong một range được chỉ định với smooth interpolation
	/// </summary>
	public class FogDensityController : MonoBehaviour
	{
		[Header("Fog Density Settings")]
		[Tooltip("Minimum fog density value")]
		[Range(0f, 1f)]
		public float minDensity = 0.01f;

		[Tooltip("Maximum fog density value")]
		[Range(0f, 1f)]
		public float maxDensity = 0.1f;

		[Header("Change Speed Settings")]
		[Tooltip("Minimum time (in seconds) to change from current to target density")]
		public float minChangeDuration = 10f;

		[Tooltip("Maximum time (in seconds) to change from current to target density")]
		public float maxChangeDuration = 20f;

		[Header("Random Delta Range")]
		[Tooltip("Minimum delta change for density (how much density can change per update)")]
		[Range(0f, 0.1f)]
		public float minDeltaChange = 0.0001f;

		[Tooltip("Maximum delta change for density (how much density can change per update)")]
		[Range(0f, 0.1f)]
		public float maxDeltaChange = 0.0005f;

		[Header("Smoothness")]
		[Tooltip("Smoothing factor for density interpolation (higher = smoother, slower). Recommended: 3-8 for visible but smooth changes")]
		[Range(0.1f, 20f)]
		public float smoothness = 5f;

		[Tooltip("Use adaptive smoothness based on change duration (makes transitions more consistent)")]
		public bool useAdaptiveSmoothness = true;

		[Header("Debug")]
		[Tooltip("Enable debug logs")]
		public bool enableDebug = false;

		private float currentDensity;
		private float targetDensity;
		private float currentVelocity;
		private float nextTargetTime;
		private float timeSinceLastTargetChange;
		private float currentSmoothTime;

		private void Start()
		{
			// Initialize with current fog density or random value
			currentDensity = RenderSettings.fogDensity;
			
			// Ensure fog is enabled
			if (!RenderSettings.fog)
			{
				RenderSettings.fog = true;
				if (enableDebug)
				{
					Debug.Log("[FogDensityController] Fog was disabled, enabling it now");
				}
			}

			// Clamp initial density to valid range
			currentDensity = Mathf.Clamp(currentDensity, minDensity, maxDensity);
			
			// Set initial random target
			targetDensity = Random.Range(minDensity, maxDensity);
			SetNewRandomTarget();
		}

		private void Update()
		{
			// Update time since last target change
			timeSinceLastTargetChange += Time.deltaTime;

			// Check if we should set a new random target
			if (timeSinceLastTargetChange >= nextTargetTime)
			{
				SetNewRandomTarget();
				timeSinceLastTargetChange = 0f;
			}

			// Calculate smooth time - use adaptive based on change duration if enabled
			if (useAdaptiveSmoothness)
			{
				// Adaptive smoothness: use a portion of the change duration for smooth time
				// This ensures the transition takes approximately the full duration
				currentSmoothTime = (nextTargetTime * 0.8f) / smoothness;
				currentSmoothTime = Mathf.Max(0.1f, currentSmoothTime); // Minimum smooth time
			}
			else
			{
				currentSmoothTime = 1f / smoothness;
			}

			// Smooth interpolation towards target density using SmoothDamp for natural easing
			float smoothedDensity = Mathf.SmoothDamp(
				currentDensity, 
				targetDensity, 
				ref currentVelocity, 
				currentSmoothTime, 
				Mathf.Infinity, 
				Time.deltaTime
			);

			// Add subtle random delta variation for natural micro-variations (much smaller)
			float randomDelta = Random.Range(minDeltaChange, maxDeltaChange);
			float randomDirection = Random.Range(-1f, 1f);
			float deltaChange = randomDelta * randomDirection;
			
			// Combine smooth interpolation with random delta, but clamp to valid range
			currentDensity = Mathf.Clamp(smoothedDensity + deltaChange, minDensity, maxDensity);

			// Apply to render settings
			RenderSettings.fogDensity = currentDensity;

			if (enableDebug && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
			{
				Debug.Log($"[FogDensityController] Density: {currentDensity:F4}, Target: {targetDensity:F4}, Delta: {deltaChange:F4}");
			}
		}

		/// <summary>
		/// Sets a new random target density and change duration
		/// </summary>
		private void SetNewRandomTarget()
		{
			targetDensity = Random.Range(minDensity, maxDensity);
			nextTargetTime = Random.Range(minChangeDuration, maxChangeDuration);
			
			if (enableDebug)
			{
				Debug.Log($"[FogDensityController] New target: {targetDensity:F4}, Duration: {nextTargetTime:F2}s");
			}
		}

		/// <summary>
		/// Manually set target density (will be overridden by random after nextTargetTime)
		/// </summary>
		/// <param name="density">Target density value</param>
		public void SetTargetDensity(float density)
		{
			targetDensity = Mathf.Clamp(density, minDensity, maxDensity);
		}

		/// <summary>
		/// Get current fog density
		/// </summary>
		/// <returns>Current fog density value</returns>
		public float GetCurrentDensity()
		{
			return currentDensity;
		}

		/// <summary>
		/// Get target fog density
		/// </summary>
		/// <returns>Target fog density value</returns>
		public float GetTargetDensity()
		{
			return targetDensity;
		}

		private void OnValidate()
		{
			// Ensure min <= max
			if (minDensity > maxDensity)
			{
				minDensity = maxDensity;
			}

			if (minChangeDuration > maxChangeDuration)
			{
				minChangeDuration = maxChangeDuration;
			}

			if (minDeltaChange > maxDeltaChange)
			{
				minDeltaChange = maxDeltaChange;
			}

			// Clamp values to valid ranges
			minDensity = Mathf.Clamp01(minDensity);
			maxDensity = Mathf.Clamp01(maxDensity);
			minChangeDuration = Mathf.Max(0.1f, minChangeDuration);
			maxChangeDuration = Mathf.Max(minChangeDuration, maxChangeDuration);
			minDeltaChange = Mathf.Clamp(minDeltaChange, 0f, 0.1f);
			maxDeltaChange = Mathf.Clamp(maxDeltaChange, minDeltaChange, 0.1f);
		}
	}
}
