using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// UI 面板层级，从底层到顶层排列
/// </summary>
public enum E_UILayer
{
    BottomLayer,
    MiddleLayer,
    TopLayer,
    SystemLayer,
}

class PanelRecord
{
    public PanelRecord(BasePanel panel, bool closeOnBack, bool loadedByManager)
    {
        Panel = panel;
        CloseOnBack = closeOnBack;
        LoadedByManager = loadedByManager;
    }

    public BasePanel Panel { get; private set; }
    public bool CloseOnBack { get; private set; }
    public bool LoadedByManager { get; private set; }
}

/// <summary>
/// UI 管理器
/// 只负责 UI 根节点初始化、面板加载、挂载、查询、关闭和释放
/// </summary>
public class UIManager : SingletonMono<UIManager>
{

    private readonly Dictionary<string, PanelRecord> _panels = new Dictionary<string, PanelRecord>();
    private readonly List<string> _panelStack = new List<string>();

    private readonly string _panelAddressPrefix = "ui/";
    private readonly string _uiRootKey = "prefab/ui/ui_root";
    private readonly string _eventSystemKey = "prefab/event_system";

    //UIRoot组件缓存
    public RectTransform canvasRectTrans;
    private Transform _bottomLayerTrans;
    private Transform _middleLayerTrans;
    private Transform _topLayerTrans;
    private Transform _systemLayerTrans;

    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    //初始化记录
    private bool _eventSystemLoaded;
    private bool _uiRootLoaded;
    private bool _initFailed;


    #region  Public Methods

    /// <summary>
    /// 初始化 EventSystem 和 UIRootUIRoot 加载失败时初始化不会完成
    /// </summary>
    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
        {
            yield break;
        }

        _isInitialized = false;
        _eventSystemLoaded = false;
        _uiRootLoaded = false;
        _initFailed = false;

        if (EventSystem.current == null)
        {
            ResourcesManager.Instance.AddressablesLoadAsync<GameObject>(_eventSystemKey, HandleEventSystemLoad);
        }
        else
        {
            _eventSystemLoaded = true;
        }

        if (!_uiRootLoaded)
        {
            ResourcesManager.Instance.AddressablesLoadAsync<GameObject>(_uiRootKey, HandleUIRootLoad);
        }

        yield return new WaitUntil(() => _eventSystemLoaded && _uiRootLoaded);

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
    /// 显示指定面板已打开的面板会刷新并移动到栈顶；未打开的面板会先异步加载
    /// </summary>
    /// <typeparam name="T">面板脚本类型</typeparam>
    /// <param name="panelName">面板名称，同时作为资源路径后缀</param>
    /// <param name="layer">面板挂载层级</param>
    /// <param name="callback">面板打开完成回调，加载失败时传入 null</param>
    /// <param name="data">传递给面板生命周期的数据</param>
    /// <param name="closeOnBack">是否允许通过返回逻辑关闭</param>
    public void ShowPanel<T>(
        string panelName,
        E_UILayer layer = E_UILayer.MiddleLayer,
        UnityAction<T> callback = null,
        object data = null,
        bool closeOnBack = true
        ) where T : BasePanel
    {

        if (string.IsNullOrEmpty(panelName))
        {
            Debug.LogError("UIManager.ShowPanel requires a valid panel name.");
            callback?.Invoke(null);
            return;
        }

        if (_panels.ContainsKey(panelName))
        {
            BasePanel panel = _panels[panelName].Panel;
            panel.Refresh(data);
            panel.Open(data);
            MovePanelToTop(panelName);
            callback?.Invoke(panel as T);
            return;
        }

        LoadPanelAsync<T>(panelName, panel =>
        {
            if (panel == null)
            {
                callback?.Invoke(null);
                return;
            }

            RegisterPanelInternal(panelName, panel, layer, closeOnBack, true, data);
            callback?.Invoke(panel);
        });
    }

    /// <summary>
    /// 注册一个已经存在的面板实例，并交由 UIManager 管理层级、栈和生命周期
    /// </summary>
    /// <param name="panelName">面板唯一名称</param>
    /// <param name="panel">已存在的面板实例</param>
    /// <param name="layer">面板挂载层级</param>
    /// <param name="closeOnBack">是否允许通过返回逻辑关闭</param>
    /// <param name="data">传递给面板生命周期的数据</param>
    public void RegisterPanel(
        string panelName,
        BasePanel panel,
        E_UILayer layer = E_UILayer.MiddleLayer,
        bool closeOnBack = true,
        object data = null)
    {
        RegisterPanelInternal(panelName, panel, layer, closeOnBack, false, data);
    }

