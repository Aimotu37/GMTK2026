public sealed class ItemsConfig
{
    public int ItemId { get; }
    public int CaseId { get; }
    public int DisplayOrder { get; }
    public string NameKey { get; }
    public string ClueKey { get; }

    public ItemsConfig(
        int _itemId,
        int _caseId,
        int _displayOrder,
        string _nameKey,
        string _clueKey)
    {
        ItemId = _itemId;
        CaseId = _caseId;
        DisplayOrder = _displayOrder;
        NameKey = _nameKey;
        ClueKey = _clueKey;
    }
}
