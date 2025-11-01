using System;
using System.Threading.Tasks;

namespace Services.Core
{
    /// <summary>
    /// Authentication service interface for handling user authentication
    /// Can be implemented by Firebase, custom backend, or other providers
    /// </summary>
    public interface IAuthService : IService
    {
        /// <summary>
        /// Event fired when authentication state changes
        /// </summary>
        event Action<bool> OnAuthStateChanged;

        /// <summary>
        /// Event fired when user signs in successfully
        /// </summary>
        event Action<UserInfo> OnSignInSuccess;

        /// <summary>
        /// Event fired when sign in fails
        /// </summary>
        event Action<string> OnSignInFailed;

        /// <summary>
        /// Check if user is currently authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Get current user information if authenticated
        /// </summary>
        UserInfo CurrentUser { get; }

        /// <summary>
        /// Sign in with Google
        /// </summary>
        Task<AuthResult> SignInWithGoogleAsync();

        /// <summary>
        /// Sign in with email and password
        /// </summary>
        Task<AuthResult> SignInWithEmailAsync(string email, string password);

        /// <summary>
        /// Sign up with email and password
        /// </summary>
        Task<AuthResult> SignUpWithEmailAsync(string email, string password);

        /// <summary>
        /// Sign out current user
        /// </summary>
        Task SignOutAsync();

        /// <summary>
        ///札Get authentication token for API calls
        /// </summary>
        Task<string> GetAuthTokenAsync();
    }

    /// <summary>
    /// User information model
    /// </summary>
    [Serializable]
    public class UserInfo
    {
        public string UID { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string PhotoURL { get; set; }
        public string ProviderId { get; set; }

        public UserInfo()
        {
        }

        public UserInfo(string uid, string email, string displayName, string photoURL, string providerId)
        {
            UID = uid;
            Email = email;
            DisplayName = displayName;
            PhotoURL = photoURL;
            ProviderId = providerId;
        }
    }

    /// <summary>
    /// Authentication result model
    /// </summary>
    [Serializable]
    public class AuthResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public UserInfo User { get; set; }

        public static AuthResult CreateSuccess(UserInfo user)
        {
            return new AuthResult
            {
                Success = true,
                User = user
            };
        }

        public static AuthResult CreateFailure(string errorMessage)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}

