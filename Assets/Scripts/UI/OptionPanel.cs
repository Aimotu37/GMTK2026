using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

public class OptionPanel : BasePanel
{
    private const string OptionButton1Name = "OptionButton1";
    private const string OptionButton2Name = "OptionButton2";
    private const string OptionButton3Name = "OptionButton3";
    private const string QuitButtonName = "QuitButton";

    public event Action<string> Option1Clicked;
    public event Action<string> Option2Clicked;
    public event Action<string> Option3Clicked;
    public event Action QuitClicked;

    List<Button> buttons = new List<Button>();

    void Start()
    {
        buttons.Add(FindComponent<Button>(OptionButton1Name));
        buttons.Add(FindComponent<Button>(OptionButton2Name));
        buttons.Add(FindComponent<Button>(OptionButton3Name));
    }

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case OptionButton1Name:
                Option1Clicked?.Invoke(buttonName);
                break;

            case OptionButton2Name:
                Option2Clicked?.Invoke(buttonName);
                break;

            case OptionButton3Name:
                Option3Clicked?.Invoke(buttonName);
                break;

            case QuitButtonName:
                QuitClicked?.Invoke();
                break;
        }
    }

    public Dictionary<string, int> InitButtonText(
        IReadOnlyList<OptionsConfig> options,
        IReadOnlyList<string> localizedTexts)
    {
        Dictionary<string, int> buttonBindOptionId = new Dictionary<string, int>();
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].GetComponentInChildren<TMP_Text>().text = localizedTexts[i];
            buttonBindOptionId.Add(buttons[i].name, options[i].OptionId);
        }
        return buttonBindOptionId;
    }
}
