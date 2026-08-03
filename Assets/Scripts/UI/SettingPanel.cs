using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    private const string BackButtonName = "BackButton";
    private const string BackToMainButtonName = "BackToMainButton";

    private const string BgmVSliderName = "BgmVolumeSlider";
    private const string SfxVSliderName = "SfxVolumeSlider";

    public event Action<float> OnBGMVolumeChange;
    public event Action<float> OnSFXVolumeChange;
    public event Action BackToMainClicked;
    public event Action<int> LanguageSelected;
    public event Action OnBackClick;

    [SerializeField] private Slider _bgmVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Dropdown _languageDropdown;
    public Text _bgmVolumeText;
    public Text _sfxVolumeText;

    protected override void Awake()
    {
        base.Awake();
        if (_languageDropdown == null)
        {
            Debug.LogError("SettingPanel requires a configured LanguageDropdown.", this);
            return;
        }

        _languageDropdown.onValueChanged.AddListener(HandleLanguageSelected);
    }

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case BackButtonName:
                OnBackClick?.Invoke();
                break;
            case BackToMainButtonName:
                BackToMainClicked?.Invoke();
                break;
        }
    }

    protected override void OnSliderValueChange(string sliderName)
    {
        float volume = 0f;
        switch (sliderName)
        {
            case BgmVSliderName:
                volume = (float)Math.Round(_bgmVolumeSlider.value, 2, MidpointRounding.AwayFromZero);
                _bgmVolumeText.text = 100 * volume + "/100";
                OnBGMVolumeChange?.Invoke(volume);
                break;
            case SfxVSliderName:
                volume = (float)Math.Round(_sfxVolumeSlider.value, 2, MidpointRounding.AwayFromZero);
                _sfxVolumeText.text = 100 * volume + "/100";
                OnSFXVolumeChange?.Invoke(volume);
                break;
        }
    }

    public void SetVolume(List<float> data)
    {
        _bgmVolumeSlider.value = data[0];
        _bgmVolumeText.text = 100 * data[0] + "/100";
        _sfxVolumeSlider.value = data[1];
        _sfxVolumeText.text = 100 * data[1] + "/100";
    }

    public void SetBackToMainVisable(bool isVisable)
    {
        FindComponent<Button>(BackToMainButtonName).gameObject.SetActive(isVisable);
        _languageDropdown.gameObject.SetActive(!isVisable);
    }

    public void SetLanguageOptions(
        string chineseLabel,
        string englishLabel,
        string languageCode)
    {
        if (_languageDropdown == null) return;

        _languageDropdown.ClearOptions();
        _languageDropdown.AddOptions(new List<string>
        {
            chineseLabel,
            englishLabel
        });
        _languageDropdown.SetValueWithoutNotify(
            languageCode == SettingsManager.EnglishLanguageCode ? 1 : 0);
        _languageDropdown.RefreshShownValue();
    }

    public void SetLanguageDropdownInteractable(bool interactable)
    {
        if (_languageDropdown != null)
        {
            _languageDropdown.interactable = interactable;
        }
    }

    private void OnDestroy()
    {
        if (_languageDropdown != null)
        {
            _languageDropdown.onValueChanged.RemoveListener(HandleLanguageSelected);
        }
    }

    private void HandleLanguageSelected(int index)
    {
        LanguageSelected?.Invoke(index);
    }
}