    /// <summary>
    /// 关闭并释放指定面板，同时从面板记录和栈中移除
    /// </summary>
    public void HidePanel(string panelName)
    {
        if (string.IsNullOrEmpty(panelName) || !_panels.ContainsKey(panelName))
        {
            return;
        }

        PanelRecord record = _panels[panelName];
        if (record.Panel != null)
        {
            record.Panel.Close();
        }

        _panels.Remove(panelName);
        _panelStack.Remove(panelName);
        ReleasePanelInstance(record);
    }

    /// <summary>
    /// 获取已打开或已注册的指定面板
    /// </summary>
    public T GetPanel<T>(string panelName) where T : BasePanel
    {
        if (string.IsNullOrEmpty(panelName) || !_panels.ContainsKey(panelName))
        {
            return null;
        }

        return _panels[panelName].Panel as T;
    }

    /// <summary>
    /// 获取指定 UI 层级的 Transform
    /// </summary>
    public Transform GetPanelLayer(E_UILayer layer)
    {
        switch (layer)
        {
            case E_UILayer.BottomLayer:
                return _bottomLayerTrans;
            case E_UILayer.MiddleLayer:
                return _middleLayerTrans;
            case E_UILayer.TopLayer:
                return _topLayerTrans;
            case E_UILayer.SystemLayer:
                return _systemLayerTrans;
            default:
                return null;
        }
    }

    /// <summary>
    /// 获取当前栈顶面板；如果栈中存在失效记录，会顺带清理
    /// </summary>
    public BasePanel GetTopPanel()
    {
        for (int i = _panelStack.Count - 1; i >= 0; i--)
        {
            string panelName = _panelStack[i];
            if (_panels.ContainsKey(panelName))
            {
                return _panels[panelName].Panel;
            }

            _panelStack.RemoveAt(i);
        }

        return null;
    }

