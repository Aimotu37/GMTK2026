using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathPanelPresenter : MonoBehaviour
{
    private DeathPanel _panel;
    [SerializeField] private string _panelName = "death_panel";
    [SerializeField] private E_UILayer _panelLayer = E_UILayer.MiddleLayer;

    private void OnEnable()
    {
        _panel = GetComponent<DeathPanel>();
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        if (_panel == null)
        {
            Debug.LogError(name + $" failed to show panel: {_panelName}");
            return;
        }

        _panel.BackToMainClicked += HandleBackToMian;
        _panel.RetryClicked += HandleTetry;
    }

    private void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.BackToMainClicked -= HandleBackToMian;
            _panel.RetryClicked -= HandleTetry;
        }
    }

    private void HandleBackToMian()
    {
        GameManager.Instance.LoadMainMenu();
        UIManager.Instance.HidePanel(_panelName);
    }

    private void HandleTetry()
    {
        GameManager.Instance.RetryCurrentCase();
        UIManager.Instance.HidePanel(_panelName);
    }
}
