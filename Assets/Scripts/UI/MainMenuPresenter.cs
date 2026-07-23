using UnityEngine;

[DisallowMultipleComponent]
public sealed class MainMenuPresenter : MonoBehaviour
{
    [SerializeField] private string _panelName = "main_menu_panel";
    [SerializeField] private E_UILayer _panelLayer = E_UILayer.MiddleLayer;

    private MainMenuPanel _panel;

    private void Start()
    {
        if (!UIManager.Instance.IsInitialized)
        {
            Debug.LogError("MainMenuPresenter requires UIManager to be initialized before entering MainMenu.");
            return;
        }

        UIManager.Instance.ShowPanel<MainMenuPanel>(
            _panelName,
            _panelLayer,
            OnPanelShown,
            closeOnBack: false);
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

        if (UIManager.Instance.IsInitialized)
        {
            UIManager.Instance.HidePanel(_panelName);
        }
    }

    private void OnPanelShown(MainMenuPanel panel)
    {
        if (panel == null)
        {
            Debug.LogError($"MainMenuPresenter failed to show panel: {_panelName}");
            return;
        }

        _panel = panel;
        _panel.NewGameClicked += HandleNewGameClicked;
        _panel.SetContinueBTNInteractble(SaveManager.Instance.HasGameSavedData());
        _panel.ContinueClicked += HandleContinueClicked;
        _panel.SettingsClicked += HandleSettingsClicked;
        _panel.QuitClicked += HandleQuitClicked;
    }

    private void HandleNewGameClicked()
    {
        GameManager.Instance.StartNewGame();
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
        //settingPresenter?.OpenPanel();
    }

    private void HandleQuitClicked()
    {
        GameManager.Instance.QuitGame();
    }
}
