using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]

public class DataManager : SingletonMono<DataManager>
{
    private Dictionary<string, object> _configCache = new Dictionary<string, object>();
    //初始化变量
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    private bool _initFailed;

    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
        {
            yield break;
        }

        _initFailed = false;

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

    private void LoadConfig<T>(string key) where T : ScriptableObject
    {
        T asset = Resources.Load<T>($"Configs/{key}");
        if (asset != null)
            _configCache[key] = asset;
        else
            Debug.LogWarning($"Config {key} not found.");
    }

    public T GetConfig<T>(string key) where T : ScriptableObject
    {
        if (_configCache.ContainsKey(key))
        {
            return _configCache[key] as T;
        }
        return null;
    }

}
