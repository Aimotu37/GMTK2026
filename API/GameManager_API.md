# GameManager API

## 功能

负责游戏状态管理、主流程控制、场景切换入口和部分游戏行为逻辑。

## API

| 接口                       | 功能                         | 调用方            |
| -------------------------- | ---------------------------- | ----------------- |
| InitializeAsync()          | 异步初始化游戏管理器         | 启动流程          |
| SwitchGameState(GameState) | 切换游戏状态并同步输入控制   | 游戏流程/系统     |
| StartNewGame()             | 启动新游戏，加载互动测试场景 | 游戏流程/UI       |
| PauseGame()                | 切换游戏暂停与继续状态       | UI/系统           |
| GameOver()                 | 结束游戏并触发游戏结束事件   | 游戏流程          |
| QuitGame()                 | 退出游戏或停止编辑模式       | 游戏流程/测试     |
| BackToMenu()               | 返回主菜单并切换菜单场景     | UI/系统           |
| CheckWord(string)          | 检查单词条件并显示线索动画   | 交互逻辑          |
| CurrentState               | 获取当前游戏状态             | 系统/UI           |
| IsInitialized              | 判断管理器是否已初始化       | 启动流程/系统检查 |

## 调用规范

- 游戏流程切换应通过 `SwitchGameState`、`PauseGame`、`GameOver`、`StartNewGame` 等接口完成。
- 不要直接修改 `_currentState`，应使用公开方法统一处理状态切换逻辑。
- `CheckWord` 会消耗内部词数并触发提示动画，只有在游戏玩法逻辑中使用。
- 禁止直接操作 `Time.timeScale` 或输入使能逻辑，应通过 `GameManager` 和 `InputManager` 间接控制。
