# InputManager API

## 功能

封装输入读取逻辑，支持按键、轴和鼠标输入，并根据输入使能状态返回结果。

## API

| 接口                  | 功能                   | 调用方            |
| --------------------- | ---------------------- | ----------------- |
| InitializeAsync()     | 异步初始化输入管理器   | 启动流程          |
| SetInputEnabled(bool) | 启用或禁用输入读取     | 游戏流程/系统     |
| GetKeyDown(KeyCode)   | 检测按键按下           | 玩法逻辑          |
| GetKey(KeyCode)       | 检测按键按住           | 玩法逻辑          |
| GetKeyUp(KeyCode)     | 检测按键抬起           | 玩法逻辑          |
| GetAxis(string)       | 获取模拟轴输入值       | 玩法逻辑          |
| GetAxisRaw(string)    | 获取原始模拟轴输入值   | 玩法逻辑          |
| GetMousePosition()    | 获取鼠标坐标           | UI/交互           |
| GetMouseDown(int)     | 检测鼠标按下           | 玩法逻辑          |
| GetMouse(int)         | 检测鼠标按住           | 玩法逻辑          |
| GetMouseUp(int)       | 检测鼠标抬起           | 玩法逻辑          |
| IsInitialized         | 判断管理器是否已初始化 | 启动流程/系统检查 |

## 调用规范

- 所有输入读取应通过 `InputManager` 的封装接口完成。
- 当输入禁用时，所有按键和鼠标查询会返回 false，轴查询返回 0，鼠标位置返回 `Vector2.zero`。
- 不要直接调用 `UnityEngine.Input`，避免绕过输入开关控制。
- `SetInputEnabled` 应由游戏状态切换逻辑统一管理。
