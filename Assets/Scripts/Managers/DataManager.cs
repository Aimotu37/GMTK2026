using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : SingletonMono<DataManager>
{
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
}
