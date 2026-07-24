# ScenesManager API

## 功能

负责游戏场景的切换、加载、卸载与切换状态管理。

## API

| 接口 | 功能 | 调用方 |
|-|-|-|
| InitializeAsync() | 异步初始化场景管理器 | 启动流程 |
| SwitchGameScene(string, UnityAction<bool>) | 以 Additive 模式切换游戏内容场景，完成后回调结果 | 场景管理/流程 |
| SwitchScene(string, UnityAction) | 切换场景并在加载完成后执行回调 | 场景管理/流程 |
| LoadSceneAsync(string, UnityAction) | 异步加载场景，成功后执行回调 | 场景管理/流程 |
| IsSwitching | 获取当前场景切换状态 | 系统检查 |
| CurrentContentSceneName | 获取当前加载的内容场景名称 | 系统/流程 |
| IsInitialized | 判断管理器是否已初始化 | 启动流程/系统检查 |

## 调用规范

- 场景切换应优先使用 `SwitchGameScene` 或 `LoadSceneAsync`，避免直接调用 `SceneManager.LoadSceneAsync`。
- `sceneName` 不能为空且不得为 `Scene_Persistent`。
- 如果正在切换场景，重复调用会立即回调失败，需通过回调判断切换结果。
- `SwitchScene` 仅对目标场景执行加载并调用回调，不保证内容场景切换行为。
- 不要直接访问 `_currentGameSceneName` 或 `_isSwitching`。
