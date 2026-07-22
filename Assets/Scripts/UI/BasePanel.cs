using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BasePanel : MonoBehaviour
{
    protected readonly Dictionary<string, List<UIBehaviour>> _components =
        new Dictionary<string, List<UIBehaviour>>();

    public string PanelName { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsOpen { get; private set; }

    protected virtual void Awake()
    {
        CacheComponents<Button>();
        CacheComponents<Text>();
        CacheComponents<Image>();
        CacheComponents<Toggle>();
        CacheComponents<Slider>();
        CacheComponents<ScrollRect>();
    }

    public void Init(string panelName)
    {
        if (IsInitialized)
        {
            return;
        }

        PanelName = panelName;
        IsInitialized = true;
        OnInit();
    }

    public void Open(object data = null)
    {
        if (!IsInitialized)
        {
            Init(gameObject.name);
        }

        IsOpen = true;
        gameObject.SetActive(true);
        OnOpen(data);
    }

    public void Refresh(object data = null)
    {
        OnRefresh(data);
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        OnClose();
        IsOpen = false;
        gameObject.SetActive(false);
    }

    protected T FindComponent<T>(string name) where T : UIBehaviour
    {
        if (!_components.TryGetValue(name, out List<UIBehaviour> components))
        {
            return null;
        }

        foreach (UIBehaviour component in components)
        {
            if (component is T result)
            {
                return result;
            }
        }

        return null;
    }

    private void CacheComponents<T>() where T : UIBehaviour
    {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            string componentName = component.gameObject.name;
            if (!_components.TryGetValue(componentName, out List<UIBehaviour> cachedComponents))
            {
                cachedComponents = new List<UIBehaviour>();
                _components.Add(componentName, cachedComponents);
            }

            cachedComponents.Add(component);

            if (component is Button button)
            {
                button.onClick.AddListener(() => OnButtonClick(componentName));
            }

            if (component is Slider slider)
            {
                slider.onValueChanged.AddListener((value) => OnSliderValueChange(componentName));
            }
        }
    }

    protected virtual void OnInit() { }
    protected virtual void OnOpen(object data) { }
    protected virtual void OnRefresh(object data) { }
    protected virtual void OnClose() { }
    protected virtual void OnButtonClick(string buttonName) { }
    protected virtual void OnSliderValueChange(string sliderName) { }
}
