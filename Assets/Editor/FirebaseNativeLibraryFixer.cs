using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Editor script to fix Firebase native library plugin settings for macOS
/// Fixes missing bundle files and incorrect plugin importer settings
/// </summary>
public class FirebaseNativeLibraryFixer
{
    private const string PLUGINS_X86_64_PATH = "Assets/Firebase/Plugins/x86_64";
    
    [MenuItem("Tools/Firebase/Fix Native Library Settings (macOS)")]
    private static void FixNativeLibrarySettings()
    {
        Debug.Log("[FirebaseNativeLibraryFixer] Đang kiểm tra và sửa cấu hình native libraries...");
        
        if (!Directory.Exists(PLUGINS_X86_64_PATH))
        {
            Debug.LogError($"[FirebaseNativeLibraryFixer] Không tìm thấy thư mục: {PLUGINS_X86_64_PATH}");
            return;
        }
        
        bool needsFix = false;
        string[] allFiles = Directory.GetFiles(PLUGINS_X86_64_PATH, "*", SearchOption.TopDirectoryOnly);
        string[] allDirs = Directory.GetDirectories(PLUGINS_X86_64_PATH);
        
        // Fix all .dll files (Windows - should be enabled for Editor on macOS)
        foreach (string filePath in allFiles.Where(f => f.EndsWith(".dll")))
        {
            string assetPath = filePath.Replace('\\', '/');
            PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer != null)
            {
                if (!importer.GetCompatibleWithEditor())
                {
                    importer.SetCompatibleWithEditor(true);
                    importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
                    importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, true);
                    importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    needsFix = true;
                    Debug.Log($"[FirebaseNativeLibraryFixer] ✅ Đã enable Editor cho: {Path.GetFileName(assetPath)}");
                }
            }
        }
        
        // Fix all .so files (Linux/macOS - should be enabled for Editor + StandaloneOSX)
        foreach (string filePath in allFiles.Where(f => f.EndsWith(".so")))
        {
            string assetPath = filePath.Replace('\\', '/');
            PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer != null)
            {
                bool fileNeedsFix = false;
                if (!importer.GetCompatibleWithEditor())
                {
                    importer.SetCompatibleWithEditor(true);
                    fileNeedsFix = true;
                }
                if (!importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX))
                {
                    importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);
                    fileNeedsFix = true;
                }
                if (!importer.GetCompatibleWithPlatform(BuildTarget.StandaloneLinux64))
                {
                    importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, true);
                    fileNeedsFix = true;
                }
                if (fileNeedsFix)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    needsFix = true;
                    Debug.Log($"[FirebaseNativeLibraryFixer] ✅ Đã enable Editor + macOS/Linux cho: {Path.GetFileName(assetPath)}");
                }
            }
        }
        
        // Fix all .bundle directories (macOS native format - preferred)
        foreach (string dirPath in allDirs.Where(d => d.EndsWith(".bundle")))
        {
            string assetPath = dirPath.Replace('\\', '/');
            PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer != null)
            {
                bool bundleNeedsFix = false;
                if (!importer.GetCompatibleWithEditor())
                {
                    importer.SetCompatibleWithEditor(true);
                    bundleNeedsFix = true;
                }
                if (!importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX))
                {
                    importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);
                    bundleNeedsFix = true;
                }
                if (bundleNeedsFix)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    needsFix = true;
                    Debug.Log($"[FirebaseNativeLibraryFixer] ✅ Đã enable Editor + macOS cho: {Path.GetFileName(assetPath)}");
                }
            }
        }
        
        if (needsFix)
        {
            AssetDatabase.Refresh();
            Debug.Log("[FirebaseNativeLibraryFixer] ✅ Đã sửa cấu hình tất cả native libraries!");
            Debug.Log("[FirebaseNativeLibraryFixer] Vui lòng đóng và mở lại Unity Editor, sau đó thử chạy lại game.");
        }
        else
        {
            Debug.Log("[FirebaseNativeLibraryFixer] ✅ Cấu hình native libraries đã đúng.");
        }
        
        // Show diagnostic info
        ShowDiagnosticInfo();
    }
    
    private static void ShowDiagnosticInfo()
    {
        Debug.Log("=== Firebase Native Library Diagnostic ===");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Editor: {Application.isEditor}");
        Debug.Log($"Plugins Path: {PLUGINS_X86_64_PATH}");
        
        if (!Directory.Exists(PLUGINS_X86_64_PATH))
        {
            Debug.LogError($"❌ Thư mục không tồn tại: {PLUGINS_X86_64_PATH}");
            Debug.Log("==========================================");
            return;
        }
        
        string[] allFiles = Directory.GetFiles(PLUGINS_X86_64_PATH, "*", SearchOption.TopDirectoryOnly);
        string[] allDirs = Directory.GetDirectories(PLUGINS_X86_64_PATH);
        
        int dllCount = allFiles.Count(f => f.EndsWith(".dll"));
        int soCount = allFiles.Count(f => f.EndsWith(".so"));
        int bundleCount = allDirs.Count(d => d.EndsWith(".bundle"));
        
        Debug.Log($"Tìm thấy {dllCount} file .dll, {soCount} file .so, {bundleCount} thư mục .bundle");
        
        // Check a few key files
        string[] keyFiles = {
            "FirebaseCppApp-13_4_0.dll",
            "FirebaseCppApp-13_4_0.so",
            "FirebaseCppAuth.dll",
            "FirebaseCppAuth.so",
            "FirebaseCppFirestore.dll",
            "FirebaseCppFirestore.so"
        };
        
        foreach (string keyFile in keyFiles)
        {
            string fullPath = Path.Combine(PLUGINS_X86_64_PATH, keyFile).Replace('\\', '/');
            if (File.Exists(fullPath))
            {
                PluginImporter importer = AssetImporter.GetAtPath(fullPath) as PluginImporter;
                if (importer != null)
                {
                    bool editorEnabled = importer.GetCompatibleWithEditor();
                    bool osxEnabled = importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX);
                    Debug.Log($"  {keyFile}: Editor={editorEnabled}, StandaloneOSX={osxEnabled}");
                }
            }
        }
        
        // Check bundles
        foreach (string bundlePath in allDirs.Where(d => d.EndsWith(".bundle")))
        {
            string assetPath = bundlePath.Replace('\\', '/');
            PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer != null)
            {
                bool editorEnabled = importer.GetCompatibleWithEditor();
                bool osxEnabled = importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX);
                Debug.Log($"  {Path.GetFileName(assetPath)}: Editor={editorEnabled}, StandaloneOSX={osxEnabled}");
            }
        }
        
        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            if (bundleCount == 0 && soCount == 0)
            {
                Debug.LogWarning("⚠️ Không tìm thấy native libraries cho macOS!");
                Debug.LogWarning("Bạn có thể cần reimport Firebase packages từ Firebase Unity SDK");
            }
            else if (bundleCount == 0 && soCount > 0)
            {
                Debug.Log("ℹ️ Sử dụng .so files cho macOS Editor (bundle không có)");
            }
        }
        
        Debug.Log("==========================================");
    }
    
    [InitializeOnLoadMethod]
    private static void AutoCheckOnLoad()
    {
        // Only check once when Unity starts
        if (SessionState.GetBool("FirebaseNativeLibraryChecked", false))
        {
            return;
        }
        
        SessionState.SetBool("FirebaseNativeLibraryChecked", true);
        
        // Check if we're on macOS Editor
        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            if (!Directory.Exists(PLUGINS_X86_64_PATH))
            {
                Debug.LogWarning($"[FirebaseNativeLibraryFixer] ⚠️ Không tìm thấy thư mục: {PLUGINS_X86_64_PATH}");
                Debug.LogWarning("[FirebaseNativeLibraryFixer] Firebase SDK có thể chưa được cài đặt đúng cách.");
                return;
            }
            
            string[] allFiles = Directory.GetFiles(PLUGINS_X86_64_PATH, "*", SearchOption.TopDirectoryOnly);
            string[] allDirs = Directory.GetDirectories(PLUGINS_X86_64_PATH);
            
            bool hasSoFiles = allFiles.Any(f => f.EndsWith(".so"));
            bool hasBundleDirs = allDirs.Any(d => d.EndsWith(".bundle"));
            
            if (!hasSoFiles && !hasBundleDirs)
            {
                Debug.LogWarning("[FirebaseNativeLibraryFixer] ⚠️ Không tìm thấy Firebase native libraries cho macOS!");
                Debug.LogWarning("[FirebaseNativeLibraryFixer] Chạy 'Tools → Firebase → Fix Native Library Settings (macOS)' để kiểm tra và sửa.");
            }
            else
            {
                // Auto-fix if needed (silent mode) - check both .so and .bundle files
                bool needsAutoFix = false;
                
                // Check .so files
                foreach (string filePath in allFiles.Where(f => f.EndsWith(".so")))
                {
                    string assetPath = filePath.Replace('\\', '/');
                    PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                    if (importer != null && !importer.GetCompatibleWithEditor())
                    {
                        needsAutoFix = true;
                        break;
                    }
                }
                
                // Check .bundle directories if no .so files need fixing
                if (!needsAutoFix)
                {
                    foreach (string dirPath in allDirs.Where(d => d.EndsWith(".bundle")))
                    {
                        string assetPath = dirPath.Replace('\\', '/');
                        PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                        if (importer != null && !importer.GetCompatibleWithEditor())
                        {
                            needsAutoFix = true;
                            break;
                        }
                    }
                }
                
                if (needsAutoFix)
                {
                    Debug.Log("[FirebaseNativeLibraryFixer] ⚠️ Phát hiện native libraries chưa được enable cho Editor!");
                    Debug.Log("[FirebaseNativeLibraryFixer] Đang tự động sửa cấu hình...");
                    EditorApplication.delayCall += () => 
                    {
                        FixNativeLibrarySettings();
                        Debug.Log("[FirebaseNativeLibraryFixer] ✅ Đã sửa xong! Vui lòng ĐÓNG VÀ MỞ LẠI Unity Editor để áp dụng thay đổi.");
                    };
                }
                else
                {
                    Debug.Log("[FirebaseNativeLibraryFixer] ✅ Cấu hình native libraries đã đúng cho macOS Editor.");
                }
            }
        }
    }
}

