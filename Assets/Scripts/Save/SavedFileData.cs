using System;
using System.Collections.Generic;

[Serializable]
public class SavedFileData
{
    public int saveVersion;
    public string savedAtUtc;
    public Dictionary<string, ISavedData> savedDataDictionary = new Dictionary<string, ISavedData>();
}

public interface ISavedData { }

[Serializable]
public sealed class SettingsSavedData
{
    public int version = 1;
    public float bgmVolume = 0.7f;
    public float sfxVolume = 0.4f;
    public string languageCode = "zh-Hans";
}

[Serializable]
public sealed class GameProgressSavedData : ISavedData
{
    public int currentCaseId;
    public bool gameCompleted;
    public bool case1001TutorialCompleted;
}
