using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;  // 明确指定 Random 为 UnityEngine.Random


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

    public CaseDataSO CurrentCaseData => _currentCaseData;

    private const int DEFAULT_WORDS = 4;

    //单局游戏变量
    private CaseDataSO _currentCaseData;
    private ItemDataSO _currentCaseItems;
    private OptionDataSO _currentCaseOtions;
    private DemonSpeakDataSO _demonSpeak;
    private DebuffDataSO _debuff;

    private Dictionary<int, ItemData> _items = new Dictionary<int, ItemData>();
    public Dictionary<int, ItemData> Items => _items;
    private Dictionary<int, OptionData> _options = new Dictionary<int, OptionData>();
    public Dictionary<int, OptionData> Options => _options;

    //对话次数
    private int _currentWords = DEFAULT_WORDS;
    //案件数据相关
    private List<int> allCaseIds = new List<int>() { 1001, 1002, 1003 };
    private int _currentCaseId;
    private int _currentCaseIndex;
    private int _iscurrentCasePassed;
    //线索相关
    private List<int> _gotCaseItemIds = new List<int>();
    public List<int> GotCaseItemIds => _gotCaseItemIds;
    private int latestItemId;
    public int LatestItemId => latestItemId;
    //当前buff
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
                Time.timeScale = 0f;
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
        ResetAllGameRuntimeData();
        PrepareCaseData(_currentCaseId);
        ScenesManager.Instance?.LoadSceneAsync(Scenes.CaseScenePrefix + _currentCaseId, () =>
        {
            speakerZone = FindAnyObjectByType<SpeakerZone>();
            interaction = InteractionController.Instance;
            interaction.SetSpeakerZone(speakerZone);
            flow = FlowController.Instance;
            flow.FlowInit();
            CaptureSceneSnapshot();
        });
        SwitchGameState(GameState.Playing);
    }

    public void RetryCurrentCase()
    {
        flow.FlowInit(playIntroStory: false);
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
        EventManager.Instance.EventTrigger(GameEvents.CountChanged, _currentWords);
        //需要恢复案件线索板
        UIManager.Instance.HidePanel("case_board_panel");
        UIManager.Instance.ShowPanel<CaseBoardPanel>("case_board_panel");

        SwitchGameState(GameState.Playing);
        if (InputManager.Instance != null) InputManager.Instance.SetInputEnabled(true);
    }
    //是否最后一案判断
    public bool IsLastCase()
    {
        return _currentCaseIndex >= allCaseIds.Count - 1;
    }

    public void NextCase()
    {
        if (IsLastCase())
        {
            flow.FlowStateChange(GameFlowState.TrueEnd);
            return;
        }

        _currentCaseIndex += 1;
        _currentCaseId = allCaseIds[_currentCaseIndex];
        _currentWords = DEFAULT_WORDS;

        _items.Clear();
        _options.Clear();

        _gotCaseItemIds.Clear();

        _sceneSnapshots.Clear();
        PrepareCaseData(_currentCaseId);
        ScenesManager.Instance?.LoadSceneAsync(Scenes.CaseScenePrefix + _currentCaseId, () =>
        {
            speakerZone = FindAnyObjectByType<SpeakerZone>();
            interaction = InteractionController.Instance;
            interaction.SetSpeakerZone(speakerZone);
            flow = FlowController.Instance;
            flow.FlowInit();
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
        _currentCaseData = DataManager.Instance.GetCase(caseId);
        _currentCaseItems = DataManager.Instance.GetItems(caseId);
        _items.Clear();
        foreach (var item in _currentCaseItems.itemDatas)
        {
            _items.Add(item.itemID, item);
        }
        _currentCaseOtions = DataManager.Instance.GetOptions(caseId);
        _options.Clear();
        foreach (var option in _currentCaseOtions.optionDatas)
        {
            _options.Add(option.optionID, option);
        }
        _demonSpeak = DataManager.Instance.GetDemonSpeak();
        _debuff = DataManager.Instance.GetDebuffData();
    }

    // 在活动场景中捕获需要恢复的对象的克隆快照（禁用），保存到 DontDestroyOnLoad 下
    private void CaptureSceneSnapshot()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid()) return;
        if (_sceneSnapshots.ContainsKey(active.name)) return; // 已有快照不重复捕获

        GameObject root = new GameObject($"_SceneSnapshot_{active.name}");
        //DontDestroyOnLoad(root);

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

    public void CheckWord(int itemId)
    {
        _currentWords--;
        EventManager.Instance.EventTrigger<int>(GameEvents.CountChanged, _currentWords);
        if (_currentWords <= 0)
        {
            flow.FlowStateChange(GameFlowState.Death);
        }
        else
        {
            string clue = $"已提取{_items[itemId].itemName}留声：{_items[itemId].clueText}";
            GotCaseItemIds.Add(itemId);
            latestItemId = itemId;
            flow.ShowCluePopup(clue);
        }
    }

    public void CheckCaseWin(int optionId)
    {
        if (_options[optionId].isCorrect)
        {
            flow.ShowSuccess(_currentCaseId);
        }
    }

    public void DemonSpeak()
    {
        int index = Random.Range(0, 3);
        EventManager.Instance.EventTrigger(GameEvents.DemonSpeak, _demonSpeak.demonSpeakDatas[index].defaultSpeak);
    }
    /// <summary>随机取一条恶魔被击败发言（真结局用），不触发事件，交给调用方自行展示</summary>
    public string GetRandomDefeatSpeak()
    {
        if (_demonSpeak == null || _demonSpeak.demonSpeakDatas == null || _demonSpeak.demonSpeakDatas.Count == 0)
        {
            return string.Empty;
        }
        int index = Random.Range(0, _demonSpeak.demonSpeakDatas.Count);
        return _demonSpeak.demonSpeakDatas[index].defeatSpeak;
    }
   /* public void DemonDefeatSpeak()
    {
        int index = Random.Range(0, _demonSpeak.demonSpeakDatas.Count);
        EventManager.Instance.EventTrigger(GameEvents.DemonSpeak, _demonSpeak.demonSpeakDatas[index].defeatSpeak);
    }
   原代码*/
    /// <summary>
    /// 播放开场剧情（恶魔契约对话），整局游戏只在第一次“开始游戏”时播放一次，播完后回调
    /// </summary>
    public void PlayOpeningStory(Action onComplete)
    {
        DemonSpeakDataSO demonSpeak = DataManager.Instance.GetDemonSpeak();
        List<string> lines = demonSpeak != null ? demonSpeak.openingLines : null;
        if (lines == null || lines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(PlayOpeningLines(lines, onComplete));
    }

    private IEnumerator PlayOpeningLines(List<string> lines, Action onComplete)
    {
        StoryDialoguePanel panel = null;
        yield return GetOrLoadStoryDialoguePanel(p => panel = p);
        
        if (panel == null)
        {
            Debug.LogWarning("PlayOpeningLines: 未找到 story_dialogue_panel，跳过开场剧情播放。");
            onComplete?.Invoke();
            yield break;
        }

        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            yield return PlayLineAndWaitForContinue(panel, line);
        }

        UIManager.Instance.HidePanel("story_dialogue_panel");
        onComplete?.Invoke();
    }

    /// <summary>
    /// 获取（必要时加载）通用剧情对话框，通过回调返回实例（找不到时回调传 null）
    /// </summary>
    public IEnumerator GetOrLoadStoryDialoguePanel(Action<StoryDialoguePanel> onReady)
    {
        StoryDialoguePanel panel = UIManager.Instance.GetPanel<StoryDialoguePanel>("story_dialogue_panel");
        if (panel != null)
        {
            onReady?.Invoke(panel);
            yield break;
        }

        bool panelLoaded = false;
        UIManager.Instance.ShowPanel<StoryDialoguePanel>("story_dialogue_panel", E_UILayer.TopLayer, (p) =>
        {
            panel = p;
            panelLoaded = true;
        });
        yield return new WaitUntil(() => panelLoaded);
        onReady?.Invoke(panel);
    }

    /// <summary>播完一句并等玩家点箭头推进（点击时若还在打字会先跳字，需要再点一次才会推进）</summary>
    public IEnumerator PlayLineAndWaitForContinue(StoryDialoguePanel panel, string line)
    {
        bool advance = false;
        Action onContinue = () => advance = true;
        panel.ContinueClicked += onContinue;
        panel.PlayLine(line, null);
        yield return new WaitUntil(() => advance);
        panel.ContinueClicked -= onContinue;
    }

    private void ResetAllGameRuntimeData()
    {
        _currentCaseIndex = 0;
        _currentCaseId = allCaseIds[_currentCaseIndex];
        _currentWords = DEFAULT_WORDS;

        _items.Clear();
        _options.Clear();

        _gotCaseItemIds.Clear();

        _sceneSnapshots.Clear();
    }
}
