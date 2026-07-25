using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuccessPresenter : MonoBehaviour
{
    private SuccessPanel _panel;

    [SerializeField]
    private string _panelName = "success_panel";
    [SerializeField]
    private E_UILayer _panelLayer = E_UILayer.TopLayer;

    void OnEnable()
    {
        _panel = GetComponent<SuccessPanel>();
        RegisterEvents();

        EventManager.Instance.EventRegister<string>(GameEvents.CaseTruthShow, ShowTruth);
    }

    void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.NextCaseClicked -= HandleNextCase;
        }
        EventManager.Instance.EventUnregister<string>(GameEvents.CaseTruthShow, ShowTruth);
    }

    private void RegisterEvents()
    {
        if (_panel == null)
        {
            Debug.LogError(name + $" failed to show panel: {_panelName}");
            return;
        }

        _panel.NextCaseClicked += HandleNextCase;
    }

    private void HandleNextCase()
    {
        GameManager.Instance.NextCase();
        UIManager.Instance.HidePanel(_panelName);
    }

    private void ShowTruth(string truthText)
    {
        _panel.ShowCaseTruth(truthText);
    }
}
