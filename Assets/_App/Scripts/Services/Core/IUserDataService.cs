using System.Threading.Tasks;
using System.Collections.Generic;
using Services.Data;

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

        /// <summary>
        /// Save level progress (level completion and stars) to Firestore
        /// </summary>
        /// <param name="uid">User UID</param>
        /// <param name="levelId">Level ID</param>
        /// <param name="stars">Number of stars earned</param>
        /// <param name="maxLevel">Maximum level unlocked</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveLevelProgressAsync(string uid, string levelId, int stars, int maxLevel);

        /// <summary>
        /// Save full level progress object to Firestore
        /// </summary>
        /// <param name="uid">User UID</param>
        /// <param name="levelProgress">Level progress data</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveLevelProgressAsync(string uid, UserLevelProgress levelProgress);

        /// <summary>
        /// Load level progress from Firestore
        /// </summary>
        /// <param name="uid">User UID</param>
        /// <returns>Dictionary of levelId -> stars, and maxLevel</returns>
        Task<(Dictionary<string, int> levelStars, int maxLevel)> LoadLevelProgressAsync(string uid);

        /// <summary>
        /// Load level progress as UserLevelProgress object from Firestore
        /// </summary>
        /// <param name="uid">User UID</param>
        /// <returns>UserLevelProgress object if found, null otherwise</returns>
        Task<UserLevelProgress> LoadLevelProgressObjectAsync(string uid);
    }
}

