using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Services.Core;
using Services.Managers;
using Services.Data;
using DG.Tweening;
using Core.UI;
using TowerDefense.UI;
using TowerDefense.UI.Inventory;

namespace Services.UI
{
    /// <summary>
    /// UI Manager for handling main menu UI
    /// Manages user data display, resources, and level selection
    /// </summary>
    public class UIMenuManager : MonoBehaviour
    {
        [Header("User Data UI (Top Left)")]
        [SerializeField] private Image playerLevelIcon;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressBarText;
        [SerializeField] private GameObject userDataPanel;

        [Header("Resources UI (Top Right)")]
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI diamondText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private GameObject resourcesPanel;

        [Header("Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button inventoryButton;

        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private LevelSelectScreen levelSelectScreen;
        [SerializeField] private SimpleMainMenuPage titleMenu;
        [SerializeField] private InventoryUIManager inventoryPanel;

        [Header("Settings")]
        [SerializeField] private bool autoUpdateUI = true;
        [Tooltip("Thời gian animation khi chuyển đổi giữa các panel (giây)")]
        [SerializeField] private float panelTransitionDuration = 0.3f;
        [Tooltip("Loại animation cho panel switching (Fade, Scale, Slide)")]
        [SerializeField] private UIAnimationHelper.AnimationType panelAnimationType = UIAnimationHelper.AnimationType.Fade;

        [Header("Default Values")]
        [Tooltip("Giá trị mặc định nếu không tìm thấy data")]
        [SerializeField] private int defaultPlayerLevel = 1;
        [SerializeField] private int defaultMaxProgress = 1000;
        [SerializeField] private int defaultEnergy = 100;
        [SerializeField] private int defaultDiamond = 0;
        [SerializeField] private int defaultGold = 0;

        private IAuthService authService;
        private IUserDataService userDataService;
        private bool isServiceReady = false;
        private int currentEnergy = 0;
        private int currentDiamond = 0;
        private int currentGold = 0;
        private int currentLevel = 1;
        private int currentProgress = 0;
        private int maxProgress = 1000;

        private void Awake()
        {
            // Initialize UI panels
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            if (userDataPanel != null)
            {
                userDataPanel.SetActive(true);
            }

            if (resourcesPanel != null)
            {
                resourcesPanel.SetActive(true);
            }

            if (levelSelectScreen != null)
            {
                // LevelSelectScreen sẽ được quản lý bởi MainMenu system
                // Đảm bảo GameObject active nhưng Canvas có thể disabled
                GameObject levelSelectObj = levelSelectScreen.gameObject;
                if (levelSelectObj != null)
                {
                    levelSelectObj.SetActive(true); // GameObject phải active để Show() hoạt động
                    
                    // Disable Canvas nếu có để ẩn ban đầu
                    Canvas levelSelectCanvas = levelSelectScreen.canvas;
                    if (levelSelectCanvas != null)
                    {
                        levelSelectCanvas.enabled = false;
                    }
                    else
                    {
                        // Nếu không có canvas riêng, disable GameObject
                        levelSelectObj.SetActive(false);
                    }
                }
            }

            if (inventoryPanel != null)
            {
                // Inventory panel sẽ được quản lý bởi InventoryUIManager
                // Đảm bảo GameObject active nhưng có thể disabled ban đầu
                GameObject inventoryObj = inventoryPanel.gameObject;
                if (inventoryObj != null)
                {
                    inventoryObj.SetActive(true); // GameObject phải active để OpenInventory() hoạt động
                    
                    // Disable ban đầu để ẩn inventory panel
                    Canvas inventoryCanvas = inventoryPanel.GetComponent<Canvas>();
                    if (inventoryCanvas != null)
                    {
                        inventoryCanvas.enabled = false;
                    }
                    else
                    {
                        // Nếu không có canvas riêng, disable GameObject
                        inventoryObj.SetActive(false);
                    }
                }
            }
        }

        private void Start()
        {
            // Start coroutine to wait for services to be ready
            StartCoroutine(WaitForServices());
        }

        /// <summary>
        /// Wait for Auth Service and UserData Service to be registered and initialized
        /// </summary>
        private IEnumerator WaitForServices()
        {
            // Ensure ServiceLocator exists
            _ = ServiceLocator.Instance;

            // Ensure ServicesBootstrap initializes services if not already done
            EnsureServicesBootstrap();

            // Wait for auth service to be registered (max 5 seconds)
            float timeout = 5f;
            float elapsed = 0f;

            while (authService == null && elapsed < timeout)
            {
                authService = ServiceLocator.Instance.GetService<IAuthService>();

                if (authService == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }
            }

            if (authService == null)
            {
                Debug.LogError("[UIMenuManager] Auth service not found after timeout. Make sure ServicesBootstrap is in the scene and initialized.");
                yield break;
            }

            // Wait for user data service
            elapsed = 0f;
            while (userDataService == null && elapsed < timeout)
            {
                userDataService = ServiceLocator.Instance.GetService<IUserDataService>();

                if (userDataService == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }
            }

            if (userDataService == null)
            {
                Debug.LogWarning("[UIMenuManager] UserData service not found. Some features may not work.");
            }

            // Setup UI và subscribe events
            InitializeUI();

            // Wait for service to be initialized (max 30 seconds for Firebase initialization)
            elapsed = 0f;
            float extendedTimeout = 30f;

            while (!authService.IsInitialized && elapsed < extendedTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (!authService.IsInitialized)
            {
                Debug.LogWarning("[UIMenuManager] Auth service is registered but not initialized yet after timeout.");
                Debug.Log("[UIMenuManager] UI đã được setup, sẽ tự động update khi service ready qua OnAuthStateChanged event.");
            }
            else
            {
                // Service đã initialized, update UI ngay
                if (autoUpdateUI)
                {
                    UpdateUI();
                    Debug.Log("[UIMenuManager] ✅ Service đã initialized, UI đã được update");
                }
            }
        }

        /// <summary>
        /// Ensure ServicesBootstrap exists and initializes services
        /// </summary>
        private void EnsureServicesBootstrap()
        {
            ServicesBootstrap bootstrap = FindObjectOfType<ServicesBootstrap>();

            if (bootstrap == null)
            {
                Debug.LogWarning("[UIMenuManager] ServicesBootstrap not found in scene. Creating one automatically...");
                GameObject bootstrapGO = new GameObject("ServicesBootstrap (Auto)");
                bootstrap = bootstrapGO.AddComponent<ServicesBootstrap>();
                StartCoroutine(EnsureServicesInitializedAfterFrame(bootstrap));
            }
            else
            {
                if (!ServiceLocator.Instance.IsServiceRegistered<IAuthService>())
                {
                    Debug.LogWarning("[UIMenuManager] ServicesBootstrap found but services not initialized. Initializing now...");
                    bootstrap.InitializeServices();
                }
            }
        }

        /// <summary>
        /// Coroutine to ensure services are initialized after Awake has run
        /// </summary>
        private IEnumerator EnsureServicesInitializedAfterFrame(ServicesBootstrap bootstrap)
        {
            yield return new WaitForEndOfFrame();

            if (!ServiceLocator.Instance.IsServiceRegistered<IAuthService>())
            {
                Debug.Log("[UIMenuManager] Services not auto-initialized, initializing manually...");
                bootstrap.InitializeServices();
            }
        }

        /// <summary>
        /// Initialize UI components after services are ready
        /// </summary>
        private void InitializeUI()
        {
            if (authService == null)
            {
                Debug.LogWarning("[UIMenuManager] Cannot initialize UI: authService is null");
                return;
            }

            isServiceReady = true;

            // Subscribe to events
            authService.OnAuthStateChanged += OnAuthStateChanged;

            // Setup button listeners
            SetupButtonListeners();

            // Update UI based on current auth state
            if (autoUpdateUI)
            {
                UpdateUI();
            }

            Debug.Log($"[UIMenuManager] UI initialized successfully. Service initialized: {authService.IsInitialized}");
        }

        private void SetupButtonListeners()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OnPlayButtonClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }

            if (inventoryButton != null)
            {
                inventoryButton.onClick.RemoveAllListeners();
                inventoryButton.onClick.AddListener(OnInventoryButtonClicked);
            }
        }

        private void OnPlayButtonClicked()
        {
            Debug.Log("[UIMenuManager] Play button clicked - showing level select screen");

            if (levelSelectScreen == null)
            {
                Debug.LogWarning("[UIMenuManager] LevelSelectScreen is not assigned!");
                return;
            }

            // Đảm bảo GameObject của levelSelectScreen được active
            GameObject levelSelectObj = levelSelectScreen.gameObject;
            if (levelSelectObj != null && !levelSelectObj.activeSelf)
            {
                levelSelectObj.SetActive(true);
            }

            // Hide main menu panel với callback để show level select sau khi hide xong
            if (mainMenuPanel != null)
            {
                UIAnimationHelper.HidePanel(
                    mainMenuPanel, 
                    panelAnimationType, 
                    panelTransitionDuration,
                    onComplete: () =>
                    {
                        // Show level select screen sau khi main menu đã hide
                        if (levelSelectScreen != null)
                        {
                            levelSelectScreen.Show();
                            Debug.Log("[UIMenuManager] ✅ LevelSelectScreen đã được hiển thị");
                        }
                    }
                );
            }
            else
            {
                // Nếu không có mainMenuPanel, show level select ngay
                levelSelectScreen.Show();
                Debug.Log("[UIMenuManager] ✅ LevelSelectScreen đã được hiển thị (không có mainMenuPanel)");
            }
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("[UIMenuManager] Settings button clicked");
            // TODO: Implement settings menu
        }

        private void OnInventoryButtonClicked()
        {
            Debug.Log("[UIMenuManager] Inventory button clicked - opening inventory panel");

            if (inventoryPanel == null)
            {
                Debug.LogWarning("[UIMenuManager] InventoryUIManager is not assigned!");
                return;
            }

            // Open inventory panel
            inventoryPanel.OpenInventory();
            Debug.Log("[UIMenuManager] ✅ Inventory panel đã được mở");
        }

        /// <summary>
        /// Public method to return to main menu from level select screen
        /// </summary>
        public void ReturnToMainMenu()
        {
            Debug.Log("[UIMenuManager] ReturnToMainMenu() called - hiding level select and showing main menu");

            if (levelSelectScreen == null)
            {
                Debug.LogWarning("[UIMenuManager] LevelSelectScreen is null! Cannot hide.");
                return;
            }

            // Get the target object to hide (canvas or gameObject)
            GameObject levelSelectObj = levelSelectScreen.canvas != null 
                ? levelSelectScreen.canvas.gameObject 
                : levelSelectScreen.gameObject;

            if (levelSelectObj == null)
            {
                Debug.LogWarning("[UIMenuManager] LevelSelectScreen GameObject is null! Cannot hide.");
                return;
            }

            // Hide level select screen với callback để show main menu sau khi hide xong
            UIAnimationHelper.HidePanel(
                levelSelectObj,
                levelSelectScreen.animationType,
                levelSelectScreen.animationDuration,
                onComplete: () =>
                {
                    // Disable canvas or GameObject sau khi hide xong
                    if (levelSelectScreen.canvas != null)
                    {
                        levelSelectScreen.canvas.enabled = false;
                    }
                    else
                    {
                        levelSelectObj.SetActive(false);
                    }

                    // Show main menu panel sau khi level select đã hide
                    if (mainMenuPanel != null)
                    {
                        UIAnimationHelper.ShowPanel(
                            mainMenuPanel,
                            panelAnimationType,
                            panelTransitionDuration,
                            onComplete: () =>
                            {
                                Debug.Log("[UIMenuManager] ✅ Main menu đã được hiển thị");
                            }
                        );
                    }
                    else
                    {
                        Debug.LogWarning("[UIMenuManager] mainMenuPanel is null! Cannot show main menu.");
                    }
                }
            );
        }

        private void OnAuthStateChanged(bool isAuthenticated)
        {
            Debug.Log($"[UIMenuManager] Auth state changed: {isAuthenticated}");

            if (autoUpdateUI)
            {
                UpdateUI();
            }
        }

        /// <summary>
        /// Update all UI elements with current user data and resources
        /// </summary>
        private void UpdateUI()
        {
            if (!isServiceReady || authService == null)
            {
                Debug.Log("[UIMenuManager] UpdateUI: Service not ready yet");
                return;
            }

            bool isAuthenticated = authService.IsAuthenticated;

            if (isAuthenticated && authService.CurrentUser != null)
            {
                UpdateUserDataUI(authService.CurrentUser);
            }
            else
            {
                // Show default values if not authenticated
                UpdateUserDataUI(null);
            }

            // Update resources (có thể load từ Firestore hoặc local storage)
            UpdateResourcesUI();
        }

        /// <summary>
        /// Update user data UI (top left): level, name, progress bar
        /// </summary>
        private void UpdateUserDataUI(UserInfo user)
        {
            if (user != null)
            {
                // Update player level
                // Có thể tính từ LevelProgress.MaxLevel hoặc từ một field riêng
                currentLevel = defaultPlayerLevel;
                if (user.LevelProgress != null)
                {
                    // Level có thể được tính từ số level đã hoàn thành
                    int completedLevels = user.LevelProgress.LevelStars?.Count ?? 0;
                    currentLevel = Mathf.Max(defaultPlayerLevel, completedLevels + 1);
                }

                // Update player name
                string displayName = !string.IsNullOrEmpty(user.DisplayName) ? user.DisplayName : user.Email;
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = "Player";
                }

                // Update progress bar
                // Có thể tính từ experience points hoặc từ level progress
                currentProgress = 0;
                maxProgress = defaultMaxProgress;
                
                // Tính progress từ level stars hoặc experience
                if (user.LevelProgress != null && user.LevelProgress.LevelStars != null)
                {
                    int totalStars = 0;
                    foreach (var stars in user.LevelProgress.LevelStars.Values)
                    {
                        totalStars += stars;
                    }
                    // Giả sử mỗi level có 3 stars, progress = (totalStars / (maxLevel * 3)) * maxProgress
                    currentProgress = Mathf.Min(maxProgress, (totalStars * maxProgress) / (currentLevel * 3));
                }

                // Update UI elements
                if (playerLevelText != null)
                {
                    playerLevelText.text = currentLevel.ToString();
                }

                if (playerNameText != null)
                {
                    playerNameText.text = displayName;
                }

                if (progressBar != null)
                {
                    progressBar.value = maxProgress > 0 ? (float)currentProgress / maxProgress : 0f;
                }

                if (progressBarText != null)
                {
                    progressBarText.text = $"{currentProgress}/{maxProgress}";
                }

                Debug.Log($"[UIMenuManager] Updated user data UI: Level={currentLevel}, Name={displayName}, Progress={currentProgress}/{maxProgress}");
            }
            else
            {
                // Default values when not authenticated
                if (playerLevelText != null)
                {
                    playerLevelText.text = defaultPlayerLevel.ToString();
                }

                if (playerNameText != null)
                {
                    playerNameText.text = "Guest";
                }

                if (progressBar != null)
                {
                    progressBar.value = 0f;
                }

                if (progressBarText != null)
                {
                    progressBarText.text = "0/0";
                }
            }
        }

