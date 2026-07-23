using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : BasePanel
{
    private const string ContinueButtonName = "ContinueButton";
    private const string NewGameButtonName = "NewGameButton";
    private const string SettingsButtonName = "SettingsButton";
    private const string QuitButtonName = "QuitButton";

    public event Action NewGameClicked;
    public event Action ContinueClicked;
    public event Action SettingsClicked;
    public event Action QuitClicked;

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case NewGameButtonName:
                NewGameClicked?.Invoke();
                break;

            case ContinueButtonName:
                ContinueClicked?.Invoke();
                break;

            case SettingsButtonName:
                SettingsClicked?.Invoke();
                break;

            case QuitButtonName:
                QuitClicked?.Invoke();
                break;
        }
    }

    public void SetContinueBTNInteractble(bool isInteractable)
    {
        foreach (var ui in _components[ContinueButtonName])
        {
            if (ui is Button button)
            {
                button.interactable = isInteractable;
            }
        }
    }
}
