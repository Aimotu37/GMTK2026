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
            _panel.BackgroundClicked -= HandleNextCase;
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
        _panel.BackgroundClicked += HandleNextCase;
    }

    private void HandleNextCase()
    {
        AudioManager.Instance.StartPlaySound("sfx_10_12", false);
        FlowController.Instance.ShowCaseTruth();
    }

    private void ShowTruth(string truthText)
    {
        AudioManager.Instance.StartPlaySound("sfx_02_11_14", false);
        _panel.ShowCaseTruth(truthText);
    }
}
