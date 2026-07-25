using UnityEngine;

/// <summary>
/// 调试/测试专用脚本：随便挂在 Scene_Persistent 里任意一个物体上（比如新建一个空物体叫 DebugTestHelper）。
/// 运行时按功能键快速跳转到各个流程节点，方便测试，不参与正式游戏逻辑，正式发布前可以直接把这个物体删掉/禁用。
///
/// 快捷键：
/// F1  跳过主菜单+开场剧情，直接开始新游戏（案件一）
/// F2  直接跳到案件二（不用先打案件一）
/// F3  直接跳到案件三（不用先打案件一二）
/// F4  无视概率，强制触发一次诅咒
/// F5  强制判定当前案件死亡
/// F6  强制判定当前案件成功
/// F7  强制跳到真结局（需要已经进过至少一个案件场景）
/// F8  在 Console 打印当前流程状态
/// </summary>
public class DebugTestHelper : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[测试] F1 跳过开场剧情，直接进案件一");
            GameManager.Instance.StartNewGame();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[测试] F2 跳到案件二");
            GameManager.Instance.DebugJumpToCase(1002);
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("[测试] F3 跳到案件三");
            GameManager.Instance.DebugJumpToCase(1003);
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("[测试] F4 强制触发一次诅咒");
            GameManager.Instance.DebugForceTriggerDebuff();
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("[测试] F5 强制判定死亡");
            GameManager.Instance.DebugForceDeath();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.Log("[测试] F6 强制判定成功");
            GameManager.Instance.DebugForceSuccess();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            Debug.Log("[测试] F7 强制跳到真结局");
            GameManager.Instance.DebugForceTrueEnd();
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            var flow = FlowController.Instance;
            Debug.Log($"[测试] 当前流程状态: {(flow != null ? flow.CurrentState.ToString() : "FlowController 还没创建")}");
        }
    }
}
