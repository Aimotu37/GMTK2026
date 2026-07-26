using System;
using UnityEngine;

[Serializable]
public class StoryDialogueLineData
{
    [TextArea(2, 10)]
    public string text;
    public bool showPortrait;
    public Sprite portrait;
}
