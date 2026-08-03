public sealed class DemonLinesConfig
{
    public int DemonId { get; }
    public string DefaultKey { get; }
    public string HurtKey { get; }
    public string MockKey { get; }
    public int DefeatDialogueId { get; }
    public float DurationSeconds { get; }

    public DemonLinesConfig(
        int _demonId,
        string _defaultKey,
        string _hurtKey,
        string _mockKey,
        int _defeatDialogueId,
        float _durationSeconds)
    {
        DemonId = _demonId;
        DefaultKey = _defaultKey;
        HurtKey = _hurtKey;
        MockKey = _mockKey;
        DefeatDialogueId = _defeatDialogueId;
        DurationSeconds = _durationSeconds;
    }
}
