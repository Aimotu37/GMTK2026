using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Events;

//基于里氏转换原则封装事件对象
public interface IEventInfo { }

public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> eventAction;

    public EventInfo(UnityAction<T> action)
    {
        eventAction = action;
    }
}

public class EventInfo : IEventInfo
{
    public UnityAction eventAction;

    public EventInfo(UnityAction action)
    {
        eventAction = action;
    }
}

class GameEvents
{
    //游戏外事件
    public const string NewGameStart = "OnNewGameStart";
    public const string GamePause = "OnGamePause";
    public const string GameOver = "OnGameOver";
    public const string GameStateChange = "OnStateChange";
    public const string GameSceneSwitch = "OnSceneSwitch";
    //游戏内事件
    public const string DropItemOnZone = "OnItemOnZone";
    public const string CountChanged = "OnCountChanged";
    public const string CheckCaseClues = "OnCheckCaseClues";
    public const string DemonSpeak = "OnDemonSpeak";
    public const string DebuffEffect = "OnDebuffEffect";
}

/// <summary>
/// 事件中心管理器
/// 负责事件的注册、注销和触发
/// </summary>
public class EventManager : Singleton<EventManager>
{
    //事件字典：使用<string, UnityAction>来存储事件名和对应的事件方法
    private Dictionary<string, IEventInfo> eventDictionary = new Dictionary<string, IEventInfo>();

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
        }
        else
        {
            _isInitialized = true;
        }
    }

    /// <summary>
    /// 事件注册函数方法
    /// 为由eventName指定的事件注册一个方法
    /// </summary>
    /// <param name="eventName">事件名Key</param>
    /// <param name="action">需要添加的事件</param>
    public void EventRegister<T>(string eventName, UnityAction<T> action)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            (eventDictionary[eventName] as EventInfo<T>).eventAction += action;
        }
        else
        {
            eventDictionary.Add(eventName, new EventInfo<T>(action));
        }
    }

    public void EventRegister(string eventName, UnityAction action)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            (eventDictionary[eventName] as EventInfo).eventAction += action;
        }
        else
        {
            eventDictionary.Add(eventName, new EventInfo(action));
        }
    }

    /// <summary>
    /// 事件移除注册函数方法
    /// 为由eventName指定的事件移除一个已经注册的方法
    /// </summary>
    /// <param name="eventName">事件名Key</param>
    /// <param name="action">需要添加的事件</param>
    public void EventUnregister<T>(string eventName, UnityAction<T> action)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            (eventDictionary[eventName] as EventInfo<T>).eventAction -= action;
        }
    }

    public void EventUnregister(string eventName, UnityAction action)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            (eventDictionary[eventName] as EventInfo).eventAction -= action;
        }
    }
    /// <summary>
    /// 事件触发方法
    /// 用于触发由eventName指定的事件
    /// 如果事件名不存在，则不执行任何操作
    /// </summary>
    /// <param name="eventName">事件名Key</param>
    /// <param name="info">传递给事件处理方法的信息</param>
    public void EventTrigger<T>(string eventName, T info)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            (eventDictionary[eventName] as EventInfo<T>).eventAction.Invoke(info);
        }
    }

    public void EventTrigger(string eventName)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            (eventDictionary[eventName] as EventInfo).eventAction.Invoke();
        }
    }

    public void ClearEvents()
    {
        eventDictionary.Clear();
    }
}
