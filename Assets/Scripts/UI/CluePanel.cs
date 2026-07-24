using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CluePanel : BasePanel
{
    private TMP_Text textMesh;
    private Text text;
    private CanvasGroup canvasGroup;


    void OnEnable()
    {
        textMesh = FindComponent<TMP_Text>("ClueText");
        text = FindComponent<Text>("ClueText");
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetClueText(string clue)
    {
        textMesh.text = clue;
        text.text = clue;
    }

    public void SetCanvasGroupAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
    }

    protected override void OnOpen(object data)
    {
        if (data is string clue)
        {
            SetClueText(clue);
        }
    }

    public TMP_Text GetClueTextComponent()
    {
        return textMesh;
    }
}
