using System.Collections;

public sealed class CoreServicesStartModule : IGameStartModule
{
    public string ModuleName => "CoreServices";
    public float ProgressWeight => 2f;
    public bool IsInitialized { get; private set; }

    public IEnumerator Initialize()
    {
        if (IsInitialized)
        {
            yield break;
        }

        yield return DataManager.Instance.InitializeAsync();
        if (!DataManager.Instance.IsInitialized)
        {
            throw new System.InvalidOperationException("DataManager initialization did not complete.");
        }

        yield return EventManager.Instance.InitializeAsync();
        if (!EventManager.Instance.IsInitialized)
        {
            throw new System.InvalidOperationException("EventManager initialization did not complete.");
        }

        yield return ResourcesManager.Instance.InitializeAsync();
        if (!ResourcesManager.Instance.IsInitialized)
        {
            throw new System.InvalidOperationException("ResourcesManager initialization did not complete.");
        }

        yield return ScenesManager.Instance.InitializeAsync();
        if (!ScenesManager.Instance.IsInitialized)
        {
            throw new System.InvalidOperationException("ScenesManager initialization did not complete.");
        }

        yield return SaveManager.Instance.InitializeAsync();
        if (!SaveManager.Instance.IsInitialized)
        {
            throw new System.InvalidOperationException("SaveManager initialization did not complete.");
        }

        IsInitialized = true;
    }
}
