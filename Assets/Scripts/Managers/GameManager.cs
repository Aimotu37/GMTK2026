using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public enum GameState
{
    MainMenu,
    Playing,
    Pause,
    GameOver,
    GameVictory
}
public class GameManager : SingletonMono<GameManager>
{
    private GameState _currentState = GameState.MainMenu;
    public GameState CurrentState => _currentState;

    private const int DEFAULT_WORDS = 4;

    //单局游戏变量
    private List<int> pendingCaseIds = new List<int>();
    private List<int> completedCaseIds = new List<int>();
    private List<int> demonSpeakIds = new List<int>();
    private List<int> debuffIds = new List<int>();

    private List<int> currentCaseOptionIds = new List<int>();
    private List<int> currentCaseItemIds = new List<int>();
    private int _currentWords = DEFAULT_WORDS;
    private int _currentCaseId;
    private int _currentDebuffId;

    private bool _isCorrect;

    [SerializeField]
    private SpeakerZone speakerZone;
    private FlowController flow;
    private InteractionController interaction;

    // 场景快照：按场景名保存一个用于恢复的禁用克隆根对象
    private Dictionary<string, GameObject> _sceneSnapshots = new Dictionary<string, GameObject>();

    //初始化变量
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    private bool _initFailed;

    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
        {
            yield break;
        }

        _initFailed = false;

        if (_initFailed)
        {
            _isInitialized = false;
            Debug.LogError(this.name + " Initialization Failed.");
        }
        else
        {
            _isInitialized = true;
            Debug.Log(this.name + " Initialization Successful.");
        }
    }

    public void SwitchGameState(GameState targetState)
    {
        if (_currentState == targetState) return;
        _currentState = targetState;

        switch (targetState)
        {
            case GameState.MainMenu:
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Pause:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                break;
            case GameState.GameVictory:
                break;
        }

        if (InputManager.Instance != null)
        {
            bool enableInput = targetState == GameState.Playing;
            InputManager.Instance.SetInputEnabled(enableInput);
        }
    }

    //游戏行为
    public void StartNewGame()
    {
        _currentCaseId = 1;
        _currentWords = DEFAULT_WORDS;
        PrepareCaseData(_currentCaseId);
        ScenesManager.Instance?.LoadSceneAsync(Scenes.Interaction_Test_SceneName, () =>
        {
            UIManager.Instance.HidePanel("main_menu_panel");
            speakerZone = FindAnyObjectByType<SpeakerZone>();
            interaction = InteractionController.Instance;
            interaction.SetSpeakerZone(speakerZone);
            flow = FlowController.Instance;
            // 捕获场景初始快照，便于后续不切场景复原
            CaptureSceneSnapshot();
        });
        SwitchGameState(GameState.Playing);
    }

    public void RetryCurrentCase()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid())
        {
            Debug.LogWarning("RetryCurrentCase: 无效的活动场景。");
            return;
        }

        // 禁用输入并重置交互器状态
        if (InputManager.Instance != null) InputManager.Instance.SetInputEnabled(false);
        if (interaction == null) interaction = InteractionController.Instance;
        interaction?.ResetController();

        // 如果没有快照则退回到场景重载作为兜底
        if (!_sceneSnapshots.TryGetValue(active.name, out var snapshotRoot) || snapshotRoot == null)
        {
            Debug.LogWarning($"RetryCurrentCase: 场景 {active.name} 没有快照，使用场景重载作为回退。");
            ScenesManager.Instance?.LoadSceneAsync(active.name, () =>
            {
                speakerZone = FindAnyObjectByType<SpeakerZone>();
                interaction = InteractionController.Instance;
                interaction?.SetSpeakerZone(speakerZone);
                flow = FlowController.Instance;
                _currentWords = DEFAULT_WORDS;
            });
            return;
        }

        // 销毁当前场景中可交互对象（Item、SpeakerZone），以便用快照恢复
        Item[] items = GameObject.FindObjectsOfType<Item>(true);
        foreach (var it in items)
        {
            if (it == null) continue;
            if (!it.gameObject.scene.IsValid()) continue;
            if (it.gameObject.scene == active) GameObject.Destroy(it.gameObject);
        }

        // 从快照中实例化备份对象到活动场景
        for (int i = 0; i < snapshotRoot.transform.childCount; i++)
        {
            var snap = snapshotRoot.transform.GetChild(i).gameObject;
            if (snap == null) continue;
            GameObject inst = GameObject.Instantiate(snap);
            inst.SetActive(true);
            SceneManager.MoveGameObjectToScene(inst, active);
        }

        // 恢复单局变量并启用输入
        _currentWords = DEFAULT_WORDS;
        SwitchGameState(GameState.Playing);
        if (InputManager.Instance != null) InputManager.Instance.SetInputEnabled(true);
    }

    public void NextCase()
    {
        _currentCaseId += 1;
        PrepareCaseData(_currentCaseId);
        ScenesManager.Instance?.LoadSceneAsync(Scenes.Interaction_Test_1_SceneName, () =>
        {
            speakerZone = FindAnyObjectByType<SpeakerZone>();
            interaction = InteractionController.Instance;
            interaction.SetSpeakerZone(speakerZone);
            flow = FlowController.Instance;
            CaptureSceneSnapshot();
        });
    }

    public void PauseGame()
    {
        if (_currentState == GameState.Playing)
        {
            SwitchGameState(GameState.Pause);
        }
        else if (_currentState == GameState.Pause)
        {
            SwitchGameState(GameState.Playing);
        }
    }

    public void GameOver()
    {
        SwitchGameState(GameState.GameOver);
        //1、局内交互锁定
        EventManager.Instance.EventTrigger(GameEvents.GameOver);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SwitchGameState(GameState.MainMenu);
        ScenesManager.Instance.LoadSceneAsync(Scenes.MainMenuSceneName, () =>
        {
            UIManager.Instance.ShowPanel<MainMenuPanel>("main_menu_panel");
        });
    }

    //游戏场景内业务逻辑
    public void PrepareCaseData(int caseId)
    {
        _currentCaseId = caseId;
        //TODO：必须，加载当前案件配置
    }

    // 在活动场景中捕获需要恢复的对象的克隆快照（禁用），保存到 DontDestroyOnLoad 下
    private void CaptureSceneSnapshot()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid()) return;
        if (_sceneSnapshots.ContainsKey(active.name)) return; // 已有快照不重复捕获

        GameObject root = new GameObject($"_SceneSnapshot_{active.name}");
        DontDestroyOnLoad(root);

        // 仅捕获需要恢复的类型，避免克隆单例或管理器
        Item[] items = GameObject.FindObjectsOfType<Item>(true);
        foreach (var it in items)
        {
            if (it == null) continue;
            if (it.gameObject.scene != active) continue;
            GameObject clone = GameObject.Instantiate(it.gameObject);
            clone.SetActive(false);
            clone.transform.SetParent(root.transform, false);
        }
        _sceneSnapshots[active.name] = root;
    }

    public void CheckWord(string itemId)
    {
        _currentWords--;
        if (_currentWords <= 0)
        {
            flow.FlowStateChange(GameFlowState.Death);
        }
        else if (_currentWords == 1)
        {
            CheckWin("1");
        }
        else
        {
            flow.ShowCluePopup(itemId);
        }
    }

    public void CheckWin(string caseId)
    {
        flow.ShowSuccess(caseId);
    }
}
