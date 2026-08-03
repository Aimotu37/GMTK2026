using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingPresenter : MonoBehaviour
{
    private const string ChineseLanguageLabelKey = "settings.language.chinese";
    private const string EnglishLanguageLabelKey = "settings.language.english";

    private SettingPanel _panel;
    private bool _isChangingLanguage;

    [SerializeField]
    private string _panelName = "main_setting_panel";
    [SerializeField]
    private E_UILayer _panelLayer = E_UILayer.TopLayer;

    void OnEnable()
    {
        _panel = GetComponent<SettingPanel>();
        RegisterEvents();
        OnPanelShown();
    }

    private void RegisterEvents()
    {
        if (_panel == null)
        {
            Debug.LogError(name + $" failed to show panel: {_panelName}");
            return;
        }

        _panel.SetVolume(new List<float>()
        {
            SettingsManager.Instance.BgmVolume,
            SettingsManager.Instance.SfxVolume
        });
        _panel.SetLanguageDropdownInteractable(false);
        _panel.OnBGMVolumeChange += SetBgmVolume;
        _panel.OnSFXVolumeChange += SetSFxVolume;
        _panel.LanguageSelected += HandleLanguageSelected;
        _panel.OnBackClick += HandleBackClick;
        _panel.BackToMainClicked += HandleBackToMain;
        StartCoroutine(InitializeLanguageDropdown());
    }

    private void OnPanelShown()
    {
        _panel?.SetBackToMainVisable(GameManager.Instance.CurrentState != GameState.MainMenu);
    }

    private void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.OnBGMVolumeChange -= SetBgmVolume;
            _panel.OnSFXVolumeChange -= SetSFxVolume;
            _panel.LanguageSelected -= HandleLanguageSelected;
            _panel.OnBackClick -= HandleBackClick;
            _panel.BackToMainClicked -= HandleBackToMain;
        }
    }

    private void SetBgmVolume(float volume)
    {
        AudioManager.Instance.SetBGMVolume(volume);
        SettingsManager.Instance.SetBgmVolume(volume);
        _panel._bgmVolumeText.text = 100 * volume + "/100";
    }

    private void SetSFxVolume(float volume)
    {
        AudioManager.Instance.SetSoundVolume(volume);
        SettingsManager.Instance.SetSfxVolume(volume);
        _panel._sfxVolumeText.text = 100 * volume + "/100";
    }

    private IEnumerator InitializeLanguageDropdown()
    {
        yield return RefreshLanguageOptions();
        if (_panel != null)
        {
            _panel.SetLanguageDropdownInteractable(true);
        }
    }

    private void HandleLanguageSelected(int index)
    {
        string languageCode = index == 1
            ? SettingsManager.EnglishLanguageCode
            : SettingsManager.ChineseLanguageCode;
        StartCoroutine(ChangeLanguage(languageCode));
    }

    private IEnumerator ChangeLanguage(string languageCode)
    {
        if (_isChangingLanguage) yield break;

        _isChangingLanguage = true;
        _panel.SetLanguageDropdownInteractable(false);

        bool changed = false;
        yield return DataManager.Instance.SetLocaleAsync(
            languageCode,
            success => changed = success);

        if (changed)
        {
            SettingsManager.Instance.SetLanguageCode(languageCode);
        }

        yield return RefreshLanguageOptions();
        if (_panel != null)
        {
            _panel.SetLanguageDropdownInteractable(true);
        }
        _isChangingLanguage = false;
    }

    private IEnumerator RefreshLanguageOptions()
    {
        string chineseLabel = ChineseLanguageLabelKey;
        string englishLabel = EnglishLanguageLabelKey;
        yield return DataManager.Instance.GetLocalizedUiTextAsync(
            ChineseLanguageLabelKey,
            value => chineseLabel = value);
        yield return DataManager.Instance.GetLocalizedUiTextAsync(
            EnglishLanguageLabelKey,
            value => englishLabel = value);

        if (_panel != null)
        {
            _panel.SetLanguageOptions(
                chineseLabel,
                englishLabel,
                SettingsManager.Instance.LanguageCode);
        }
    }

    private void HandleBackClick()
    {
        if (UIManager.Instance.IsInitialized)
        {
            UIManager.Instance.HidePanel(_panelName);
        }
    }

    private void HandleBackToMain()
    {
        GameManager.Instance.LoadMainMenu();
        UIManager.Instance.HidePanel(_panelName);
    }
}
