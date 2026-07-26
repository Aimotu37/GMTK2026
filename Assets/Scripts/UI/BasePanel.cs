using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BasePanel : MonoBehaviour
{
    private const float DefaultFadeInDuration = 0.3f;

    [SerializeField]
    private bool _fadeInOnOpen;

    [SerializeField, Min(0f)]
    private float _fadeInDuration = DefaultFadeInDuration;

    protected readonly Dictionary<string, List<UIBehaviour>> _components =
        new Dictionary<string, List<UIBehaviour>>();

    private CanvasGroup _canvasGroup;
    private Coroutine _fadeInCoroutine;

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
        CacheComponents<TMP_Text>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (!_fadeInOnOpen)
        {
            return;
        }

        if (_canvasGroup == null)
        {
            Debug.LogWarning($"{name} has fade-in enabled but no root CanvasGroup. Opening immediately.", this);
            return;
        }

        SetCanvasGroupState(0f, false);
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
        BeginFadeInIfEnabled();
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

        StopFadeInAndRestore();
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
            string componentGameObjectName = component.gameObject.name;
            if (!_components.TryGetValue(componentGameObjectName, out List<UIBehaviour> cachedComponents))
            {
                cachedComponents = new List<UIBehaviour>();
                _components.Add(componentGameObjectName, cachedComponents);
            }

            cachedComponents.Add(component);

            if (component is Button button)
            {
                button.onClick.AddListener(() => OnButtonClick(componentGameObjectName));
            }

            if (component is Slider slider)
            {
                slider.onValueChanged.AddListener((value) => OnSliderValueChange(componentGameObjectName));
            }
        }
    }

    private void BeginFadeInIfEnabled()
    {
        if (!_fadeInOnOpen || _canvasGroup == null)
        {
            return;
        }

        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
            _fadeInCoroutine = null;
        }

        SetCanvasGroupState(0f, false);

        if (_fadeInDuration <= 0f)
        {
            SetCanvasGroupState(1f, true);
            return;
        }

        _fadeInCoroutine = StartCoroutine(FadeInRoutine(_fadeInDuration));
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        SetCanvasGroupState(1f, true);
        _fadeInCoroutine = null;
    }

    private void StopFadeInAndRestore()
    {
        if (!_fadeInOnOpen || _canvasGroup == null)
        {
            return;
        }

        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
            _fadeInCoroutine = null;
        }

        SetCanvasGroupState(1f, true);
    }

    private void SetCanvasGroupState(float alpha, bool interactive)
    {
        _canvasGroup.alpha = alpha;
        _canvasGroup.interactable = interactive;
        _canvasGroup.blocksRaycasts = interactive;
    }

    protected virtual void OnInit() { }
    protected virtual void OnOpen(object data) { }
    protected virtual void OnRefresh(object data) { }
    protected virtual void OnClose() { }
    protected virtual void OnButtonClick(string buttonName) { }
    protected virtual void OnSliderValueChange(string sliderName) { }
}
