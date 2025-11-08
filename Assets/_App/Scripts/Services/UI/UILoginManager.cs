using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Services.Core;
using Services.Managers;
using DG.Tweening;

namespace Services.UI
{
    /// <summary>
    /// UI Manager for handling login/authentication UI
    /// Manages all login-related UI interactions and state
    /// </summary>
    public class UILoginManager : MonoBehaviour
    {
        [Header("Sign In Buttons")]
        [SerializeField] private Button googleSignInButton;
        [SerializeField] private Button emailSignInButton;
        
        [Header("Sign Up Buttons")]
        [SerializeField] private Button signUpConfirmButton;
        
        [Header("Panel Navigation")]
        [SerializeField] private Button switchToSignUpButton;
        [SerializeField] private Button switchToLoginButton;
        [SerializeField] private Button openLoginPanelButton;
        [SerializeField] private Button closeLoginPanelButton;
        [SerializeField] private Button closeSignUpPanelButton;

        [Header("Sign Out")]
        [SerializeField] private Button signOutButton;

        [Header("Login Panel Inputs")]
        [SerializeField] private TMP_InputField loginUsernameInputField;
        [SerializeField] private TMP_InputField loginPasswordInputField;
        
        [Header("Sign Up Panel Inputs")]
        [SerializeField] private TMP_InputField signUpUsernameInputField;
        [SerializeField] private TMP_InputField signUpPasswordInputField;

        [Header("UI Display")]
        [SerializeField] private TextMeshProUGUI userInfoText;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private TextMeshProUGUI loadingSliderText;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject signUpPanel;
        [SerializeField] private GameObject userInfoPanel;

        [Header("Settings")]
        [SerializeField] private bool autoUpdateUI = true;
        [Tooltip("Nếu true, sẽ tự động đăng xuất session cũ khi khởi động để cho phép đăng nhập lại. Đặt true để tắt auto login và yêu cầu đăng nhập mỗi lần vào game.")]
        [SerializeField] private bool autoSignOutOnStart = true;
        
        [Header("Animation Settings")]
        [Tooltip("Thời gian animation khi chuyển đổi giữa các panel (giây)")]
        [SerializeField] private float panelTransitionDuration = 0.3f;
        [Tooltip("Loại animation cho panel switching (Fade, Scale, Slide)")]
        [SerializeField] private UIAnimationHelper.AnimationType panelAnimationType = UIAnimationHelper.AnimationType.Fade;
        
        [Header("Scene Navigation")]
        [Tooltip("Tên scene menu để chuyển đến sau khi đăng nhập thành công. Để trống nếu không muốn tự động chuyển scene.")]
        [SerializeField] private string menuSceneName = "MenuScene";
        [Tooltip("Delay (giây) trước khi chuyển scene sau khi đăng nhập thành công")]
        [SerializeField] private float sceneTransitionDelay = 1f;
        
        [Header("Loading Panel Settings")]
        [Tooltip("Thời gian hiển thị loading panel khi vào game (giây)")]
        [SerializeField] private float initialLoadingDuration = 2.5f;
        [Tooltip("Thời gian hiển thị loading panel trước khi load scene sau khi đăng nhập thành công (giây)")]
        [SerializeField] private float loginSuccessLoadingDuration = 2f;

        private IAuthService authService;
        private bool isServiceReady = false;
        private bool hasCheckedButtonsAfterInit = false; // Flag để chỉ check buttons một lần sau khi init
        private bool hasAutoSignedOut = false; // Flag để tránh đăng xuất auto login nhiều lần
        private Vector2 loadingPanelOriginalPosition; // Lưu vị trí ban đầu của loading panel

