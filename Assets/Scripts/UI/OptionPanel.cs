using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class OptionPanel : BasePanel
{
    private const string OptionButton1Name = "OptionButton1";
    private const string OptionButton2Name = "OptionButton2";
    private const string OptionButton3Name = "OptionButton3";
    private const string QuitButtonName = "QuitButton";

    public event Action Option1Clicked;
    public event Action Option2Clicked;
    public event Action Option3Clicked;
    public event Action QuitClicked;

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case OptionButton1Name:
                Option1Clicked?.Invoke();
                break;

            case OptionButton2Name:
                Option2Clicked?.Invoke();
                break;

            case OptionButton3Name:
                Option3Clicked?.Invoke();
                break;

            case QuitButtonName:
                QuitClicked?.Invoke();
                break;
        }
    }
}
