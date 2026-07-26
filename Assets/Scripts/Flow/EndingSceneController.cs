using UnityEngine;

/// <summary>
/// Scene_Ending 入口：播放随机结局台词，播放结束后返回主菜单。
/// </summary>
public sealed class EndingSceneController : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.PlayEndingStory(GameManager.Instance.LoadMainMenu);
    }
}