        private void Awake()
        {
            // Initialize UI - show loading panel first
            if (loadingPanel != null)
            {
                // Lưu vị trí ban đầu của loading panel
                RectTransform loadingRectTransform = loadingPanel.GetComponent<RectTransform>();
                if (loadingRectTransform != null)
                {
                    loadingPanelOriginalPosition = loadingRectTransform.anchoredPosition;
                }
                
                loadingPanel.SetActive(true);
                // Reset alpha nếu có CanvasGroup để đảm bảo animation hoạt động đúng
                CanvasGroup loadingCanvasGroup = loadingPanel.GetComponent<CanvasGroup>();
                if (loadingCanvasGroup != null)
                {
                    loadingCanvasGroup.alpha = 1f;
                }
                
                // Reset loading slider
                if (loadingSlider != null)
                {
                    loadingSlider.value = 0f;
                }
                
                // Reset loading slider text
                if (loadingSliderText != null)
                {
                    loadingSliderText.text = "0%";
                }
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (loginPanel != null)
            {
                loginPanel.SetActive(false);
            }

            if (signUpPanel != null)
            {
                signUpPanel.SetActive(false);
            }

            if (userInfoPanel != null)
            {
                userInfoPanel.SetActive(false);
            }
            
            // Setup panel switch buttons immediately so they work even before service is ready
            // These buttons should always be enabled (except when authenticated)
            if (switchToSignUpButton != null)
            {
                switchToSignUpButton.interactable = true;
            }
            
            if (switchToLoginButton != null)
            {
                switchToLoginButton.interactable = false; // Will be on login panel when opened
            }
        }

        private void Start()
        {
            // Start initial loading animation
            StartCoroutine(ShowInitialLoading());
            
            // Start coroutine to wait for service to be ready
            StartCoroutine(WaitForAuthService());
        }

        /// <summary>
        /// Show loading panel when game starts, then hide it after duration
        /// </summary>
        private IEnumerator ShowInitialLoading()
        {
            // Reset loading panel position and slider
            ResetLoadingPanel();
            
            // Show loading panel with slide animation
            if (loadingPanel != null)
            {
                UIAnimationHelper.ShowPanel(loadingPanel, UIAnimationHelper.AnimationType.SlideBottom, panelTransitionDuration);
            }
            
            // Animate loading slider from 0 to 1
            StartCoroutine(AnimateLoadingSlider(initialLoadingDuration));
            
            // Wait for initial loading duration
            yield return new WaitForSeconds(initialLoadingDuration);
            
            // Hide loading panel with slide animation
            if (loadingPanel != null)
            {
                UIAnimationHelper.HidePanel(loadingPanel, UIAnimationHelper.AnimationType.SlideBottom, panelTransitionDuration);
            }
            
            // Show main menu panel after loading is done
            yield return new WaitForSeconds(panelTransitionDuration);
            
            if (mainMenuPanel != null && !mainMenuPanel.activeSelf)
            {
                mainMenuPanel.SetActive(true);
                UIAnimationHelper.ShowPanel(mainMenuPanel, panelAnimationType, panelTransitionDuration);
            }
        }

        /// <summary>
        /// Wait for Auth Service to be registered and initialized
        /// </summary>
        private IEnumerator WaitForAuthService()
        {
            // Ensure ServiceLocator exists
            _ = ServiceLocator.Instance;
            
            // Ensure ServicesBootstrap initializes services if not already done
            EnsureServicesBootstrap();
            
            // Wait for service to be registered (max 5 seconds)
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
                Debug.LogError("[UILoginManager] Auth service not found after timeout. Make sure ServicesBootstrap is in the scene and initialized.");
                SetStatus("Lỗi: Authentication service chưa được khởi tạo", true);
                yield break;
            }
            
            // Setup UI và subscribe events ngay lập tức để nhận được notification khi service ready
            InitializeUI();
            
            // Wait for service to be initialized (max 30 seconds for Firebase initialization)
            // Nhưng không block - events sẽ update UI khi service ready
            elapsed = 0f;
            float extendedTimeout = 30f;
            float lastStatusUpdate = 0f;
            
            while (!authService.IsInitialized && elapsed < extendedTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
                
                // Log progress mỗi 2 giây (không show popup)
                if (elapsed - lastStatusUpdate >= 2f)
                {
                    Debug.Log($"[UILoginManager] Đang khởi tạo authentication service... ({Mathf.FloorToInt(elapsed)}s)");
                    lastStatusUpdate = elapsed;
                }
            }
            
            if (!authService.IsInitialized)
            {
                Debug.LogWarning("[UILoginManager] Auth service is registered but not initialized yet after timeout.");
                Debug.Log("[UILoginManager] UI đã được setup, sẽ tự động update khi service ready qua OnAuthStateChanged event.");
            }
            else
            {
                // Check if user is already authenticated (from previous session)
                // Luôn đăng xuất session cũ để yêu cầu đăng nhập mỗi lần vào game
                if (authService.IsAuthenticated && !hasAutoSignedOut)
                {
                    Debug.Log("[UILoginManager] User đã authenticated từ session cũ, đang tự động đăng xuất để yêu cầu đăng nhập lại...");
                    hasAutoSignedOut = true; // Đánh dấu đã đăng xuất auto login
                    // Tự động đăng xuất session cũ
                    StartCoroutine(SignOutAndUpdateUI());
                }
                else
                {
                    // Service đã initialized trong loop, update UI ngay
                    if (autoUpdateUI)
                    {
                        UpdateUI();
                        // Không hiển thị popup, chỉ log
                        Debug.Log("[UILoginManager] ✅ Service đã initialized, sẵn sàng đăng nhập");
                        // Force enable buttons để đảm bảo chúng được enable
                        ForceEnableLoginButtons();
                    }
                }
            }
        }

        /// <summary>
        /// Coroutine to handle sign out when auto sign out is enabled
        /// </summary>
        private IEnumerator SignOutAndUpdateUI()
        {
            Debug.Log("[UILoginManager] Starting auto sign out...");
            
            // Create a task and wait for it
            System.Threading.Tasks.Task signOutTask = null;
            try
            {
                signOutTask = authService.SignOutAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UILoginManager] Lỗi khi đăng xuất tự động: {e.Message}");
                yield break;
            }

            if (signOutTask != null)
            {
                // Wait for task to complete
                while (!signOutTask.IsCompleted)
                {
                    yield return new WaitForSeconds(0.1f);
                }

                if (signOutTask.IsFaulted)
                {
                    Debug.LogError($"[UILoginManager] Lỗi khi đăng xuất tự động: {signOutTask.Exception?.GetBaseException()?.Message ?? "Unknown error"}");
                }
                else
                {
                    Debug.Log("[UILoginManager] ✅ Auto sign out completed successfully");
                }
            }

            // Wait a bit more to ensure auth state has updated
            yield return new WaitForSeconds(0.2f);

            // Update UI after sign out
            if (autoUpdateUI)
            {
                UpdateUI();
                
                // Force enable buttons after sign out
                if (!authService.IsAuthenticated)
                {
                    ForceEnableLoginButtons();
                    // Chỉ log không show popup
                    Debug.Log("[UILoginManager] ✅ Buttons đã được enable sau khi auto sign out");
                }
                else
                {
                    Debug.LogWarning("[UILoginManager] User vẫn authenticated sau khi sign out, có thể có vấn đề");
                }
            }
        }

        /// <summary>
        /// Ensure ServicesBootstrap exists and initializes services
        /// </summary>
        private void EnsureServicesBootstrap()
        {
            // Check if ServicesBootstrap already exists in scene
            ServicesBootstrap bootstrap = FindObjectOfType<ServicesBootstrap>();
            
            if (bootstrap == null)
            {
                Debug.LogWarning("[UILoginManager] ServicesBootstrap not found in scene. Creating one automatically...");
                GameObject bootstrapGO = new GameObject("ServicesBootstrap (Auto)");
                bootstrap = bootstrapGO.AddComponent<ServicesBootstrap>();
                // Note: Awake() will be called automatically and will initialize services if autoInitializeOnAwake is true
                // If autoInitializeOnAwake is false, we need to initialize manually
                // Wait a frame to ensure Awake() has been called
                StartCoroutine(EnsureServicesInitializedAfterFrame(bootstrap));
            }
            else
            {
                // Ensure it's initialized
                if (!ServiceLocator.Instance.IsServiceRegistered<IAuthService>())
                {
                    Debug.LogWarning("[UILoginManager] ServicesBootstrap found but services not initialized. Initializing now...");
                    bootstrap.InitializeServices();
                }
            }
        }

