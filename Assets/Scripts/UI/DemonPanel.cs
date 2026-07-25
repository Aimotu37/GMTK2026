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

    void Start()
    {
        demonIcon = FindComponent<Image>("Demon");
        demonSpeakText = FindComponent<TMP_Text>("DemonSpeakText");
        countText = FindComponent<TMP_Text>("Number");
        debuff = FindComponent<TMP_Text>("DebuffDesc");
        debuffIcon = FindComponent<Image>("DebuffIcon");
        typewriter = GetComponent<TypewriterEffect>();
    }

    void OnDestroy()
    {
        OnChangeCount -= SetCountText;
        OnDemonSpeak -= SetDemonSpeak;
        OnDebuff -= SetDebuff;
    }

    private void SetCountText(int count)
    {
        countText.text = count.ToString();
    }

    private void SetDemonSpeak(string text)
    {
        typewriter.SetTextMesh(demonSpeakText as TextMeshProUGUI);
        typewriter.StartTyping(text);
    }

    /// <summary>
    /// 播放一段剧情/结局文本，完成后触发回调（用于初始剧情、真结局等需要等待播放结束再切换流程的场景）
    /// </summary>
    public void PlayStory(string text, Action onComplete)
    {
        typewriter.SetTextMesh(demonSpeakText as TextMeshProUGUI);
        typewriter.StartTyping(text, onComplete);
    }

    private void SetDebuff(string text)
    {
        typewriter.SetTextMesh(debuff as TextMeshProUGUI);
        typewriter.StartTyping(text);
    }
}
