using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_FIRESTORE_AVAILABLE
using Firebase.Firestore;
#endif
using Services.Core;

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
#endif
    }
}

