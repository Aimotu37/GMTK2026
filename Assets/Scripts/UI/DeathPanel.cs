using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DeathPanel : BasePanel
{
    private const string BackToMainButtonName = "BackToMainButton";
    private const string RetryButtonName = "RetryButton";

    public event Action BackToMainClicked;
    public event Action RetryClicked;

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case BackToMainButtonName:
                BackToMainClicked?.Invoke();
                break;

            case RetryButtonName:
                RetryClicked?.Invoke();
                break;
        }
    }
}