        /// <summary>
        /// Coroutine to ensure services are initialized after Awake has run
        /// </summary>
        private IEnumerator EnsureServicesInitializedAfterFrame(ServicesBootstrap bootstrap)
        {
            // Wait for end of frame to ensure Awake() has been called
            yield return new WaitForEndOfFrame();
            
            // Check if services are registered (they should be if autoInitializeOnAwake is true)
            if (!ServiceLocator.Instance.IsServiceRegistered<IAuthService>())
            {
                Debug.Log("[UILoginManager] Services not auto-initialized, initializing manually...");
                bootstrap.InitializeServices();
            }
        }

        /// <summary>
        /// Initialize UI components after service is ready
        /// Subscribe to events ngay để nhận được notification khi service initialized
        /// </summary>
        private void InitializeUI()
        {
            if (authService == null)
            {
                Debug.LogWarning("[UILoginManager] Cannot initialize UI: authService is null");
                return;
            }

            isServiceReady = true;

            // Subscribe to events TRƯỚC - để nhận được notification ngay khi service initialized
            authService.OnAuthStateChanged += OnAuthStateChanged;
            authService.OnSignInSuccess += OnSignInSuccess;
            authService.OnSignInFailed += OnSignInFailed;

            // Setup button listeners
            SetupButtonListeners();

            // Update UI based on current auth state (nếu service đã initialized)
            if (autoUpdateUI)
            {
                UpdateUI();
                
                // Nếu service chưa initialized, log để biết sẽ được update qua event
                if (!authService.IsInitialized)
                {
                    Debug.Log("[UILoginManager] Service chưa initialized, UI sẽ được update tự động khi OnAuthStateChanged được trigger.");
                }
            }
            
            Debug.Log($"[UILoginManager] UI initialized successfully. Service initialized: {authService.IsInitialized}");
        }

        private void Update()
        {
            // Check if service is ready and update UI if needed
            if (isServiceReady && authService != null && authService.IsInitialized)
            {
                // Kiểm tra nếu buttons bị disable nhưng service đã ready và user chưa authenticated
                if (autoUpdateUI && !authService.IsAuthenticated)
                {
                    bool shouldForceEnable = false;
                    
                    // Kiểm tra từng button
                    if (googleSignInButton != null && !googleSignInButton.interactable)
                    {
                        shouldForceEnable = true;
                        Debug.LogWarning("[UILoginManager] Google SignIn button is disabled but should be enabled!");
                    }
                    if (emailSignInButton != null && !emailSignInButton.interactable)
                    {
                        shouldForceEnable = true;
                        Debug.LogWarning("[UILoginManager] Email SignIn button is disabled but should be enabled!");
                    }
                    if (signUpConfirmButton != null && !signUpConfirmButton.interactable)
                    {
                        shouldForceEnable = true;
                        Debug.LogWarning("[UILoginManager] SignUp button is disabled but should be enabled!");
                    }
                    
                    if (shouldForceEnable)
                    {
                        Debug.Log("[UILoginManager] Detected buttons disabled incorrectly, forcing enable...");
                        ForceEnableLoginButtons();
                    }
                    
                    // Đánh dấu đã check sau khi initialized
                    if (!hasCheckedButtonsAfterInit)
                    {
                        hasCheckedButtonsAfterInit = true;
                    }
                }
            }
        }

        private void SetupButtonListeners()
        {
            if (googleSignInButton != null)
            {
                googleSignInButton.onClick.RemoveAllListeners();
                googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
            }

            if (emailSignInButton != null)
            {
                emailSignInButton.onClick.RemoveAllListeners();
                emailSignInButton.onClick.AddListener(OnEmailSignInClicked);
            }

            if (signUpConfirmButton != null)
            {
                signUpConfirmButton.onClick.RemoveAllListeners();
                signUpConfirmButton.onClick.AddListener(OnSignUpClicked);
            }

            if (switchToSignUpButton != null)
            {
                switchToSignUpButton.onClick.RemoveAllListeners();
                switchToSignUpButton.onClick.AddListener(SwitchToSignUpPanel);
            }

            if (switchToLoginButton != null)
            {
                switchToLoginButton.onClick.RemoveAllListeners();
                switchToLoginButton.onClick.AddListener(SwitchToLoginPanel);
            }

            if (openLoginPanelButton != null)
            {
                openLoginPanelButton.onClick.RemoveAllListeners();
                openLoginPanelButton.onClick.AddListener(OpenLoginPanel);
            }

            if (closeLoginPanelButton != null)
            {
                closeLoginPanelButton.onClick.RemoveAllListeners();
                closeLoginPanelButton.onClick.AddListener(CloseLoginPanel);
            }

            if (closeSignUpPanelButton != null)
            {
                closeSignUpPanelButton.onClick.RemoveAllListeners();
                closeSignUpPanelButton.onClick.AddListener(CloseSignUpPanel);
            }

            if (signOutButton != null)
            {
                signOutButton.onClick.RemoveAllListeners();
                signOutButton.onClick.AddListener(OnSignOutClicked);
            }
        }

