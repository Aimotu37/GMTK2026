public sealed class MetaConfig
{
    public int SchemaVersion { get; }
    public string ContentVersion { get; }
    public int DefaultCaseId { get; }

    public MetaConfig(
        int _schemaVersion,
        string _contentVersion,
        int _defaultCaseId)
    {
        SchemaVersion = _schemaVersion;
        ContentVersion = _contentVersion;
        DefaultCaseId = _defaultCaseId;
    }
}
