using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_FIRESTORE_AVAILABLE
using Firebase.Firestore;
#endif
using Services.Core;
using Services.Data;

namespace Services.Firestore
{
    /// <summary>
    /// Microservice for managing User Data
    /// Handles only User Data domain operations
    /// </summary>
    public class UserDataService : FirestoreServiceBase, IUserDataService
    {
        private const string DEFAULT_COLLECTION_USERS = "users";
        private string COLLECTION_USERS => DEFAULT_COLLECTION_USERS;

        protected override string GetServiceName() => "UserDataService";

        public async Task<bool> SaveUserDataAsync(UserInfo userInfo)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return false;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return false;
            }

            if (userInfo == null || string.IsNullOrEmpty(userInfo.UID))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid user info: UID is required");
                return false;
            }

            try
            {
                DocumentReference userDocRef = firestore.Collection(COLLECTION_USERS).Document(userInfo.UID);

                Dictionary<string, object> userData = new Dictionary<string, object>
                {
                    { "uid", userInfo.UID },
                    { "email", userInfo.Email ?? "" },
                    { "displayName", userInfo.DisplayName ?? "" },
                    { "photoURL", userInfo.PhotoURL ?? "" },
                    { "providerId", userInfo.ProviderId ?? "" },
                    { "lastLoginAt", Timestamp.GetCurrentTimestamp() },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                DocumentSnapshot existingDoc = await userDocRef.GetSnapshotAsync();
                
                if (existingDoc.Exists)
                {
                    Dictionary<string, object> updateData = new Dictionary<string, object>
                    {
                        { "email", userInfo.Email ?? "" },
                        { "displayName", userInfo.DisplayName ?? "" },
                        { "photoURL", userInfo.PhotoURL ?? "" },
                        { "providerId", userInfo.ProviderId ?? "" },
                        { "lastLoginAt", Timestamp.GetCurrentTimestamp() },
                        { "updatedAt", Timestamp.GetCurrentTimestamp() }
                    };

                    await userDocRef.UpdateAsync(updateData);
                    Debug.Log($"[{GetServiceName()}] Updated user data for UID: {userInfo.UID}");
                }
                else
                {
                    userData["createdAt"] = Timestamp.GetCurrentTimestamp();
                    await userDocRef.SetAsync(userData);
                    Debug.Log($"[{GetServiceName()}] Created user data for UID: {userInfo.UID}");
                }

                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when saving user data.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error saving user data: {errorMsg}");
                }
                return false;
            }
#endif
        }

        public async Task<UserInfo> LoadUserDataAsync(string uid)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return null;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return null;
            }

            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid UID");
                return null;
            }

            try
            {
                DocumentReference userDocRef = firestore.Collection(COLLECTION_USERS).Document(uid);
                DocumentSnapshot snapshot = await userDocRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    UserInfo userInfo = ParseUserData(data);
                    Debug.Log($"[{GetServiceName()}] Loaded user data for UID: {uid}");
                    return userInfo;
                }
                else
                {
                    Debug.LogWarning($"[{GetServiceName()}] User data not found for UID: {uid}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error loading user data: {e.Message}");
                return null;
            }
#endif
        }

