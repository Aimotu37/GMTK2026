using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameFlowState
{
    Initiating,
    StoryPlaying,     // 剧情播放中
    Exploring,        // 探索中
    CluePopup,        // 线索弹窗展示中（禁止交互）
    OptionChoosing,   // 选项页面
    Success,          // 成功还原
    TruthShowing,     // 展示真相
    CaseFail,            // 死亡
    TrueEnd           // 真结局
}

public class FlowController : SingletonMono<FlowController>
{
    [SerializeField]
    private GameFlowState _currentState;
    public GameFlowState CurrentState => _currentState;
    /// <summary>当前是否处于"点击空白处继续"的等待状态（成功页看真相 / 死亡页重试）</summary>  
    public bool IsWaitForClickEmpty => _currentState == GameFlowState.Success || _currentState == GameFlowState.CaseFail;

    //流程运行变量
    private float clueDuration = 1.5f;
    private float demonSpeakInterval = 10.0f;
    private string _pendingClueText;
    private int _currentCase;
    private Coroutine demonSpeak;
    private bool _skipIntroStory;


    /// <summary>
    /// 初始化流程；playIntroStory 为 false 时跳过案件剧情播放，直接进入探索（用于重试当前案件）
    /// </summary>
    public void FlowInit(bool playIntroStory = true)
    {
        ReSetAllFlowData();
        _skipIntroStory = !playIntroStory;
        FlowStateChange(GameFlowState.Initiating);
    }

    public void ShowCluePopup(string clue)
    {
        _pendingClueText = string.IsNullOrEmpty(clue) ? "未知线索" : clue;
        FlowStateChange(GameFlowState.CluePopup);
    }

    public void ShowOption(bool isShow)
    {
        if (isShow)
        {
            FlowStateChange(GameFlowState.OptionChoosing);
        }
        else
        {
            UIManager.Instance.HidePanel("option_panel");
            FlowStateChange(GameFlowState.Exploring);
        }
    }

    public void ShowSuccess(int caseId)
    {
        _currentCase = caseId;
        FlowStateChange(GameFlowState.Success);
    }

    /// <summary>成功页点击空白处触发：展示案件真相，读完后自动进入下一案件</summary>
    public void ShowCaseTruth()
    {
        FlowStateChange(GameFlowState.TruthShowing);
    }

    public void FlowStateChange(GameFlowState state)
    {
        _currentState = state;
        TransitionTo(state);
    }

    public void ReSetAllFlowData()
    {
        _currentState = GameFlowState.Initiating;
        _pendingClueText = "";
        _currentCase = 0;
        if (demonSpeak != null)
            StopCoroutine(demonSpeak);
        demonSpeak = null;
    }

    private void TransitionTo(GameFlowState newState)
    {
        // 可选：退出当前状态的清理

        _currentState = newState;
        switch (newState)
        {
            case GameFlowState.Initiating:
                StartCoroutine(HandleFlowInitialize());
                break;
            case GameFlowState.StoryPlaying:
                StartCoroutine(HandleStoryPlay(2.0f));
                break;
            case GameFlowState.Exploring:
                StartCoroutine(HandleExplore());
                break;
            case GameFlowState.CluePopup:
                StartCoroutine(HandleCluePopup());
                break;
            case GameFlowState.OptionChoosing:
                StartCoroutine(HandleOptionChose());
                break;

            case GameFlowState.Success:
                StartCoroutine(HandleSuccess());
                break;

            case GameFlowState.TruthShowing:
                StartCoroutine(HandleCaseTruthShow());
                break;

            case GameFlowState.CaseFail:
                StartCoroutine(HandleCaseFail());
                break;

            case GameFlowState.TrueEnd:
                StartCoroutine(HandleTrueEnd());
                break;
        }
    }