        private async void OnGoogleSignInClicked()
        {
            if (!ValidateServiceReady())
            {
                return;
            }

            SetButtonsInteractable(false);

            try
            {
                var result = await authService.SignInWithGoogleAsync();

                if (result.Success)
                {
                    // Không hiển thị message thành công vì sẽ chuyển scene ngay
                    UpdateUserInfo(result.User);
                    
                    // Chuyển đến scene menu sau khi đăng nhập thành công
                    StartCoroutine(LoadMenuSceneAfterDelay());
                }
                else
                {
                    SetStatus($"Đăng nhập thất bại: {result.ErrorMessage}", true);
                }
            }
            catch (System.Exception e)
            {
                SetStatus($"Lỗi: {e.Message}", true);
                Debug.LogError($"[UILoginManager] Google sign-in error: {e}");
            }
            finally
            {
                SetButtonsInteractable(true);
            }
        }

        private async void OnEmailSignInClicked()
        {
            if (!ValidateServiceReady())
            {
                return;
            }

            if (!ValidateLoginInputs())
            {
                return;
            }

            // Convert username to email format if needed (Firebase requires email format)
            string username = loginUsernameInputField.text.Trim();
            string email = ConvertUsernameToEmail(username);
            string password = loginPasswordInputField.text;

            SetButtonsInteractable(false);

            try
            {
                var result = await authService.SignInWithEmailAsync(email, password);

                if (result.Success)
                {
                    // Không hiển thị message thành công vì sẽ chuyển scene ngay
                    UpdateUserInfo(result.User);
                    ClearPasswordField();
                    
                    // Chuyển đến scene menu sau khi đăng nhập thành công
                    StartCoroutine(LoadMenuSceneAfterDelay());
                }
                else
                {
                    SetStatus($"Đăng nhập thất bại: {result.ErrorMessage}", true);
                }
            }
            catch (System.Exception e)
            {
                SetStatus($"Lỗi: {e.Message}", true);
                Debug.LogError($"[UILoginManager] Email sign-in error: {e}");
            }
            finally
            {
                SetButtonsInteractable(true);
            }
        }

        private async void OnSignUpClicked()
        {
            if (!ValidateServiceReady())
            {
                return;
            }

            if (!ValidateSignUpInputs())
            {
                return;
            }

            // Convert username to email format if needed (Firebase requires email format)
            string username = signUpUsernameInputField.text.Trim();
            string email = ConvertUsernameToEmail(username);
            string password = signUpPasswordInputField.text;

            SetButtonsInteractable(false);

            try
            {
                var result = await authService.SignUpWithEmailAsync(email, password);

                if (result.Success)
                {
                    // Không hiển thị message thành công vì sẽ chuyển scene ngay
                    UpdateUserInfo(result.User);
                    ClearSignUpFields();
                    
                    // Chuyển đến scene menu sau khi đăng ký và đăng nhập thành công
                    StartCoroutine(LoadMenuSceneAfterDelay());
                }
                else
                {
                    SetStatus($"Tạo tài khoản thất bại: {result.ErrorMessage}", true);
                }
            }
            catch (System.Exception e)
            {
                SetStatus($"Lỗi: {e.Message}", true);
                Debug.LogError($"[UILoginManager] Sign-up error: {e}");
            }
            finally
            {
                SetButtonsInteractable(true);
            }
        }

        private async void OnSignOutClicked()
        {
            if (!ValidateServiceReady() || !authService.IsAuthenticated)
            {
                return;
            }

            SetButtonsInteractable(false);

            try
            {
                await authService.SignOutAsync();
                // Không hiển thị message thành công, chỉ update UI
                UpdateUserInfo(null);
                ClearPasswordField();
                
                // Force update UI to show main menu panel and enable buttons
                UpdateUI();
                
                // Ensure main menu panel is visible với animation
                if (userInfoPanel != null && userInfoPanel.activeSelf)
                {
                    UIAnimationHelper.HidePanel(userInfoPanel, panelAnimationType, panelTransitionDuration);
                }
                if (loginPanel != null && loginPanel.activeSelf)
                {
                    UIAnimationHelper.HidePanel(loginPanel, panelAnimationType, panelTransitionDuration);
                }
                if (signUpPanel != null && signUpPanel.activeSelf)
                {
                    UIAnimationHelper.HidePanel(signUpPanel, panelAnimationType, panelTransitionDuration);
                }
                if (mainMenuPanel != null && !mainMenuPanel.activeSelf)
                {
                    UIAnimationHelper.ShowPanel(mainMenuPanel, panelAnimationType, panelTransitionDuration);
                }
                
                Debug.Log("[UILoginManager] ✅ Đăng xuất thành công. Main menu panel đã được hiển thị.");
            }
            catch (System.Exception e)
            {
                SetStatus($"Lỗi khi đăng xuất: {e.Message}", true);
                Debug.LogError($"[UILoginManager] Sign-out error: {e}");
            }
            finally
            {
                SetButtonsInteractable(true);
            }
        }

        private void OnAuthStateChanged(bool isAuthenticated)
        {
            bool serviceInitialized = authService?.IsInitialized ?? false;
            Debug.Log($"[UILoginManager] Auth state changed: {isAuthenticated}, Service Initialized: {serviceInitialized}");
            
            // Nếu phát hiện user đã authenticated từ session cũ (auto login), đăng xuất ngay
            // Đảm bảo user phải đăng nhập mỗi lần vào game
            // Chỉ đăng xuất một lần để tránh loop
            if (serviceInitialized && isAuthenticated && !hasCheckedButtonsAfterInit && !hasAutoSignedOut)
            {
                Debug.Log("[UILoginManager] ⚠️ Phát hiện auto login từ session cũ. Đang đăng xuất để yêu cầu đăng nhập lại...");
                hasAutoSignedOut = true; // Đánh dấu đã đăng xuất auto login
                StartCoroutine(SignOutAndUpdateUI());
                return; // Không update UI cho đến khi đăng xuất xong
            }
            
            // Update UI when auth state changes (đặc biệt quan trọng khi service vừa initialized)
            if (autoUpdateUI)
            {
                // Force update UI để enable buttons nếu service đã initialized
                UpdateUI();
                
                // Nếu service đã initialized và user chưa authenticated, force enable buttons
                if (serviceInitialized && !isAuthenticated)
                {
                    ForceEnableLoginButtons();
                }
                
                // Reset flag để có thể check lại nếu cần
                if (serviceInitialized)
                {
                    hasCheckedButtonsAfterInit = true;
                    // Log button states for debugging
                    Debug.Log($"[UILoginManager] Buttons state after initialization - " +
                             $"Google SignIn: {googleSignInButton?.interactable ?? false}, " +
                             $"Email SignIn: {emailSignInButton?.interactable ?? false}, " +
                             $"SignUp: {signUpConfirmButton?.interactable ?? false}");
                }
            }
            
            // Nếu service vừa mới initialized, chỉ log không show popup
            if (serviceInitialized)
            {
                if (isAuthenticated)
                {
                    // User đã authenticated từ session trước - đã được xử lý ở trên
                    Debug.Log("[UILoginManager] User đã authenticated, nhưng sẽ được đăng xuất để yêu cầu đăng nhập lại.");
                }
                else
                {
                    // Chỉ log không show popup khi ready
                    Debug.Log("[UILoginManager] ✅ Service đã initialized, buttons đã được enable!");
                }
            }
        }
    
