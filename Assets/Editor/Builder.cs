using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;

/// <summary>
/// Class để tự động hóa quá trình build của Unity với nhiều cấu hình và nền tảng khác nhau.
/// Được thiết kế để chạy từ Jenkins hoặc các hệ thống CI/CD khác.
/// </summary>
public class Builder
{
    // Thêm một phương thức mới để tracking tiến độ build
    private class BuildProgressTracker : IDisposable
    {
        private StreamWriter logWriter;
        private string logFilePath;

        public BuildProgressTracker(string outputPath)
        {
            // Tạo thư mục logs nếu chưa tồn tại
            Directory.CreateDirectory(Path.Combine(outputPath, "logs"));

            // Tạo file log chi tiết
            logFilePath = Path.Combine(outputPath, "logs", $"build_detailed_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            logWriter = new StreamWriter(logFilePath, true);

            // Đăng ký các sự kiện log
            Application.logMessageReceived += HandleLog;

            LogMessage("Build Tracking Started", LogType.Log);
        }

        private void HandleLog(string logMessage, string stackTrace, LogType type)
        {
            // Ghi log chi tiết với thời gian và loại log
            string formattedLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] {logMessage}";

            // Ghi vào file
            logWriter.WriteLine(formattedLog);
            logWriter.Flush();

            // In ra console để dễ theo dõi
            switch (type)
            {
                case LogType.Error:
                    Console.Error.WriteLine(formattedLog);
                    break;
                case LogType.Warning:
                    Console.WriteLine($"WARNING: {formattedLog}");
                    break;
                default:
                    Console.WriteLine(formattedLog);
                    break;
            }
        }

        public void LogMessage(string message, LogType type = LogType.Log)
        {
            HandleLog(message, "", type);
        }

        public void Dispose()
        {
            // Hủy đăng ký sự kiện và đóng file log
            Application.logMessageReceived -= HandleLog;
            logWriter?.Close();
            logWriter?.Dispose();

            Debug.Log($"Detailed build log saved to: {logFilePath}");
        }
    }

    static void PerformBuild()
    {
        // Lấy đường dẫn build output
        string buildOutput = Environment.GetEnvironmentVariable("BUILD_OUTPUT") ?? "Build";

        // Sử dụng BuildProgressTracker để ghi log chi tiết
        using (var progressTracker = new BuildProgressTracker(buildOutput))
        {
            try
            {
                // Các bước build như cũ...
                string targetPlatform = Environment.GetEnvironmentVariable("TARGET_PLATFORM") ?? "Windows";
                string buildType = Environment.GetEnvironmentVariable("BUILD_TYPE") ?? "Release";
                string gameName = Environment.GetEnvironmentVariable("GAME_NAME") ?? "UnityGame";

                // Log chi tiết các thông tin build
                progressTracker.LogMessage($"Starting build for platform: {targetPlatform}");
                progressTracker.LogMessage($"Build type: {buildType}");

                BuildTarget buildTarget = GetBuildTarget(targetPlatform);
                BuildTargetGroup buildTargetGroup = GetBuildTargetGroup(buildTarget);
                BuildOptions buildOptions = GetBuildOptions(buildType);

                string buildPath = GetBuildPath(buildOutput, gameName, targetPlatform);

                // Ghi log các bước build
                progressTracker.LogMessage($"Build output path: {buildPath}");

                // Kiểm tra và log các scene được build
                string[] scenes = GetEnabledScenes();
                progressTracker.LogMessage($"Scenes to build: {string.Join(", ", scenes)}");

                // Thiết lập build
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = buildPath,
                    targetGroup = buildTargetGroup,
                    target = buildTarget,
                    options = buildOptions
                };

                // Thực hiện build và tracking
                BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                BuildSummary summary = report.summary;

                // Log chi tiết kết quả build
                if (summary.result == BuildResult.Succeeded)
                {
                    progressTracker.LogMessage($"Build completed successfully!", LogType.Log);
                    progressTracker.LogMessage($"Total build time: {summary.totalTime.TotalSeconds:F2} seconds",
                        LogType.Log);
                    progressTracker.LogMessage($"Total build size: {summary.totalSize / 1048576:F2} MB", LogType.Log);

                    // Thoát với mã thành công
                    EditorApplication.Exit(0);
                }
                else
                {
                    progressTracker.LogMessage($"Build failed with result: {summary.result}", LogType.Error);
                    progressTracker.LogMessage($"Total errors: {summary.totalErrors}", LogType.Error);

                    // Thoát với mã lỗi
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception e)
            {
                // Ghi log lỗi chi tiết
                progressTracker.LogMessage($"Build failed with exception: {e.Message}", LogType.Error);
                progressTracker.LogMessage($"Stack trace: {e.StackTrace}", LogType.Error);

                EditorApplication.Exit(1);
            }
        }
    }

