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
    private string _currentCase;

    public Item currentDragItem;

    private void Start()
    {
        FlowStateChange(GameFlowState.Initiating);
    }

    public void ShowCluePopup(string clue)
    {
        _pendingClueText = string.IsNullOrEmpty(clue) ? "未知线索" : clue;
        FlowStateChange(GameFlowState.CluePopup);
    }

    public void ShowSuccess(string caseId)
    {
        _currentCase = caseId;
        FlowStateChange(GameFlowState.Success);
    }

    public void FlowStateChange(GameFlowState state)
    {
        _currentState = state;
        TransitionTo(state);
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
                //SceneManager.LoadScene(OPTION_SCENE);
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
                break;
        }
    }

    private IEnumerator HandleFlowInitialize()
    {
        print("加载案件场景");
        yield return new WaitForSeconds(1.0f);
        print("加载案件场景完成");
        _currentState = GameFlowState.StoryPlaying;
        TransitionTo(_currentState);
    }

    private IEnumerator HandleStoryPlay(float duration)
    {
        //播放剧情动画
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        print("播放动画结束");
        _currentState = GameFlowState.Exploring;
        TransitionTo(_currentState);
    }

    private IEnumerator HandleExplore()
    {
        GameManager.Instance.DemonSpeak();
        StartCoroutine(DemonSpeakPeriodically());
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
            UIManager.Instance.HidePanel("clue_panel");
        }
        InputManager.Instance.SetInputEnabled(true);
    }

    private IEnumerator HandleOptionChose()
    {
        // 1.选项页面播弹出
        yield break;
    }

    private IEnumerator HandleSuccess()
    {
        // 1.成功推理页面
        yield return new WaitForSeconds(0.5f);
        UIManager.Instance.ShowPanel<SuccessPanel>("success_panel", E_UILayer.TopLayer);
        yield break;
    }

    private IEnumerator HandleTruthShowe()
    {
        // 1.展示真相页面
        yield break;
    }

    private IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.GameOver();
        UIManager.Instance.ShowPanel<DeathPanel>("death_panel", E_UILayer.TopLayer);
        yield break;
    }

    private IEnumerator HandleTrueEnd()
    {
        // 1.结局页面
        // 2.结局对话文本
        yield break;
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
