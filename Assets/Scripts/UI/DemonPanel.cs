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
        debuff = FindComponent<TMP_Text>("DebuffName");
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
