public sealed class OptionsConfig
{
    public int OptionId { get; }
    public int CaseId { get; }
    public int DisplayOrder { get; }
    public string TextKey { get; }
    public bool IsCorrect { get; }

    public OptionsConfig(
        int _optionId,
        int _caseId,
        int _displayOrder,
        string _textKey,
        bool _isCorrect)
    {
        OptionId = _optionId;
        CaseId = _caseId;
        DisplayOrder = _displayOrder;
        TextKey = _textKey;
        IsCorrect = _isCorrect;
    }
}
