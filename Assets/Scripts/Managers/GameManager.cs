using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


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

    //游戏变量
    int currentWords = 4;

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
        //1、局内交互锁定
        EventManager.Instance.EventTrigger(GameEvents.GameOver);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SwitchGameState(GameState.MainMenu);
        ScenesManager.Instance?.LoadSceneAsync("Scene_Main", null);
    }

    public bool CheckWord(string itemID)
    {
        if (currentWords <= 0)
        {
            return false;
        }

        currentWords--;
        print(currentWords);
        //UpdateWordUI();


        //string clue = WordMatcher.GetClueByItemID(itemID);
        string clue = "this is test";
        ShowClueAnimation(clue);
        return true;
    }

    private void ShowClueAnimation(string clue)
    {
        InputManager.Instance.SetInputEnabled(false);

        InteractionController.Instance.typewriter.StartTyping(clue, () =>
        {
            InputManager.Instance.SetInputEnabled(true);
        });
    }
}
