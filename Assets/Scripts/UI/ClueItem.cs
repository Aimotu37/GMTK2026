using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClueItem : BasePanel
{
    private const string LockedClueLabelKey = "clue.locked";

    public Image background;
    public Image lockIcon;
    public TMP_Text clueText;

    private DataManager _dataManager;
    private Coroutine _refreshCoroutine;
    private string _clueLocalizationKey;

    protected override void Awake()
    {
        base.Awake();
        background = FindComponent<Image>("Background");
        lockIcon = FindComponent<Image>("LockIcon");
        clueText = FindComponent<TMP_Text>("ClueText");
    }

    private void OnEnable()
    {
        _dataManager = DataManager.Instance;
        _dataManager.LocaleChanged += HandleLocaleChanged;
        RefreshCurrentText();
    }

    private void OnDisable()
    {
        if (_dataManager != null)
        {
            _dataManager.LocaleChanged -= HandleLocaleChanged;
        }

        StopRefresh();
    }

    public void SetUnlockedText(string text, string localizationKey)
    {
        StopRefresh();
        _clueLocalizationKey = localizationKey;
        lockIcon.gameObject.SetActive(false);
        clueText.text = text;
    }

    public void SetLocked()
    {
        _clueLocalizationKey = null;
        lockIcon.gameObject.SetActive(true);
        RefreshCurrentText();
    }

    private void HandleLocaleChanged(string localeCode)
    {
        RefreshCurrentText();
    }

    private void RefreshCurrentText()
    {
        if (clueText == null || lockIcon == null || _dataManager == null)
        {
            return;
        }

        StopRefresh();
        _refreshCoroutine = StartCoroutine(RefreshCurrentTextRoutine());
    }

    private IEnumerator RefreshCurrentTextRoutine()
    {
        bool isLocked = lockIcon.gameObject.activeSelf;
        string localizationKey = isLocked
            ? LockedClueLabelKey
            : _clueLocalizationKey;
        if (string.IsNullOrWhiteSpace(localizationKey))
        {
            _refreshCoroutine = null;
            yield break;
        }

        string localizedText = localizationKey;
        if (isLocked)
        {
            yield return _dataManager.GetLocalizedUiTextAsync(
                localizationKey,
                value => localizedText = value);
        }
        else
        {
            yield return _dataManager.GetLocalizedTextAsync(
                localizationKey,
                value => localizedText = value);
        }

        clueText.text = localizedText;
        _refreshCoroutine = null;
    }

    private void StopRefresh()
    {
        if (_refreshCoroutine == null) return;

        StopCoroutine(_refreshCoroutine);
        _refreshCoroutine = null;
    }
}
