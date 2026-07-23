using UnityEngine;
using System;


public class CounterManager : MonoBehaviour
{

    public static CounterManager Instance { get; private set; }


    [Header("初始调查次数")]
    [SerializeField]
    private int initialCount = 10;



    // 当前剩余次数
    private int currentCount;



    /// <summary>
    /// 获取当前调查次数
    /// </summary>
    public int CurrentCount
    {
        get
        {
            return currentCount;
        }
    }



    /*
     * 次数变化事件
     *
     * 参数：
     * 当前剩余次数
     *
     * 用途：
     * UI刷新
     * 阶段事件检测
     */
    public event Action<int> OnCountChanged;



    /*
     * 次数归零事件
     *
     * 用途：
     * 玩家死亡
     * 游戏失败
     */
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

        currentCount = initialCount;


        NotifyCountChanged();

    }





    /// <summary>
    /// 消耗调查次数
    ///
    /// 示例：
    /// 调查一个物品消耗1次
    ///
    /// ConsumeCount(1)
    /// </summary>
    public void ConsumeCount(int amount)
    {

        if (amount <= 0)
        {
            Debug.LogWarning(
                "ConsumeCount参数必须大于0"
            );

            return;
        }



        currentCount -= amount;



        if (currentCount < 0)
        {
            currentCount = 0;
        }



        NotifyCountChanged();



        CheckZero();

    }





    /// <summary>
    /// 增加调查次数
    ///
    /// 示例：
    /// 道具恢复次数
    ///
    /// AddCount(2)
    /// </summary>
    public void AddCount(int amount)
    {

        if (amount <= 0)
        {
            Debug.LogWarning(
                "AddCount参数必须大于0"
            );

            return;
        }



        currentCount += amount;



        NotifyCountChanged();

    }





    /// <summary>
    /// 强制设置当前次数
    ///
    /// 用途：
    /// 测试
    /// 特殊剧情
    /// </summary>
    public void SetCount(int value)
    {

        currentCount = Mathf.Max(value, 0);



        NotifyCountChanged();



        CheckZero();

    }





    /// <summary>
    /// 获取当前次数是否耗尽
    /// </summary>
    public bool IsEmpty()
    {

        return currentCount <= 0;

    }





    private void CheckZero()
    {

        if (currentCount <= 0)
        {

            OnCountZero?.Invoke();

        }

    }





    private void NotifyCountChanged()
    {

        OnCountChanged?.Invoke(currentCount);

    }


}