#if FIREBASE_FIRESTORE_AVAILABLE
        private UserInfo ParseUserData(Dictionary<string, object> data)
        {
            try
            {
                UserInfo userInfo = new UserInfo();
                
                if (data.ContainsKey("uid") && data["uid"] != null)
                {
                    userInfo.UID = data["uid"].ToString();
                }
                
                if (data.ContainsKey("email") && data["email"] != null)
                {
                    userInfo.Email = data["email"].ToString();
                }
                
                if (data.ContainsKey("displayName") && data["displayName"] != null)
                {
                    userInfo.DisplayName = data["displayName"].ToString();
                }
                
                if (data.ContainsKey("photoURL") && data["photoURL"] != null)
                {
                    userInfo.PhotoURL = data["photoURL"].ToString();
                }
                
                if (data.ContainsKey("providerId") && data["providerId"] != null)
                {
                    userInfo.ProviderId = data["providerId"].ToString();
                }

                return userInfo;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error parsing user data: {e.Message}");
                return null;
            }
        }

        public async Task<bool> SaveLevelProgressAsync(string uid, string levelId, int stars, int maxLevel)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return false;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return false;
            }

            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(levelId))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid parameters: uid and levelId are required");
                return false;
            }

            try
            {
                DocumentReference userDocRef = firestore.Collection(COLLECTION_USERS).Document(uid);

                // Get existing document to merge data
                DocumentSnapshot existingDoc = await userDocRef.GetSnapshotAsync();
                
                Dictionary<string, object> updateData = new Dictionary<string, object>
                {
                    { "updatedAt", Timestamp.GetCurrentTimestamp() },
                    { "maxLevel", maxLevel }
                };

                Dictionary<string, object> levelProgress;
                if (existingDoc.Exists && existingDoc.ContainsField("levelProgress"))
                {
                    // Convert from Firestore's dictionary representation
                    Dictionary<string, object> docDict = existingDoc.ToDictionary();
                    if (docDict.ContainsKey("levelProgress") && docDict["levelProgress"] is Dictionary<string, object> progressDict)
                    {
                        levelProgress = progressDict;
                    }
                    else
                    {
                        levelProgress = new Dictionary<string, object>();
                    }
                }
                else
                {
                    levelProgress = new Dictionary<string, object>();
                }

                // Update level progress: store as { levelId: stars }
                Dictionary<string, object> levelData = new Dictionary<string, object>
                {
                    { "stars", stars }
                };

                levelProgress[levelId] = levelData;
                updateData["levelProgress"] = levelProgress;

                if (existingDoc.Exists)
                {
                    await userDocRef.UpdateAsync(updateData);
                    Debug.Log($"[{GetServiceName()}] Updated level progress for user {uid}: level {levelId} = {stars} stars, maxLevel = {maxLevel}");
                }
                else
                {
                    updateData["uid"] = uid;
                    updateData["createdAt"] = Timestamp.GetCurrentTimestamp();
                    await userDocRef.SetAsync(updateData);
                    Debug.Log($"[{GetServiceName()}] Created level progress for user {uid}: level {levelId} = {stars} stars, maxLevel = {maxLevel}");
                }

                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when saving level progress.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error saving level progress: {errorMsg}");
                }
                return false;
            }
