# UIManager API

## 功能

负责 UI 根节点初始化、面板加载、挂载、查询、关闭和释放管理。

## API

| 接口 | 功能 | 调用方 |
|-|-|-|
| InitializeAsync() | 异步初始化 UI 管理器，加载 EventSystem 和 UIRoot | 启动流程 |
| ShowPanel<T>(string, E_UILayer, UnityAction<T>, object, bool) | 打开或刷新指定面板，支持异步加载并回调 | 界面逻辑 |
| RegisterPanel(string, BasePanel, E_UILayer, bool, object) | 注册已存在面板实例并交由 UIManager 管理 | 界面逻辑 |
| HidePanel(string) | 关闭并释放指定面板 | 界面逻辑 |
| GetPanel<T>(string) | 获取已打开或已注册面板实例 | 界面逻辑 |
| GetPanelLayer(E_UILayer) | 获取指定 UI 层级的 Transform | 界面布局 |
| GetTopPanel() | 获取当前栈顶有效面板 | 界面逻辑 |
| CloseTopPanel() | 关闭当前栈顶可关闭面板 | 界面逻辑 |
| HandleBack() | 处理返回操作，默认等价于关闭栈顶面板 | 界面逻辑 |
| IsInitialized | 判断管理器是否已初始化 | 启动流程/系统检查 |

## 调用规范

- 所有 UI 面板的打开、关闭和查询应通过 `UIManager` 完成。
- 面板资源地址以 `ui/` 为前缀，`panelName` 与资源路径后缀对应。
- `ShowPanel` 会自动加载资源、初始化面板并挂载到指定层级。
- 若面板已打开，`ShowPanel` 会调用 `Refresh`、`Open` 并移动到栈顶。
- `RegisterPanel` 仅用于已存在实例，UIManager 不负责释放外部传入的面板实例。
- `HidePanel` 会释放由 UIManager 加载的面板实例；外部实例则销毁对象。
- 不要直接操作 `_panels`、`_panelStack` 或默认 UI 层级节点。
