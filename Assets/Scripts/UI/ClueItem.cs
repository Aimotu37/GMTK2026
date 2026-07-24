using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClueItem : BasePanel
{
    public Image background;
    public Image lockIcon;
    public TMP_Text clueText;

    void Start()
    {
        background = FindComponent<Image>("Background");
        lockIcon = FindComponent<Image>("LockIcon");
        clueText = FindComponent<TMP_Text>("ClueText");
    }
}
