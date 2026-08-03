using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionPresenter : MonoBehaviour
{
    private OptionPanel _panel;
    [SerializeField] private string _panelName = "option_panel";
    [SerializeField] private E_UILayer _panelLayer = E_UILayer.MiddleLayer;

    Dictionary<string, int> buttonBindOptionId = new Dictionary<string, int>();

    private void Start()
    {
        _panel = GetComponent<OptionPanel>();
        RegisterEvents();
        OnPanelShown();
    }

    private void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.Option1Clicked -= HandleOption;
            _panel.Option2Clicked -= HandleOption;
            _panel.Option3Clicked -= HandleOption;
        }
    }

    private void OnPanelShown()
    {
        StartCoroutine(InitializeOptions());
    }

    private IEnumerator InitializeOptions()
    {
        var options = new List<OptionsConfig>();
        AudioManager.Instance.StartPlaySound("sfx_03_08", false);
        foreach (OptionsConfig option in GameManager.Instance.Options.Values)
        {
            options.Add(option);
        }

        options.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));

        var localizedTexts = new List<string>(options.Count);
        foreach (OptionsConfig option in options)
        {
            string localizedText = option.TextKey;
            yield return DataManager.Instance.GetLocalizedTextAsync(
                option.TextKey,
                value => localizedText = value);
            localizedTexts.Add(localizedText);
        }

        buttonBindOptionId = _panel.InitButtonText(options, localizedTexts);
    }

    private void RegisterEvents()
    {
        if (_panel == null)
        {
            Debug.LogError($"MainMenuPresenter failed to show panel: {_panelName}");
            return;
        }

        _panel.Option1Clicked += HandleOption;
        _panel.Option2Clicked += HandleOption;
        _panel.Option3Clicked += HandleOption;
    }

    private void HandleOption(string name)
    {
        GameManager.Instance.CheckCaseWin(buttonBindOptionId[name]);
        AudioManager.Instance.StartPlaySound("sfx_01_07_09", false);
        UIManager.Instance.HidePanel("option_panel");
    }

}
