using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Data/CreateItemData")]
public class ItemDataSO : ScriptableObject
{
    public int caseID;
    public List<ItemData> itemDatas;
}

[Serializable]
public class ItemData
{
    public int itemID;
    public int caseID;
    public string itemName;
    public string clueText;
    public string resPath;
}