    /// <summary>
    /// 关闭当前栈顶且允许返回关闭的面板
    /// </summary>
    public bool CloseTopPanel()
    {
        for (int i = _panelStack.Count - 1; i >= 0; i--)
        {
            string panelName = _panelStack[i];
            if (!_panels.ContainsKey(panelName))
            {
                _panelStack.RemoveAt(i);
                continue;
            }

            if (!_panels[panelName].CloseOnBack)
            {
                return false;
            }

            HidePanel(panelName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 处理返回操作，默认等价于关闭栈顶面板
    /// </summary>
    public bool HandleBack()
    {
        return CloseTopPanel();
    }

    #endregion

    #region  Private Methods

    /// <summary>
    /// 确保场景中存在EventSystem
    /// 通过Addressables加载预制体
    /// </summary>
    private void HandleEventSystemLoad(GameObject eventSystemObject)
    {
        if (eventSystemObject == null)
        {
            Debug.LogError($"UIManager failed to load EventSystem prefab: {_eventSystemKey}");
            _initFailed = true;
        }
        _eventSystemLoaded = true;
    }

    /// <summary>
    /// 加载 UIRoot 预制体，并缓存四个标准 UI 层级
    /// </summary>
    private void HandleUIRootLoad(GameObject UIGameObject)
    {
        if (UIGameObject == null)
        {
            Debug.LogError($"UIManager failed to load UIRoot prefab: {_uiRootKey}");
            _initFailed = true;
        }
        else
        {
            canvasRectTrans = UIGameObject.transform as RectTransform;
            if (canvasRectTrans == null)
            {
                Debug.LogError("UIManager requires UIRoot to use RectTransform.");
                _initFailed = true;
            }

            _bottomLayerTrans = canvasRectTrans.Find("BottomLayer");
            _middleLayerTrans = canvasRectTrans.Find("MiddleLayer");
            _topLayerTrans = canvasRectTrans.Find("TopLayer");
            _systemLayerTrans = canvasRectTrans.Find("SystemLayer");

            if (_bottomLayerTrans == null ||
                _middleLayerTrans == null ||
                _topLayerTrans == null ||
                _systemLayerTrans == null)
            {
                Debug.LogError("UIRoot prefab must contain BottomLayer, MiddleLayer, TopLayer and SystemLayer.");
                _initFailed = true;
                canvasRectTrans = null;
            }
        }
        _uiRootLoaded = true;
    }

    private void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// 异步加载面板预制体，并校验目标面板组件是否存在
    /// </summary>
    private void LoadPanelAsync<T>(string panelName, UnityAction<T> callback) where T : BasePanel
    {
        string panelAddress = ResolvePanelAddress(panelName);
        ResourcesManager.Instance.AddressablesLoadAsync<GameObject>(panelAddress, obj =>
        {
            if (obj == null)
            {
                Debug.LogError($"UI panel address not found: {panelAddress}");
                callback?.Invoke(null);
                return;
            }

            T panel = obj.GetComponent<T>();
            if (panel == null)
            {
                Debug.LogError($"UI panel {panelName} is missing component {typeof(T).Name}.");
                ResourcesManager.Instance.ReleaseAddressable(obj);
                callback?.Invoke(null);
                return;
            }

            callback?.Invoke(panel);
        });
    }

    /// <summary>
    /// 将 UIManager 使用的面板名转换为 Addressables 地址
    /// </summary>
    private string ResolvePanelAddress(string panelName)
    {
        return _panelAddressPrefix + panelName;
    }

    /// <summary>
    /// 注册面板的内部实现，统一处理挂载、初始化、入栈和打开生命周期
    /// </summary>
    private void RegisterPanelInternal(
        string panelName,
        BasePanel panel,
        E_UILayer layer,
        bool closeOnBack,
        bool loadedByManager,
        object data)
    {
        if (string.IsNullOrEmpty(panelName))
        {
            Debug.LogError("UIManager.RegisterPanel requires a valid panel name.");
            ReleaseInvalidPanel(panel, loadedByManager);
            return;
        }

        if (panel == null)
        {
            Debug.LogError($"UIManager.RegisterPanel received null panel: {panelName}");
            return;
        }

        if (_panels.ContainsKey(panelName))
        {
            HidePanel(panelName);
        }

        Transform layerTransform = GetPanelLayer(layer);
        if (layerTransform == null)
        {
            Debug.LogError($"UI layer missing for panel {panelName}: {layer}");
            ReleaseInvalidPanel(panel, loadedByManager);
            return;
        }

        MountPanel(panel, layerTransform);
        panel.Init(panelName);

        _panels[panelName] = new PanelRecord(panel, closeOnBack, loadedByManager);
        MovePanelToTop(panelName);

        panel.Open(data);
    }

    /// <summary>
    /// 将面板挂到指定 UI 层级，并尽量拉伸铺满父节点
    /// </summary>
    private void MountPanel(BasePanel panel, Transform layerTransform)
    {
        panel.transform.SetParent(layerTransform, false);

        RectTransform rectTransform = panel.transform as RectTransform;
        if (rectTransform != null)
        {
            StretchToParent(rectTransform);
            return;
        }

        panel.transform.localScale = Vector3.one;
        panel.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// 将指定面板移动到返回栈顶
    /// </summary>
    private void MovePanelToTop(string panelName)
    {
        _panelStack.Remove(panelName);
        _panelStack.Add(panelName);
    }

    /// <summary>
    /// 注册失败时释放由 UIManager 加载出来的无效面板；外部传入的面板不在这里销毁
    /// </summary>
    private void ReleaseInvalidPanel(BasePanel panel, bool loadedByManager)
    {
        if (panel == null || !loadedByManager)
        {
            return;
        }

        ReleasePanelInstance(new PanelRecord(panel, true, loadedByManager));
    }

    /// <summary>
    /// 释放面板实例UIManager 加载的实例使用 Addressables 释放
    /// </summary>
    private void ReleasePanelInstance(PanelRecord record)
    {
        if (record == null || record.Panel == null)
        {
            return;
        }

        if (record.LoadedByManager)
        {
            ResourcesManager.Instance.ReleaseAddressable(record.Panel.gameObject);
            return;
        }

        GameObject.Destroy(record.Panel.gameObject);
    }
    #endregion
}
