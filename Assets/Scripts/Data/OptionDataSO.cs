using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OptionData", menuName = "Data/CreateOptionData")]
public class OptionDataSO : ScriptableObject
{
    public int caseID;
    public List<OptionData> optionDatas;
}

[Serializable]
public class OptionData
{
    public int optionID;
    public int caseID;
    public string optionText;
    public bool isCorrect;
}