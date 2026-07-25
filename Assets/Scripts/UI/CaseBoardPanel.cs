using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CaseBoardPanel : BasePanel
{

    private const string TruthButtonName = "RestoreTruthButton";
    [SerializeField] private Transform content;
    private List<ClueItem> clutItems = new List<ClueItem>();

    public event Action OnTruthClicked;

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case TruthButtonName:
                OnTruthClicked?.Invoke();
                break;
        }
    }

    void Start()
    {
        for (int i = 0; i < content.childCount; i++)
        {
            clutItems.Add(content.GetChild(i).GetComponent<ClueItem>());
        }
    }

    public void ChangeClueState(int index, string text)
    {
        if (index < 0 || index >= clutItems.Count)
            return;

        clutItems[index].lockIcon.gameObject.SetActive(false);
        clutItems[index].clueText.text = text;

        StartCoroutine(RefreshLayoutNextFrame());
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
