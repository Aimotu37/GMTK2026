# CounterManager API


## 功能

管理玩家剩余发声次数。


## API


|接口|功能|调用方|
|-|-|-|
|ConsumeCount(int)|消耗发声次数|交互系统|
|AddCount(int)|增加次数|道具/事件|
|SetCount(int)|强制设置次数|测试|
|CurrentCount|获取当前次数|UI|
|OnCountChanged|次数变化通知|UI/事件|
|OnCountZero|次数归零通知|游戏结束|


## 调用规范

所有次数修改必须通过CounterManager。


禁止：

currentCount--;
