using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CaseData", menuName = "Data/CreateCaseData")]
public class CaseDataSO : ScriptableObject
{
    public int caseID;
    public string caseName;
    public string sceneBg;
    [TextArea(3, 8)]
    public string storyText;
    [TextArea(3, 8)]
    public string truthText;
}
