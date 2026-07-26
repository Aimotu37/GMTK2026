using System;
using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用剧情对话框：2D背景 + 圆形立绘 + 底部对话文本，点击箭头跳过打字/推进下一句。
/// 用于开场剧情、每案剧情页、真结局等叙事场景，探索期间的HUD发言仍用 demon_panel。
/// </summary>
public class StoryDialoguePanel : BasePanel
{
    private const string ContinueButtonName = "ContinueButton";
    private const float DefaultFadeDuration = 0.25f;

    private Image background;
    private Image portrait;
    private TMP_Text dialogueText;
    private CanvasGroup canvasGroup;

    private TypewriterEffect typewriter;

    /// <summary>当前句已经播完、玩家点击箭头要求进入下一句时触发</summary>
    public event Action ContinueClicked;

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("StoryDialoguePanel requires a CanvasGroup component.");
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void Start()
    {
        background = FindComponent<Image>("Background");
        portrait = FindComponent<Image>("Portrait");
        dialogueText = FindComponent<TMP_Text>("DialogueText");
        typewriter = GetComponent<TypewriterEffect>();
    }

    protected override void OnButtonClick(string buttonName)
    {
        if (buttonName != ContinueButtonName) return;
        AudioManager.Instance.StartPlaySound("sfx_02_11_14", false);
        if (typewriter != null && typewriter.IsTyping)
        {
            typewriter.SkipTyping();
        }
        else
        {
            ContinueClicked?.Invoke();
        }
    }

    public void SetBackground(Sprite sprite)
    {
        if (background != null) background.sprite = sprite;
    }

    public void SetPortrait(Sprite sprite)
    {
        if (portrait == null) return;
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
    }

    /// <summary>
    /// 播放一句对话文本（打字机效果），onTypingComplete 在这一句打完字时触发（不是玩家点了下一句）
    /// </summary>
    public void PlayLine(StoryDialogueLineData line, Action onTypingComplete)
    {
        ApplyPortrait(line);
        typewriter.SetTextMesh(dialogueText as TextMeshProUGUI);
        typewriter.StartTyping(line != null ? line.text : string.Empty, onTypingComplete);
    }

    private void ApplyPortrait(StoryDialogueLineData line)
    {
        if (portrait == null) return;

        if (line == null || !line.showPortrait)
        {
            SetPortrait(null);
            return;
        }

        if (line.portrait == null)
        {
            Debug.LogWarning("Story dialogue line is configured to show a portrait but has no Sprite assigned.");
            SetPortrait(null);
            return;
        }

        SetPortrait(line.portrait);
    }

    public IEnumerator FadeIn(float duration = DefaultFadeDuration)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        if (canvasGroup.alpha >= 0.999f || duration <= 0f)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            yield break;
        }

        float start = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 1f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public IEnumerator FadeOut(float duration = DefaultFadeDuration)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        if (canvasGroup.alpha <= 0.001f || duration <= 0f)
        {
            canvasGroup.alpha = 0f;
            yield break;
        }

        float start = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
