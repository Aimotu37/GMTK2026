using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用剧情对话框：2D背景 + 圆形立绘 + 底部对话文本，点击箭头跳过打字/推进下一句。
/// 用于开场剧情、每案剧情页、真结局等叙事场景，探索期间的HUD发言仍用 demon_panel。
/// </summary>
public class StoryDialoguePanel : BasePanel
{
    private const string ContinueButtonName = "ContinueButton";

    private Image background;
    private Image portrait;
    private TMP_Text dialogueText;

    private TypewriterEffect typewriter;

    /// <summary>当前句已经播完、玩家点击箭头要求进入下一句时触发</summary>
    public event Action ContinueClicked;

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
        if (portrait != null) portrait.sprite = sprite;
    }

    /// <summary>
    /// 播放一句对话文本（打字机效果），onTypingComplete 在这一句打完字时触发（不是玩家点了下一句）
    /// </summary>
    public void PlayLine(string text, Action onTypingComplete)
    {
        typewriter.SetTextMesh(dialogueText as TextMeshProUGUI);
        typewriter.StartTyping(text, onTypingComplete);
    }
}
