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

        _panel.BackToMainClicked += HandleBackToMain;
        _panel.RetryClicked += HandleRetry;
        _panel.BackgroundClicked += HandleRetry;
    }

    private void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.BackToMainClicked -= HandleBackToMain;
            _panel.RetryClicked -= HandleRetry;
            _panel.BackgroundClicked -= HandleRetry;
        }
    }

    private void HandleBackToMain()
    {
        GameManager.Instance.LoadMainMenu();
        UIManager.Instance.HidePanel(_panelName);
    }

    private void HandleRetry()
    {
        AudioManager.Instance.StartPlaySound("sfx_13", false);
        bool retryOwnsPanelDismissal = GameManager.Instance.RetryCurrentCase();
        if (!retryOwnsPanelDismissal)
        {
            UIManager.Instance.HidePanel(_panelName);
        }
    }
}
