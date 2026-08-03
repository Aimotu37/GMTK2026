using System.Collections;

public sealed class RuntimeServicesStartModule : IGameStartModule
{
    public string ModuleName => "RuntimeServices";
    public float ProgressWeight => 1f;
    public bool IsInitialized { get; private set; }

    public IEnumerator Initialize()
    {
        if (IsInitialized)
        {
            yield break;
        }

        SettingsManager.Instance.Initialize();

        bool localeApplied = false;
        yield return DataManager.Instance.SetLocaleAsync(
            SettingsManager.Instance.LanguageCode,
            success => localeApplied = success);
        if (!localeApplied)
        {
            UnityEngine.Debug.LogWarning("Saved language could not be applied.");
        }

        yield return AudioManager.Instance.InitializeAsync();
        if (!AudioManager.Instance.IsInitialized)
        {
            throw new System.InvalidOperationException("AudioManager initialization did not complete.");
        }
        //CameraController.Instance.InitializeService();
        IsInitialized = true;
    }
}
