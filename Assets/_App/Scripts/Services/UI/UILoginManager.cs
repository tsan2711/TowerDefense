using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Services.Core;
using Services.Managers;

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

        [Header("Sign Out")]
        [SerializeField] private Button signOutButton;

        [Header("Login Panel Inputs")]
        [SerializeField] private TMP_InputField loginUsernameInputField;
        [SerializeField] private TMP_InputField loginPasswordInputField;
        
        [Header("Sign Up Panel Inputs")]
        [SerializeField] private TMP_InputField signUpUsernameInputField;
        [SerializeField] private TMP_InputField signUpPasswordInputField;
        [SerializeField] private TMP_InputField signUpConfirmPasswordInputField;

        [Header("UI Display")]
        [SerializeField] private TextMeshProUGUI userInfoText;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject signUpPanel;
        [SerializeField] private GameObject userInfoPanel;

        [Header("Settings")]
        [SerializeField] private bool autoUpdateUI = true;
        [Tooltip("Nếu true, sẽ tự động đăng xuất session cũ khi khởi động để cho phép đăng nhập lại")]
        [SerializeField] private bool autoSignOutOnStart = false;
        
        [Header("Scene Navigation")]
        [Tooltip("Tên scene menu để chuyển đến sau khi đăng nhập thành công. Để trống nếu không muốn tự động chuyển scene.")]
        [SerializeField] private string menuSceneName = "MenuScene";
        [Tooltip("Delay (giây) trước khi chuyển scene sau khi đăng nhập thành công")]
        [SerializeField] private float sceneTransitionDelay = 1f;

        private IAuthService authService;
        private bool isServiceReady = false;
        private bool hasCheckedButtonsAfterInit = false; // Flag để chỉ check buttons một lần sau khi init

        private void Awake()
        {
            // Initialize UI - show login panel by default
            if (loginPanel != null)
            {
                loginPanel.SetActive(true);
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
                switchToLoginButton.interactable = false; // Already on login panel
            }
        }

        private void Start()
        {
            // Start coroutine to wait for service to be ready
            StartCoroutine(WaitForAuthService());
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
                
                // Update status mỗi 2 giây (thay vì logic phức tạp trước đó)
                if (elapsed - lastStatusUpdate >= 2f)
                {
                    SetStatus($"Đang khởi tạo authentication service... ({Mathf.FloorToInt(elapsed)}s)", false);
                    lastStatusUpdate = elapsed;
                }
            }
            
            if (!authService.IsInitialized)
            {
                Debug.LogWarning("[UILoginManager] Auth service is registered but not initialized yet after timeout.");
                SetStatus("Đang khởi tạo authentication service... (có thể mất thêm thời gian)", false);
                Debug.Log("[UILoginManager] UI đã được setup, sẽ tự động update khi service ready qua OnAuthStateChanged event.");
            }
            else
            {
                // Check if user is already authenticated (from previous session)
                if (authService.IsAuthenticated && autoSignOutOnStart)
                {
                    Debug.Log("[UILoginManager] User đã authenticated từ session cũ, đang tự động đăng xuất...");
                    SetStatus("Đang đăng xuất session cũ...", false);
                    // Start async sign out in a coroutine-friendly way
                    StartCoroutine(SignOutAndUpdateUI());
                }
                else
                {
                    // Service đã initialized trong loop, update UI ngay
                    if (autoUpdateUI)
                    {
                        UpdateUI();
                        if (!authService.IsAuthenticated)
                        {
                            SetStatus("Sẵn sàng đăng nhập", false);
                            // Force enable buttons để đảm bảo chúng được enable
                            ForceEnableLoginButtons();
                        }
                        else
                        {
                            // User đã authenticated, hiển thị thông tin và hướng dẫn đăng xuất
                            SetStatus("Đã đăng nhập từ session trước. Nhấn 'Đăng xuất' để đăng nhập lại.", false);
                            Debug.Log("[UILoginManager] User đã authenticated từ session cũ. Buttons login/signup bị disable. User có thể đăng xuất để đăng nhập lại.");
                        }
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
                    SetStatus("Sẵn sàng đăng nhập", false);
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

            SetStatus("Đang đăng nhập với Google...", false);
            SetButtonsInteractable(false);

            try
            {
                var result = await authService.SignInWithGoogleAsync();

                if (result.Success)
                {
                    SetStatus("Đăng nhập thành công!", false);
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

            SetStatus("Đang đăng nhập...", false);
            SetButtonsInteractable(false);

            try
            {
                var result = await authService.SignInWithEmailAsync(email, password);

                if (result.Success)
                {
                    SetStatus("Đăng nhập thành công!", false);
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

            SetStatus("Đang tạo tài khoản...", false);
            SetButtonsInteractable(false);

            try
            {
                var result = await authService.SignUpWithEmailAsync(email, password);

                if (result.Success)
                {
                    SetStatus("Tạo tài khoản thành công! Đã tự động đăng nhập.", false);
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

            SetStatus("Đang đăng xuất...", false);
            SetButtonsInteractable(false);

            try
            {
                await authService.SignOutAsync();
                SetStatus("Đã đăng xuất thành công. Sẵn sàng đăng nhập lại.", false);
                UpdateUserInfo(null);
                ClearPasswordField();
                
                // Force update UI to show login panel and enable buttons
                UpdateUI();
                
                // Ensure login panel is visible
                if (loginPanel != null)
                {
                    loginPanel.SetActive(true);
                }
                if (signUpPanel != null)
                {
                    signUpPanel.SetActive(false);
                }
                
                Debug.Log("[UILoginManager] ✅ Đăng xuất thành công. Login/signup buttons đã được enable.");
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
            
            // Nếu service vừa mới initialized, thông báo ready
            if (serviceInitialized)
            {
                if (isAuthenticated)
                {
                    // User đã authenticated từ session trước
                    SetStatus("Đã đăng nhập từ session trước. Nhấn 'Đăng xuất' để đăng nhập lại.", false);
                    Debug.Log("[UILoginManager] ✅ Service đã initialized nhưng user đã authenticated. Buttons login/signup bị disable. User có thể đăng xuất để đăng nhập lại.");
                }
                else
                {
                    SetStatus("Sẵn sàng đăng nhập", false);
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

            // Update panel visibility
            if (isAuthenticated)
            {
                // Hide both login and signup panels when authenticated
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
                    userInfoPanel.SetActive(true);
                }
            }
            else
            {
                // Show appropriate panel based on current state
                // (Don't change active panel, just ensure userInfoPanel is hidden)
                if (userInfoPanel != null)
                {
                    userInfoPanel.SetActive(false);
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
            if (MessagePanel.IsReady())
            {
                MessagePanel.instance.ShowMessage(message, isError);
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
                SetStatus("Service đang khởi tạo. Vui lòng đợi...", false);
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
            if (signUpUsernameInputField == null || signUpPasswordInputField == null || signUpConfirmPasswordInputField == null)
            {
                SetStatus("Lỗi: Input fields không được cấu hình", true);
                return false;
            }

            string username = signUpUsernameInputField.text.Trim();
            string password = signUpPasswordInputField.text;
            string confirmPassword = signUpConfirmPasswordInputField.text;

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

            if (string.IsNullOrEmpty(confirmPassword))
            {
                SetStatus("Vui lòng xác nhận mật khẩu", true);
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

            // Password match validation
            if (password != confirmPassword)
            {
                SetStatus("Mật khẩu xác nhận không khớp", true);
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
            if (signUpPasswordInputField != null)
            {
                signUpPasswordInputField.text = "";
            }
            if (signUpConfirmPasswordInputField != null)
            {
                signUpConfirmPasswordInputField.text = "";
            }
        }

        /// <summary>
        /// Switch to Sign Up panel
        /// </summary>
        public void SwitchToSignUpPanel()
        {
            if (loginPanel != null)
            {
                loginPanel.SetActive(false);
            }

            if (signUpPanel != null)
            {
                signUpPanel.SetActive(true);
            }
            
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

        /// <summary>
        /// Switch to Login panel
        /// </summary>
        public void SwitchToLoginPanel()
        {
            if (signUpPanel != null)
            {
                signUpPanel.SetActive(false);
            }

            if (loginPanel != null)
            {
                loginPanel.SetActive(true);
            }
            
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
            if (MessagePanel.IsReady())
            {
                MessagePanel.instance.ClearMessage();
            }
        }

        /// <summary>
        /// Coroutine để chuyển đến scene menu sau một khoảng delay
        /// </summary>
        private IEnumerator LoadMenuSceneAfterDelay()
        {
            // Kiểm tra nếu đã có tên scene menu được cấu hình
            if (string.IsNullOrEmpty(menuSceneName))
            {
                Debug.LogWarning("[UILoginManager] Menu scene name chưa được cấu hình. Không chuyển scene.");
                yield break;
            }
            
            // Đợi một khoảng thời gian trước khi chuyển scene
            yield return new WaitForSeconds(sceneTransitionDelay);
            
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

        private void OnDestroy()
        {
            if (authService != null)
            {
                authService.OnAuthStateChanged -= OnAuthStateChanged;
                authService.OnSignInSuccess -= OnSignInSuccess;
                authService.OnSignInFailed -= OnSignInFailed;
            }
        }
    }
}

