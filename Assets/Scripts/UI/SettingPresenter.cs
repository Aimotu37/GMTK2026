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
            AudioManager.Instance.BgmVolume,
            AudioManager.Instance.SfxVolume
        });
        _panel.OnBGMVolumeChange += SetBgmVolume;
        _panel.OnSFXVolumeChange += SetSFxVolume;
        _panel.SaveGameClick += HandleSaveGame;
        _panel.OnAboutUsClick += HandleAboutUs;
        _panel.OnBackClick += HandleBackClick;
    }

    private void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.OnBGMVolumeChange -= SetBgmVolume;
            _panel.OnSFXVolumeChange -= SetSFxVolume;
            _panel.SaveGameClick -= HandleSaveGame;
            _panel.OnAboutUsClick -= HandleAboutUs;
            _panel.OnBackClick -= HandleBackClick;
        }

    }

    private void SetBgmVolume(float volume)
    {
        AudioManager.Instance.SetBGMVolume(volume);
        _panel._bgmVolumeText.text = 100 * volume + "/100";
    }

    private void SetSFxVolume(float volume)
    {
        AudioManager.Instance.SetSoundVolume(volume);
        _panel._sfxVolumeText.text = 100 * volume + "/100";
    }

    private void HandleSaveGame()
    {
        SaveManager.Instance.SaveGameData();
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
}
