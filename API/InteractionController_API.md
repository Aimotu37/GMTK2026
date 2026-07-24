# InteractionController API

## 功能

管理玩家对场景中可交互物体的拖拽交互，检测是否落入发声槽，并根据交互结果触发接受或拒绝逻辑。

## 相关文件

- `Assets/Scripts/Interaction/InteractionController.cs`：交互控制器核心实现。
- `Assets/Scripts/Interaction/IInteractive.cs`：交互对象接口定义。
- `Assets/Scripts/Interaction/Item.cs`：实现 `IInteractive` 的可拖拽物体。

## 相关类型

- `IInteractive`：交互对象接口。
  - `ItemID`：对象唯一标识，`InteractionController` 使用该 ID 识别对象。
  - `IsInteractable`：是否允许拖拽与交互。
  - `IsDragging`：当前是否处于拖拽状态。
  - `OnDragStart(Vector3)` / `OnDragUpdate(Vector3)` / `OnDragEnd(bool)`：拖拽生命周期方法。
  - `GetCollider()`：返回用于场景碰撞检测的 `Collider2D`。
- `Item`：实现 `IInteractive` 的交互物体。
  - 负责拖拽缩放、拖拽开始、拖拽更新、拖拽结束后的吸附或回退行为。
  - `OnDragEnd(bool isAccepted)`：接受时吸附并销毁；拒绝时回到原位。

## API

| 接口                | 功能                                   | 调用方            |
| ------------------- | -------------------------------------- | ----------------- |
| Instance            | 获取单例实例                           | 游戏逻辑/系统     |
| Update()            | 每帧处理鼠标按下、拖拽、抬起事件       | 系统              |
| OnMouseDownOnItem() | 处理鼠标按下并开始拖拽可交互物体       | 交互逻辑          |
| OnMouseDragOnItem() | 处理物体拖拽更新位置                   | 交互逻辑          |
| OnMouseUpOnItem()   | 处理鼠标抬起，完成或取消拖拽并检测落区 | 交互逻辑          |
| AcceptCurrentDrop() | 接受当前拖拽物体的投放                 | 交互逻辑          |
| RejectCurrentDrop() | 拒绝当前拖拽物体的投放                 | 交互逻辑          |
| ResetController()   | 重置当前交互状态，取消未完成拖拽       | 场景重置/失败恢复 |
| DropZonePosition    | 获取发声槽位置                         | UI/效果           |
| OnItemDroppedToZone | 投放到槽区时的事件回调                 | 交互逻辑          |

## 调用规范

- 交互操作应通过 `InteractionController.Instance` 获取单例对象。
- 鼠标拖拽流程由 `OnMouseDownOnItem`、`OnMouseDragOnItem`、`OnMouseUpOnItem` 组成，通常由 `Update` 自动驱动，不要直接绕过这些方法。
- `AcceptCurrentDrop` 与 `RejectCurrentDrop` 仅在当前存在 `currentItem` 时生效。
- `ResetController` 用于重置异常交互状态，避免遗留拖拽对象。
- `DropZonePosition` 仅用于读取发声槽位置，不应直接修改。
- 交互判定依赖 `dropZoneCollider` 与 `Item` 的碰撞区域，确保相关组件已正确配置。
- 仅当射线检测到 `IInteractive` 对象且其 `IsInteractable` 为 true 时，才会开始拖拽。
- 不要直接修改 `currentItem`、`isDragging` 或内部拖拽偏移逻辑。
