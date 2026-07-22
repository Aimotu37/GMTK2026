public sealed class GameStartProgress
{
    public GameStartProgress(string moduleName, float progress)
    {
        ModuleName = moduleName;
        Progress = progress;
    }

    public string ModuleName { get; }
    public float Progress { get; }
}
