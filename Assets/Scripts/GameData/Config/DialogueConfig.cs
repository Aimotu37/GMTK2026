public enum DialogueType
{
    Opening,
    CaseStory,
    CaseTruth,
    DemonDefeat
}

public sealed class DialogueConfig
{
    public int DialogueId { get; }
    public DialogueType DialogueType { get; }
    public int OwnerId { get; }
    public int Sequence { get; }
    public string TextKey { get; }
    public bool ShowPortrait { get; }
    public string PortraitAddress { get; }

    public DialogueConfig(
        int _dialogueId,
        DialogueType _dialogueType,
        int _ownerId,
        int _sequence,
        string _textKey,
        bool _showPortrait,
        string _portraitAddress)
    {
        DialogueId = _dialogueId;
        DialogueType = _dialogueType;
        OwnerId = _ownerId;
        Sequence = _sequence;
        TextKey = _textKey;
        ShowPortrait = _showPortrait;
        PortraitAddress = _portraitAddress;
    }
}
