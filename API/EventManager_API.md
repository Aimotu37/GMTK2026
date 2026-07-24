# EventManager API

## 功能

负责全局事件注册、注销、触发和清理，支持泛型事件与无参数事件。

## API

| 接口                                       | 功能                       | 调用方            |
| ------------------------------------------ | -------------------------- | ----------------- |
| InitializeAsync()                          | 异步初始化事件管理器       | 启动流程          |
| EventRegister<T>(string, UnityAction<T>)   | 注册带参数事件处理方法     | 业务逻辑/模块     |
| EventRegister(string, UnityAction)         | 注册无参数事件处理方法     | 业务逻辑/模块     |
| EventUnregister<T>(string, UnityAction<T>) | 取消注册带参数事件处理方法 | 业务逻辑/模块     |
| EventUnregister(string, UnityAction)       | 取消注册无参数事件处理方法 | 业务逻辑/模块     |
| EventTrigger<T>(string, T)                 | 触发带参数事件             | 业务逻辑/系统     |
| EventTrigger(string)                       | 触发无参数事件             | 业务逻辑/系统     |
| ClearEvents()                              | 清空所有已注册事件         | 场景切换/资源清理 |
| IsInitialized                              | 判断管理器是否已初始化     | 启动流程/系统检查 |

## 调用规范

- 所有事件行为必须通过 `EventManager` 进行注册、注销和触发。
- 事件名应保持唯一且一致，可参考 `GameEvents` 中预定义的事件名常量。
- 注册与注销必须成对使用，避免内存泄漏或重复调用。
- 触发事件前应确保对应事件名已注册，否则不执行任何操作。
- 禁止直接访问或修改内部 `eventDictionary`。
