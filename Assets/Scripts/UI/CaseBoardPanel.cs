using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CaseBoardPanel : BasePanel
{

    private const string TruthButtonName = "RestoreTruthButton";
    private const string SettingButtonName = "SettingButton";

    [SerializeField] private Transform content;
    private List<ClueItem> clutItems = new List<ClueItem>();
    private TMP_Text _caseNameText;

    public event Action OnTruthClicked;
    public event Action OnSettingClicked;

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case TruthButtonName:
                OnTruthClicked?.Invoke();
                break;
            case SettingButtonName:
                OnSettingClicked?.Invoke();
                break;
        }
    }

    void Start()
    {
        for (int i = 0; i < content.childCount; i++)
        {
            clutItems.Add(content.GetChild(i).GetComponent<ClueItem>());
        }
        if (_caseNameText == null)
        {
            _caseNameText = FindComponent<TMP_Text>("CaseName");
        }
    }

    public void ChangeClueState(int index, string text, string localizationKey)
    {
        if (index < 0 || index >= clutItems.Count)
            return;

        clutItems[index].SetUnlockedText(text, localizationKey);

        StartCoroutine(RefreshLayoutNextFrame());
    }

    /// <summary>把已解锁的线索重新盖回未获得状态（恶魔"覆盖线索"诅咒用）</summary>
    public void CoverClueState(int index)
    {
        if (index < 0 || index >= clutItems.Count)
            return;

        clutItems[index].SetLocked();

        StartCoroutine(RefreshLayoutNextFrame());
    }

    public void SetCaseName(string caseName)
    {
        _caseNameText.text = caseName;
    }

    public void SetTruthInteractable(bool interactable)
    {
        FindComponent<Button>(TruthButtonName).interactable = interactable;
    }


    private IEnumerator RefreshLayoutNextFrame()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
        Canvas.ForceUpdateCanvases();
    }

}
