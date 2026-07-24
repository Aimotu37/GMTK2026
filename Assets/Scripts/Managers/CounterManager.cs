using UnityEngine;
using System;


public class CounterManager : MonoBehaviour
{

    public static CounterManager Instance { get; private set; }

    [Header("初始调查次数")]
    [SerializeField]
    private int initialCount = 10;
    private int _currentCount;
    public int CurrentCount => _currentCount;

    public event Action<int> OnCountChanged;
    public event Action OnCountZero;

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {

        _currentCount = initialCount;
        NotifyCountChanged();
    }

    public void ConsumeCount(int amount)
    {

        if (amount <= 0)
        {
            Debug.LogWarning(
                "ConsumeCount参数必须大于0"
            );

            return;
        }
        _currentCount -= amount;

        if (_currentCount < 0)
        {
            _currentCount = 0;
        }
        NotifyCountChanged();
        CheckZero();
    }

    public void AddCount(int amount)
    {

        if (amount <= 0)
        {
            Debug.LogWarning(
                "AddCount参数必须大于0"
            );

            return;
        }
        _currentCount += amount;
        NotifyCountChanged();
    }

    public void SetCount(int value)
    {
        _currentCount = Mathf.Max(value, 0);
        NotifyCountChanged();
        CheckZero();
    }

    public bool IsEmpty()
    {
        return _currentCount <= 0;
    }

    private void CheckZero()
    {

        if (_currentCount <= 0)
        {
            OnCountZero?.Invoke();
        }
    }

    private void NotifyCountChanged()
    {
        OnCountChanged?.Invoke(_currentCount);
    }
}