        private void OnSignInSuccess(UserInfo user)
        {
            Debug.Log($"[UILoginManager] Sign in success: {user?.Email ?? "Unknown"}");
            UpdateUserInfo(user);
        }

        private void OnSignInFailed(string error)
        {
            Debug.LogError($"[UILoginManager] Sign in failed: {error}");
            SetStatus($"Lỗi đăng nhập: {error}", true);
        }

        private void UpdateUI()
        {
            if (!isServiceReady || authService == null)
            {
                // If service is not ready, disable all buttons (except panel switch buttons)
                SetButtonsInteractable(false);
                Debug.Log("[UILoginManager] UpdateUI: Service not ready yet");
                return;
            }

            bool isAuthenticated = authService.IsAuthenticated;
            bool serviceInitialized = authService.IsInitialized;

            // Log để debug
            if (!serviceInitialized)
            {
                Debug.Log("[UILoginManager] UpdateUI: Service not initialized yet - buttons will be disabled");
            }
            else
            {
                Debug.Log($"[UILoginManager] UpdateUI: Service initialized - enabling login/signup buttons. Authenticated: {isAuthenticated}");
            }

            // Update button states - chỉ enable khi service đã initialized và chưa authenticated
            bool shouldEnableLoginButtons = !isAuthenticated && serviceInitialized;
            
            Debug.Log($"[UILoginManager] Button state calculation - " +
                     $"isAuthenticated: {isAuthenticated}, " +
                     $"serviceInitialized: {serviceInitialized}, " +
                     $"shouldEnableLoginButtons: {shouldEnableLoginButtons}");
            
            if (googleSignInButton != null)
            {
                googleSignInButton.interactable = shouldEnableLoginButtons;
                Debug.Log($"[UILoginManager] Google SignIn button interactable set to: {googleSignInButton.interactable}");
            }
            else
            {
                Debug.LogWarning("[UILoginManager] googleSignInButton is null!");
            }

            if (emailSignInButton != null)
            {
                emailSignInButton.interactable = shouldEnableLoginButtons;
                Debug.Log($"[UILoginManager] Email SignIn button interactable set to: {emailSignInButton.interactable}");
            }
            else
            {
                Debug.LogWarning("[UILoginManager] emailSignInButton is null!");
            }

            if (signUpConfirmButton != null)
            {
                signUpConfirmButton.interactable = shouldEnableLoginButtons;
                Debug.Log($"[UILoginManager] SignUp button interactable set to: {signUpConfirmButton.interactable}");
            }
            else
            {
                Debug.LogWarning("[UILoginManager] signUpConfirmButton is null!");
            }

            // Panel switch buttons should always be enabled (to allow navigation between panels)
            // even when service is not initialized yet
            if (switchToSignUpButton != null)
            {
                switchToSignUpButton.interactable = !isAuthenticated;
            }

            if (switchToLoginButton != null)
            {
                switchToLoginButton.interactable = !isAuthenticated;
            }

            if (signOutButton != null)
            {
                signOutButton.interactable = isAuthenticated;
            }

            // Update panel visibility với animation mượt mà
            if (isAuthenticated)
            {
                // Hide all panels when authenticated and show user info panel
                if (mainMenuPanel != null && mainMenuPanel.activeSelf)
                {
                    UIAnimationHelper.HidePanel(mainMenuPanel, panelAnimationType, panelTransitionDuration);
                }
                if (loginPanel != null && loginPanel.activeSelf)
                {
                    UIAnimationHelper.HidePanel(loginPanel, panelAnimationType, panelTransitionDuration);
                }
                if (signUpPanel != null && signUpPanel.activeSelf)
                {
                    UIAnimationHelper.HidePanel(signUpPanel, panelAnimationType, panelTransitionDuration);
                }
                if (userInfoPanel != null && !userInfoPanel.activeSelf)
                {
                    UIAnimationHelper.ShowPanel(userInfoPanel, panelAnimationType, panelTransitionDuration);
                }
            }
            else
            {
                // Show appropriate panel based on current state
                // (Don't change active panel, just ensure userInfoPanel is hidden)
                if (userInfoPanel != null && userInfoPanel.activeSelf)
                {
                    UIAnimationHelper.HidePanel(userInfoPanel, panelAnimationType, panelTransitionDuration);
                }
            }

            // Update user info if authenticated
            if (isAuthenticated && authService.CurrentUser != null)
            {
                UpdateUserInfo(authService.CurrentUser);
            }
            else
            {
                UpdateUserInfo(null);
            }
        }

