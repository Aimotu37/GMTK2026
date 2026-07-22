using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 资源加载管理器，提供Resources和Addressables加载方案
/// </summary>
public class ResourcesManager : SingletonMono<ResourcesManager>
{
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    private bool _initFailed;

    #region Public Methods

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
    /// 使用 Resources 同步加载GameObject 会自动实例化
    /// </summary>
    public T ResourceLoad<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Resources path is empty.");
            return null;
        }

        T asset = Resources.Load<T>(path);
        return asset is GameObject ? GameObject.Instantiate(asset) : asset;
    }

    /// <summary>
    /// 使用 Resources 异步加载GameObject 会自动实例化
    /// </summary>
    public void ResourceLoadAsync<T>(string path, UnityAction<T> callback) where T : Object
    {
        StartCoroutine(ResourceLoadAsyncRoutine(path, callback));
    }

    /// <summary>
    /// 使用 Addressables 同步加载GameObject 会通过 Addressables.InstantiateAsync 实例化
    /// </summary>
    [System.Obsolete]
    public T AddressablesLoad<T>(string address) where T : Object
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("Addressables address is empty.");
            return null;
        }

        if (typeof(T) == typeof(GameObject))
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address);
            GameObject instance = handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return instance as T;
            }

            if (handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
            }
            Debug.LogError($"Addressables instantiate failed: {address}");
            return null;
        }

        AsyncOperationHandle<T> assetHandle = Addressables.LoadAssetAsync<T>(address);
        T asset = assetHandle.WaitForCompletion();

        if (assetHandle.Status == AsyncOperationStatus.Succeeded)
        {
            return asset;
        }

        if (assetHandle.IsValid())
        {
            Addressables.Release(assetHandle);
        }
        Debug.LogError($"Addressables load failed: {address}");
        return null;
    }

    /// <summary>
    /// 使用 Addressables 异步加载GameObject 会通过 Addressables.InstantiateAsync 实例化
    /// </summary>
    public void AddressablesLoadAsync<T>(string address, UnityAction<T> callback = null) where T : Object
    {
        StartCoroutine(AddressablesLoadAsyncRoutine(address, callback));
    }


    /// <summary>
    /// 释放 Resources 实例,非 GameObject 资源由 UnloadUnusedAssets 清理
    /// </summary>
    public void ReleaseResource(Object resource)
    {
        if (resource is GameObject gameObject)
        {
            GameObject.Destroy(gameObject);
        }
    }

    /// <summary>
    /// 释放 Addressables 资源或实例
    /// </summary>
    public void ReleaseAddressable(Object resource)
    {
        if (resource == null)
        {
            return;
        }

        if (resource is GameObject gameObject)
        {
            Addressables.ReleaseInstance(gameObject);
            return;
        }

        Addressables.Release(resource);
    }

    /// <summary>
    /// 卸载Resources加载的资源
    /// </summary>
    public AsyncOperation UnloadUnusedAssets()
    {
        return Resources.UnloadUnusedAssets();
    }
    #endregion

    #region Private Methods
    private IEnumerator ResourceLoadAsyncRoutine<T>(string path, UnityAction<T> callback) where T : Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Resources path is empty.");
            callback?.Invoke(null);
            yield break;
        }

        ResourceRequest request = Resources.LoadAsync<T>(path);
        yield return request;

        T asset = request.asset as T;
        callback?.Invoke(asset is GameObject ? GameObject.Instantiate(asset) : asset);
    }

    private IEnumerator AddressablesLoadAsyncRoutine<T>(string address, UnityAction<T> callback) where T : Object
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("Addressables address is empty.");
            callback?.Invoke(null);
            yield break;
        }

        if (typeof(T) == typeof(GameObject))
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                callback?.Invoke(handle.Result as T);
                yield break;
            }

            if (handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
            }
            Debug.LogError($"Addressables instantiate failed: {address}");
            callback?.Invoke(null);
            yield break;
        }

        AsyncOperationHandle<T> assetHandle = Addressables.LoadAssetAsync<T>(address);
        yield return assetHandle;

        if (assetHandle.Status == AsyncOperationStatus.Succeeded)
        {
            callback?.Invoke(assetHandle.Result);
            yield break;
        }

        if (assetHandle.IsValid())
        {
            Addressables.Release(assetHandle);
        }
        Debug.LogError($"Addressables load failed: {address}");
        callback?.Invoke(null);
    }
    #endregion
}
