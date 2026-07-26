using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
public sealed class BootstrapLoadingView : MonoBehaviour
{
    [Header("启动加载界面UI组件")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _progressFill;
    [SerializeField] private Text _statusText;
    [SerializeField] private Text _progressText;
    [SerializeField] private Text _failureText;

    public Canvas Canvas => _canvas;
    public CanvasGroup CanvasGroup => _canvasGroup;
    public bool IsVisible => gameObject.activeSelf;
    public string CurrentModuleName { get; private set; }
    public float Progress { get; private set; }
    public string FailureMessage { get; private set; }

    private void Awake()
    {
        if (_canvas == null)
        {
            _canvas = GetComponent<Canvas>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void ShowInitializing()
    {
        Show();
        CurrentModuleName = string.Empty;
        Progress = 0f;
        FailureMessage = string.Empty;
        SetProgressFill(0f);
        SetText(_statusText, "正在开始游戏...");
        SetText(_progressText, "0%");
        SetFailureVisible(false);
    }

    public void SetProgress(GameStartProgress progress)
    {
        if (progress == null)
        {
            return;
        }

        Show();
        CurrentModuleName = progress.ModuleName;
        Progress = Mathf.Clamp01(progress.Progress);
        SetProgressFill(Progress);
        SetText(_statusText, $"初始化 {CurrentModuleName}...");
        SetText(_progressText, $"{Mathf.RoundToInt(Progress * 100f)}%");
    }

    public void ShowSceneLoading(string sceneName)
    {
        Show();
        CurrentModuleName = sceneName;
        SetText(_statusText, $"加载 {sceneName}...");
        SetProgressFill(1f);
        SetText(_progressText, "100%");
    }

    public void ShowFailure(GameInitializationFailure failure)
    {
        Show();
        string systemName = failure != null ? failure.SystemName : "Unknown";
        string message = failure != null ? failure.Message : "Unknown startup error.";
        FailureMessage = $"{systemName}: {message}";
        SetText(_statusText, "Startup failed");
        SetText(_failureText, FailureMessage);
        SetFailureVisible(true);
    }

    public IEnumerator Hide(float duration)
    {
        float safeDuration = Mathf.Max(0f, duration);
        if (safeDuration <= 0f)
        {
            gameObject.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / safeDuration);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;
    }

    private void SetProgressFill(float progress)
    {
        if (_progressFill != null)
        {
            _progressFill.fillAmount = progress;
        }
    }

    private static void SetText(Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void SetFailureVisible(bool visible)
    {
        if (_failureText != null)
        {
            _failureText.gameObject.SetActive(visible);
        }
    }
}
