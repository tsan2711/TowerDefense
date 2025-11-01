using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Editor script to check if Firebase Firestore package is installed
/// Automatically manages define symbol FIREBASE_FIRESTORE_AVAILABLE
/// </summary>
[InitializeOnLoad]
public class FirebaseFirestoreChecker
{
    private const string FIRESTORE_DLL_PATH = "Assets/Firebase/Plugins/Firebase.Firestore.dll";
    private const string FIRESTORE_DEPENDENCIES_PATH = "Assets/Firebase/Editor/FirestoreDependencies.xml";
    private const string DEFINE_SYMBOL = "FIREBASE_FIRESTORE_AVAILABLE";
    
    static FirebaseFirestoreChecker()
    {
        EditorApplication.delayCall += UpdateDefineSymbols;
    }
    
    private static void UpdateDefineSymbols()
    {
        bool hasDll = File.Exists(FIRESTORE_DLL_PATH);
        bool hasDependencies = File.Exists(FIRESTORE_DEPENDENCIES_PATH);
        bool isInstalled = hasDll && hasDependencies;
        
        // Get current define symbols for all build targets
        BuildTargetGroup[] targetGroups = new BuildTargetGroup[]
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.WebGL
        };
        
        bool symbolAdded = false;
        bool symbolRemoved = false;
        
        foreach (var targetGroup in targetGroups)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            bool hasSymbol = defines.Contains(DEFINE_SYMBOL);
            
            if (isInstalled && !hasSymbol)
            {
                // Add symbol
                defines = string.IsNullOrEmpty(defines) 
                    ? DEFINE_SYMBOL 
                    : defines + ";" + DEFINE_SYMBOL;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
                symbolAdded = true;
            }
            else if (!isInstalled && hasSymbol)
            {
                // Remove symbol
                var defineList = defines.Split(';').Where(d => d != DEFINE_SYMBOL).ToArray();
                defines = string.Join(";", defineList);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
                symbolRemoved = true;
            }
        }
        
        if (symbolAdded)
        {
            Debug.Log($"[FirebaseFirestoreChecker] Đã tự động thêm define symbol '{DEFINE_SYMBOL}'. " +
                "Firebase Firestore package đã được phát hiện và code sẽ được compile với Firestore support.");
        }
        else if (symbolRemoved)
        {
            Debug.LogWarning($"[FirebaseFirestoreChecker] Đã xóa define symbol '{DEFINE_SYMBOL}'. " +
                "Firebase Firestore package không được tìm thấy.");
        }
        
        // Show warning dialog only if package is missing
        if (!isInstalled && !symbolRemoved) // Only show if we just detected it's missing
        {
            string message = "⚠️ Firebase Firestore package chưa được cài đặt!\n\n";
            message += "Code của bạn đang sử dụng Firebase Firestore nhưng package chưa được import.\n\n";
            message += "Cách khắc phục:\n";
            message += "1. Tải Firebase Unity SDK từ: https://firebase.google.com/download/unity\n";
            message += "2. Import package FirebaseFirestore.unitypackage\n";
            message += "3. Xem chi tiết trong file FIREBASE_FIRESTORE_INSTALL.md\n\n";
            message += "Lưu ý: Code hiện tại đã được sửa để có thể compile mà không cần package, ";
            message += "nhưng các chức năng Firestore sẽ không hoạt động cho đến khi package được cài đặt.";
            
            bool openGuide = EditorUtility.DisplayDialog(
                "Firebase Firestore Missing",
                message,
                "Mở hướng dẫn",
                "Bỏ qua"
            );
            
            if (openGuide)
            {
                string guidePath = Path.Combine(Application.dataPath, "../FIREBASE_FIRESTORE_INSTALL.md");
                if (File.Exists(guidePath))
                {
                    EditorUtility.RevealInFinder(guidePath);
                    // Try to open with default application (may not work on all systems)
                    try
                    {
                        System.Diagnostics.Process.Start(guidePath);
                    }
                    catch
                    {
                        // Ignore if can't open
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "File không tìm thấy",
                        "Không tìm thấy file FIREBASE_FIRESTORE_INSTALL.md tại:\n" + guidePath,
                        "OK"
                    );
                }
            }
        }
    }
    
    [MenuItem("Tools/Firebase/Check Firestore Installation")]
    private static void ManualCheck()
    {
        UpdateDefineSymbols();
        
        bool hasDll = File.Exists(FIRESTORE_DLL_PATH);
        bool hasDependencies = File.Exists(FIRESTORE_DEPENDENCIES_PATH);
        
        string status = hasDll && hasDependencies 
            ? "✅ Firebase Firestore đã được cài đặt" 
            : "❌ Firebase Firestore chưa được cài đặt";
        
        EditorUtility.DisplayDialog(
            "Firebase Firestore Status",
            status + "\n\n" +
            $"DLL: {(hasDll ? "✅ Tìm thấy" : "❌ Không tìm thấy")}\n" +
            $"Dependencies: {(hasDependencies ? "✅ Tìm thấy" : "❌ Không tìm thấy")}",
            "OK"
        );
    }
}


