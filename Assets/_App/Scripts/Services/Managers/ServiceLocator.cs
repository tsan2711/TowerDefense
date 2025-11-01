using System;
using System.Collections.Generic;
using UnityEngine;
using Services.Core;

namespace Services.Managers
{
    /// <summary>
    /// Service Locator pattern for managing microservices
    /// Allows easy access to services and supports dependency injection
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator instance;
        private Dictionary<Type, IService> services = new Dictionary<Type, IService>();
        private Dictionary<Type, MonoBehaviour> serviceMonoBehaviours = new Dictionary<Type, MonoBehaviour>();

        public static ServiceLocator Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("ServiceLocator");
                    instance = go.AddComponent<ServiceLocator>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        /// <summary>
        /// Register a service instance
        /// </summary>
        public void RegisterService<T>(T service) where T : class, IService
        {
            Type type = typeof(T);
            
            if (services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered. Replacing...");
                UnregisterService<T>();
            }

            services[type] = service;

            // If it's a MonoBehaviour, also track it
            if (service is MonoBehaviour mb)
            {
                serviceMonoBehaviours[type] = mb;
            }

            Debug.Log($"[ServiceLocator] Registered service: {type.Name}");
        }

        /// <summary>
        /// Get a service instance
        /// </summary>
        public T GetService<T>() where T : class, IService
        {
            Type type = typeof(T);
            
            if (services.TryGetValue(type, out IService service))
            {
                return service as T;
            }

            Debug.LogError($"[ServiceLocator] Service {type.Name} not found. Make sure it's registered.");
            return null;
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public void UnregisterService<T>() where T : class, IService
        {
            Type type = typeof(T);
            
            if (services.ContainsKey(type))
            {
                services[type].Shutdown();
                services.Remove(type);
                
                if (serviceMonoBehaviours.ContainsKey(type))
                {
                    serviceMonoBehaviours.Remove(type);
                }
                
                Debug.Log($"[ServiceLocator] Unregistered service: {type.Name}");
            }
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool IsServiceRegistered<T>() where T : class, IService
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Initialize all registered services
        /// FirebaseInitializationService must be initialized first
        /// </summary>
        public void InitializeAllServices()
        {
            // First, initialize FirebaseInitializationService if it exists
            Type firebaseInitType = typeof(FirebaseInitializationService);
            if (services.ContainsKey(firebaseInitType) && !services[firebaseInitType].IsInitialized)
            {
                try
                {
                    Debug.Log("[ServiceLocator] Initializing FirebaseInitializationService first...");
                    services[firebaseInitType].Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceLocator] Failed to initialize FirebaseInitializationService: {ex.Message}");
                    Debug.LogError($"[ServiceLocator] Exception: {ex}");
                    // Continue initializing other services even if this fails
                }
            }

            // Then initialize all other services
            foreach (var kvp in services)
            {
                // Skip FirebaseInitializationService as it's already initialized
                if (kvp.Key == firebaseInitType)
                {
                    continue;
                }

                var service = kvp.Value;
                if (!service.IsInitialized)
                {
                    try
                    {
                        service.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceLocator] Failed to initialize service {service.GetType().Name}: {ex.Message}");
                        Debug.LogError($"[ServiceLocator] Exception: {ex}");
                        // Continue initializing other services even if one fails
                    }
                }
            }
        }

        /// <summary>
        /// Shutdown all registered services
        /// </summary>
        public void ShutdownAllServices()
        {
            foreach (var service in services.Values)
            {
                service.Shutdown();
            }
        }

        private void OnDestroy()
        {
            ShutdownAllServices();
        }
    }
}

