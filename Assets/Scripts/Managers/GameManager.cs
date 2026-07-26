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
public class GameManager : SingletonMono<GameManager>, ISaveable
{
    public string SavedId => "Game_Saved";
    private const int DEFAULT_WORDS = 4;
    private List<int> allCaseIds = new List<int>() { 1001, 1002, 1003 };
    //每次发言后触发诅咒的概率（0~1），不是每次都触发
    private const float DEBUFF_TRIGGER_CHANCE = 0.2f;
    //游戏运行变量
    //运行时保存变量
    //案件数据相关
    private int _currentCaseId;
    private int _currentCaseIndex;
    private int _iscurrentCasePassed;
    private bool _gameCompleted;

    //运行时不保存变量
    //对话次数
    private int _currentWords;
    private GameState _currentState;
    private int latestItemId;
    private int _currentDebuffId;
    //线索相关
    private List<int> _gotCaseItemIds = new List<int>();

    public GameState CurrentState => _currentState;
    public List<int> GotCaseItemIds => _gotCaseItemIds;
    public CaseDataSO CurrentCaseData => _currentCaseData;
    public int LatestItemId => latestItemId;
    public int CurrentCaseIndex => _currentCaseIndex;

    //单局游戏变量
    private CaseDataSO _currentCaseData;
    private ItemDataSO _currentCaseItems;
    private OptionDataSO _currentCaseOtions;
    private DemonSpeakDataSO _demonSpeak;
    private DebuffDataSO _debuff;

    private Dictionary<int, ItemData> _items = new Dictionary<int, ItemData>();
    private Dictionary<int, OptionData> _options = new Dictionary<int, OptionData>();

