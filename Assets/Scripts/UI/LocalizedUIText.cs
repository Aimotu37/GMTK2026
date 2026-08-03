using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LocalizedUIText : MonoBehaviour
{
    [SerializeField] private string _localizationKey;

    private TMP_Text _tmpText;
    private Text _legacyText;
    private DataManager _dataManager;
    private Coroutine _refreshCoroutine;

    private void Awake()
    {
        _tmpText = GetComponent<TMP_Text>();
        _legacyText = GetComponent<Text>();

        if (_tmpText == null && _legacyText == null)
        {
            Debug.LogError($"{name} requires a TMP_Text or Text component.", this);
        }
    }

    private void OnEnable()
    {
        _dataManager = DataManager.Instance;
        _dataManager.LocaleChanged += HandleLocaleChanged;
        RefreshText();
    }

    private void OnDisable()
    {
        if (_dataManager != null)
        {
            _dataManager.LocaleChanged -= HandleLocaleChanged;
        }
        if (_refreshCoroutine != null)
        {
            StopCoroutine(_refreshCoroutine);
            _refreshCoroutine = null;
        }
    }

    private void HandleLocaleChanged(string localeCode)
    {
        RefreshText();
    }

    private void RefreshText()
    {
        if (string.IsNullOrWhiteSpace(_localizationKey))
        {
            return;
        }

        if (_refreshCoroutine != null)
        {
            StopCoroutine(_refreshCoroutine);
        }

        _refreshCoroutine = StartCoroutine(RefreshTextRoutine());
    }

    private IEnumerator RefreshTextRoutine()
    {
        string localizedText = _localizationKey;
        yield return _dataManager.GetLocalizedUiTextAsync(
            _localizationKey,
            value => localizedText = value);

        if (_tmpText != null)
        {
            _tmpText.text = localizedText;
        }
        else if (_legacyText != null)
        {
            _legacyText.text = localizedText;
        }

        _refreshCoroutine = null;
    }
}
