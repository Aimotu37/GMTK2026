using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DebuffData", menuName = "Data/CreateDebuffData")]
public class DebuffDataSO : ScriptableObject
{
    public List<DebuffData> debuffDatas;
}

[Serializable]
public class DebuffData
{
    public int debuffID;
    public string debuffName;
    public EffectType effectType;
    public string effectParam;
    public string debuffDesc;
}

public enum EffectType
{
    CoveClue,
    ReduceSpeak
}