    /// <summary>
    /// Lấy BuildTarget dựa trên tên nền tảng.
    /// </summary>
    private static BuildTarget GetBuildTarget(string platform)
    {
        switch (platform.ToLower())
        {
            case "windows":
            case "win":
            case "win64":
                return BuildTarget.StandaloneWindows64;

            case "win32":
                return BuildTarget.StandaloneWindows;

            case "macos":
            case "mac":
            case "osx":
                return BuildTarget.StandaloneOSX;

            case "linux":
                return BuildTarget.StandaloneLinux64;

            case "android":
                return BuildTarget.Android;

            case "ios":
                return BuildTarget.iOS;

            case "webgl":
                return BuildTarget.WebGL;

            case "tvos":
                return BuildTarget.tvOS;

            case "ps4":
                return BuildTarget.PS4;

            case "ps5":
                return BuildTarget.PS5;

            case "xboxone":
                return BuildTarget.XboxOne;

            case "switch":
                return BuildTarget.Switch;

            default:
                Debug.LogWarning($"Unknown platform '{platform}', defaulting to Windows 64-bit");
                return BuildTarget.StandaloneWindows64;
        }
    }

    /// <summary>
    /// Lấy BuildTargetGroup tương ứng với BuildTarget.
    /// </summary>
    private static BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneOSX:
            case BuildTarget.StandaloneLinux64:
                return BuildTargetGroup.Standalone;

            case BuildTarget.iOS:
                return BuildTargetGroup.iOS;

            case BuildTarget.Android:
                return BuildTargetGroup.Android;

            case BuildTarget.WebGL:
                return BuildTargetGroup.WebGL;

            case BuildTarget.tvOS:
                return BuildTargetGroup.tvOS;

            case BuildTarget.PS4:
                return BuildTargetGroup.PS4;

            case BuildTarget.PS5:
                return BuildTargetGroup.PS5;

            case BuildTarget.XboxOne:
                return BuildTargetGroup.XboxOne;

            case BuildTarget.Switch:
                return BuildTargetGroup.Switch;

