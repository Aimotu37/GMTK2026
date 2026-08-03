using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
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
    public CaseConfig CurrentCaseConfig => _currentCaseConfig;
    public int LatestItemId => latestItemId;
    public int CurrentCaseIndex => _currentCaseIndex;

    //单局游戏变量
    private CaseConfig _currentCaseConfig;
    private IReadOnlyList<DemonLinesConfig> _demonLines = new List<DemonLinesConfig>();
    private IReadOnlyList<DebuffConfig> _debuffs = new List<DebuffConfig>();

    private Dictionary<int, ItemsConfig> _items = new Dictionary<int, ItemsConfig>();
    private Dictionary<int, OptionsConfig> _options = new Dictionary<int, OptionsConfig>();
    private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _portraitHandles =
        new Dictionary<string, AsyncOperationHandle<Sprite>>();

    public Dictionary<int, ItemsConfig> Items => _items;
    public Dictionary<int, OptionsConfig> Options => _options;


    private bool _isCorrect;

    [SerializeField]
    private SpeakerZone speakerZone;
    private FlowController flow;
    private InteractionController interaction;

    // 场景快照：按场景名保存一个用于恢复的禁用克隆根对象
    private Dictionary<string, GameObject> _sceneSnapshots = new Dictionary<string, GameObject>();
    private bool _isSceneTransitioning;

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

        var cases = new List<CaseConfig>(DataManager.Instance.ConfigSnapshot.Cases.Values);
        cases.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));
        allCaseIds.Clear();
        foreach (CaseConfig config in cases)
        {
            allCaseIds.Add(config.CaseId);
        }

        if (allCaseIds.Count == 0)
        {
            _initFailed = true;
            Debug.LogError("Game config does not contain any cases.");
        }

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

    private void OnDestroy()
    {
        foreach (AsyncOperationHandle<Sprite> handle in _portraitHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        _portraitHandles.Clear();
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
    }

    //游戏行为
    public void StartNewGame()
    {
        ResetAllGameRuntimeData();
        PrepareCaseData(_currentCaseId);
        string sceneName = _currentCaseConfig.SceneName;
        ScenesManager.Instance?.SwitchGameScene(sceneName, sceneLoaded =>
        {
            if (!sceneLoaded)
            {
                Debug.LogError($"Failed to load first case scene {sceneName}.");
                RecoverToMainMenuWhileBlack();
                return;
            }

            speakerZone = FindAnyObjectByType<SpeakerZone>();
            interaction = InteractionController.Instance;
            flow = FlowController.Instance;
            if (speakerZone == null || interaction == null || flow == null)
            {
                Debug.LogError($"First case scene {sceneName} is missing required gameplay objects.");
                RecoverToMainMenuWhileBlack();
                return;
            }

            interaction.SetSpeakerZone(speakerZone);
            EndSceneTransition();
            flow.FlowInit();
            CaptureSceneSnapshot();
            SaveManager.Instance.SaveGameData();
            SwitchGameState(GameState.Playing);
        });
    }

    public bool RetryCurrentCase()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid())
        {
            Debug.LogWarning("RetryCurrentCase: 无效的活动场景。");
            return false;
        }

        // 如果没有快照则退回到场景重载作为兜底
        if (!_sceneSnapshots.TryGetValue(active.name, out var snapshotRoot) || snapshotRoot == null)
        {
            _gotCaseItemIds.Clear();
            if (InputManager.Instance != null) InputManager.Instance.SetInputEnabled(false);
            if (interaction == null) interaction = InteractionController.Instance;
            interaction?.ResetController();

            Debug.LogWarning($"RetryCurrentCase: 场景 {active.name} 没有快照，使用场景重载作为回退。");
            ScenesManager.Instance?.LoadSceneAsync(active.name, () =>
            {
                speakerZone = FindAnyObjectByType<SpeakerZone>();
                interaction = InteractionController.Instance;
                interaction?.SetSpeakerZone(speakerZone);
                flow = FlowController.Instance;
                _currentWords = GetCurrentCaseInitialWords();
            });
            return false;
        }

        if (!TryBeginSceneTransition()) return true;
        StartCoroutine(RetryCurrentCaseRoutine(active, snapshotRoot));
        return true;
    }

    private IEnumerator RetryCurrentCaseRoutine(Scene active, GameObject snapshotRoot)
    {
        ScreenFader fader = null;
        yield return GetOrLoadScreenFader(value => fader = value);
        if (fader == null)
        {
            Debug.LogError("RetryCurrentCase failed to load the screen fader.");
            EndSceneTransition();
            yield break;
        }

        yield return fader.FadeOut(0.5f);
        UIManager.Instance.HidePanel("death_panel");

        _gotCaseItemIds.Clear();
        if (InputManager.Instance != null) InputManager.Instance.SetInputEnabled(false);
        if (interaction == null) interaction = InteractionController.Instance;
        interaction?.ResetController();

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
        _currentWords = GetCurrentCaseInitialWords();
        EventManager.Instance.EventTrigger(GameEvents.CountChanged, _currentWords);
        EndSceneTransition();
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
        if (!TryBeginSceneTransition()) return;
        StartCoroutine(NextCaseRoutine());
    }

    private IEnumerator NextCaseRoutine()
    {
        ScreenFader fader = null;
        yield return GetOrLoadScreenFader(value => fader = value);
        if (fader == null)
        {
            Debug.LogError("NextCase failed to load the screen fader.");
            EndSceneTransition();
            yield break;
        }

        yield return fader.FadeOut(0.5f);
        UIManager.Instance.HidePanel("story_dialogue_panel");
        UIManager.Instance.HidePanel("success_panel");

        if (IsLastCase())
        {
            _gameCompleted = true;
            SaveManager.Instance.SaveGameData();
            EndSceneTransition();
            flow.FlowStateChange(GameFlowState.TrueEnd);
            yield break;
        }

        _currentCaseIndex += 1;
        _currentCaseId = allCaseIds[_currentCaseIndex];
        _currentWords = DEFAULT_WORDS;

        _items.Clear();
        _options.Clear();

        _gotCaseItemIds.Clear();

        _sceneSnapshots.Clear();
        PrepareCaseData(_currentCaseId);
        string sceneName = _currentCaseConfig.SceneName;
        ScenesManager.Instance?.SwitchGameScene(sceneName, sceneLoaded =>
        {
            if (!sceneLoaded)
            {
                Debug.LogError($"Failed to load next case scene {sceneName}.");
                RecoverToMainMenuWhileBlack();
                return;
            }

            speakerZone = FindAnyObjectByType<SpeakerZone>();
            interaction = InteractionController.Instance;
            flow = FlowController.Instance;
            if (speakerZone == null || interaction == null || flow == null)
            {
                Debug.LogError($"Next case scene {sceneName} is missing required gameplay objects.");
                RecoverToMainMenuWhileBlack();
                return;
            }

            interaction.SetSpeakerZone(speakerZone);
            EndSceneTransition();
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
        if (!TryBeginSceneTransition()) return;
        StartCoroutine(LoadMainMenuWithTransitionRoutine());
    }

    private IEnumerator LoadMainMenuWithTransitionRoutine()
    {
        ScreenFader fader = null;
        yield return GetOrLoadScreenFader(value => fader = value);
        if (fader == null)
        {
            Debug.LogError("LoadMainMenu failed to load the screen fader.");
            EndSceneTransition();
            yield break;
        }

        yield return fader.FadeOut(0.5f);
        LoadMainMenuImmediately(_ => StartCoroutine(FadeInAndEndSceneTransition()));
    }

    internal void LoadMainMenuImmediately(Action<bool> onLoaded)
    {
        Time.timeScale = 1f;
        SwitchGameState(GameState.MainMenu);
        ScenesManager.Instance.SwitchGameScene(Scenes.MainMenuSceneName, sceneLoaded =>
        {
            if (!sceneLoaded)
            {
                Debug.LogError($"Failed to load main menu scene {Scenes.MainMenuSceneName}.");
                onLoaded?.Invoke(false);
                return;
            }

            UIManager.Instance.HidePanel("demon_panel");
            UIManager.Instance.HidePanel("case_board_panel");
            UIManager.Instance.HidePanel("story_dialogue_panel");
            UIManager.Instance.HidePanel("success_panel");
            UIManager.Instance.HidePanel("death_panel");
            UIManager.Instance.HidePanel("main_setting_panel");
            UIManager.Instance.ShowPanel<MainMenuPanel>("main_menu_panel", E_UILayer.MiddleLayer, panel =>
            {
                bool menuReady = panel != null;
                if (menuReady)
                {
                    AudioManager.Instance.StopPlayBGM();
                    AudioManager.Instance.StartPlayBGM("loop");
                }
                else
                {
                    Debug.LogError("Failed to show main_menu_panel.");
                }
                onLoaded?.Invoke(menuReady);
            });
        });
    }

    public void LoadOpeningScene()
    {
        if (!TryBeginSceneTransition()) return;
        StartCoroutine(LoadOpeningSceneRoutine());
    }

    private IEnumerator LoadOpeningSceneRoutine()
    {
        ScreenFader fader = null;
        yield return GetOrLoadScreenFader(value => fader = value);
        if (fader == null)
        {
            Debug.LogError("LoadOpeningScene failed to load the screen fader.");
            EndSceneTransition();
            yield break;
        }

        yield return fader.FadeOut(0.5f);
        ScenesManager.Instance.SwitchGameScene(Scenes.OpeningSceneName, sceneLoaded =>
        {
            if (!sceneLoaded)
            {
                Debug.LogError($"Failed to load opening scene {Scenes.OpeningSceneName}.");
                RecoverToMainMenuWhileBlack();
                return;
            }

            UIManager.Instance.HidePanel("main_menu_panel");
            EndSceneTransition();
        });
    }

    public void LoadEndingScene()
    {
        if (!TryBeginSceneTransition()) return;
        StartCoroutine(LoadEndingSceneRoutine());
    }

    private IEnumerator LoadEndingSceneRoutine()
    {
        ScreenFader fader = null;
        yield return GetOrLoadScreenFader(value => fader = value);
        if (fader == null)
        {
            Debug.LogError("LoadEndingScene failed to load the screen fader.");
            EndSceneTransition();
            yield break;
        }

        yield return fader.FadeOut(0.5f);
        Time.timeScale = 1f;
        SwitchGameState(GameState.GameVictory);
        ScenesManager.Instance.SwitchGameScene(Scenes.EndingSceneName, sceneLoaded =>
        {
            if (!sceneLoaded)
            {
                Debug.LogError($"Failed to load ending scene {Scenes.EndingSceneName}.");
                RecoverToMainMenuWhileBlack();
                return;
            }

            AudioManager.Instance.StopPlayBGM();
            AudioManager.Instance.StartPlayBGM("ending");
            UIManager.Instance.HidePanel("main_menu_panel");
            EndSceneTransition();
        });
    }

    //游戏场景内业务逻辑
    public void PrepareCaseData(int caseId)
    {
        _currentCaseId = caseId;
        _currentCaseConfig = DataManager.Instance.GetCaseConfig(caseId);
        _items.Clear();
        foreach (ItemsConfig item in DataManager.Instance.GetItemConfigs(caseId))
        {
            _items.Add(item.ItemId, item);
        }
        _options.Clear();
        foreach (OptionsConfig option in DataManager.Instance.GetOptionConfigs(caseId))
        {
            _options.Add(option.OptionId, option);
        }
        _currentWords = _currentCaseConfig.InitialWords;
        _demonLines = DataManager.Instance.GetDemonLinesConfigs();
        _debuffs = DataManager.Instance.GetDebuffConfigs();
    }

    private int GetCurrentCaseInitialWords()
    {
        return _currentCaseConfig != null
            ? _currentCaseConfig.InitialWords
            : DEFAULT_WORDS;
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
            StartCoroutine(ShowLocalizedClue(itemId));
        }
    }

    private IEnumerator ShowLocalizedClue(int itemId)
    {
        if (!_items.TryGetValue(itemId, out ItemsConfig item))
        {
            Debug.LogError($"Item config {itemId} does not exist.");
            yield break;
        }

        string itemName = item.NameKey;
        string clueText = item.ClueKey;
        yield return DataManager.Instance.GetLocalizedTextAsync(
            item.NameKey,
            value => itemName = value);
        yield return DataManager.Instance.GetLocalizedTextAsync(
            item.ClueKey,
            value => clueText = value);

        GotCaseItemIds.Add(itemId);
        latestItemId = itemId;
        flow.ShowCluePopup($"{itemName}: {clueText}");
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
        if (_debuffs == null || _debuffs.Count == 0) yield break;

        var candidates = new List<DebuffConfig>();
        foreach (DebuffConfig config in _debuffs)
        {
            if (config.EffectType == DebuffType.ReduceSpeak)
            {
                candidates.Add(config);
            }
        }

        if (candidates.Count == 0) yield break;

        int index = Random.Range(0, candidates.Count);
        DebuffConfig debuff = candidates[index];
        _currentDebuffId = debuff.DebuffId;

        string description = debuff.DescriptionKey;
        string debuffName = debuff.NameKey;
        yield return DataManager.Instance.GetLocalizedTextAsync(
            debuff.DescriptionKey,
            value => description = value);
        yield return DataManager.Instance.GetLocalizedTextAsync(
            debuff.NameKey,
            value => debuffName = value);

        EventManager.Instance.EventTrigger(GameEvents.DemonSpeak, description);
        EventManager.Instance.EventTrigger(GameEvents.DebuffEffect, debuffName);

        // 给玩家留时间读完诅咒描述，再让效果真正生效
        yield return new WaitForSeconds(2.0f);

        int reduceAmount = Mathf.RoundToInt(debuff.EffectParam);
        if (reduceAmount > 0)
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
        if (_options[optionId].IsCorrect)
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
        StartCoroutine(PlayRandomDemonLine(config => config.DefaultKey));
    }

    private IEnumerator PlayRandomDemonLine(Func<DemonLinesConfig, string> selectKey)
    {
        if (_demonLines == null || _demonLines.Count == 0) yield break;

        DemonLinesConfig config = _demonLines[Random.Range(0, _demonLines.Count)];
        string localizationKey = selectKey(config);
        string text = localizationKey;
        yield return DataManager.Instance.GetLocalizedTextAsync(
            localizationKey,
            value => text = value);
        if (!string.IsNullOrEmpty(text))
        {
            EventManager.Instance.EventTrigger(GameEvents.DemonSpeak, text);
        }
    }

    /// <summary>成功还原案件时，让恶魔说一句受伤发言（复用 demon_panel 常驻发言区）</summary>
    public void DemonHurtSpeak()
    {
        StartCoroutine(PlayRandomDemonLine(config => config.HurtKey));
    }

    /// <summary>玩家死亡时，让恶魔说一句嘲讽发言（复用 demon_panel 常驻发言区）</summary>
    public void DemonMockSpeak()
    {
        StartCoroutine(PlayRandomDemonLine(config => config.MockKey));
    }

    /// <summary>
    /// 播放开场剧情（恶魔契约对话），整局游戏只在第一次“开始游戏”时播放一次，播完后回调
    /// </summary>
    public void PlayOpeningStory(Action onComplete)
    {
        IReadOnlyList<DialogueConfig> lines = DataManager.Instance.GetOpeningDialogues();
        if (lines == null || lines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(PlayOpeningLines(lines, onComplete));
    }

    private IEnumerator PlayOpeningLines(
        IReadOnlyList<DialogueConfig> lines,
        Action onComplete)
    {
        StoryDialoguePanel panel = null;
        yield return GetOrLoadStoryDialoguePanel(p => panel = p);

        if (panel == null)
        {
            Debug.LogWarning("PlayOpeningLines: 未找到 story_dialogue_panel，跳过开场剧情播放。");
            onComplete?.Invoke();
            yield break;
        }

        bool revealOnNextLine = true;
        foreach (DialogueConfig line in lines)
        {
            yield return PlayDialogueAndWaitForContinue(panel, line, revealOnNextLine);
            revealOnNextLine = false;
        }

        onComplete?.Invoke();
    }

    public void PlayEndingStory(Action onComplete)
    {
        StartCoroutine(PlayEndingStoryRoutine(onComplete));
    }

    private IEnumerator PlayEndingStoryRoutine(Action onComplete)
    {
        DialogueConfig endingLine = GetRandomDefeatDialogueConfig();
        if (endingLine == null)
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

        yield return PlayDialogueAndWaitForContinue(panel, endingLine, revealScreen: true);
        onComplete?.Invoke();
    }

    private DialogueConfig GetRandomDefeatDialogueConfig()
    {
        if (_demonLines == null || _demonLines.Count == 0)
        {
            return null;
        }

        DemonLinesConfig demon = _demonLines[Random.Range(0, _demonLines.Count)];
        return DataManager.Instance.GetDialogueConfig(demon.DefeatDialogueId);
    }

    public IEnumerator PlayDialogueAndWaitForContinue(
        StoryDialoguePanel panel,
        DialogueConfig config,
        bool revealScreen = false)
    {
        StoryDialogueLineData line = null;
        yield return CreateDialogueLine(config, value => line = value);
        if (line == null) yield break;

        yield return PlayLineAndWaitForContinue(panel, line, revealScreen);
    }

    private IEnumerator CreateDialogueLine(
        DialogueConfig config,
        Action<StoryDialogueLineData> onReady)
    {
        string localizedText = config.TextKey;
        yield return DataManager.Instance.GetLocalizedTextAsync(
            config.TextKey,
            value => localizedText = value);

        var line = new StoryDialogueLineData
        {
            text = localizedText,
            showPortrait = config.ShowPortrait
        };

        if (config.ShowPortrait)
        {
            yield return GetPortrait(config.PortraitAddress, sprite => line.portrait = sprite);
        }

        onReady?.Invoke(line);
    }

    private IEnumerator GetPortrait(string address, Action<Sprite> onReady)
    {
        if (!_portraitHandles.TryGetValue(address, out AsyncOperationHandle<Sprite> handle))
        {
            handle = Addressables.LoadAssetAsync<Sprite>(address);
            _portraitHandles.Add(address, handle);
        }

        if (!handle.IsDone)
        {
            yield return handle;
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            onReady?.Invoke(handle.Result);
            yield break;
        }

        Debug.LogWarning($"Failed to load dialogue portrait '{address}'.");
        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }
        _portraitHandles.Remove(address);
        onReady?.Invoke(null);
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

    public IEnumerator HideStoryDialoguePanelWithFade(float duration = 0.25f)
    {
        StoryDialoguePanel panel = UIManager.Instance.GetPanel<StoryDialoguePanel>("story_dialogue_panel");
        if (panel == null) yield break;

        yield return panel.FadeOut(duration);
        if (UIManager.Instance.GetPanel<StoryDialoguePanel>("story_dialogue_panel") == panel)
        {
            UIManager.Instance.HidePanel("story_dialogue_panel");
        }
    }

    /// <summary>播完一句并等玩家点箭头推进（点击时若还在打字会先跳字，需要再点一次才会推进）</summary>
    public IEnumerator PlayLineAndWaitForContinue(StoryDialoguePanel panel, StoryDialogueLineData line, bool revealScreen = false)
    {
        bool advance = false;
        Action onContinue = () => advance = true;
        panel.ContinueClicked += onContinue;
        panel.PlayLine(line, null);
        yield return panel.FadeIn();
        if (revealScreen)
        {
            yield return FadeInScreenIfNeeded();
        }
        yield return new WaitUntil(() => advance);
        panel.ContinueClicked -= onContinue;
    }


    /// <summary>
    /// 开场剧情播放完成后调用：渐黑挡住案件一场景加载，再真正进入案件一
    /// </summary>
    public void FadeToBlack(float fadeDuration = 1.5f)
    {
        if (!TryBeginSceneTransition()) return;
        StartCoroutine(FadeToBlackThenStartNewGameRoutine(fadeDuration));
    }

    private IEnumerator FadeToBlackThenStartNewGameRoutine(float fadeDuration)
    {
        ScreenFader fader = null;
        yield return GetOrLoadScreenFader(f => fader = f);

        if (fader == null)
        {
            Debug.LogError("FadeToBlack failed to load the screen fader.");
            EndSceneTransition();
            yield break;
        }

        yield return fader.FadeOut(fadeDuration);
        UIManager.Instance.HidePanel("story_dialogue_panel");
        StartNewGame();
    }

    public void ContinueGame()
    {
        if (!TryBeginSceneTransition()) return;
        StartCoroutine(ContinueGameRoutine());
    }

    private IEnumerator ContinueGameRoutine()
    {
        ScreenFader fader = null;
        yield return GetOrLoadScreenFader(value => fader = value);

        if (fader == null)
        {
            Debug.LogError("ContinueGame failed to load the screen fader.");
            EndSceneTransition();
            yield break;
        }

        yield return fader.FadeOut(0.5f);

        if (!SaveManager.Instance.LoadGameData())
        {
            yield return fader.FadeIn(0.5f);
            EndSceneTransition();
        }
    }

    private void RevealCurrentScreenAfterContinueFailure()
    {
        StartCoroutine(FadeInAndEndSceneTransition());
    }

    private void ReturnToMainMenuAfterContinueFailure()
    {
        RecoverToMainMenuWhileBlack();
    }

    private bool TryBeginSceneTransition()
    {
        if (_isSceneTransitioning) return false;
        _isSceneTransitioning = true;
        return true;
    }

    private void EndSceneTransition()
    {
        _isSceneTransitioning = false;
    }

    private void RecoverToMainMenuWhileBlack()
    {
        LoadMainMenuImmediately(_ => StartCoroutine(FadeInAndEndSceneTransition()));
    }

    private IEnumerator FadeInAndEndSceneTransition(float duration = 0.5f)
    {
        yield return FadeInScreenIfNeeded(duration);
        EndSceneTransition();
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
        ScenesManager.Instance?.LoadSceneAsync(_currentCaseConfig.SceneName, () =>
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

    /// <summary>【测试专用】跳过前面所有案件，直接触发真结局</summary>
    public void DebugForceTrueEnd()
    {
        if (flow == null) flow = FlowController.Instance;
        if (_demonLines == null || _demonLines.Count == 0)
        {
            Debug.LogWarning("DebugForceTrueEnd: 恶魔配置数据为空。");
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
            RevealCurrentScreenAfterContinueFailure();
            return;
        }

        int caseIndex = allCaseIds.IndexOf(gameData.currentCaseId);
        if (caseIndex < 0)
        {
            Debug.LogError("Game save data contains invalid progress values.");
            RevealCurrentScreenAfterContinueFailure();
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
            EndSceneTransition();
            LoadEndingScene();
            return;
        }

        PrepareCaseData(_currentCaseId);

        if (InputManager.Instance != null)
            InputManager.Instance.SetInputEnabled(false);

        string sceneName = _currentCaseConfig.SceneName;
        ScenesManager.Instance.SwitchGameScene(sceneName, sceneLoaded =>
        {
            if (!sceneLoaded)
            {
                Debug.LogError($"Failed to load saved case scene {sceneName}.");
                ReturnToMainMenuAfterContinueFailure();
                return;
            }

            speakerZone = FindAnyObjectByType<SpeakerZone>();
            interaction = InteractionController.Instance;
            flow = FlowController.Instance;
            if (speakerZone == null || interaction == null || flow == null)
            {
                Debug.LogError("Saved case scene is missing required gameplay objects.");
                ReturnToMainMenuAfterContinueFailure();
                return;
            }

            interaction.SetSpeakerZone(speakerZone);
            EndSceneTransition();
            flow.FlowInit(playIntroStory: true);
            CaptureSceneSnapshot();
            SwitchGameState(GameState.Playing);
        });
    }
}
