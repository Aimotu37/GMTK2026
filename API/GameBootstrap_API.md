# GameBootstrap API

## 功能

负责游戏启动流程的初始化与启动管线控制，管理启动进度、启动状态、失败信息以及主菜单场景加载。

## 相关文件

- `Assets/Scripts/GameStartup/GameBootstrap.cs`：游戏启动器核心实现。
- `Assets/Scripts/GameStartup/BootstrapLoadingView.cs`：启动页面显示与进度反馈。
- `Assets/Scripts/GameStartup/GameStartPipeline.cs`：启动模块管线定义与执行逻辑。
- `Assets/Scripts/GameStartup/CoreServicesStartModule.cs`：核心服务启动模块。
- `Assets/Scripts/GameStartup/RuntimeServicesStartModule.cs`：运行时服务启动模块。
- `Assets/Scripts/GameStartup/UIStartModule.cs`：UI 启动模块。

## 相关类型

- `BootstrapState`：启动器状态枚举。
  - `None`：未初始化。
  - `Initializing`：正在启动。
  - `Ready`：启动完成。
  - `Failed`：启动失败。
- `BootstrapLoadingView`：启动界面组件，负责显示初始化、加载和失败状态。
- `GameStartPipeline`：启动模块执行管线，按顺序运行多个 `IGameStartModule`。
- `GameInitializationFailure`：启动失败信息结构，包含失败系统名和失败原因。
- `GameManager` / `ScenesManager`：启动完成后用于初始化游戏管理和加载主菜单场景。

## API

| 接口              | 功能                                                                   | 调用方        |
| ----------------- | ---------------------------------------------------------------------- | ------------- |
| RunStartup()      | 运行游戏启动流程，按顺序初始化核心、运行时和 UI 服务，并加载主菜单场景 | 启动流程      |
| State             | 获取当前启动状态                                                       | 系统/调试     |
| CurrentModuleName | 获取当前正在执行的启动模块名称                                         | 启动流程/调试 |
| Progress          | 获取当前启动进度                                                       | 启动流程/界面 |
| Failure           | 获取启动失败信息                                                       | 启动流程/调试 |

## 调用规范

- 游戏启动应通过 `GameBootstrap` 的 `RunStartup()` 协程执行，避免在其他位置重复执行启动逻辑。
- `RunStartup()` 在 `State` 为 `Ready` 或 `Initializing` 时会直接返回，不会重复启动。
- 启动流程异常会将 `State` 置为 `Failed`，并通过 `Failure` 提供失败信息。
- 成功完成后会初始化 `GameManager`，并加载主菜单场景 `Scenes.MainMenuSceneName`。
- `GameBootstrap` 依赖 `BootstrapLoadingView`，若 `loadingView` 未配置会直接失败。
- 不要直接修改 `State`、`Progress`、`CurrentModuleName` 或 `Failure`，应通过启动流程内部机制读取。
- 启动流程中的模块顺序由 `CreatePipeline()` 定义，新增启动模块应在该方法中注册。