    private IEnumerator HandleFlowInitialize()
    {
        UIManager.Instance.HidePanel("main_menu_panel");
        yield return null;
        //需要恢复案件线索板
        UIManager.Instance.HidePanel("case_board_panel");
        UIManager.Instance.ShowPanel<CaseBoardPanel>("case_board_panel");
        UIManager.Instance.HidePanel("demon_panel");
        UIManager.Instance.ShowPanel<DemonPanel>("demon_panel", E_UILayer.TopLayer, (panel) =>
        {
            EventManager.Instance.EventTrigger(GameEvents.CheckCaseClues, GameManager.Instance.GotCaseItemIds.Count > 0);
            _currentState = _skipIntroStory ? GameFlowState.Exploring : GameFlowState.StoryPlaying;
            TransitionTo(_currentState);
        });
    }

    private IEnumerator HandleStoryPlay(float duration)
    {
        //播放剧情动画：读取当前案件的剧情文本，通过恶魔面板打字机播放
        string story = GameManager.Instance.CurrentCaseData != null
            ? GameManager.Instance.CurrentCaseData.storyText
            : null;

        if (string.IsNullOrEmpty(story))
        {
            // 没有配置剧情文本时，退回到纯等待，避免卡流程
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        //print("播放动画结束");
        else
        {
            StoryDialoguePanel panel = null;
            yield return GameManager.Instance.GetOrLoadStoryDialoguePanel(p => panel = p);

            if (panel == null)
            {
                Debug.LogWarning("HandleStoryPlay: 未找到 story_dialogue_panel，无法播放剧情文本。");
            }
            else
            {
                // 先把真正的案件文案设置进去开始打字（此时屏幕如果还是黑的，玩家看不到面板默认内容）
                bool advance = false;
                Action onContinue = () => advance = true;
                panel.ContinueClicked += onContinue;
                panel.PlayLine(story, null);
                yield return GameManager.Instance.FadeInScreenIfNeeded();
                yield return new WaitUntil(() => advance);
                panel.ContinueClicked -= onContinue;
                UIManager.Instance.HidePanel("story_dialogue_panel");
            }
        }
        FlowStateChange(GameFlowState.Exploring);
    }

    private IEnumerator HandleExplore()
    {
        GameManager.Instance.DemonSpeak();
        demonSpeak = StartCoroutine(DemonSpeakPeriodically());
        string caseName = GameManager.Instance.CurrentCaseData.caseName;
        UIManager.Instance.GetPanel<CaseBoardPanel>("case_board_panel").SetCaseName(caseName);
        yield break;
    }

    private IEnumerator HandleCluePopup()
    {
        InputManager.Instance.SetInputEnabled(false);

        bool requestCompleted = false;
        bool panelOpened = false;
        BasePanel tempPanel = new BasePanel();

        UIManager.Instance.ShowPanel<CluePanel>("clue_panel", E_UILayer.MiddleLayer, (panel) =>
        {
            tempPanel = panel;
            if (panel != null)
            {
                panel.SetClueText(_pendingClueText);
                panelOpened = true;
            }
            requestCompleted = true;
        });

        yield return new WaitUntil(() => requestCompleted);

        if (panelOpened)
        {
            yield return new WaitForSeconds(3.0f);
            float elapsed = clueDuration;
            while (elapsed > 0f)
            {
                elapsed -= Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / clueDuration);
                (tempPanel as CluePanel).SetCanvasGroupAlpha(t);
                yield return null;
            }
            ItemData item = GameManager.Instance.Items[GameManager.Instance.LatestItemId];
            EventManager.Instance.EventTrigger(GameEvents.DropItemOnZone, item);
            EventManager.Instance.EventTrigger(GameEvents.CheckCaseClues, GameManager.Instance.GotCaseItemIds.Count > 0);
            UIManager.Instance.HidePanel("clue_panel");
        }
        InputManager.Instance.SetInputEnabled(true);

        // 线索弹窗完全播完、玩家能重新操作之后，才决定这次要不要触发诅咒，跟线索信息错开成两拍
        yield return new WaitForSeconds(0.3f);
        if (GameManager.Instance.CurrentCaseIndex == 3)
            GameManager.Instance.TryTriggerDebuff();
    }

    private IEnumerator HandleOptionChose()
    {
        // 1.选项页面播弹出
        UIManager.Instance.ShowPanel<OptionPanel>("option_panel");
        yield break;
    }

