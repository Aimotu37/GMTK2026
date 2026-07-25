using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DemonSpeakData", menuName = "Data/CreateDemonSpeakData")]
public class DemonSpeakDataSO : ScriptableObject
{
    public List<DemonSpeakData> demonSpeakDatas;

    // 开场剧情（恶魔契约对话），整局游戏只在最开始播放一次，与具体案件无关
    [TextArea(2, 5)]
    public List<string> openingLines;
}

[Serializable]
public class DemonSpeakData
{
    public int id;
    public string defaultSpeak;
    public string hurtSpeak;
    public string defeatSpeak;
    public string mockSpeak;
    public string duration;
}
