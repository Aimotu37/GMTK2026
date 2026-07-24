using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CaseBoardPanel : BasePanel
{
    [SerializeField] private Transform content;
    private List<ClueItem> clutItems = new List<ClueItem>();

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


    private IEnumerator RefreshLayoutNextFrame()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
        Canvas.ForceUpdateCanvases();
    }
}
