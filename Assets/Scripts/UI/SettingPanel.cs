using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SettingPanel : BasePanel
{
    private const string SaveGameButtonName = "SaveGameButton";
    private const string AboutUsButtonName = "AboutUsButton";
    private const string BackButtonName = "BackButton";

    private const string BgmVSliderName = "BgmVolumeSlider";
    private const string SfxVSliderName = "SfxVolumeSlider";

    public event Action<float> OnBGMVolumeChange;
    public event Action<float> OnSFXVolumeChange;
    public event Action SaveGameClick;
    public event Action OnAboutUsClick;
    public event Action OnBackClick;

    [SerializeField] private Slider _bgmVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    public Text _bgmVolumeText;
    public Text _sfxVolumeText;

    protected override void OnButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case SaveGameButtonName:
                SaveGameClick?.Invoke();
                break;
            case AboutUsButtonName:
                OnAboutUsClick?.Invoke();
                break;
            case BackButtonName:
                OnBackClick?.Invoke();
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
}
