using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class DeathPanel : BasePanel, IPointerClickHandler
{
    private const string BackToMainButtonName = "BackToMainButton";
    private const string RetryButtonName = "RetryButton";

    public event Action BackToMainClicked;
    public event Action RetryClicked;
    public event Action BackgroundClicked;

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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsOpen)
        {
            BackgroundClicked?.Invoke();
        }
    }
}
