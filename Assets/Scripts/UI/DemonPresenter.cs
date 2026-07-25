using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemonPresenter : MonoBehaviour
{
    private DemonPanel _panel;
    [SerializeField] private string _panelName = "main_menu_panel";
    [SerializeField] private E_UILayer _panelLayer = E_UILayer.MiddleLayer;

    void OnEnable()
    {
        EventManager.Instance.EventRegister<int>(GameEvents.CountChanged, SetCountValue);
        EventManager.Instance.EventRegister<string>(GameEvents.DemonSpeak, SetDemonSpeak);
        EventManager.Instance.EventRegister<string>(GameEvents.DebuffEffect, SetDebuffDesc);
        EventManager.Instance.EventRegister<string>(GameEvents.ChoseCaseOption, SetDemonSpeak);
    }

    private void Start()
    {
        _panel = GetComponent<DemonPanel>();
    }

    void OnDestroy()
    {
        EventManager.Instance.EventUnregister<int>(GameEvents.CountChanged, SetCountValue);
        EventManager.Instance.EventUnregister<string>(GameEvents.DemonSpeak, SetDemonSpeak);
        EventManager.Instance.EventUnregister<string>(GameEvents.DebuffEffect, SetDebuffDesc);
        EventManager.Instance.EventUnregister<string>(GameEvents.ChoseCaseOption, SetDemonSpeak);
    }

    private void SetCountValue(int count)
    {
        _panel.OnChangeCount.Invoke(count);
    }

    private void SetDemonSpeak(string text)
    {
        _panel.OnDemonSpeak.Invoke(text);
    }

    private void SetDebuffDesc(string text)
    {
        _panel.OnDebuff.Invoke(text);
    }
}
