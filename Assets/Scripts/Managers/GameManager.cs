using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum GameState
{
    MainMenu,
    Playing,
    Pause,
    GameOver,
    GameVictory
}
public class GameManager : SingletonMono<GameManager>
{
    private GameState _currentState = GameState.MainMenu;
    public GameState CurrentState => _currentState;

    //初始化变量
    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    private bool _initFailed;

    public IEnumerator InitializeAsync()
    {
        if (_isInitialized)
        {
            yield break;
        }

        _initFailed = false;

        if (_initFailed)
        {
            _isInitialized = false;
            Debug.LogError(this.name + " Initialization Failed.");
        }
        else
        {
            _isInitialized = true;
            Debug.Log(this.name + " Initialization Successful.");
        }
    }

    public void SwitchGameState(GameState targetState)
    {
        if (_currentState == targetState) return;
        _currentState = targetState;

        switch (targetState)
        {
            case GameState.MainMenu:
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Pause:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                break;
            case GameState.GameVictory:
                break;
        }

        if (InputManager.Instance != null)
        {
            bool enableInput = targetState == GameState.Playing;
            InputManager.Instance.SetInputEnabled(enableInput);
        }
    }

    // 5. 流程控制快捷方法（外部直接调用，不用记复杂API）
    public void StartNewGame()
    {
        ScenesManager.Instance?.LoadSceneAsync("Scene_Level_1", null);
        SwitchGameState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (_currentState == GameState.Playing)
        {
            SwitchGameState(GameState.Pause);
        }
        else if (_currentState == GameState.Pause)
        {
            SwitchGameState(GameState.Playing);
        }
    }

    public void GameOver()
    {
        SwitchGameState(GameState.GameOver);
        // 延迟2秒后显示结算UI（或者直接通过事件驱动）
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f; 
        SwitchGameState(GameState.MainMenu);
        ScenesManager.Instance?.LoadSceneAsync("Scene_Main", null);
    }
}
