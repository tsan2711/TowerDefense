using System.Threading.Tasks;

namespace Services.Core
{
    /// <summary>
    /// Service interface for managing User Data
    /// Microservice: Handles only User Data domain
    /// </summary>
    public interface IUserDataService : IService
    {
        /// <summary>
        /// Save or update user data in Firestore users collection
        /// Collection will be created automatically if it doesn't exist
        /// </summary>
        /// <param name="userInfo">User information to save</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveUserDataAsync(UserInfo userInfo);

        /// <summary>
        /// Load user data by UID
        /// </summary>
        /// <param name="uid">User UID</param>
        /// <returns>UserInfo if found, null otherwise</returns>
        Task<UserInfo> LoadUserDataAsync(string uid);
    }
}

