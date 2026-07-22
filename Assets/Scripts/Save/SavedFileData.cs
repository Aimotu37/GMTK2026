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
public sealed class GameProgressSavedData : ISavedData
{
    public string currentChapterId;
    public string currentGameSceneId;
}

[Serializable]
public sealed class AudioSavedData : ISavedData
{
    public float bgmVolume;
    public float sfxVolume;
}