    public Dictionary<int, ItemData> Items => _items;
    public Dictionary<int, OptionData> Options => _options;


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
            _currentState = GameState.MainMenu;
            _currentWords = DEFAULT_WORDS;
            (this as ISaveable).RegisterSaveable();
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
            SaveManager.Instance.SaveGameData();
        });
        SwitchGameState(GameState.Playing);
    }

    public void RetryCurrentCase()
    {
        _gotCaseItemIds.Clear();
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
        flow.FlowInit(playIntroStory: false);
        SwitchGameState(GameState.Playing);
        if (InputManager.Instance != null) InputManager.Instance.SetInputEnabled(true);
    }
    //是否最后一案判断
    public bool IsLastCase()
    {
        return _currentCaseIndex >= allCaseIds.Count - 1;
    }

    public bool IsValidCaseId(int caseId)
    {
        return allCaseIds.Contains(caseId);
    }

    public void NextCase()
    {
        if (IsLastCase())
        {
            _gameCompleted = true;
            SaveManager.Instance.SaveGameData();
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
            SaveManager.Instance.SaveGameData();
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
            UIManager.Instance.HidePanel("demon_panel");
            UIManager.Instance.HidePanel("case_board_panel");
            UIManager.Instance.ShowPanel<MainMenuPanel>("main_menu_panel");
        });
    }

    public void LoadEndingScene()
    {
        Time.timeScale = 1f;
        SwitchGameState(GameState.GameVictory);
        ScenesManager.Instance.SwitchGameScene(Scenes.EndingSceneName, sceneLoaded =>
        {
            if (!sceneLoaded)
            {
                Debug.LogError($"Failed to load ending scene {Scenes.EndingSceneName}.");
                LoadMainMenu();
                return;
            }

            UIManager.Instance.HidePanel("main_menu_panel");
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

    public void CheckWord(int itemId)
    {
        _currentWords--;
        EventManager.Instance.EventTrigger<int>(GameEvents.CountChanged, _currentWords);
        if (_currentWords <= 0)
        {
            flow.FlowStateChange(GameFlowState.CaseFail);
        }
        else
        {
            string clue = $"已提取{_items[itemId].itemName}留声：{_items[itemId].clueText}";
            GotCaseItemIds.Add(itemId);
            latestItemId = itemId;
            flow.ShowCluePopup(clue);
            //TriggerRandomDebuff();
        }
    }

    /// <summary>按概率决定这次是否触发诅咒；由线索弹窗播完之后调用，避免跟线索信息同时挤在一起</summary>
    public void TryTriggerDebuff()
    {
        if (Random.value < DEBUFF_TRIGGER_CHANCE)
        {
            StartCoroutine(TriggerRandomDebuffRoutine());
        }
    }
    /// <summary>
    /// 每次发言后随机触发一条诅咒：先完整显示 DebuffDesc（描述，恶魔发言区）/ DebuffName（名称，"恶魔的诅咒"小框），
    /// 留时间给玩家读完，之后才真正扣发言次数、判定是否死亡——避免死亡流程自己的嘲讽发言把诅咒描述立刻覆盖掉。
    /// 目前只启用"发言次数减少"效果，"覆盖线索"（CoverRandomClue）先不参与随机，逻辑已就绪，需要时把下面的过滤条件去掉即可。
    /// </summary>
    private IEnumerator TriggerRandomDebuffRoutine()
    {
        if (_debuff == null || _debuff.debuffDatas == null || _debuff.debuffDatas.Count == 0) yield break;
        List<DebuffData> candidates = _debuff.debuffDatas.FindAll(d => d.effectType == EffectType.ReduceSpeak);
        if (candidates.Count == 0) yield break;

        int index = Random.Range(0, candidates.Count);
        DebuffData debuff = candidates[index];
        _currentDebuffId = debuff.debuffID;

        EventManager.Instance.EventTrigger(GameEvents.DemonSpeak, debuff.debuffDesc);
        EventManager.Instance.EventTrigger(GameEvents.DebuffEffect, debuff.debuffName);

        // 给玩家留时间读完诅咒描述，再让效果真正生效
        yield return new WaitForSeconds(2.0f);

        if (int.TryParse(debuff.effectParam, out int reduceAmount) && reduceAmount > 0)
        {
            _currentWords = Mathf.Max(0, _currentWords - reduceAmount);
            EventManager.Instance.EventTrigger(GameEvents.CountChanged, _currentWords);
            if (_currentWords <= 0)
            {
                flow.FlowStateChange(GameFlowState.CaseFail);
            }
        }

    }

    /// <summary>从已获得的线索里随机挑一条，重新盖回"未获得"状态</summary>
    private void CoverRandomClue()
    {
        if (_gotCaseItemIds.Count == 0) return;

        int pick = Random.Range(0, _gotCaseItemIds.Count);
        int itemId = _gotCaseItemIds[pick];
        _gotCaseItemIds.RemoveAt(pick);

        EventManager.Instance.EventTrigger(GameEvents.CoverClue, itemId);
        EventManager.Instance.EventTrigger(GameEvents.CheckCaseClues, _gotCaseItemIds.Count > 0);
    }

    public void CheckCaseWin(int optionId)
    {
        if (_options[optionId].isCorrect)
        {
            flow.ShowSuccess(_currentCaseId);
        }
        else
        {
            flow.FlowStateChange(GameFlowState.CaseFail);
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
        DemonSpeakDataSO demonSpeak = _demonSpeak;
        if (demonSpeak == null && DataManager.Instance != null)
        {
            demonSpeak = DataManager.Instance.GetDemonSpeak();
        }

        if (demonSpeak == null || demonSpeak.demonSpeakDatas == null || demonSpeak.demonSpeakDatas.Count == 0)
        {
            return string.Empty;
        }
        int index = Random.Range(0, demonSpeak.demonSpeakDatas.Count);
        return demonSpeak.demonSpeakDatas[index].defeatSpeak;
    }
    /* public void DemonDefeatSpeak()
     {
         int index = Random.Range(0, _demonSpeak.demonSpeakDatas.Count);
         EventManager.Instance.EventTrigger(GameEvents.DemonSpeak, _demonSpeak.demonSpeakDatas[index].defeatSpeak);
     }
    原代码*/

    /// <summary>成功还原案件时，让恶魔说一句受伤发言（复用 demon_panel 常驻发言区）</summary>
    public void DemonHurtSpeak()
    {
        if (_demonSpeak == null || _demonSpeak.demonSpeakDatas == null || _demonSpeak.demonSpeakDatas.Count == 0) return;
        int index = Random.Range(0, _demonSpeak.demonSpeakDatas.Count);
        string speak = _demonSpeak.demonSpeakDatas[index].hurtSpeak;
        if (!string.IsNullOrEmpty(speak))
        {
            EventManager.Instance.EventTrigger(GameEvents.DemonSpeak, speak);
        }
    }

    /// <summary>玩家死亡时，让恶魔说一句嘲讽发言（复用 demon_panel 常驻发言区）</summary>
    public void DemonMockSpeak()
    {
        if (_demonSpeak == null || _demonSpeak.demonSpeakDatas == null || _demonSpeak.demonSpeakDatas.Count == 0) return;
        int index = Random.Range(0, _demonSpeak.demonSpeakDatas.Count);
        string speak = _demonSpeak.demonSpeakDatas[index].mockSpeak;
        if (!string.IsNullOrEmpty(speak))
        {
            EventManager.Instance.EventTrigger(GameEvents.DemonSpeak, speak);
        }
    }

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

    public void PlayEndingStory(Action onComplete)
    {
        StartCoroutine(PlayEndingStoryRoutine(onComplete));
    }

    private IEnumerator PlayEndingStoryRoutine(Action onComplete)
    {
        string endingLine = GetRandomDefeatSpeak();
        if (string.IsNullOrEmpty(endingLine))
        {
            Debug.LogWarning("PlayEndingStory: 未找到有效的结局台词，返回主菜单。");
            onComplete?.Invoke();
            yield break;
        }

        StoryDialoguePanel panel = null;
        yield return GetOrLoadStoryDialoguePanel(p => panel = p);
        if (panel == null)
        {
            Debug.LogWarning("PlayEndingStory: 未找到 story_dialogue_panel，返回主菜单。");
            onComplete?.Invoke();
            yield break;
        }

        yield return PlayLineAndWaitForContinue(panel, endingLine);
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


    /// <summary>
    /// 开场剧情播放完成后调用：渐黑挡住案件一场景加载，再真正进入案件一
    /// </summary>
    public void FadeToBlackThenStartNewGame(float fadeDuration = 1.5f)
    {
        StartCoroutine(FadeToBlackThenStartNewGameRoutine(fadeDuration));
    }

    private IEnumerator FadeToBlackThenStartNewGameRoutine(float fadeDuration)
    {
        ScreenFader fader = null;
        yield return GetOrLoadScreenFader(f => fader = f);

        if (fader != null)
        {
            yield return fader.FadeOut(fadeDuration);
        }

        StartNewGame();
    }

    /// <summary>
    /// 获取（必要时加载）全屏渐变遮罩，通过回调返回实例（找不到时回调传 null）
    /// </summary>
    public IEnumerator GetOrLoadScreenFader(Action<ScreenFader> onReady)
    {
        ScreenFader fader = UIManager.Instance.GetPanel<ScreenFader>("screen_fader");
        if (fader != null)
        {
            onReady?.Invoke(fader);
            yield break;
        }

        bool faderLoaded = false;
        UIManager.Instance.ShowPanel<ScreenFader>("screen_fader", E_UILayer.SystemLayer, (p) =>
        {
            fader = p;
            faderLoaded = true;
        });
        yield return new WaitUntil(() => faderLoaded);
        onReady?.Invoke(fader);
    }

    /// <summary>如果当前屏幕是黑的（渐黑遮罩存在），渐显出来；没有遮罩则什么都不做</summary>
    public IEnumerator FadeInScreenIfNeeded(float duration = 0.8f)
    {
        ScreenFader fader = UIManager.Instance.GetPanel<ScreenFader>("screen_fader");
        if (fader != null)
        {
            yield return fader.FadeIn(duration);
        }
    }

    /// <summary>
    /// 通用转场：渐隐（黑屏挡住）→ 执行 duringBlack（切换面板/内容，此时玩家看不到）→ 渐显。
    /// 用于任意"上一屏内容→下一屏内容"之间需要遮盖硬切的地方。
    /// </summary>
    public IEnumerator FadeTransition(Action duringBlack, float fadeOutDuration = 0.5f, float fadeInDuration = 0.5f)
    {
        ScreenFader fader = null;
        yield return GetOrLoadScreenFader(f => fader = f);

        if (fader != null)
        {
            yield return fader.FadeOut(fadeOutDuration);
        }

        duringBlack?.Invoke();
        yield return null; // 让 SetActive/Hide 等操作先生效一帧，再开始渐显

        if (fader != null)
        {
            yield return fader.FadeIn(fadeInDuration);
        }
    }

    private void ResetAllGameRuntimeData()
    {
        _currentCaseIndex = 0;
        _currentCaseId = allCaseIds[_currentCaseIndex];
        _currentWords = DEFAULT_WORDS;
        _gameCompleted = false;
        latestItemId = 0;
        _currentDebuffId = 0;

        _items.Clear();
        _options.Clear();

        _gotCaseItemIds.Clear();

        _sceneSnapshots.Clear();
    }
    // ============ 以下方法仅供 DebugTestHelper 测试使用，不参与正式游戏流程 ============

    /// <summary>【测试专用】无视概率，强制触发一次诅咒</summary>
    public void DebugForceTriggerDebuff()
    {
        StartCoroutine(TriggerRandomDebuffRoutine());
    }

    /// <summary>【测试专用】直接跳转到指定案件（1001/1002/1003），不需要按顺序打前面的案件</summary>
    public void DebugJumpToCase(int caseId)
    {
        int idx = allCaseIds.IndexOf(caseId);
        if (idx < 0)
        {
            Debug.LogWarning($"DebugJumpToCase: 找不到案件ID {caseId}");
            return;
        }

        _currentCaseIndex = idx;
        _currentCaseId = caseId;
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
        SwitchGameState(GameState.Playing);
    }

    /// <summary>【测试专用】跳过前面所有案件，直接触发真结局（需要已经进过至少一个案件场景，_demonSpeak 才有数据）</summary>
    public void DebugForceTrueEnd()
    {
        if (flow == null) flow = FlowController.Instance;
        if (_demonSpeak == null)
        {
            Debug.LogWarning("DebugForceTrueEnd: 还没进过任何案件场景，_demonSpeak 数据为空，先按 F1/F2/F3 进一个案件再测。");
            return;
        }
        flow.FlowStateChange(GameFlowState.TrueEnd);
    }

    /// <summary>【测试专用】强制判定当前案件死亡</summary>
    public void DebugForceDeath()
    {
        if (flow == null) flow = FlowController.Instance;
        flow.FlowStateChange(GameFlowState.CaseFail);
    }

    /// <summary>【测试专用】强制判定当前案件成功</summary>
    public void DebugForceSuccess()
    {
        if (flow == null) flow = FlowController.Instance;
        flow.ShowSuccess(_currentCaseId);
    }

    public ISavedData SaveData()
    {
        if (!allCaseIds.Contains(_currentCaseId) ||
            (ScenesManager.Instance != null && ScenesManager.Instance.IsSwitching))
        {
            Debug.LogWarning("Game progress cannot be saved before a case is ready.");
            return null;
        }

        return new GameProgressSavedData
        {
            currentCaseId = _currentCaseId,
            gameCompleted = _gameCompleted
        };
    }

    public void LoadData(ISavedData data)
    {
        if (!(data is GameProgressSavedData gameData))
        {
            Debug.LogError("Game save data has an invalid type.");
            return;
        }

        int caseIndex = allCaseIds.IndexOf(gameData.currentCaseId);
        if (caseIndex < 0)
        {
            Debug.LogError("Game save data contains invalid progress values.");
            return;
        }

        _currentCaseId = gameData.currentCaseId;
        _currentCaseIndex = caseIndex;
        _currentWords = DEFAULT_WORDS;
        _gameCompleted = gameData.gameCompleted;
        latestItemId = 0;
        _currentDebuffId = 0;
        _items.Clear();
        _options.Clear();
        _gotCaseItemIds.Clear();
        _sceneSnapshots.Clear();

        if (_gameCompleted)
        {
            LoadEndingScene();
            return;
        }

        PrepareCaseData(_currentCaseId);

        if (InputManager.Instance != null)
            InputManager.Instance.SetInputEnabled(false);

        string sceneName = Scenes.CaseScenePrefix + _currentCaseId;
        ScenesManager.Instance.SwitchGameScene(sceneName, sceneLoaded =>
        {
            if (!sceneLoaded)
            {
                Debug.LogError($"Failed to load saved case scene {sceneName}.");
                LoadMainMenu();
                return;
            }

            speakerZone = FindAnyObjectByType<SpeakerZone>();
            interaction = InteractionController.Instance;
            if (speakerZone == null || interaction == null)
            {
                Debug.LogError("Saved case scene is missing its interaction objects.");
                LoadMainMenu();
                return;
            }

            interaction.SetSpeakerZone(speakerZone);
            flow = FlowController.Instance;
            flow.FlowInit(playIntroStory: true);
            CaptureSceneSnapshot();
            SwitchGameState(GameState.Playing);
        });
    }
}
