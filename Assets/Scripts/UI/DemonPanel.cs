using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class DemonPanel : BasePanel
{
    private Image demonIcon;
    private TMP_Text demonSpeakText;
    private TMP_Text countText;
    private TMP_Text debuff;
    private Image debuffIcon;
    private CountDecreaseFeedback countFeedback;
    private int displayedCount;
    private bool hasDisplayedCount;

    public Action<int> OnChangeCount;
    public Action<string> OnDemonSpeak;
    public Action<string> OnDebuff;

    private TypewriterEffect typewriter;

    void OnEnable()
    {
        OnChangeCount += SetCountText;
        OnDemonSpeak += SetDemonSpeak;
        OnDebuff += SetDebuff;
    }

    protected override void Awake()
    {
        base.Awake();
        demonIcon = FindComponent<Image>("Demon");
        demonSpeakText = FindComponent<TMP_Text>("DemonSpeakText");
        countText = FindComponent<TMP_Text>("Number");
        debuff = FindComponent<TMP_Text>("DebuffName");
        debuffIcon = FindComponent<Image>("DebuffIcon");
        typewriter = GetComponent<TypewriterEffect>();
        countFeedback = GetComponent<CountDecreaseFeedback>();

        if (countText != null)
        {
            hasDisplayedCount = int.TryParse(countText.text, out displayedCount);
            countFeedback?.Initialize(countText);
        }
    }

    void OnDisable()
    {
        OnChangeCount -= SetCountText;
        OnDemonSpeak -= SetDemonSpeak;
        OnDebuff -= SetDebuff;
    }

    private void SetCountText(int count)
    {
        if (countText == null)
        {
            return;
        }

        bool animate = CountFeedbackPolicy.ShouldAnimate(
            hasDisplayedCount, displayedCount, count);
        int oldValue = displayedCount;
        displayedCount = count;
        hasDisplayedCount = true;

        if (countFeedback == null)
        {
            countText.text = count.ToString();
            return;
        }

        if (animate)
        {
            countFeedback.PlayDecrease(oldValue, count);
        }
        else
        {
            countFeedback.SetImmediate(count);
        }
    }

    private void SetDemonSpeak(string text)
    {
        typewriter.SetTextMesh(demonSpeakText as TextMeshProUGUI);
        typewriter.StartTyping(text);
    }

    private void SetDebuff(string text)
    {
        //typewriter.SetTextMesh(debuff as TextMeshProUGUI);
        //typewriter.StartTyping(text);
        // 不走打字机：小框只是个短标签，直接瞬间显示，避免跟顶部恶魔发言抢同一个打字机导致顶部那句被打断卡住
        if (debuff != null)
        {
            debuff.text = text;
        }
    }
}
