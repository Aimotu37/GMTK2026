using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]

public class DataManager : SingletonMono<DataManager>
{

    private const int CaseCount = 3;
    private List<string> caseKeys = new List<string>() { "config/case_1001", "config/case_1002", "config/case_1003" };
    private List<string> itemKeys = new List<string>() { "config/items_1001", "config/items_1002", "config/items_1003" };
    private List<string> optionKeys = new List<string>() { "config/options_1001", "config/options_1002", "config/options_1003" };

    private Dictionary<int, CaseDataSO> _caseConfigCache = new Dictionary<int, CaseDataSO>();
    private Dictionary<int, ItemDataSO> _itemConfigCache = new Dictionary<int, ItemDataSO>();
    private Dictionary<int, OptionDataSO> _optionConfigCache = new Dictionary<int, OptionDataSO>();
    private DemonSpeakDataSO _demonSpeakConfigCache;
    private DebuffDataSO _debuffConfigCache;

    //初始化变量
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    private bool _initFailed;
    private bool _cachedCases;
    private bool _cachedItems;
    private bool _cachedOptions;
    private bool _cachedDemonSpeaks;
    private bool _cachedDebuffs;

    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
        {
            yield break;
        }

        _initFailed = false;
        _cachedCases = false;
        _cachedItems = false;
        _cachedOptions = false;
        _cachedDemonSpeaks = false;
        _cachedDebuffs = false;

        if (!_cachedCases)
        {
            foreach (var key in caseKeys)
            {
                LoadConfig<CaseDataSO>(key);
            }
        }
        if (!_cachedItems)
        {
            foreach (var key in itemKeys)
            {
                LoadConfig<ItemDataSO>(key);
            }
        }
        if (!_cachedOptions)
        {
            foreach (var key in optionKeys)
            {
                LoadConfig<OptionDataSO>(key);
            }
        }
        if (!_cachedDemonSpeaks)
        {
            LoadConfig<DemonSpeakDataSO>("config/demon_speaks");
        }
        if (!_cachedDebuffs)
        {
            LoadConfig<DebuffDataSO>("config/debuffs");
        }

        yield return new WaitUntil(() => _cachedCases && _cachedItems && _cachedOptions && _cachedDemonSpeaks && _cachedDebuffs);

        if (_initFailed)
        {
            _isInitialized = false;
            Debug.LogError(this.name + " Initialization Failed.");
        }
        else
        {
            _isInitialized = true;
            Debug.Log(this.name + " Initialization Successful.");
        }
    }

    public CaseDataSO GetCase(int caseID)
    {
        return _caseConfigCache[caseID];
    }

    public ItemDataSO GetItems(int caseID)
    {
        return _itemConfigCache[caseID];
    }

    public OptionDataSO GetOptions(int caseID)
    {
        return _optionConfigCache[caseID];
    }

    public DemonSpeakDataSO GetDemonSpeak()
    {
        return _demonSpeakConfigCache;
    }

    public DebuffDataSO GetDebuffData()
    {
        return _debuffConfigCache;
    }

    private void LoadConfig<T>(string configkey) where T : ScriptableObject
    {
        ResourcesManager.Instance.AddressablesLoadAsync<T>(configkey, (T) =>
        {
            if (T is CaseDataSO caseData)
            {
                _caseConfigCache.Add(caseData.caseID, caseData);
                _cachedCases = _caseConfigCache.Count == CaseCount;
            }

            if (T is ItemDataSO itemData)
            {
                _itemConfigCache.Add(itemData.caseID, itemData);
                _cachedItems = _itemConfigCache.Count == CaseCount;
            }

            if (T is OptionDataSO optionData)
            {
                _optionConfigCache.Add(optionData.caseID, optionData);
                _cachedOptions = _optionConfigCache.Count == CaseCount;
            }

            if (T is DemonSpeakDataSO demonSpeakData)
            {
                _demonSpeakConfigCache = demonSpeakData;
                _cachedDemonSpeaks = true;
            }

            if (T is DebuffDataSO debuffData)
            {
                _debuffConfigCache = debuffData;
                _cachedDebuffs = true;
            }
        });
    }
}
