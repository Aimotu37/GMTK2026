using System.Collections;

public sealed class UIStartModule : IGameStartModule
{
    public string ModuleName => "UI";
    public float ProgressWeight => 1f;
    public bool IsInitialized { get; private set; }

    public IEnumerator Initialize()
    {
        if (IsInitialized)
        {
            yield break;
        }

        yield return UIManager.Instance.InitializeAsync();
        if (!UIManager.Instance.IsInitialized)
        {
            throw new System.InvalidOperationException("UIManager initialization did not complete.");
        }

        IsInitialized = true;
    }
}
