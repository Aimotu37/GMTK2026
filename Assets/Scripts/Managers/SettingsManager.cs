using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public sealed class SettingsManager : SingletonMono<SettingsManager>
{
    public const string ChineseLanguageCode = "zh-Hans";
    public const string EnglishLanguageCode = "en";

    private const int CurrentSettingsVersion = 1;
    private const string SettingsFileName = "settings.json";
    private const string TempSettingsFileName = "settings.tmp";

    private SettingsSavedData _data = CreateDefaultData();

    public bool IsInitialized { get; private set; }
    public float BgmVolume => _data.bgmVolume;
    public float SfxVolume => _data.sfxVolume;
    public string LanguageCode => _data.languageCode;

    public void Initialize()
    {
        if (IsInitialized) return;

        string path = GetSettingsFilePath();
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SettingsSavedData loaded = JsonConvert.DeserializeObject<SettingsSavedData>(json);
                if (loaded == null || loaded.version != CurrentSettingsVersion)
                    throw new InvalidDataException("Invalid or unsupported settings version.");

                loaded.bgmVolume = Mathf.Clamp01(loaded.bgmVolume);
                loaded.sfxVolume = Mathf.Clamp01(loaded.sfxVolume);
                loaded.languageCode = NormalizeLanguageCode(loaded.languageCode);
                _data = loaded;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Settings load failed, using defaults: {exception.Message}");
                _data = CreateDefaultData();
                SaveSettings();
            }
        }
        else
        {
            _data = CreateDefaultData();
            SaveSettings();
        }

        IsInitialized = true;
    }

    public void SetBgmVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);
        if (Mathf.Approximately(_data.bgmVolume, clamped)) return;

        _data.bgmVolume = clamped;
        SaveSettings();
    }

    public void SetSfxVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);
        if (Mathf.Approximately(_data.sfxVolume, clamped)) return;

        _data.sfxVolume = clamped;
        SaveSettings();
    }

    public void SetLanguageCode(string languageCode)
    {
        string normalized = NormalizeLanguageCode(languageCode);
        if (string.Equals(_data.languageCode, normalized, StringComparison.Ordinal)) return;

        _data.languageCode = normalized;
        SaveSettings();
    }

    private void SaveSettings()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_data, Formatting.Indented);
            string settingsPath = GetSettingsFilePath();
            string tempPath = GetTempSettingsFilePath();
            File.WriteAllText(tempPath, json);

            if (File.Exists(settingsPath))
                File.Replace(tempPath, settingsPath, null);
            else
                File.Move(tempPath, settingsPath);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Settings save failed: {exception.Message}");
        }
    }

    private static SettingsSavedData CreateDefaultData()
    {
        return new SettingsSavedData
        {
            version = CurrentSettingsVersion,
            bgmVolume = 0.7f,
            sfxVolume = 0.4f,
            languageCode = ChineseLanguageCode
        };
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        return string.Equals(languageCode, EnglishLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? EnglishLanguageCode
            : ChineseLanguageCode;
    }

    private static string GetSettingsFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SettingsFileName);
    }

    private static string GetTempSettingsFilePath()
    {
        return Path.Combine(Application.persistentDataPath, TempSettingsFileName);
    }
}
