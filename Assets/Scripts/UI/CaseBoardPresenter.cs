using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaseBoradPresenter : MonoBehaviour
{
    private CaseBoardPanel _panel;
    [SerializeField] private string _panelName = "main_menu_panel";
    [SerializeField] private E_UILayer _panelLayer = E_UILayer.MiddleLayer;

    void OnEnable()
    {
        EventManager.Instance.EventRegister<ItemData>(GameEvents.DropItemOnZone, SetClue);
    }

    private void Start()
    {
        _panel = GetComponent<CaseBoardPanel>();
    }

    void OnDestroy()
    {
        EventManager.Instance.EventUnregister<ItemData>(GameEvents.DropItemOnZone, SetClue);
    }

    private void SetClue(ItemData item)
    {
        int index = item.itemID % 100;
        string text = item.clueText;
        _panel.ChangeClueState(index - 1, text);
    }

}
