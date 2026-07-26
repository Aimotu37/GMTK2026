using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SettingPresenter : MonoBehaviour
{
    private SettingPanel _panel;

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
        _panel.OnBGMVolumeChange += SetBgmVolume;
        _panel.OnSFXVolumeChange += SetSFxVolume;
        _panel.OnAboutUsClick += HandleAboutUs;
        _panel.OnBackClick += HandleBackClick;
        _panel.BackToMainClicked += HandleBackToMain;
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
            _panel.OnAboutUsClick -= HandleAboutUs;
            _panel.OnBackClick -= HandleBackClick;
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

    //TODO:待处理关于我们
    private void HandleAboutUs()
    {

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