            default:
                Debug.LogWarning($"Unknown BuildTarget '{target}', defaulting to Standalone");
                return BuildTargetGroup.Standalone;
        }
    }

    /// <summary>
    /// Lấy BuildOptions dựa trên loại build.
    /// </summary>
    private static BuildOptions GetBuildOptions(string buildType)
    {
        BuildOptions options = BuildOptions.None;

        switch (buildType.ToLower())
        {
            case "debug":
                options |= BuildOptions.Development;
                options |= BuildOptions.AllowDebugging;
                break;

            case "development":
                options |= BuildOptions.Development;
                break;

            case "profile":
                options |= BuildOptions.Development;
                options |= BuildOptions.ConnectWithProfiler;
                break;

            case "deepprofile":
                options |= BuildOptions.Development;
                options |= BuildOptions.EnableDeepProfilingSupport;
                break;

            case "release":
                // Không thêm tùy chọn gì, vì BuildOptions.None là mặc định cho Release build
                break;

            default:
                Debug.LogWarning($"Unknown build type '{buildType}', defaulting to Release");
                break;
        }

        // Kiểm tra xem có cần bật InstallInBuildFolder hay không
        string installInBuildFolder = Environment.GetEnvironmentVariable("INSTALL_IN_BUILD_FOLDER");
        if (!string.IsNullOrEmpty(installInBuildFolder) && installInBuildFolder.ToLower() == "true")
        {
            options |= BuildOptions.InstallInBuildFolder;
        }

        // Kiểm tra xem có cần bật chế độ tự động kết nối hay không
        string autoConnect = Environment.GetEnvironmentVariable("AUTO_CONNECT_PROFILER");
        if (!string.IsNullOrEmpty(autoConnect) && autoConnect.ToLower() == "true")
        {
            options |= BuildOptions.ConnectWithProfiler;
        }

        return options;
    }

    /// <summary>
    /// Xác định đường dẫn đầu ra và tên file dựa trên nền tảng.
    /// </summary>
    private static string GetBuildPath(string buildOutput, string gameName, string platform)
    {
        string fileName;
        string extension = "";

        switch (platform.ToLower())
        {
            case "windows":
            case "win":
            case "win64":
            case "win32":
                extension = ".exe";
                break;

            case "android":
                extension = ".apk";
                // Unity cũng hỗ trợ .aab (Android App Bundle) cho Google Play
                string buildFormat = Environment.GetEnvironmentVariable("ANDROID_BUILD_FORMAT");
                if (!string.IsNullOrEmpty(buildFormat) && buildFormat.ToLower() == "aab")
                {
                    extension = ".aab";
                }

                break;

            case "ios":
            case "tvos":
                // iOS builds tạo ra một thư mục Xcode project
                extension = "";
                break;

            case "webgl":
                // WebGL builds tạo ra một thư mục chứa index.html và các assets
                extension = "";
                break;

            default:
                // Đối với các nền tảng khác, không thêm extension
                extension = "";
                break;
        }

        // Tạo tên file dựa trên tên game và platform
        fileName = $"{gameName}_{platform}{extension}";

        // Kết hợp với đường dẫn đầu ra
        return Path.Combine(buildOutput, fileName);
    }

    /// <summary>
    /// Đảm bảo thư mục tồn tại trước khi build.
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        try
        {
            // Nếu là file, lấy thư mục chứa file
            if (Path.HasExtension(path))
            {
                path = Path.GetDirectoryName(path);
            }

            // Tạo thư mục và log chi tiết
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"Created directory: {path}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create directory {path}: {ex.Message}");
            throw; // Rethrow để Jenkins biết build thất bại
        }
    }

    /// <summary>
    /// Lấy danh sách các scene được bật trong Build Settings.
    /// </summary>
    private static string[] GetEnabledScenes()
    {
        // Hiện tại đã có log, nhưng có thể thêm chi tiết hơn
        string[] scenesLog = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        Debug.Log($"Total enabled scenes: {scenesLog.Length}");
        foreach (var scene in scenesLog)
        {
            Debug.Log($"Enabled scene: {scene}");
        }

        // Kiểm tra xem có scenes được chỉ định cụ thể không
        string customScenes = Environment.GetEnvironmentVariable("CUSTOM_SCENES");
        if (!string.IsNullOrEmpty(customScenes))
        {
            // Sử dụng danh sách scenes được chỉ định
            string[] scenes = customScenes.Split(',');
            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = scenes[i].Trim();
            }

            Debug.Log($"Using custom scenes: {string.Join(", ", scenes)}");
            return scenes;
        }

        // Nếu không, sử dụng các scene trong Build Settings
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }

    /// <summary>
    /// Ghi thông tin build vào log.
    /// </summary>
    private static void LogBuildInfo(string platform, string buildType, string buildPath)
    {
        Debug.Log("========== Build Information ==========");
        Debug.Log($"Platform: {platform}");
        Debug.Log($"Build Type: {buildType}");
        Debug.Log($"Output Path: {buildPath}");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"BuildTarget: {GetBuildTarget(platform)}");
        Debug.Log($"Build Options: {GetBuildOptions(buildType)}");
        Debug.Log($"Start Time: {DateTime.Now}");
        Debug.Log("======================================");
    }

    /// <summary>
    /// Thực hiện các thiết lập cần thiết trước khi build.
    /// </summary>
    private static void PreBuildSetup(BuildTargetGroup targetGroup, BuildTarget target, string buildType)
    {
        try
        {
            // Ví dụ: thiết lập scripting backend cho Android
            if (target == BuildTarget.Android)
            {
                string scriptingBackend = Environment.GetEnvironmentVariable("ANDROID_SCRIPTING_BACKEND");
                if (!string.IsNullOrEmpty(scriptingBackend))
                {
                    ScriptingImplementation backend = ScriptingImplementation.Mono2x;

                    switch (scriptingBackend.ToLower())
                    {
                        case "il2cpp":
                            backend = ScriptingImplementation.IL2CPP;
                            break;
                        case "mono":
                            backend = ScriptingImplementation.Mono2x;
                            break;
                    }

                    PlayerSettings.SetScriptingBackend(targetGroup, backend);
                    Debug.Log($"Set Android scripting backend to: {backend}");
                }

                // Thiết lập target architecture
                string targetArchitecture = Environment.GetEnvironmentVariable("ANDROID_TARGET_ARCHITECTURE");
                if (!string.IsNullOrEmpty(targetArchitecture))
                {
                    AndroidArchitecture arch = AndroidArchitecture.ARMv7;

                    switch (targetArchitecture.ToLower())
                    {
                        case "armv7":
                            arch = AndroidArchitecture.ARMv7;
                            break;
                        case "arm64":
                            arch = AndroidArchitecture.ARM64;
                            break;
                        case "x86":
                            arch = AndroidArchitecture.X86;
                            break;
                        case "x86_64":
                            arch = AndroidArchitecture.X86_64;
                            break;
                        case "all":
                            arch = AndroidArchitecture.All;
                            break;
                    }

                    PlayerSettings.Android.targetArchitectures = arch;
                    Debug.Log($"Set Android target architecture to: {arch}");
                }
            }

            // Thiết lập cho iOS
            if (target == BuildTarget.iOS)
            {
                string teamId = Environment.GetEnvironmentVariable("IOS_TEAM_ID");
                if (!string.IsNullOrEmpty(teamId))
                {
                    PlayerSettings.iOS.appleDeveloperTeamID = teamId;
                    Debug.Log($"Set iOS Team ID to: {teamId}");
                }
            }

            // Thiết lập chung cho mọi nền tảng
            string bundleVersion = Environment.GetEnvironmentVariable("BUNDLE_VERSION");
            if (!string.IsNullOrEmpty(bundleVersion))
            {
                PlayerSettings.bundleVersion = bundleVersion;
                Debug.Log($"Set Bundle Version to: {bundleVersion}");
            }

            string bundleIdentifier = Environment.GetEnvironmentVariable("BUNDLE_IDENTIFIER");
            if (!string.IsNullOrEmpty(bundleIdentifier))
            {
                PlayerSettings.SetApplicationIdentifier(targetGroup, bundleIdentifier);
                Debug.Log($"Set Bundle Identifier to: {bundleIdentifier}");
            }

            // Thiết lập define symbols dựa trên loại build
            string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            List<string> symbols = new List<string>(defineSymbols.Split(';'));

            // Thêm các symbols tùy theo loại build
            if (buildType.ToLower() == "debug")
            {
                if (!symbols.Contains("DEBUG"))
                {
                    symbols.Add("DEBUG");
                }
            }

            // Cập nhật define symbols
            string newDefineSymbols = string.Join(";", symbols.ToArray());
            if (newDefineSymbols != defineSymbols)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefineSymbols);
                Debug.Log($"Set Scripting Define Symbols to: {newDefineSymbols}");
            }
            
            // Thêm xử lý plugin cho Android
            if (target == BuildTarget.Android)
            {
                // Đảm bảo thư mục Plugins tồn tại
                string pluginsPath = Path.Combine(Application.dataPath, "Plugins", "Android");
                if (!Directory.Exists(pluginsPath))
                {
                    Directory.CreateDirectory(pluginsPath);
                    Debug.Log($"Created Android Plugins directory: {pluginsPath}");
                }

                // Kiểm tra và xử lý các plugin cần thiết
                string gradlePath = Path.Combine(pluginsPath, "mainTemplate.gradle");
                if (!File.Exists(gradlePath))
                {
                    // Tạo file gradle template nếu chưa tồn tại
                    File.WriteAllText(gradlePath, GetDefaultGradleTemplate());
                    Debug.Log($"Created default gradle template: {gradlePath}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Pre-build setup failed: {ex.Message}");
            throw;
        }
    }
    
    private static string GetDefaultGradleTemplate()
    {
        return @"
// Unity default gradle template
buildscript {
    repositories {
        google()
        mavenCentral()
    }
    dependencies {
        classpath 'com.android.tools.build:gradle:4.2.2'
    }
}

apply plugin: 'com.android.library'

android {
    compileSdkVersion 30
    buildToolsVersion '30.0.3'

    defaultConfig {
        minSdkVersion 21
        targetSdkVersion 30
    }
}

dependencies {
    // Add any necessary dependencies
}
";
    }

    /// <summary>
    /// Thực hiện các hành động sau khi build thành công.
    /// </summary>
    private static void PostBuildActions(string buildPath, string platform, string buildType)
    {
        // Tạo file build_info.txt
        try
        {
            string buildInfoPath;

            // Xác định đường dẫn đến file build_info.txt
            if (Path.HasExtension(buildPath))
            {
                // Nếu buildPath là file, lưu file build_info bên cạnh
                buildInfoPath = Path.Combine(Path.GetDirectoryName(buildPath), "build_info.txt");
            }
            else
            {
                // Nếu buildPath là thư mục, lưu file build_info trong thư mục đó
                buildInfoPath = Path.Combine(buildPath, "build_info.txt");
            }

            // Thu thập thông tin build
            string buildInfo = $"Game: {Environment.GetEnvironmentVariable("GAME_NAME") ?? "UnityGame"}\n" +
                               $"Version: {PlayerSettings.bundleVersion}\n" +
                               $"Build Number: {Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "1"}\n" +
                               $"Platform: {platform}\n" +
                               $"Build Type: {buildType}\n" +
                               $"Branch: {Environment.GetEnvironmentVariable("GIT_BRANCH") ?? "Unknown"}\n" +
                               $"Commit: {Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "Unknown"}\n" +
                               $"Build Date: {DateTime.Now}\n" +
                               $"Unity Version: {Application.unityVersion}\n" +
                               $"Builder: {Environment.UserName}@{Environment.MachineName}";

            // Ghi thông tin vào file
            File.WriteAllText(buildInfoPath, buildInfo);
            Debug.Log($"Created build info file at: {buildInfoPath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to create build info file: {e.Message}");
        }

        // Sao chép build vào thư mục khác nếu được chỉ định
        string additionalOutputPath = Environment.GetEnvironmentVariable("ADDITIONAL_OUTPUT_PATH");
        if (!string.IsNullOrEmpty(additionalOutputPath))
        {
            try
            {
                if (Path.HasExtension(buildPath))
                {
                    // Nếu buildPath là file, sao chép file
                    string fileName = Path.GetFileName(buildPath);
                    string destPath = Path.Combine(additionalOutputPath, fileName);

                    // Đảm bảo thư mục đích tồn tại
                    Directory.CreateDirectory(additionalOutputPath);

                    // Sao chép file
                    File.Copy(buildPath, destPath, true);
                    Debug.Log($"Copied build file to: {destPath}");
                }
                else
                {
                    // Nếu buildPath là thư mục, sao chép toàn bộ thư mục
                    string dirName = new DirectoryInfo(buildPath).Name;
                    string destPath = Path.Combine(additionalOutputPath, dirName);

                    // Xóa thư mục đích nếu đã tồn tại
                    if (Directory.Exists(destPath))
                    {
                        Directory.Delete(destPath, true);
                    }

                    // Sao chép thư mục
                    DirectoryCopy(buildPath, destPath, true);
                    Debug.Log($"Copied build directory to: {destPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to copy build to additional output path: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Sao chép thư mục và các thư mục con.
    /// </summary>
    private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        // Lấy thông tin thư mục nguồn
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        // Kiểm tra xem thư mục nguồn có tồn tại không
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                $"Source directory does not exist or could not be found: {sourceDirName}");
        }

        // Tạo thư mục đích nếu chưa tồn tại
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Sao chép các file
        foreach (FileInfo file in dir.GetFiles())
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, true);
        }

        // Sao chép các thư mục con nếu được yêu cầu
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }
}