        /// <summary>
        /// Update resources UI (top right): energy, diamond, gold
        /// </summary>
        private void UpdateResourcesUI()
        {
            // TODO: Load resources từ Firestore hoặc local storage
            // Hiện tại sử dụng default values hoặc có thể load từ UserDataService

            // Nếu có user authenticated, có thể load resources từ Firestore
            if (authService != null && authService.IsAuthenticated && authService.CurrentUser != null && userDataService != null)
            {
                // Load resources từ Firestore (cần implement trong UserDataService)
                // Hiện tại sử dụng default values
                StartCoroutine(LoadResourcesFromFirestore());
            }
            else
            {
                // Use default values
                currentEnergy = defaultEnergy;
                currentDiamond = defaultDiamond;
                currentGold = defaultGold;
                UpdateResourcesText();
            }
        }

        /// <summary>
        /// Load resources from Firestore (async)
        /// </summary>
        private IEnumerator LoadResourcesFromFirestore()
        {
            // TODO: Implement actual Firestore loading
            // For now, use default values
            currentEnergy = defaultEnergy;
            currentDiamond = defaultDiamond;
            currentGold = defaultGold;

            UpdateResourcesText();
            yield return null;
        }

        /// <summary>
        /// Update resource text displays
        /// </summary>
        private void UpdateResourcesText()
        {
            if (energyText != null)
            {
                energyText.text = $"{currentEnergy} / {defaultEnergy} +";
            }

            if (diamondText != null)
            {
                diamondText.text = $"{currentDiamond:N0} +";
            }

            if (goldText != null)
            {
                goldText.text = $"{currentGold:N0} +";
            }

            Debug.Log($"[UIMenuManager] Updated resources UI: Energy={currentEnergy}, Diamond={currentDiamond}, Gold={currentGold}");
        }

