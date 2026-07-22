public sealed class GameInitializationFailure
{
    public GameInitializationFailure(string systemName, string message)
    {
        SystemName = systemName;
        Message = message;
    }

    public string SystemName { get; }
    public string Message { get; }
}
