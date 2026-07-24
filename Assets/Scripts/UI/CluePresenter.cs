using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CluePresenter : MonoBehaviour
{
    private CluePanel _panel;
    [SerializeField] private string _panelName = "clue_panel";
    [SerializeField] private E_UILayer _panelLayer = E_UILayer.MiddleLayer;

    private void OnEnable()
    {
        _panel = GetComponent<CluePanel>();
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        if (_panel == null)
        {
            Debug.LogError(name + $" failed to show panel: {_panelName}");
            return;
        }
    }

    public void SetCluePanelText(string clue)
    {
        _panel.SetClueText(clue);
    }
}
