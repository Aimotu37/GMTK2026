using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DemonSpeakData", menuName = "Data/CreateDemonSpeakData")]
public class DemonSpeakDataSO : ScriptableObject
{
    public List<DemonSpeakData> demonSpeakDatas;
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
