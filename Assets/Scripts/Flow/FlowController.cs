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
    Death,            // 死亡
    TrueEnd           // 真结局
}

public class FlowController : SingletonMono<FlowController>
{
    [SerializeField]
    private GameFlowState _currentState;
    public GameFlowState CurrentState => _currentState;

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
                // 在成功场景上弹出真相弹窗（由SuccessController处理）
                // 这里不切场景，只发状态通知
                //UIManager.Instance.ShowTruthPopup();
                break;

            case GameFlowState.Death:
                StartCoroutine(HandleDeath());
                break;

            case GameFlowState.TrueEnd:
                //SceneManager.LoadScene(TRUE_END_SCENE);
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
                yield return GameManager.Instance.PlayLineAndWaitForContinue(panel, story);
                UIManager.Instance.HidePanel("story_dialogue_panel");
            }
         }

        _currentState = GameFlowState.Exploring;
        TransitionTo(_currentState);
    }

    private IEnumerator HandleExplore()
    {
        GameManager.Instance.DemonSpeak();
        demonSpeak = StartCoroutine(DemonSpeakPeriodically());
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
    }

    private IEnumerator HandleOptionChose()
    {
        // 1.选项页面播弹出
        UIManager.Instance.ShowPanel<OptionPanel>("option_panel");
        yield break;
    }

    private IEnumerator HandleSuccess()
    {
        // 1.成功推理页面
        yield return new WaitForSeconds(0.5f);
        UIManager.Instance.ShowPanel<SuccessPanel>("success_panel", E_UILayer.MiddleLayer);
        yield break;
    }

    private IEnumerator HandleTruthShow()
    {
        // 1.展示真相页面
        yield break;
    }

    private IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.GameOver();
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
        panel.PlayLine(speak, null);
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
