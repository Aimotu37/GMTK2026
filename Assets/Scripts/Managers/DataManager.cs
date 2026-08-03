using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]

public class DataManager : SingletonMono<DataManager>
{
    private const string GameContentTableName = "GameContent";
    private const string UiTableName = "UI";
    private readonly GameConfigLoader _gameConfigLoader = new GameConfigLoader();

    //初始化变量
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;
    public GameConfigSnapshot ConfigSnapshot { get; private set; }
    public string CurrentLocaleCode => LocalizationSettings.SelectedLocale?.Identifier.Code;
    public event Action<string> LocaleChanged;

    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
        {
            yield break;
        }

        ConfigSnapshot = null;

        IEnumerator configLoading = _gameConfigLoader.Load();
        try
        {
            while (configLoading.MoveNext())
            {
                yield return configLoading.Current;
            }
        }
        finally
        {
            (configLoading as IDisposable)?.Dispose();
        }

        if (!_gameConfigLoader.IsLoaded)
        {
            throw new InvalidOperationException("Game config loading did not complete.");
        }

        ConfigSnapshot = _gameConfigLoader.Snapshot;
        _isInitialized = true;
        Debug.Log(this.name + " Initialization Successful.");
    }

    public CaseConfig GetCaseConfig(int caseId)
    {
        GameConfigSnapshot snapshot = RequireConfigSnapshot();
        if (!snapshot.Cases.TryGetValue(caseId, out CaseConfig config))
        {
            throw new KeyNotFoundException($"Case config {caseId} does not exist.");
        }

        return config;
    }

    public IReadOnlyList<ItemsConfig> GetItemConfigs(int caseId)
    {
        GameConfigSnapshot snapshot = RequireConfigSnapshot();
        GetCaseConfig(caseId);

        var result = new List<ItemsConfig>();
        foreach (ItemsConfig config in snapshot.Items.Values)
        {
            if (config.CaseId == caseId)
            {
                result.Add(config);
            }
        }

        result.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
        return result.AsReadOnly();
    }

    public IReadOnlyList<OptionsConfig> GetOptionConfigs(int caseId)
    {
        GameConfigSnapshot snapshot = RequireConfigSnapshot();
        GetCaseConfig(caseId);

        var result = new List<OptionsConfig>();
        foreach (OptionsConfig config in snapshot.Options.Values)
        {
            if (config.CaseId == caseId)
            {
                result.Add(config);
            }
        }

        result.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
        return result.AsReadOnly();
    }

    public DemonLinesConfig GetDemonLinesConfig(int demonId)
    {
        GameConfigSnapshot snapshot = RequireConfigSnapshot();
        if (!snapshot.DemonLines.TryGetValue(demonId, out DemonLinesConfig config))
        {
            throw new KeyNotFoundException($"Demon lines config {demonId} does not exist.");
        }

        return config;
    }

    public DebuffConfig GetDebuffConfig(int debuffId)
    {
        GameConfigSnapshot snapshot = RequireConfigSnapshot();
        if (!snapshot.Debuffs.TryGetValue(debuffId, out DebuffConfig config))
        {
            throw new KeyNotFoundException($"Debuff config {debuffId} does not exist.");
        }

        return config;
    }

    public DialogueConfig GetDialogueConfig(int dialogueId)
    {
        GameConfigSnapshot snapshot = RequireConfigSnapshot();
        if (!snapshot.Dialogues.TryGetValue(dialogueId, out DialogueConfig config))
        {
            throw new KeyNotFoundException($"Dialogue config {dialogueId} does not exist.");
        }

        return config;
    }

    public IReadOnlyList<DemonLinesConfig> GetDemonLinesConfigs()
    {
        GameConfigSnapshot snapshot = RequireConfigSnapshot();
        var result = new List<DemonLinesConfig>(snapshot.DemonLines.Values);
        result.Sort((left, right) => left.DemonId.CompareTo(right.DemonId));
        return result.AsReadOnly();
    }

    public IReadOnlyList<DebuffConfig> GetDebuffConfigs()
    {
        GameConfigSnapshot snapshot = RequireConfigSnapshot();
        var result = new List<DebuffConfig>(snapshot.Debuffs.Values);
        result.Sort((left, right) => left.DebuffId.CompareTo(right.DebuffId));
        return result.AsReadOnly();
    }

    public IReadOnlyList<DialogueConfig> GetOpeningDialogues()
    {
        GameConfigSnapshot snapshot = RequireConfigSnapshot();
        var result = new List<DialogueConfig>();

        foreach (DialogueConfig config in snapshot.Dialogues.Values)
        {
            if (config.DialogueType == DialogueType.Opening)
            {
                result.Add(config);
            }
        }

        result.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
        return result.AsReadOnly();
    }

    public IEnumerator GetLocalizedTextAsync(
        string localizationKey,
        Action<string> onCompleted)
    {
        return GetLocalizedTextAsync(
            GameContentTableName,
            localizationKey,
            onCompleted);
    }

    public IEnumerator GetLocalizedUiTextAsync(
        string localizationKey,
        Action<string> onCompleted)
    {
        return GetLocalizedTextAsync(
            UiTableName,
            localizationKey,
            onCompleted);
    }

    private IEnumerator GetLocalizedTextAsync(
        string tableName,
        string localizationKey,
        Action<string> onCompleted)
    {
        if (string.IsNullOrWhiteSpace(localizationKey))
        {
            throw new ArgumentException(
                "Localization key cannot be null or empty.",
                nameof(localizationKey));
        }

        string localizedText = localizationKey;
        bool isCompleted = false;
        AsyncOperationHandle<string> handle =
            LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                tableName,
                localizationKey);

        Action<AsyncOperationHandle<string>> complete = operation =>
        {
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                localizedText = operation.Result;
            }
            else
            {
                Debug.LogWarning($"Failed to localize key '{localizationKey}'.");
            }

            isCompleted = true;
        };

        if (handle.IsDone)
        {
            complete(handle);
        }
        else
        {
            handle.Completed += complete;
        }

        yield return new WaitUntil(() => isCompleted);
        onCompleted?.Invoke(localizedText);
    }

    public IEnumerator SetLocaleAsync(
        string localeCode,
        Action<bool> onCompleted = null)
    {
        if (string.IsNullOrWhiteSpace(localeCode))
        {
            Debug.LogWarning("Locale code cannot be null or empty.");
            onCompleted?.Invoke(false);
            yield break;
        }

        AsyncOperationHandle<LocalizationSettings> initialization =
            LocalizationSettings.InitializationOperation;
        if (!initialization.IsDone)
        {
            yield return initialization;
        }

        if (initialization.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning("Localization initialization failed.");
            onCompleted?.Invoke(false);
            yield break;
        }

        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale == null)
        {
            Debug.LogWarning($"Locale '{localeCode}' is not available.");
            onCompleted?.Invoke(false);
            yield break;
        }

        if (LocalizationSettings.SelectedLocale != locale)
        {
            LocalizationSettings.SelectedLocale = locale;
            LocaleChanged?.Invoke(locale.Identifier.Code);
        }

        onCompleted?.Invoke(true);
    }

    private GameConfigSnapshot RequireConfigSnapshot()
    {
        if (ConfigSnapshot == null)
        {
            throw new InvalidOperationException("Game config has not been initialized.");
        }

        return ConfigSnapshot;
    }
}
