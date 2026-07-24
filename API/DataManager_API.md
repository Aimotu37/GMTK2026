# DataManager API

## 功能

负责配置数据的缓存与读取，基于 Resources 加载 ScriptableObject 配置资源。

## API

| 接口                 | 功能                                             | 调用方            |
| -------------------- | ------------------------------------------------ | ----------------- |
| InitializeAsync()    | 异步初始化数据管理器                             | 启动流程          |
| GetConfig<T>(string) | 获取指定键的配置对象，返回 ScriptableObject 类型 | 业务逻辑/系统     |
| IsInitialized        | 判断管理器是否已初始化                           | 启动流程/系统检查 |

## 调用规范

- 所有配置数据读取必须通过 `DataManager.GetConfig<T>(key)`。
- 配置资源应放在 `Resources/Configs/` 下，key 对应资源文件名。
- 不要直接使用 `Resources.Load` 或绕过缓存字典。
- 不要修改 `DataManager` 内部缓存 `_configCache`。
