using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionPresenter : MonoBehaviour
{
    private OptionPanel _panel;
    [SerializeField] private string _panelName = "option_panel";
    [SerializeField] private E_UILayer _panelLayer = E_UILayer.MiddleLayer;

    private void Start()
    {
        _panel = GetComponent<OptionPanel>();
        RegisterEvents();
    }

    private void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.Option1Clicked -= HandleOption1;
            _panel.Option1Clicked -= HandleOption2;
            _panel.Option1Clicked -= HandleOption3;
        }
    }

    private void RegisterEvents()
    {
        if (_panel == null)
        {
            Debug.LogError($"MainMenuPresenter failed to show panel: {_panelName}");
            return;
        }

        _panel.Option1Clicked += HandleOption1;
        _panel.Option1Clicked += HandleOption2;
        _panel.Option1Clicked += HandleOption3;
    }

    private void HandleOption1()
    {

    }

    private void HandleOption2()
    {

    }
    private void HandleOption3()
    {

    }
}