#endif
        }

        public async Task<(Dictionary<string, int> levelStars, int maxLevel)> LoadLevelProgressAsync(string uid)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return (new Dictionary<string, int>(), 0);
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return (new Dictionary<string, int>(), 0);
            }

            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid UID");
                return (new Dictionary<string, int>(), 0);
            }

            try
            {
                DocumentReference userDocRef = firestore.Collection(COLLECTION_USERS).Document(uid);
                DocumentSnapshot snapshot = await userDocRef.GetSnapshotAsync();

                Dictionary<string, int> levelStars = new Dictionary<string, int>();
                int maxLevel = 0;

                if (snapshot.Exists)
                {
                    // Load maxLevel
                    if (snapshot.ContainsField("maxLevel"))
                    {
                        maxLevel = snapshot.GetValue<int>("maxLevel");
                    }

                    // Load levelProgress
                    if (snapshot.ContainsField("levelProgress"))
                    {
                        Dictionary<string, object> snapshotDict = snapshot.ToDictionary();
                        if (snapshotDict.ContainsKey("levelProgress"))
                        {
                            var levelProgressValue = snapshotDict["levelProgress"];
                            if (levelProgressValue is Dictionary<string, object> levelProgress)
                            {
                                foreach (var kvp in levelProgress)
                                {
                                    string levelId = kvp.Key;
                                    if (kvp.Value is Dictionary<string, object> levelData)
                                    {
                                        if (levelData.ContainsKey("stars"))
                                        {
                                            int stars = Convert.ToInt32(levelData["stars"]);
                                            levelStars[levelId] = stars;
                                        }
                                    }
                                }
                            }
                        }

                        Debug.Log($"[{GetServiceName()}] Loaded level progress for user {uid}: {levelStars.Count} levels, maxLevel = {maxLevel}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[{GetServiceName()}] User data not found for UID: {uid}");
                }

                return (levelStars, maxLevel);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error loading level progress: {e.Message}");
                return (new Dictionary<string, int>(), 0);
            }
#endif
        }

        public async Task<bool> SaveLevelProgressAsync(string uid, UserLevelProgress levelProgress)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return false;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return false;
            }

            if (string.IsNullOrEmpty(uid) || levelProgress == null)
            {
                Debug.LogError($"[{GetServiceName()}] Invalid parameters: uid and levelProgress are required");
                return false;
            }

            try
            {
                DocumentReference userDocRef = firestore.Collection(COLLECTION_USERS).Document(uid);
                DocumentSnapshot existingDoc = await userDocRef.GetSnapshotAsync();

                Dictionary<string, object> updateData = new Dictionary<string, object>
                {
                    { "updatedAt", Timestamp.GetCurrentTimestamp() },
                    { "maxLevel", levelProgress.MaxLevel }
                };

                // Convert levelProgress.LevelStars to Firestore format
                Dictionary<string, object> levelProgressDict = new Dictionary<string, object>();
                if (levelProgress.LevelStars != null)
                {
                    foreach (var kvp in levelProgress.LevelStars)
                    {
                        Dictionary<string, object> levelData = new Dictionary<string, object>
                        {
                            { "stars", kvp.Value }
                        };
                        levelProgressDict[kvp.Key] = levelData;
                    }
                }

                updateData["levelProgress"] = levelProgressDict;

                if (existingDoc.Exists)
                {
                    await userDocRef.UpdateAsync(updateData);
                    Debug.Log($"[{GetServiceName()}] Updated level progress object for user {uid}: {levelProgress.LevelStars.Count} levels, maxLevel = {levelProgress.MaxLevel}");
                }
                else
                {
                    updateData["uid"] = uid;
                    updateData["createdAt"] = Timestamp.GetCurrentTimestamp();
                    await userDocRef.SetAsync(updateData);
                    Debug.Log($"[{GetServiceName()}] Created level progress object for user {uid}: {levelProgress.LevelStars.Count} levels, maxLevel = {levelProgress.MaxLevel}");
                }

                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when saving level progress object.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error saving level progress object: {errorMsg}");
                }
                return false;
            }
#endif
        }

        public async Task<UserLevelProgress> LoadLevelProgressObjectAsync(string uid)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return null;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return null;
            }

            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid UID");
                return null;
            }

            try
            {
                DocumentReference userDocRef = firestore.Collection(COLLECTION_USERS).Document(uid);
                DocumentSnapshot snapshot = await userDocRef.GetSnapshotAsync();

                UserLevelProgress levelProgress = new UserLevelProgress();

                if (snapshot.Exists)
                {
                    // Load maxLevel
                    if (snapshot.ContainsField("maxLevel"))
                    {
                        levelProgress.MaxLevel = snapshot.GetValue<int>("maxLevel");
                    }

                    // Load levelProgress
                    if (snapshot.ContainsField("levelProgress"))
                    {
                        Dictionary<string, object> snapshotDict = snapshot.ToDictionary();
                        if (snapshotDict.ContainsKey("levelProgress"))
                        {
                            var levelProgressValue = snapshotDict["levelProgress"];
                            if (levelProgressValue is Dictionary<string, object> levelProgressDict)
                            {
                                foreach (var kvp in levelProgressDict)
                                {
                                    string levelId = kvp.Key;
                                    if (kvp.Value is Dictionary<string, object> levelData)
                                    {
                                        if (levelData.ContainsKey("stars"))
                                        {
                                            int stars = Convert.ToInt32(levelData["stars"]);
                                            levelProgress.LevelStars[levelId] = stars;
                                        }
                                    }
                                }
                            }
                        }

                        Debug.Log($"[{GetServiceName()}] Loaded level progress object for user {uid}: {levelProgress.LevelStars.Count} levels, maxLevel = {levelProgress.MaxLevel}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[{GetServiceName()}] User data not found for UID: {uid}");
                    return levelProgress; // Return empty object instead of null
                }

                return levelProgress;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error loading level progress object: {e.Message}");
                return null;
            }
#endif
        }
#endif
    }
}

