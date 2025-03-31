using UnityEditor;
using System.IO;
using System;

[InitializeOnLoad]
public class KeystoreHelper
{
    static KeystoreHelper()
    {
        ApplyKeystoreSettings();
    }

    public static void ApplyKeystoreSettings()
    {
        try
        {
            // Kiểm tra biến môi trường
            string keystorePath = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYSTORE_PATH");
            string keystorePass = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYSTORE_PASS");
            string keyAlias = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYALIAS");
            string keyPass = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYPASS");

            if (string.IsNullOrEmpty(keystorePath) || !File.Exists(keystorePath))
            {
                // Thử đọc từ file keystore.properties nếu không có biến môi trường
                string propertiesPath = Path.Combine(Directory.GetCurrentDirectory(), "keystore", "keystore.properties");
                if (File.Exists(propertiesPath))
                {
                    string[] lines = File.ReadAllLines(propertiesPath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length != 2) continue;

                        switch (parts[0].Trim())
                        {
                            case "KEYSTORE_PATH":
                                keystorePath = parts[1].Trim();
                                break;
                            case "KEYSTORE_PASSWORD":
                                keystorePass = parts[1].Trim();
                                break;
                            case "KEYSTORE_ALIAS":
                                keyAlias = parts[1].Trim();
                                break;
                            case "KEYSTORE_ALIAS_PASSWORD":
                                keyPass = parts[1].Trim();
                                break;
                        }
                    }
                }
            }

            // Áp dụng cài đặt keystore
            if (!string.IsNullOrEmpty(keystorePath) && File.Exists(keystorePath))
            {
                UnityEditor.PlayerSettings.Android.keystoreName = keystorePath;
                UnityEditor.PlayerSettings.Android.keystorePass = keystorePass;
                UnityEditor.PlayerSettings.Android.keyaliasName = keyAlias;
                UnityEditor.PlayerSettings.Android.keyaliasPass = keyPass;
                
                UnityEngine.Debug.Log($"Applied keystore settings. Path: {keystorePath}, Alias: {keyAlias}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Keystore file not found at: {keystorePath}. Using default keystore settings.");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error applying keystore settings: {e.Message}");
        }
    }
}