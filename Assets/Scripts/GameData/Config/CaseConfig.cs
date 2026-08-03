using System.Collections.Generic;

public sealed class CaseConfig
{
    public int CaseId { get; }
    public int SortOrder { get; }
    public string SceneName { get; }
    public string NameKey { get; }
    public string SceneLabelKey { get; }
    public int InitialWords { get; }

    public IReadOnlyList<DialogueConfig> StoryLines { get; }
    public IReadOnlyList<DialogueConfig> TruthLines { get; }

    public CaseConfig(
        int _caseId,
        int _sortOrder,
        string _sceneName,
        string _nameKey,
        string _sceneLabelKey,
        int _initialWords,
        IReadOnlyList<DialogueConfig> _storyLines,
        IReadOnlyList<DialogueConfig> _truthLines)
    {
        CaseId = _caseId;
        SortOrder = _sortOrder;
        SceneName = _sceneName;
        NameKey = _nameKey;
        SceneLabelKey = _sceneLabelKey;
        InitialWords = _initialWords;
        StoryLines = _storyLines;
        TruthLines = _truthLines;
    }
}