    private IEnumerator HandleSuccess()
    {
        // 1.成功推理页面：恶魔受伤发言 + 成功面板
        GameManager.Instance.DemonHurtSpeak();
        yield return new WaitForSeconds(0.5f);
        UIManager.Instance.ShowPanel<SuccessPanel>("success_panel", E_UILayer.MiddleLayer);
        yield break;
    }

    private IEnumerator HandleCaseTruthShow()
    {
        // 1.展示真相页面
        // 展示案件真相文本（复用通用剧情对话框），读完点击继续后自动进入下一案件/真结局
        UIManager.Instance.HidePanel("success_panel");

        string truth = GameManager.Instance.CurrentCaseData != null
            ? GameManager.Instance.CurrentCaseData.truthText
            : null;

        if (!string.IsNullOrEmpty(truth))
        {
            StoryDialoguePanel panel = null;
            yield return GameManager.Instance.GetOrLoadStoryDialoguePanel(p => panel = p);

            if (panel == null)
            {
                Debug.LogWarning("HandleTruthShow: 未找到 story_dialogue_panel，无法播放案件真相。");
            }
            else
            {
                yield return GameManager.Instance.PlayLineAndWaitForContinue(panel, truth);
                // 下一案的 HandleStoryPlay（或真结局的 HandleTrueEnd）在内容准备好后自己渐显
                ScreenFader fader = null;
                yield return GameManager.Instance.GetOrLoadScreenFader(f => fader = f);
                if (fader != null)
                {
                    yield return fader.FadeOut(0.5f);
                }
                UIManager.Instance.HidePanel("story_dialogue_panel");
            }
        }

        GameManager.Instance.NextCase();
    }

    private IEnumerator HandleCaseFail()
    {
        GameManager.Instance.DemonMockSpeak();
        yield return new WaitForSeconds(0.5f);
        //GameManager.Instance.GameOver();
        UIManager.Instance.ShowPanel<DeathPanel>("death_panel", E_UILayer.MiddleLayer);
        yield break;
    }

    private IEnumerator HandleTrueEnd()
    {

        // 结局页面复用通用剧情对话框，播放恶魔被击败发言（DemonSpeakConfig.DefeatSpeak）
        UIManager.Instance.HidePanel("case_board_panel");
        UIManager.Instance.HidePanel("demon_panel");

        string speak = GameManager.Instance.GetRandomDefeatSpeak();
        if (string.IsNullOrEmpty(speak))
        {
            yield break;
        }

        StoryDialoguePanel panel = null;
        yield return GameManager.Instance.GetOrLoadStoryDialoguePanel(p => panel = p);

        if (panel == null)
        {
            Debug.LogWarning("HandleTrueEnd: 未找到 story_dialogue_panel，无法播放结局发言。");
            yield break;
        }

        // TODO: 美术资源到位后调用 panel.SetPortrait(恶魔被击败立绘) / panel.SetBackground(结局背景)
        bool advance = false;
        Action onContinue = () => advance = true;
        panel.ContinueClicked += onContinue;
        panel.PlayLine(speak, null);
        // 内容已经开始打字，如果屏幕还是黑的（从上一步真相弹窗渐黑切过来），这里渐显揭幕
        yield return GameManager.Instance.FadeInScreenIfNeeded();
        yield return new WaitUntil(() => advance);
        panel.ContinueClicked -= onContinue;

        // 播完等玩家点一下继续，再收起对话框、转场回主菜单
        UIManager.Instance.HidePanel("story_dialogue_panel");
        GameManager.Instance.LoadMainMenu();
    }

    private void ShowClueAnimation(string clue)
    {
        InputManager.Instance.SetInputEnabled(false);

        InteractionController.Instance.typewriter.StartTyping(clue, () =>
        {
            InputManager.Instance.SetInputEnabled(true);
        });
    }

    private IEnumerator DemonSpeakPeriodically()
    {
        if (demonSpeak != null) yield break;
        float elapsed = demonSpeakInterval;
        while (elapsed > 0f)
        {
            elapsed -= Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / demonSpeakInterval);
            yield return null;
        }
        GameManager.Instance.DemonSpeak();
        StartCoroutine(DemonSpeakPeriodically());
    }

}
