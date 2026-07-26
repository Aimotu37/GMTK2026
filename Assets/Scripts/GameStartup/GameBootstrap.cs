using System.Collections;
using UnityEngine;

public enum BootstrapState
{
    None,
    Initializing,
    Ready,
    Failed
}

[DisallowMultipleComponent]
public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] private float _fadeOutDuration = 0.35f;
    [SerializeField] private BootstrapLoadingView _loadingView;

    private bool _waitingForMainMenu;

    public BootstrapState State { get; private set; }
    public string CurrentModuleName { get; private set; }
    public float Progress { get; private set; }
    public GameInitializationFailure Failure { get; private set; }

    private void Awake()
    {
        if (_loadingView == null)
        {
            Debug.LogError("GameBootstrap requires a BootstrapLoadingView from the Persistent scene.");
        }
    }

    private void OnDestroy()
    {
        if (_waitingForMainMenu)
        {
            //EventCenterMgr.Instance.EventUnregister<string>(GameEvents.SceneLoaded, HandleSceneLoaded);
        }
    }

    private IEnumerator Start()
    {
        yield return RunStartup();
    }

    public IEnumerator RunStartup()
    {
        if (State == BootstrapState.Ready || State == BootstrapState.Initializing)
        {
            yield break;
        }
        //初始化状态
        State = BootstrapState.Initializing;
        Failure = null;
        CurrentModuleName = string.Empty;
        Progress = 0f;
        if (_loadingView == null)
        {
            State = BootstrapState.Failed;
            yield break;
        }
        //加载界面显示
        _loadingView.ShowInitializing();
        yield return null;
        //游戏启动管线
        GameStartPipeline pipeline = CreatePipeline();
        yield return pipeline.Run(HandleProgress, HandleFailure);

        if (State == BootstrapState.Failed)
        {
            yield break;
        }

        yield return GameManager.Instance.InitializeAsync();
        if (!GameManager.Instance.IsInitialized)
        {
            throw new System.InvalidOperationException("GameManager initialization did not complete.");
        }
        State = BootstrapState.Ready;
        _waitingForMainMenu = true;
        //EventCenterMgr.Instance.EventRegister<string>(GameEvents.SceneLoaded, HandleSceneLoaded);
        _loadingView.ShowSceneLoading(Scenes.MainMenuSceneName);
        GameManager.Instance.LoadMainMenuImmediately(mainMenuReady =>
        {
            if (mainMenuReady)
            {
                HandleSceneLoaded(Scenes.MainMenuSceneName);
            }
            else
            {
                _waitingForMainMenu = false;
                State = BootstrapState.Failed;
                Debug.LogError("Game startup failed while loading the main menu.");
            }
        });
    }

    private static GameStartPipeline CreatePipeline()
    {
        return new GameStartPipeline(new IGameStartModule[]
        {
            new CoreServicesStartModule(),
            new RuntimeServicesStartModule(),
            new UIStartModule()
        });
    }

    private void HandleProgress(GameStartProgress progress)
    {
        CurrentModuleName = progress.ModuleName;
        Progress = progress.Progress;
        _loadingView.SetProgress(progress);
        //EventCenterMgr.Instance.EventTrigger(GameEvents.StartProgress, progress);
    }

    private void HandleFailure(GameInitializationFailure failure)
    {
        Failure = failure;
        State = BootstrapState.Failed;
        _loadingView.ShowFailure(failure);
        Debug.LogError($"Game startup module failed: {failure.SystemName}. {failure.Message}");
        //EventCenterMgr.Instance.EventTrigger(GameEvents.InitializationFailed, failure);
    }

    private void HandleSceneLoaded(string sceneName)
    {
        if (!_waitingForMainMenu || sceneName != Scenes.MainMenuSceneName)
        {
            return;
        }
        _waitingForMainMenu = false;
        //EventCenterMgr.Instance.EventUnregister<string>(GameEvents.SceneLoaded, HandleSceneLoaded);
        StartCoroutine(_loadingView.Hide(_fadeOutDuration));
    }
}