        private void SetStatus(string message, bool isError = false)
        {
            // Luôn cố gắng hiển thị message panel
            if (MessagePanel.IsReady())
            {
                MessagePanel.instance.ShowMessage(message, isError);
            }
            else
            {
                // Nếu MessagePanel chưa ready, thử tìm trong scene
                MessagePanel messagePanel = FindObjectOfType<MessagePanel>();
                if (messagePanel != null)
                {
                    messagePanel.ShowMessage(message, isError);
                }
                else
                {
                    // Nếu vẫn không tìm thấy, log warning nhưng vẫn log message
                    Debug.LogWarning($"[UILoginManager] MessagePanel chưa được khởi tạo. Message: {message}");
                }
            }
            Debug.Log($"[UILoginManager] {message}");
        }

        private void UpdateUserInfo(UserInfo user)
        {
            if (userInfoText != null)
            {
                if (user != null)
                {
                    userInfoText.text = $"Đã đăng nhập:\n" +
                                      $"Email: {user.Email ?? "N/A"}\n" +
                                      $"Tên: {user.DisplayName ?? "N/A"}\n" +
                                      $"UID: {user.UID ?? "N/A"}";
                }
                else
                {
                    userInfoText.text = "Chưa đăng nhập";
                }
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            // Chỉ set interactable nếu service đã initialized
            bool serviceReady = authService != null && authService.IsInitialized;
            bool isAuthenticated = authService?.IsAuthenticated ?? false;

            if (googleSignInButton != null)
            {
                googleSignInButton.interactable = interactable && !isAuthenticated && serviceReady;
            }

            if (emailSignInButton != null)
            {
                emailSignInButton.interactable = interactable && !isAuthenticated && serviceReady;
            }

            if (signUpConfirmButton != null)
            {
                signUpConfirmButton.interactable = interactable && !isAuthenticated && serviceReady;
            }

            // Panel switch buttons should always be enabled (to allow navigation between panels)
            // even when service is not ready or during operations
            if (switchToSignUpButton != null)
            {
                switchToSignUpButton.interactable = interactable && !isAuthenticated;
            }

            if (switchToLoginButton != null)
            {
                switchToLoginButton.interactable = interactable && !isAuthenticated;
            }

            if (signOutButton != null)
            {
                signOutButton.interactable = interactable && isAuthenticated;
            }
        }

        private bool ValidateServiceReady()
        {
            if (!isServiceReady)
            {
                SetStatus("Service chưa sẵn sàng. Vui lòng đợi...", true);
                return false;
            }
            
            if (authService == null)
            {
                SetStatus("Authentication service không tồn tại. Vui lòng kiểm tra lại.", true);
                // Try to get service again
                authService = ServiceLocator.Instance.GetService<IAuthService>();
                if (authService == null)
                {
                    return false;
                }
                // Re-initialize UI if we got the service
                InitializeUI();
            }
            
            if (!authService.IsInitialized)
            {
                // Không hiển thị message "đang khởi tạo", chỉ log
                Debug.Log("[UILoginManager] Service đang khởi tạo. Vui lòng đợi...");
                return false;
            }
            
            return true;
        }

        private bool ValidateLoginInputs()
        {
            if (loginUsernameInputField == null || loginPasswordInputField == null)
            {
                SetStatus("Lỗi: Input fields không được cấu hình", true);
                return false;
            }

            string username = loginUsernameInputField.text.Trim();
            string password = loginPasswordInputField.text;

            if (string.IsNullOrEmpty(username))
            {
                SetStatus("Vui lòng nhập tên đăng nhập", true);
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                SetStatus("Vui lòng nhập mật khẩu", true);
                return false;
            }

            // Validate username format (can be email or username@domain format)
            string email = ConvertUsernameToEmail(username);
            if (!IsValidEmailFormat(email))
            {
                SetStatus("Tên đăng nhập không hợp lệ. Vui lòng nhập email hoặc username@domain", true);
                return false;
            }

            // Password length validation
            if (password.Length < 6)
            {
                SetStatus("Mật khẩu phải có ít nhất 6 ký tự", true);
                return false;
            }

            return true;
        }

        private bool ValidateSignUpInputs()
        {
            if (signUpUsernameInputField == null || signUpPasswordInputField == null)
            {
                SetStatus("Lỗi: Input fields không được cấu hình", true);
                return false;
            }

            string username = signUpUsernameInputField.text.Trim();
            string password = signUpPasswordInputField.text;

            if (string.IsNullOrEmpty(username))
            {
                SetStatus("Vui lòng nhập tên đăng nhập", true);
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                SetStatus("Vui lòng nhập mật khẩu", true);
                return false;
            }

            // Validate username format
            string email = ConvertUsernameToEmail(username);
            if (!IsValidEmailFormat(email))
            {
                SetStatus("Tên đăng nhập không hợp lệ. Vui lòng nhập email hoặc username@domain", true);
                return false;
            }

            // Password length validation
            if (password.Length < 6)
            {
                SetStatus("Mật khẩu phải có ít nhất 6 ký tự", true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Convert username to email format for Firebase Auth
        /// If username doesn't contain '@', append @default.com
        /// </summary>
        private string ConvertUsernameToEmail(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return username;
            }

            // If already contains @, assume it's already an email
            if (username.Contains("@"))
            {
                return username;
            }

            // Otherwise, append default domain (you can change this)
            return $"{username}@default.com";
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        private bool IsValidEmailFormat(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }

            // Basic email validation
            if (!email.Contains("@") || !email.Contains("."))
            {
                return false;
            }

            // Check that @ comes before the last dot
            int atIndex = email.IndexOf("@");
            int lastDotIndex = email.LastIndexOf(".");
            
            if (atIndex <= 0 || lastDotIndex <= atIndex || lastDotIndex >= email.Length - 1)
            {
                return false;
            }

            return true;
        }

        private void ClearPasswordField()
        {
            if (loginPasswordInputField != null)
            {
                loginPasswordInputField.text = "";
            }
        }

        private void ClearSignUpFields()
        {
            if (signUpUsernameInputField != null)
            {
                signUpUsernameInputField.text = "";
            }
            if (signUpPasswordInputField != null)
            {
                signUpPasswordInputField.text = "";
            }
        }

        /// <summary>
        /// Open Login panel from main menu với animation mượt mà
        /// </summary>
        public void OpenLoginPanel()
        {
            // Kill any existing animations
            if (mainMenuPanel != null) UIAnimationHelper.KillTweens(mainMenuPanel);
            if (loginPanel != null) UIAnimationHelper.KillTweens(loginPanel);
            if (signUpPanel != null) UIAnimationHelper.KillTweens(signUpPanel);
            
            // Switch from main menu to login panel với animation
            UIAnimationHelper.SwitchPanels(
                hidePanel: mainMenuPanel,
                showPanel: loginPanel,
                duration: panelTransitionDuration,
                onComplete: () =>
                {
                    // Update button states
                    if (switchToSignUpButton != null)
                    {
                        switchToSignUpButton.interactable = true; // Can switch to signup
                    }
                    
                    if (switchToLoginButton != null)
                    {
                        switchToLoginButton.interactable = false; // Currently on login panel
                    }

                    // Clear status when switching
                    ClearStatus();
                }
            );
        }

        /// <summary>
        /// Close Login panel and return to main menu với animation mượt mà
        /// </summary>
        public void CloseLoginPanel()
        {
            // Kill any existing animations
            if (mainMenuPanel != null) UIAnimationHelper.KillTweens(mainMenuPanel);
            if (loginPanel != null) UIAnimationHelper.KillTweens(loginPanel);
            
            // Switch from login panel to main menu với animation
            UIAnimationHelper.SwitchPanels(
                hidePanel: loginPanel,
                showPanel: mainMenuPanel,
                duration: panelTransitionDuration,
                onComplete: () =>
                {
                    // Clear status when switching
                    ClearStatus();
                }
            );
        }

        /// <summary>
        /// Close Sign Up panel and return to main menu với animation mượt mà
        /// </summary>
        public void CloseSignUpPanel()
        {
            // Kill any existing animations
            if (mainMenuPanel != null) UIAnimationHelper.KillTweens(mainMenuPanel);
            if (signUpPanel != null) UIAnimationHelper.KillTweens(signUpPanel);
            
            // Switch from signup panel to main menu với animation
            UIAnimationHelper.SwitchPanels(
                hidePanel: signUpPanel,
                showPanel: mainMenuPanel,
                duration: panelTransitionDuration,
                onComplete: () =>
                {
                    // Clear status when switching
                    ClearStatus();
                }
            );
        }

        /// <summary>
        /// Switch to Sign Up panel với animation mượt mà
        /// </summary>
        public void SwitchToSignUpPanel()
        {
            // Kill any existing animations
            if (loginPanel != null) UIAnimationHelper.KillTweens(loginPanel);
            if (signUpPanel != null) UIAnimationHelper.KillTweens(signUpPanel);
            
            // Switch panels với animation
            UIAnimationHelper.SwitchPanels(
                hidePanel: loginPanel,
                showPanel: signUpPanel,
                duration: panelTransitionDuration,
                onComplete: () =>
                {
                    // Update button states
                    if (switchToSignUpButton != null)
                    {
                        switchToSignUpButton.interactable = false; // Currently on signup panel
                    }
                    
                    if (switchToLoginButton != null)
                    {
                        switchToLoginButton.interactable = true; // Can switch to login
                    }

                    // Clear status when switching
                    ClearStatus();
                }
            );
        }

        /// <summary>
        /// Switch to Login panel với animation mượt mà
        /// </summary>
        public void SwitchToLoginPanel()
        {
            // Kill any existing animations
            if (loginPanel != null) UIAnimationHelper.KillTweens(loginPanel);
            if (signUpPanel != null) UIAnimationHelper.KillTweens(signUpPanel);
            
            // Switch panels với animation
            UIAnimationHelper.SwitchPanels(
                hidePanel: signUpPanel,
                showPanel: loginPanel,
                duration: panelTransitionDuration,
                onComplete: () =>
                {
                    // Update button states
                    if (switchToLoginButton != null)
                    {
                        switchToLoginButton.interactable = false; // Currently on login panel
                    }
                    
                    if (switchToSignUpButton != null)
                    {
                        switchToSignUpButton.interactable = true; // Can switch to signup
                    }

                    // Clear status when switching
                    ClearStatus();
                }
            );
        }

        /// <summary>
        /// Public method to manually refresh UI state
        /// </summary>
        public void RefreshUI()
        {
            UpdateUI();
        }

        /// <summary>
        /// Public method to manually trigger auto sign out (useful for testing)
        /// This will sign out the current user and enable login/signup buttons
        /// </summary>
        public void TriggerAutoSignOut()
        {
            if (authService == null || !authService.IsInitialized)
            {
                Debug.LogWarning("[UILoginManager] Cannot trigger auto sign out: Service not initialized");
                return;
            }

            if (!authService.IsAuthenticated)
            {
                Debug.Log("[UILoginManager] User is not authenticated, no need to sign out");
                // Buttons should already be enabled
                ForceEnableLoginButtons();
                return;
            }

            Debug.Log("[UILoginManager] Manually triggering auto sign out...");
            StartCoroutine(SignOutAndUpdateUI());
        }

        /// <summary>
        /// Force enable login/signup buttons if service is initialized and user is not authenticated
        /// This method ensures buttons are enabled even if there was a timing issue
        /// </summary>
        private void ForceEnableLoginButtons()
        {
            if (authService == null || !authService.IsInitialized)
            {
                Debug.Log("[UILoginManager] Cannot force enable buttons: Service not initialized");
                return;
            }

            if (authService.IsAuthenticated)
            {
                Debug.Log("[UILoginManager] Cannot force enable buttons: User is already authenticated");
                return;
            }

            Debug.Log("[UILoginManager] Force enabling login/signup buttons...");
            
            if (googleSignInButton != null)
            {
                googleSignInButton.interactable = true;
                Debug.Log("[UILoginManager] ✅ Google SignIn button force enabled");
            }

            if (emailSignInButton != null)
            {
                emailSignInButton.interactable = true;
                Debug.Log("[UILoginManager] ✅ Email SignIn button force enabled");
            }

            if (signUpConfirmButton != null)
            {
                signUpConfirmButton.interactable = true;
                Debug.Log("[UILoginManager] ✅ SignUp button force enabled");
            }
        }

        /// <summary>
        /// Clear status message manually
        /// </summary>
        public void ClearStatus()
        {
            // Luôn cố gắng clear message panel
            if (MessagePanel.IsReady())
            {
                MessagePanel.instance.ClearMessage();
            }
            else
            {
                // Nếu MessagePanel chưa ready, thử tìm trong scene
                MessagePanel messagePanel = FindObjectOfType<MessagePanel>();
                if (messagePanel != null)
                {
                    messagePanel.ClearMessage();
                }
            }
        }

        /// <summary>
        /// Coroutine để chuyển đến scene menu sau khi hiển thị loading panel
        /// </summary>
        private IEnumerator LoadMenuSceneAfterDelay()
        {
            // Kiểm tra nếu đã có tên scene menu được cấu hình
            if (string.IsNullOrEmpty(menuSceneName))
            {
                Debug.LogWarning("[UILoginManager] Menu scene name chưa được cấu hình. Không chuyển scene.");
                yield break;
            }
            
            // Hide all panels first
            if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            {
                UIAnimationHelper.HidePanel(mainMenuPanel, panelAnimationType, panelTransitionDuration);
            }
            if (loginPanel != null && loginPanel.activeSelf)
            {
                UIAnimationHelper.HidePanel(loginPanel, panelAnimationType, panelTransitionDuration);
            }
            if (signUpPanel != null && signUpPanel.activeSelf)
            {
                UIAnimationHelper.HidePanel(signUpPanel, panelAnimationType, panelTransitionDuration);
            }
            if (userInfoPanel != null && userInfoPanel.activeSelf)
            {
                UIAnimationHelper.HidePanel(userInfoPanel, panelAnimationType, panelTransitionDuration);
            }
            
            // Wait for panels to hide
            yield return new WaitForSeconds(panelTransitionDuration);
            
            // Prepare loading panel - ensure it's active and visible
            if (loadingPanel != null)
            {
                // Reset loading panel position and slider before showing
                ResetLoadingPanel();
                
                loadingPanel.SetActive(true);
                // Reset alpha nếu có CanvasGroup để đảm bảo animation hoạt động đúng
                CanvasGroup loadingCanvasGroup = loadingPanel.GetComponent<CanvasGroup>();
                if (loadingCanvasGroup != null)
                {
                    loadingCanvasGroup.alpha = 0f; // Start from invisible for slide animation
                }
                
                // Show loading panel with slide animation
                UIAnimationHelper.ShowPanel(loadingPanel, UIAnimationHelper.AnimationType.SlideBottom, panelTransitionDuration);
            }
            
            // Wait for loading panel slide animation to complete
            yield return new WaitForSeconds(panelTransitionDuration);
            
            // Animate loading slider from 0 to 1
            StartCoroutine(AnimateLoadingSlider(loginSuccessLoadingDuration));
            
            // Wait for login success loading duration (2 seconds)
            yield return new WaitForSeconds(loginSuccessLoadingDuration);
            
            // Chuyển đến scene menu
            try
            {
                Debug.Log($"[UILoginManager] Đang chuyển đến scene: {menuSceneName}");
                SceneManager.LoadScene(menuSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UILoginManager] Lỗi khi chuyển scene: {e.Message}");
                SetStatus($"Lỗi khi chuyển scene: {e.Message}", true);
            }
        }

        /// <summary>
        /// Reset loading panel position to original position
        /// </summary>
        private void ResetLoadingPanel()
        {
            if (loadingPanel != null)
            {
                // Kill any existing tweens
                UIAnimationHelper.KillTweens(loadingPanel);
                
                // Reset position to original
                RectTransform loadingRectTransform = loadingPanel.GetComponent<RectTransform>();
                if (loadingRectTransform != null)
                {
                    loadingRectTransform.anchoredPosition = loadingPanelOriginalPosition;
                }
                
                // Reset slider
                if (loadingSlider != null)
                {
                    loadingSlider.value = 0f;
                }
                
                // Reset loading slider text
                if (loadingSliderText != null)
                {
                    loadingSliderText.text = "0%";
                }
            }
        }

        /// <summary>
        /// Animate loading slider from 0 to 1 over duration
        /// </summary>
        private IEnumerator AnimateLoadingSlider(float duration)
        {
            if (loadingSlider == null) yield break;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                
                // Update slider value
                loadingSlider.value = progress;
                
                // Update slider text with percentage
                if (loadingSliderText != null)
                {
                    int percentage = Mathf.RoundToInt(progress * 100f);
                    loadingSliderText.text = $"{percentage}%";
                }
                
                yield return null;
            }
            
            // Ensure slider is at 100%
            loadingSlider.value = 1f;
            if (loadingSliderText != null)
            {
                loadingSliderText.text = "100%";
            }
        }

        private void OnDestroy()
        {
            // Kill all tweens khi destroy
            if (loadingPanel != null) UIAnimationHelper.KillTweens(loadingPanel);
            if (mainMenuPanel != null) UIAnimationHelper.KillTweens(mainMenuPanel);
            if (loginPanel != null) UIAnimationHelper.KillTweens(loginPanel);
            if (signUpPanel != null) UIAnimationHelper.KillTweens(signUpPanel);
            if (userInfoPanel != null) UIAnimationHelper.KillTweens(userInfoPanel);
            
            if (authService != null)
            {
                authService.OnAuthStateChanged -= OnAuthStateChanged;
                authService.OnSignInSuccess -= OnSignInSuccess;
                authService.OnSignInFailed -= OnSignInFailed;
            }
        }
    }
}

