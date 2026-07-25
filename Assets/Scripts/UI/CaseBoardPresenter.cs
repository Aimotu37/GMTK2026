using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaseBoradPresenter : MonoBehaviour
{
    private CaseBoardPanel _panel;
    [SerializeField] private string _panelName = "case_board_panel";
    [SerializeField] private E_UILayer _panelLayer = E_UILayer.MiddleLayer;
    private bool isOpen;

    void OnEnable()
    {
        EventManager.Instance.EventRegister<ItemData>(GameEvents.DropItemOnZone, SetClue);
        EventManager.Instance.EventRegister<bool>(GameEvents.CheckCaseClues, CheckTruthInteractable);
    }

    private void Start()
    {
        _panel = GetComponent<CaseBoardPanel>();
        RegisterEvents();
    }

    void OnDestroy()
    {
        EventManager.Instance.EventUnregister<ItemData>(GameEvents.DropItemOnZone, SetClue);
        EventManager.Instance.EventUnregister<bool>(GameEvents.CheckCaseClues, CheckTruthInteractable);
        if (_panel != null)
        {
            _panel.OnTruthClicked -= HandleTruthClick;
        }
    }

    private void RegisterEvents()
    {
        if (_panel == null)
        {
            Debug.LogError(name + $" failed to show panel: {_panelName}");
            return;
        }

        _panel.OnTruthClicked += HandleTruthClick;
    }

    private void SetClue(ItemData item)
    {
        int index = item.itemID % 100;
        string text = item.clueText;
        _panel.ChangeClueState(index - 1, text);
    }

    private void CheckTruthInteractable(bool interactable)
    {
        _panel.SetTruthInteractable(interactable);
    }

    private void HandleTruthClick()
    {
        if (!isOpen)
        {
            isOpen = true;
            FlowController.Instance.ShowOption(isOpen);
        }
        else
        {
            isOpen = false;
            FlowController.Instance.ShowOption(isOpen);
        }

    }
}
