using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


class Scenes
{
    public const string PersistentSceneName = "Scene_Persistent";
    public const string MainMenuSceneName = "Scene_Main";
    public const string Level_1_SceneName = "Scene_Level_1";
    public const string Interaction_Test_SceneName = "Scene_Interaction_Test";
}

/// <summary>
/// 场景管理器
/// </summary>
public class ScenesManager : SingletonMono<ScenesManager>
{
    private bool _isSwitching;
    private string _currentGameSceneName;
    public bool IsSwitching => _isSwitching;
    public string CurrentContentSceneName => _currentGameSceneName;

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

    /// <summary>
    /// 以 Additive 模式切换内容场景，完成后通过回调返回切换结果。
    /// </summary>
    public void SwitchGameScene(string sceneName, UnityAction<bool> callback = null)
    {
        if (_isSwitching)
        {
            callback?.Invoke(false);
            return;
        }

        if (!IsValidGameScene(sceneName))
        {
            Debug.LogError($"Invalid content scene: {sceneName}");
            callback?.Invoke(false);
            return;
        }

        StartCoroutine(SwitchGameSceneCoroutine(sceneName, callback));
    }

    /// <summary>
    /// 场景转换
    /// </summary>
    public void SwitchScene(string toScene, UnityAction action)
    {
        LoadSceneAsync(toScene, action);
    }

    /// <summary>
    /// 异步加载内容场景，并仅在加载成功时执行回调。
    /// </summary>
    public void LoadSceneAsync(string sceneName, UnityAction action = null)
    {
        SwitchGameScene(sceneName, success =>
        {
            if (success)
            {
                action?.Invoke();
            }
        });
    }

    private bool IsValidGameScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) && sceneName != Scenes.PersistentSceneName;
    }


    private IEnumerator SwitchGameSceneCoroutine(string targetSceneName, UnityAction<bool> callback)
    {
        _isSwitching = true;

        //目标场景与当前场景是同一个场景
        if (_currentGameSceneName == targetSceneName)
        {
            Scene currentScene = SceneManager.GetSceneByName(targetSceneName);
            if (currentScene.IsValid() && currentScene.isLoaded)
            {
                SceneManager.SetActiveScene(currentScene);
                _isSwitching = false;
                callback?.Invoke(true);
                yield break;
            }
        }

        // 卸载上一个游戏内容场景
        if (IsValidGameScene(_currentGameSceneName))
        {
            Scene previousScene = SceneManager.GetSceneByName(_currentGameSceneName);
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(previousScene);
                if (unloadOperation != null)
                {
                    yield return unloadOperation;
                }
            }
        }


        // 采用Addictive模式加载新场景
        Scene targetScene = SceneManager.GetSceneByName(targetSceneName);
        if (!targetScene.IsValid() || !targetScene.isLoaded)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                _isSwitching = false;
                callback?.Invoke(false);
                yield break;
            }

            while (!loadOperation.isDone)
            {
                EventManager.Instance.EventTrigger("OnSceneLoading", loadOperation.progress);
                yield return null;
            }
        }

        targetScene = SceneManager.GetSceneByName(targetSceneName);
        if (!targetScene.IsValid() || !targetScene.isLoaded)
        {
            _isSwitching = false;
            callback?.Invoke(false);
            yield break;
        }

        SceneManager.SetActiveScene(targetScene);
        _currentGameSceneName = targetSceneName;
        _isSwitching = false;
        callback?.Invoke(true);
    }
}
