public enum DebuffType
{
    None = 0,
    ReduceSpeak = 1
}

public sealed class DebuffConfig
{
    public int DebuffId { get; }
    public string NameKey { get; }
    public string DescriptionKey { get; }
    public DebuffType EffectType { get; }
    public float EffectParam { get; }

    public DebuffConfig(
        int _debuffId,
        string _nameKey,
        string _descriptionKey,
        DebuffType _effectType,
        float _effectParam)
    {
        DebuffId = _debuffId;
        NameKey = _nameKey;
        DescriptionKey = _descriptionKey;
        EffectType = _effectType;
        EffectParam = _effectParam;
    }
}
