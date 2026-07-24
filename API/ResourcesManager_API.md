# ResourcesManager API

## 功能

提供统一的资源加载和释放接口，支持 Unity `Resources` 与 Addressables 两种方案。

## API

| 接口 | 功能 | 调用方 |
|-|-|-|
| InitializeAsync() | 异步初始化资源管理器 | 启动流程 |
| ResourceLoad<T>(string) | 同步加载资源；如果是 GameObject 会自动实例化 | 系统/模块 |
| ResourceLoadAsync<T>(string, UnityAction<T>) | 异步加载资源；如果是 GameObject 会自动实例化 | 系统/模块 |
| AddressablesLoad<T>(string) | 同步加载 Addressables 资源；`GameObject` 会实例化（已过时） | 系统/模块 |
| AddressablesLoadAsync<T>(string, UnityAction<T>) | 异步加载 Addressables 资源；`GameObject` 会实例化 | 系统/模块 |
| ReleaseResource(Object) | 释放 Resources 实例资源 | 资源清理 |
| ReleaseAddressable(Object) | 释放 Addressables 资源或实例 | 资源清理 |
| UnloadUnusedAssets() | 卸载未使用的 Resources 资源 | 资源清理 |
| IsInitialized | 判断管理器是否已初始化 | 启动流程/系统检查 |

## 调用规范

- 统一通过 `ResourcesManager` 进行资源加载和释放。
- `ResourceLoad` / `ResourceLoadAsync` 负责 `Resources` 路径加载，`AddressablesLoadAsync` 负责 Addressables 地址加载。
- 资源路径和地址不得为空，否则接口会输出错误并返回 null。
- `ReleaseResource` 仅销毁实例化的 `GameObject`，非 `GameObject` 资源由 `Resources.UnloadUnusedAssets` 清理。
- `ReleaseAddressable` 会根据资源类型自动调用 `Addressables.Release` 或 `Addressables.ReleaseInstance`。
- 不要直接使用 `Resources.Load`、`Addressables.LoadAssetAsync` 或 `Addressables.InstantiateAsync`。
