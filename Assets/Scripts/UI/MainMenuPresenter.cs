using UnityEngine;

[DisallowMultipleComponent]
public sealed class MainMenuPresenter : MonoBehaviour
{
    private MainMenuPanel _panel;
    [SerializeField] private string _panelName = "main_menu_panel";
    [SerializeField] private E_UILayer _panelLayer = E_UILayer.MiddleLayer;

    private void Start()
    {
        _panel = GetComponent<MainMenuPanel>();
        RegisterEvents();
    }

    private void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.NewGameClicked -= HandleNewGameClicked;
            _panel.ContinueClicked -= HandleContinueClicked;
            _panel.SettingsClicked -= HandleSettingsClicked;
            _panel.QuitClicked -= HandleQuitClicked;
        }
    }

    private void RegisterEvents()
    {
        if (_panel == null)
        {
            Debug.LogError($"MainMenuPresenter failed to show panel: {_panelName}");
            return;
        }
        _panel.NewGameClicked += HandleNewGameClicked;
        _panel.ContinueClicked += HandleContinueClicked;
        _panel.SettingsClicked += HandleSettingsClicked;
        _panel.QuitClicked += HandleQuitClicked;
    }

    private void HandleNewGameClicked()
    {
        // 切到独立的开场剧情场景（恶魔契约对话），播完由该场景负责进入案件一
        UIManager.Instance.HidePanel(_panelName);
        ScenesManager.Instance.LoadSceneAsync(Scenes.OpeningSceneName);
    }

    private void HandleContinueClicked()
    {
        if (!SaveManager.Instance.LoadGameData())
        {
            print("Load SavedData Faild");
            return;
        }

        //GameManager.Instance.ContinueGame();
    }

    private void HandleSettingsClicked()
    {
        UIManager.Instance.ShowPanel<SettingPanel>("main_setting_panel");
    }

    private void HandleQuitClicked()
    {
        GameManager.Instance.QuitGame();
    }
}
