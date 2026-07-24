# SaveManager API

## 功能

负责游戏存档的收集、写入、读取与删除；协调所有可保存对象的保存与加载行为。

## 相关文件

- `Assets/Scripts/Save/SaveManager.cs`：保存管理器核心实现。
- `Assets/Scripts/Save/ISaveable.cs`：保存接口，所有可保存对象必须实现。
- `Assets/Scripts/Save/SavedFileData.cs`：存档文件结构和可序列化保存数据类型。

## 相关类型

- `ISaveable`：可保存对象接口。
  - `SavedId`：唯一保存标识符，用于在存档字典中索引对应数据。
  - `RegisterSaveable()` / `UnregisterSaveable()`：自动将对象注册或注销到 `SaveManager`。
  - `SaveData()`：返回实现 `ISavedData` 的保存数据对象。
  - `LoadData(ISavedData)`：根据读取到的保存数据恢复对象状态。
- `SavedFileData`：存档文件顶层结构。
  - `saveVersion`：存档版本号，用于兼容性校验。
  - `savedAtUtc`：存档时间的 UTC 字符串。
  - `savedDataDictionary`：按 `SavedId` 存储的保存数据字典。
- `ISavedData`：保存数据标记接口。
- `GameProgressSavedData` / `AudioSavedData`：示例保存数据类型。

## API

| 接口                          | 功能                                         | 调用方            |
| ----------------------------- | -------------------------------------------- | ----------------- |
| InitializeAsync()             | 异步初始化保存管理器                         | 启动流程          |
| RegisterSaveable(ISaveable)   | 注册可保存对象                               | 可保存对象自身    |
| UnregisterSaveable(ISaveable) | 注销可保存对象                               | 可保存对象自身    |
| SaveGameData()                | 创建存档并写入 `persistentDataPath/save.sav` | 保存入口/UI/系统  |
| LoadGameData()                | 读取存档并分发给已注册对象加载数据           | 开始/继续游戏     |
| HasGameSavedData()            | 检查是否存在存档文件                         | UI/继续功能       |
| DeleteGameData()              | 删除存档文件和临时存档                       | 清除存档/重置     |
| IsInitialized                 | 判断管理器是否已初始化                       | 启动流程/系统检查 |

## 存档文件

- 目标存档路径：`Application.persistentDataPath/save.sav`
- 临时存档路径：`Application.persistentDataPath/temp.sav`
- 写入流程：先写入临时文件，再替换或移动到正式文件，降低写入异常风险。
- 格式：JSON 序列化，支持 `TypeNameHandling.Auto`，可存储多种具体 `ISavedData` 类型。

## 调用规范

- 可保存对象必须实现 `ISaveable`，并在适当时机调用 `RegisterSaveable()` 注册到 `SaveManager`。
- `SavedId` 应保持唯一，避免不同对象共享同一保存键。
- 读取存档前应确保目标对象已注册，否则其数据不会被恢复。
- `SaveGameData()` 会遍历当前已注册的所有 `ISaveable` 并保存其 `SaveData()`，`SaveData()` 返回 null 的对象会被忽略。
- `LoadGameData()` 会尝试为每个已注册对象加载对应数据；未找到对应键的对象不会触发 `LoadData`。
- 删除存档时会同时清除正式存档和临时存档。
- 不要直接操作保存文件路径或绕过 `SaveManager` 进行写入 / 读取。
- 如果需要新增保存数据类型，请实现 `ISavedData` 并确保作用对象的 `SaveData()` / `LoadData()` 正确序列化。
