using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CaseData", menuName = "Data/CreateCaseData")]
public class CaseDataSO : ScriptableObject
{
    public int caseID;
    public string caseName;
    public string sceneBg;
    public List<StoryDialogueLineData> storyLines;
    public List<StoryDialogueLineData> truthLines;
}
