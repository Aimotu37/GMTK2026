using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.EventSystems;

public class SuccessPanel : BasePanel, IPointerClickHandler
{
    private const string NextCaseButtonName = "NextCaseButton";

    public event Action NextCaseClicked;
    public event Action BackgroundClicked;

    [SerializeField]
    private GameObject truthBg;

    [SerializeField]
    private GameObject hintText;

    [SerializeField]
    private GameObject successBg;

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case NextCaseButtonName:
                NextCaseClicked?.Invoke();
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

    public void ShowCaseTruth(string truthText)
    {
        truthBg.SetActive(true);
        truthBg.GetComponentInChildren<TMP_Text>().text = truthText;
        successBg.SetActive(false);
        hintText.SetActive(false);
    }
}