        /// <summary>
        /// Public method to manually refresh UI state
        /// </summary>
        public void RefreshUI()
        {
            UpdateUI();
        }

        /// <summary>
        /// Public method to update resources manually
        /// </summary>
        public void UpdateResources(int energy, int diamond, int gold)
        {
            currentEnergy = energy;
            currentDiamond = diamond;
            currentGold = gold;
            UpdateResourcesText();
        }

        /// <summary>
        /// Public method to update user data manually
        /// </summary>
        public void UpdateUserData(int level, string name, int progress, int maxProgressValue)
        {
            currentLevel = level;
            currentProgress = progress;
            maxProgress = maxProgressValue;

            if (playerLevelText != null)
            {
                playerLevelText.text = level.ToString();
            }

            if (playerNameText != null)
            {
                playerNameText.text = name;
            }

            if (progressBar != null)
            {
                progressBar.value = maxProgress > 0 ? (float)progress / maxProgress : 0f;
            }

            if (progressBarText != null)
            {
                progressBarText.text = $"{progress}/{maxProgress}";
            }
        }

        private void OnDestroy()
        {
            // Kill all tweens khi destroy
            if (mainMenuPanel != null) UIAnimationHelper.KillTweens(mainMenuPanel);
            if (userDataPanel != null) UIAnimationHelper.KillTweens(userDataPanel);
            if (resourcesPanel != null) UIAnimationHelper.KillTweens(resourcesPanel);

            if (authService != null)
            {
                authService.OnAuthStateChanged -= OnAuthStateChanged;
            }
        }
    }
}

