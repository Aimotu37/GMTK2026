using UnityEngine;

/// <summary>
/// 挂在 Scene_Opening 场景中：进入场景后播放开场契约对话，播完后进入案件一
/// </summary>
public class OpeningSceneController : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.PlayOpeningStory(() =>
        {
            GameManager.Instance.StartNewGame();
        });
    }
}
