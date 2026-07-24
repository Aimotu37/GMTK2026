using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SuccessPanel : BasePanel
{
    private const string NextCaseButtonName = "NextCaseButton";

    public event Action NextCaseClicked;

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case NextCaseButtonName:
                NextCaseClicked?.Invoke();
                break;
        }
    }
}
